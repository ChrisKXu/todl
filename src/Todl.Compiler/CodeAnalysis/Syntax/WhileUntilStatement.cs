using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax;

public sealed class WhileUntilStatement : Statement
{
    public SyntaxToken WhileOrUntilToken { get; internal init; }
    public Expression ConditionExpression { get; internal init; }
    public LoopLabel LoopLabel { get; internal init; }
    public BlockStatement BlockStatement { get; internal init; }

    public override TextSpan Text
        => TextSpan.FromBounds(WhileOrUntilToken.Span.Start, BlockStatement.Text.End);
}

public sealed class LoopLabel : SyntaxNode
{
    public SyntaxToken ColonToken { get; internal init; }
    public NameExpression Label { get; internal init; }

    public override TextSpan Text
        => TextSpan.FromBounds(ColonToken.Span.Start, Label.Text.End);
}

public sealed partial class Parser
{
    private WhileUntilStatement ParseWhileUntilStatement()
    {
        var whileOrUntilToken =
            Current.Kind == SyntaxKind.WhileKeywordToken
                ? ExpectToken(SyntaxKind.WhileKeywordToken)
                : ExpectToken(SyntaxKind.UntilKeywordToken);

        var conditionExpression = ParseExpression();
        var loopLabel = Current.Kind == SyntaxKind.ColonToken
            ? ParseLoopLabelDeclaration()
            : null;

        var blockStatement = ParseBlockStatement();

        return new()
        {
            SyntaxTree = syntaxTree,
            WhileOrUntilToken = whileOrUntilToken,
            ConditionExpression = conditionExpression,
            LoopLabel = loopLabel,
            BlockStatement = blockStatement
        };
    }

    private LoopLabel ParseLoopLabelDeclaration()
    {
        var colonToken = ExpectToken(SyntaxKind.ColonToken);
        var label = ParseLoopLabel();

        return new()
        {
            SyntaxTree = syntaxTree,
            ColonToken = colonToken,
            Label = label
        };
    }
}
