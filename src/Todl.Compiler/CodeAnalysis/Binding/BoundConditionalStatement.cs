using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding;

public sealed class BoundConditionalStatement : BoundStatement
{
    public BoundExpression Condition { get; internal init; }
    public BoundStatement Consequence { get; internal init; }
    public BoundStatement Alternative { get; internal init; }

    public BoundConditionalStatement Validate()
    {
        var ifUnlessStatement = SyntaxNode as IfUnlessStatement;
        if (!Condition.ResultType.Equals(ifUnlessStatement.SyntaxTree.ClrTypeCache.BuiltInTypes.Boolean))
        {
            DiagnosticBuilder.Add(new Diagnostic()
            {
                Message = "Condition expressions need to be of boolean type",
                ErrorCode = ErrorCode.TypeMismatch,
                TextLocation = ifUnlessStatement.ConditionExpression.Text.GetTextLocation(),
                Level = DiagnosticLevel.Error
            });
        }

        return this;
    }
}

public partial class Binder
{
    private BoundConditionalStatement BindIfUnlessStatement(IfUnlessStatement ifUnlessStatement)
    {
        var inverted = ifUnlessStatement.IfOrUnlessToken.Kind == SyntaxKind.UnlessKeywordToken;

        var condition = BindExpression(ifUnlessStatement.ConditionExpression);
        var boundBlockStatement = BindBlockStatement(ifUnlessStatement.BlockStatement);

        return BoundNodeFactory.CreateBoundConditionalStatement(
            syntaxNode: ifUnlessStatement,
            condition: condition,
            consequence: inverted ? null : boundBlockStatement,
            alternative: inverted ? boundBlockStatement : null).Validate();
    }
}
