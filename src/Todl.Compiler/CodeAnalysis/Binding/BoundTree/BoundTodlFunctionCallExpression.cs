using System.Collections.Generic;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

[BoundNode]
internal sealed class BoundTodlFunctionCallExpression : BoundExpression
{
    public FunctionSymbol FunctionSymbol { get; internal set; }
    public IReadOnlyDictionary<string, BoundExpression> BoundArguments { get; internal init; }

    public override TypeSymbol ResultType
        => FunctionSymbol?.ReturnType ?? default; // TODO: we may need something like TypeSymbol.InvalidType for this

    public override BoundNode Accept(BoundTreeVisitor visitor) => visitor.VisitBoundTodlFunctionCallExpression(this);
}

public partial class Binder
{
    private BoundTodlFunctionCallExpression BindTodlFunctionCallExpression(FunctionCallExpression functionCallExpression)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        FunctionSymbol functionSymbol = null;
        IReadOnlyDictionary<string, BoundExpression> boundArguments = null;

        var arguments = functionCallExpression.Arguments.Items;

        if (arguments.Any(a => a.IsNamedArgument))
        {
            boundArguments = arguments.ToDictionary(
                argument => argument.Identifier?.Text.ToString(),
                argument => BindExpression(argument.Expression));

            functionSymbol = Scope.LookupFunctionSymbol(
                name: functionCallExpression.NameToken.Text.ToString(),
                namedArguments: boundArguments.ToDictionary(
                    item => item.Key,
                    item => item.Value.ResultType));
        }
        else
        {
            var positionalArguments = arguments.Select(argument => BindExpression(argument.Expression)).ToList();
            functionSymbol = Scope.LookupFunctionSymbol(
                name: functionCallExpression.NameToken.Text.ToString(),
                positionalArguments: positionalArguments.Select(a => a.ResultType));

            boundArguments = functionSymbol?.OrderedParameterNames
                .Zip(positionalArguments)
                .ToDictionary(t => t.First, t => t.Second);
        }

        if (functionSymbol == null || boundArguments == null)
        {
            ReportNoMatchingFunctionCandidate(diagnosticBuilder, functionCallExpression);
        }

        return BoundNodeFactory.CreateBoundTodlFunctionCallExpression(
            syntaxNode: functionCallExpression,
            functionSymbol: functionSymbol,
            boundArguments: boundArguments,
            diagnosticBuilder: diagnosticBuilder);
    }
}
