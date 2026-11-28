using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

[BoundNode]
internal sealed class BoundObjectCreationExpression : BoundExpression
{
    public ConstructorInfo ConstructorInfo { get; internal init; }
    public ImmutableArray<BoundExpression> BoundArguments { get; internal init; }

    public override BoundNode Accept(BoundTreeVisitor visitor) => visitor.VisitBoundObjectCreationExpression(this);
}

// This is not emittable, just to place a node in the bound tree to indicate this is an error
[BoundNode]
internal sealed class BoundInvalidObjectCreationExpression : BoundExpression
{
    public ImmutableArray<BoundExpression> BoundArguments { get; internal init; }

    public override BoundNode Accept(BoundTreeVisitor visitor) => visitor.VisitBoundInvalidObjectCreationExpression(this);
}

public partial class Binder
{
    private BoundExpression BindNewExpression(NewExpression newExpression)
    {
        var boundTypeExpression = BindTypeExpression(newExpression.TypeNameExpression);

        // Type unresolved (already reported); still bind args, then bail out.
        if (boundTypeExpression.ResultType is null)
        {
            var invalidBoundArguments = newExpression.Arguments.Items
                .Select(a => BindExpression(a.Expression))
                .ToImmutableArray();

            return BoundNodeFactory.CreateBoundInvalidObjectCreationExpression(
                syntaxNode: newExpression,
                boundArguments: invalidBoundArguments);
        }

        // Treating no arguments as the same way of positional arguments
        if (newExpression.Arguments.Items.IsEmpty || !newExpression.Arguments.Items[0].IsNamedArgument)
        {
            return BindNewExpressionWithPositionalArgumentsInternal(
                targetType: boundTypeExpression.ResultType,
                newExpression: newExpression);
        }

        return BindNewExpressionWithNamedArgumentsInternal(
            targetType: boundTypeExpression.ResultType,
            newExpression: newExpression);
    }

    private BoundExpression BindNewExpressionWithPositionalArgumentsInternal(
        TypeSymbol targetType,
        NewExpression newExpression)
    {
        Debug.Assert(targetType.IsNative);

        var clrType = (targetType as ClrTypeSymbol).ClrType;
        var boundArguments = newExpression.Arguments.Items.Select(a => BindExpressionDeferringNullLiteral(a.Expression)).ToImmutableArray();

        var constructorInfo = boundArguments.All(IsPlainClrArgument)
            ? WithoutNullableConversions(
                clrType.GetConstructor(boundArguments.Select(b => ((ClrTypeSymbol)b.ResultType).ClrType).ToArray()),
                boundArguments)
            : null;

        var status = OverloadResolutionStatus.NoMatch;
        if (constructorInfo is null)
        {
            var result = ResolveNullableAwareOverload(
                clrType.GetConstructors(),
                ToOverloadParameters,
                ToPositionalArguments(boundArguments));

            status = result.Status;
            constructorInfo = result.Candidate;
            boundArguments = result.Arguments.IsDefault ? boundArguments : result.Arguments;
        }

        if (constructorInfo is null)
        {
            return BindUnresolvedObjectCreation(status, newExpression, boundArguments);
        }

        return new BoundObjectCreationExpression()
        {
            SyntaxNode = newExpression,
            ConstructorInfo = constructorInfo,
            BoundArguments = boundArguments,
            ResultType = ClrTypeCache.Resolve(constructorInfo.DeclaringType),
        };
    }

    private BoundExpression BindNewExpressionWithNamedArgumentsInternal(
        TypeSymbol targetType,
        NewExpression newExpression)
    {
        Debug.Assert(targetType.IsNative);

        var clrType = (targetType as ClrTypeSymbol).ClrType;
        var arguments = newExpression.Arguments;
        var candidates = clrType.GetConstructors()
            .Where(c => c.IsPublic && c.GetParameters().Length == arguments.Items.Length);
        var argumentsDictionary = arguments.Items.ToDictionary(
            keySelector: a => a.Identifier.Value.Text.ToString(),
            elementSelector: a => BindExpressionDeferringNullLiteral(a.Expression));

        var constructorInfo = default(ConstructorInfo);
        var boundArguments = default(ImmutableArray<BoundExpression>);

        if (argumentsDictionary.Values.All(IsPlainClrArgument))
        {
            var nameAndTypes = argumentsDictionary.Select(a => new Tuple<string, Type>(a.Key, ((ClrTypeSymbol)a.Value.ResultType).ClrType)).ToHashSet();

            constructorInfo = candidates.FirstOrDefault(c =>
            {
                var parameters = c.GetParameters().Select(p => new Tuple<string, Type>(p.Name, p.ParameterType));
                return nameAndTypes.SetEquals(parameters);
            });

            if (constructorInfo is not null)
            {
                boundArguments = constructorInfo.GetParameters().OrderBy(p => p.Position).Select(p => argumentsDictionary[p.Name]).ToImmutableArray();
            }
        }

        var status = OverloadResolutionStatus.NoMatch;
        if (constructorInfo is null)
        {
            var result = ResolveNullableAwareOverload(
                candidates,
                ToOverloadParameters,
                argumentsDictionary.Select(a => (a.Key, a.Value)).ToList());

            status = result.Status;
            constructorInfo = result.Candidate;
            boundArguments = result.Arguments;
        }

        if (constructorInfo is null)
        {
            return BindUnresolvedObjectCreation(status, newExpression, argumentsDictionary.Values.ToImmutableArray());
        }

        return new BoundObjectCreationExpression()
        {
            SyntaxNode = newExpression,
            ConstructorInfo = constructorInfo,
            BoundArguments = boundArguments,
            ResultType = ClrTypeCache.Resolve(constructorInfo.DeclaringType),
        };
    }

    private BoundExpression BindUnresolvedObjectCreation(
        OverloadResolutionStatus status,
        NewExpression newExpression,
        ImmutableArray<BoundExpression> boundArguments)
    {
        if (status == OverloadResolutionStatus.Ambiguous)
        {
            ReportAmbiguousCall(newExpression, $"new {newExpression.TypeNameExpression.GetText()}");
        }
        else
        {
            ReportNoMatchingConstructorCandidate(newExpression);
        }

        return BoundNodeFactory.CreateBoundInvalidObjectCreationExpression(
            syntaxNode: newExpression,
            boundArguments: boundArguments);
    }

    private void ReportNoMatchingConstructorCandidate(
        NewExpression newExpression)
    {
        ReportDiagnostic(
            new Diagnostic()
            {
                Message = $"No matching constructor {newExpression.TypeNameExpression.GetText()} found.",
                Level = DiagnosticLevel.Error,
                TextLocation = newExpression.TypeNameExpression.GetTextLocation(),
                ErrorCode = ErrorCode.NoMatchingCandidate
            });
    }
}
