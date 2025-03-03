using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Syntax;

public sealed class WhileUntilStatement : Statement
{
    public SyntaxToken WhileOrUntilToken { get; internal init; }
    public Expression ConditionExpression { get; internal init; }
    public LoopLabel LoopLabel { get; internal init; }
    public BlockStatement BlockStatement { get; internal init; }

    public override TextSpan Text
        => TextSpan.FromTextSpans(WhileOrUntilToken.Text, BlockStatement.Text);
}

public sealed class LoopLabel : SyntaxNode
{
    public SyntaxToken ColonToken { get; internal init; }
    public NameExpression Label { get; internal init; }

    public override TextSpan Text
        => TextSpan.FromTextSpans(ColonToken.Text, Label.Text);
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
            ? ParseLoopLabel()
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

    private LoopLabel ParseLoopLabel()
    {
        var colonToken = ExpectToken(SyntaxKind.ColonToken);
        var label = ParseNameExpression();

        if (!label.IsSimpleName)
        {
            ReportDiagnostic(new()
            {
                Message = "",
                ErrorCode = ErrorCode.InvalidLoopLabel,
                TextLocation = label.Text.GetTextLocation(),
                Level = DiagnosticLevel.Error
            });
        }

        return new()
        {
            SyntaxTree = syntaxTree,
            ColonToken = colonToken,
            Label = label
        };
    }
}
