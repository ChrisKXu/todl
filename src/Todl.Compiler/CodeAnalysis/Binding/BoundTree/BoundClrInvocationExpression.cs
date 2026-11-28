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
internal sealed class BoundClrInvocationExpression : BoundExpression
{
    public BoundExpression BoundBaseExpression { get; internal init; }
    public MethodInfo MethodInfo { get; internal init; }
    public ImmutableArray<BoundExpression> BoundArguments { get; internal init; }
    public bool IsStatic => MethodInfo.IsStatic;

    public override BoundNode Accept(BoundTreeVisitor visitor) => visitor.VisitBoundClrInvocationExpression(this);
}

// This is not emittable, just to place a node in the bound tree to indicate this is an error
[BoundNode]
internal sealed class BoundInvalidInvocationExpression : BoundExpression
{
    public BoundExpression BoundBaseExpression { get; internal init; }
    public ImmutableArray<BoundExpression> BoundArguments { get; internal init; }

    public override BoundNode Accept(BoundTreeVisitor visitor) => visitor.VisitBoundInvalidInvocationExpression(this);
}

public partial class Binder
{
    private BoundExpression BindInvocationExpression(InvocationExpression invocationExpression)
    {
        var memberAccessExpression = invocationExpression.Expression as MemberAccessExpression;
        var boundBaseExpression = memberAccessExpression is null
            ? null
            : BindExpression(memberAccessExpression.BaseExpression);

        if (memberAccessExpression is not null && boundBaseExpression.ResultType.IsNative)
        {
            return BindClrInvocationExpression(invocationExpression, memberAccessExpression, boundBaseExpression);
        }

        if (invocationExpression.Expression is SimpleNameExpression simpleNameExpression)
        {
            return BindTodlInvocationExpression(invocationExpression, simpleNameExpression);
        }

        return BindInvalidInvocationExpression(invocationExpression, boundBaseExpression);
    }

    private BoundExpression BindInvalidInvocationExpression(
        InvocationExpression invocationExpression,
        BoundExpression boundBaseExpression)
    {
        ReportDiagnostic(
            new Diagnostic()
            {
                Message = $"Expression '{invocationExpression.Expression.GetText()}' is not invocable.",
                Level = DiagnosticLevel.Error,
                TextLocation = invocationExpression.Expression.GetTextLocation(),
                ErrorCode = ErrorCode.ExpressionNotInvocable
            });

        return BoundNodeFactory.CreateBoundInvalidInvocationExpression(
            syntaxNode: invocationExpression,
            boundBaseExpression: boundBaseExpression ?? BindExpression(invocationExpression.Expression),
            boundArguments: invocationExpression.Arguments.Items
                .Select(a => BindExpression(a.Expression))
                .ToImmutableArray());
    }

    private BoundExpression BindClrInvocationExpression(
        InvocationExpression invocationExpression,
        MemberAccessExpression memberAccessExpression,
        BoundExpression boundBaseExpression)
    {
        var nameToken = memberAccessExpression.MemberIdentifierToken;

        // Since all or none of the arguments of an InvocationExpression needs to be named,
        // we only need to check the first argument to see if it's a named argument to determine the others
        if (invocationExpression.Arguments.Items.Any(a => a.IsNamedArgument))
        {
            return BindInvocationWithNamedArgumentsInternal(
                boundBaseExpression: boundBaseExpression,
                nameToken: nameToken,
                invocationExpression: invocationExpression);
        }

        return BindInvocationWithPositionalArgumentsInternal(
            boundBaseExpression: boundBaseExpression,
            nameToken: nameToken,
            invocationExpression: invocationExpression);
    }

