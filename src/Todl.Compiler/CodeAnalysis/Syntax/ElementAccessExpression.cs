using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Syntax;

public sealed class ElementAccessExpression : Expression
{
    public Expression BaseExpression { get; internal init; }
    public SyntaxToken OpenBracketToken { get; internal init; }
    public Expression IndexExpression { get; internal init; }
    public SyntaxToken CloseBracketToken { get; internal init; }

    public override TextSpan Text
        => TextSpan.FromBounds(BaseExpression.Text.Start, CloseBracketToken.Span.End);
}

public sealed partial class Parser
{
    private ElementAccessExpression ParseElementAccessExpression(Expression baseExpression)
    {
        var openBracketToken = ExpectToken(SyntaxKind.OpenBracketToken);

        if (Current.Kind == SyntaxKind.CloseBracketToken)
        {
            ReportDiagnostic(new()
            {
                Message = "Element access requires an index expression",
                ErrorCode = ErrorCode.MissingIndexExpression,
                Level = DiagnosticLevel.Error,
                TextLocation = new(syntaxTree.SourceText, Current.Span)
            });
        }

        var indexExpression = ParseExpression();
        var closeBracketToken = ExpectToken(SyntaxKind.CloseBracketToken);

        return new()
        {
            SyntaxTree = syntaxTree,
            BaseExpression = baseExpression,
            OpenBracketToken = openBracketToken,
            IndexExpression = indexExpression,
            CloseBracketToken = closeBracketToken
        };
    }
}
