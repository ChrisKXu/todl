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

        if (arguments.Any(a => a.IsNamedArgument))
        {
            boundArguments = arguments.ToImmutableDictionary(
                argument => argument.Identifier?.Text.ToString(),
                argument => BindExpression(argument.Expression));

            functionSymbol = Scope.LookupFunctionSymbol(
                name: nameToken.Text.ToString(),
                namedArguments: boundArguments.ToDictionary(
                    item => item.Key,
                    item => item.Value.ResultType));
        }
        else
        {
            var positionalArguments = arguments.Select(argument => BindExpression(argument.Expression)).ToList();
            functionSymbol = Scope.LookupFunctionSymbol(
                name: nameToken.Text.ToString(),
                positionalArguments: positionalArguments.Select(a => a.ResultType));

            boundArguments = functionSymbol?.OrderedParameterNames
                .Zip(positionalArguments)
                .ToImmutableDictionary(t => t.First, t => t.Second);
        }

        if (functionSymbol == null)
        {
            ReportNoMatchingFunctionCandidate(invocationExpression, nameToken);
        }

        return BoundNodeFactory.CreateBoundTodlInvocationExpression(
            syntaxNode: invocationExpression,
            functionSymbol: functionSymbol,
            boundArguments: boundArguments,
            resultType: functionSymbol?.ReturnType ?? default); // TODO: we may need something like TypeSymbol.InvalidType for this
    }
}
