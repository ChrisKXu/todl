using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

internal readonly record struct OverloadParameter(string Name, TypeSymbol Type);

internal enum OverloadResolutionStatus
{
    Success,
    NoMatch,
    Ambiguous
}

// Arguments are converted to the parameter types and ordered by parameter position.
internal readonly record struct OverloadResolutionResult<TCandidate>(
    OverloadResolutionStatus Status,
    TCandidate Candidate,
    ImmutableArray<BoundExpression> Arguments);

public partial class Binder
{
    // An exact candidate wins; otherwise a single applicable candidate wins and several are ambiguous.
    // Names are null for positional arguments; arguments are either all positional or all named.
    private static OverloadResolutionResult<TCandidate> ResolveNullableAwareOverload<TCandidate>(
        IEnumerable<TCandidate> candidates,
        Func<TCandidate, IReadOnlyList<OverloadParameter>> getParameters,
        IReadOnlyList<(string Name, BoundExpression Value)> arguments)
        where TCandidate : class
    {
        var applicable = new List<(TCandidate Candidate, IReadOnlyList<OverloadParameter> Parameters, int[] Ranks)>();

        foreach (var candidate in candidates)
        {
            var parameters = getParameters(candidate);

            if (TryRankArguments(parameters, arguments, out var ranks))
            {
                applicable.Add((candidate, parameters, ranks));
            }
        }

        if (applicable.Count == 0)
        {
            return new(OverloadResolutionStatus.NoMatch, null, default);
        }

        var exact = applicable.Where(a => a.Ranks.All(r => r == 0)).ToList();
        var best = exact.Count > 0 ? exact : applicable;

        if (best.Count != 1)
        {
            return new(OverloadResolutionStatus.Ambiguous, null, default);
        }

        var winner = best[0];
        var converted = new BoundExpression[winner.Parameters.Count];

        for (var i = 0; i < arguments.Count; ++i)
        {
            var parameterIndex = FindParameterIndex(winner.Parameters, arguments[i].Name, i);
            converted[parameterIndex] = TryConvertImplicit(arguments[i].Value, winner.Parameters[parameterIndex].Type);
        }

        return new(OverloadResolutionStatus.Success, winner.Candidate, converted.ToImmutableArray());
    }

    private static int FindParameterIndex(IReadOnlyList<OverloadParameter> parameters, string argumentName, int argumentIndex)
    {
        if (argumentName is null)
        {
            return argumentIndex;
        }

        for (var i = 0; i < parameters.Count; ++i)
        {
            if (parameters[i].Name == argumentName)
            {
                return i;
            }
        }

        return -1;
    }

    // Ranks are per argument: 0 for identity, 1 for any other accepted conversion.
    private static bool TryRankArguments(
        IReadOnlyList<OverloadParameter> parameters,
        IReadOnlyList<(string Name, BoundExpression Value)> arguments,
        out int[] ranks)
    {
        ranks = new int[arguments.Count];

        if (parameters.Count != arguments.Count)
        {
            return false;
        }

        var seen = new HashSet<int>();

        for (var i = 0; i < arguments.Count; ++i)
        {
            var parameterIndex = FindParameterIndex(parameters, arguments[i].Name, i);
            var parameterType = parameterIndex < 0 ? null : parameters[parameterIndex].Type;

            if (parameterType is null || !seen.Add(parameterIndex))
            {
                return false;
            }

            var argument = arguments[i].Value;
            var conversion = argument is BoundNullLiteralExpression
                ? Conversions.ClassifyNullLiteral(parameterType)
                : Conversions.ClassifyImplicit(argument.ResultType, parameterType);

            switch (conversion.Kind)
            {
                case ConversionKind.Identity:
                    ranks[i] = 0;
                    break;
                case ConversionKind.ImplicitNullable:
                case ConversionKind.NullLiteral:
                    ranks[i] = 1;
                    break;
                default:
                    return false;
            }
        }

        return true;
    }

    private static IReadOnlyList<(string Name, BoundExpression Value)> ToPositionalArguments(IEnumerable<BoundExpression> arguments)
        => arguments.Select(a => ((string)null, a)).ToList();

    // Types that the reflection-based lookup can match exactly; null literals and nullable values need the nullable-aware lookup.
    private static bool IsPlainClrArgument(BoundExpression argument)
        => argument.ResultType is ClrTypeSymbol { IsNullableValueType: false };

    // The reflection binder treats T as assignable to Nullable<T>, which would skip the wrapping conversion;
    // such a match is dropped so that the nullable-aware lookup picks the candidate and converts the arguments.
    private TCandidate WithoutNullableConversions<TCandidate>(TCandidate candidate, IReadOnlyList<BoundExpression> arguments)
        where TCandidate : System.Reflection.MethodBase
    {
        if (candidate is null)
        {
            return null;
        }

        var parameters = candidate.GetParameters();

        for (var i = 0; i < parameters.Length; ++i)
        {
            var parameterType = ClrTypeCache.Resolve(parameters[i].ParameterType);

            if (parameterType is { IsNullableValueType: true } && !parameterType.Equals(arguments[i].ResultType))
            {
                return null;
            }
        }

        return candidate;
    }

    private OverloadParameter[] ToOverloadParameters(System.Reflection.MethodBase methodBase)
        => methodBase.GetParameters()
            .Select(p => new OverloadParameter(p.Name, ClrTypeCache.Resolve(p.ParameterType)))
            .ToArray();

    private static IReadOnlyList<OverloadParameter> ToOverloadParameters(FunctionSymbol functionSymbol)
        => functionSymbol.Parameters.Select(p => new OverloadParameter(p.Name, p.Type)).ToArray();

    private void ReportAmbiguousCall(SyntaxNode syntaxNode, string description)
    {
        ReportDiagnostic(
            new Diagnostic()
            {
                Message = $"The call to '{description}' is ambiguous between multiple candidates.",
                Level = DiagnosticLevel.Error,
                TextLocation = syntaxNode.GetTextLocation(),
                ErrorCode = ErrorCode.AmbiguousCall
            });
    }
}
