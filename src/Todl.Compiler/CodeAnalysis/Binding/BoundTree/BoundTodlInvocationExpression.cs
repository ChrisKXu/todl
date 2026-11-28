using System.Collections.Immutable;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

[BoundNode]
internal sealed class BoundTodlInvocationExpression : BoundExpression
{
    public FunctionSymbol FunctionSymbol { get; internal set; }
    public ImmutableDictionary<string, BoundExpression> BoundArguments { get; internal init; }

    public override BoundNode Accept(BoundTreeVisitor visitor) => visitor.VisitBoundTodlInvocationExpression(this);
}

public partial class Binder
{
    private BoundTodlInvocationExpression BindTodlInvocationExpression(
        InvocationExpression invocationExpression,
        SimpleNameExpression simpleNameExpression)
    {
        FunctionSymbol functionSymbol = null;
        var boundArguments = ImmutableDictionary<string, BoundExpression>.Empty;
        var nameToken = simpleNameExpression.IdentifierToken;

        var arguments = invocationExpression.Arguments.Items;

        var status = OverloadResolutionStatus.NoMatch;

        if (arguments.Any(a => a.IsNamedArgument))
        {
            boundArguments = arguments.ToImmutableDictionary(
                argument => argument.Identifier?.Text.ToString(),
                argument => BindExpressionDeferringNullLiteral(argument.Expression));

            functionSymbol = Scope.LookupFunctionSymbol(
                name: nameToken.Text.ToString(),
                namedArguments: boundArguments.ToDictionary(
                    item => item.Key,
                    item => item.Value.ResultType));

            if (functionSymbol is null)
            {
                var result = ResolveNullableAwareOverload(
                    Scope.LookupFunctionSymbols(nameToken.Text.ToString()),
                    ToOverloadParameters,
                    boundArguments.Select(item => (item.Key, item.Value)).ToList());

                status = result.Status;
                functionSymbol = result.Candidate;

                if (functionSymbol is not null)
                {
                    boundArguments = functionSymbol.OrderedParameterNames
                        .Zip(result.Arguments)
                        .ToImmutableDictionary(t => t.First, t => t.Second);
                }
            }
        }
        else
        {
            var positionalArguments = arguments.Select(argument => BindExpressionDeferringNullLiteral(argument.Expression)).ToList();
            functionSymbol = Scope.LookupFunctionSymbol(
                name: nameToken.Text.ToString(),
                positionalArguments: positionalArguments.Select(a => a.ResultType));

            var orderedArguments = positionalArguments.ToImmutableArray();

            if (functionSymbol is null)
            {
                var result = ResolveNullableAwareOverload(
                    Scope.LookupFunctionSymbols(nameToken.Text.ToString()),
                    ToOverloadParameters,
                    ToPositionalArguments(positionalArguments));

                status = result.Status;
                functionSymbol = result.Candidate;
                orderedArguments = result.Arguments;
            }

            boundArguments = functionSymbol?.OrderedParameterNames
                .Zip(orderedArguments)
                .ToImmutableDictionary(t => t.First, t => t.Second);
        }

        if (functionSymbol == null)
        {
            if (status == OverloadResolutionStatus.Ambiguous)
            {
                ReportAmbiguousCall(invocationExpression, nameToken.Text.ToString());
            }
            else
            {
                ReportNoMatchingFunctionCandidate(invocationExpression, nameToken);
            }
        }

        return BoundNodeFactory.CreateBoundTodlInvocationExpression(
            syntaxNode: invocationExpression,
            functionSymbol: functionSymbol,
            boundArguments: boundArguments,
            resultType: functionSymbol?.ReturnType ?? default); // TODO: we may need something like TypeSymbol.InvalidType for this
    }
}