    private BoundExpression BindInvocationWithNamedArgumentsInternal(
        BoundExpression boundBaseExpression,
        SyntaxToken nameToken,
        InvocationExpression invocationExpression)
    {
        Debug.Assert(boundBaseExpression.ResultType.IsNative);

        var type = (boundBaseExpression.ResultType as ClrTypeSymbol).ClrType;
        var isStatic = boundBaseExpression is BoundTypeExpression;
        var candidates = type
            .GetMethods()
            .Where(m => m.Name == nameToken.Text.ToString()
                && m.IsStatic == isStatic
                && !m.ContainsGenericParameters
                && m.IsPublic
                && m.GetParameters().Length == invocationExpression.Arguments.Items.Length);

        var arguments = invocationExpression.Arguments.Items.ToDictionary(
            keySelector: a => a.Identifier.Value.Text.ToString(),
            elementSelector: a => BindExpressionDeferringNullLiteral(a.Expression));

        var candidate = default(MethodInfo);
        var boundArguments = default(ImmutableArray<BoundExpression>);

        if (arguments.Values.All(IsPlainClrArgument))
        {
            var nameAndTypes = arguments.Select(a => new Tuple<string, Type>(a.Key, ((ClrTypeSymbol)a.Value.ResultType).ClrType)).ToHashSet();

            candidate = candidates.FirstOrDefault(methodInfo =>
            {
                var parameters = methodInfo.GetParameters().Select(p => new Tuple<string, Type>(p.Name, p.ParameterType));
                return nameAndTypes.SetEquals(parameters);
            });

            if (candidate is not null)
            {
                boundArguments = candidate
                    .GetParameters()
                    .OrderBy(p => p.Position)
                    .Select(p => arguments[p.Name])
                    .ToImmutableArray();
            }
        }

        var status = OverloadResolutionStatus.NoMatch;
        if (candidate is null)
        {
            var result = ResolveNullableAwareOverload(
                candidates,
                ToOverloadParameters,
                arguments.Select(a => (a.Key, a.Value)).ToList());

            status = result.Status;
            candidate = result.Candidate;
            boundArguments = result.Arguments;
        }

        if (candidate is null)
        {
            return BindUnresolvedInvocation(status, invocationExpression, nameToken, boundBaseExpression, arguments.Values.ToImmutableArray());
        }

        return BoundNodeFactory.CreateBoundClrInvocationExpression(
            syntaxNode: invocationExpression,
            boundBaseExpression: boundBaseExpression,
            methodInfo: candidate,
            boundArguments: boundArguments,
            resultType: ClrTypeCache.Resolve(candidate.ReturnType));
    }

    private BoundExpression BindInvocationWithPositionalArgumentsInternal(
        BoundExpression boundBaseExpression,
        SyntaxToken nameToken,
        InvocationExpression invocationExpression)
    {
        Debug.Assert(boundBaseExpression.ResultType.IsNative);

        var boundArguments = invocationExpression.Arguments.Items.Select(a => BindExpressionDeferringNullLiteral(a.Expression)).ToImmutableArray();
        var type = (boundBaseExpression.ResultType as ClrTypeSymbol).ClrType;
        var isStatic = boundBaseExpression is BoundTypeExpression;
        var bindingFlags = BindingFlags.Public | (isStatic ? BindingFlags.Static : BindingFlags.Instance);

        var candidate = boundArguments.All(IsPlainClrArgument)
            ? WithoutNullableConversions(
                type.GetMethod(
                    name: nameToken.Text.ToString(),
                    bindingAttr: bindingFlags,
                    binder: null,
                    types: boundArguments.Select(b => ((ClrTypeSymbol)b.ResultType).ClrType).ToArray(),
                    modifiers: null),
                boundArguments)
            : null;

        var status = OverloadResolutionStatus.NoMatch;
        if (candidate is null)
        {
            var result = ResolveNullableAwareOverload(
                type.GetMethods(bindingFlags).Where(m => m.Name == nameToken.Text.ToString() && !m.ContainsGenericParameters),
                ToOverloadParameters,
                ToPositionalArguments(boundArguments));

            status = result.Status;
            candidate = result.Candidate;
            boundArguments = result.Arguments.IsDefault ? boundArguments : result.Arguments;
        }

        if (candidate is null)
        {
            return BindUnresolvedInvocation(status, invocationExpression, nameToken, boundBaseExpression, boundArguments);
        }

        return BoundNodeFactory.CreateBoundClrInvocationExpression(
            syntaxNode: invocationExpression,
            boundBaseExpression: boundBaseExpression,
            methodInfo: candidate,
            boundArguments: boundArguments,
            resultType: ClrTypeCache.Resolve(candidate.ReturnType));
    }

    private BoundExpression BindUnresolvedInvocation(
        OverloadResolutionStatus status,
        InvocationExpression invocationExpression,
        SyntaxToken nameToken,
        BoundExpression boundBaseExpression,
        ImmutableArray<BoundExpression> boundArguments)
    {
        if (status == OverloadResolutionStatus.Ambiguous)
        {
            ReportAmbiguousCall(invocationExpression, nameToken.Text.ToString());
        }
        else
        {
            ReportNoMatchingFunctionCandidate(invocationExpression, nameToken);
        }

        return BoundNodeFactory.CreateBoundInvalidInvocationExpression(
            syntaxNode: invocationExpression,
            boundBaseExpression: boundBaseExpression,
            boundArguments: boundArguments);
    }

    private void ReportNoMatchingFunctionCandidate(
        InvocationExpression invocationExpression,
        SyntaxToken nameToken)
    {
        ReportDiagnostic(
            new Diagnostic()
            {
                Message = $"No matching function '{nameToken.Text}' found.",
                Level = DiagnosticLevel.Error,
                TextLocation = invocationExpression.GetTextLocation(nameToken.Span),
                ErrorCode = ErrorCode.NoMatchingCandidate
            });
    }
}
