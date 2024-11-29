using System.Linq;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Syntax;

public sealed class NewExpression : Expression
{
    public SyntaxToken NewKeywordToken { get; internal init; }
    public NameExpression TypeNameExpression { get; internal init; }
    public CommaSeparatedSyntaxList<Argument> Arguments { get; internal init; }

    public override TextSpan Text => TextSpan.FromTextSpans(NewKeywordToken.Text, Arguments.Text);
}

public sealed partial class Parser
{
    private NewExpression ParseNewExpression()
    {
        var newKeywordToken = ExpectToken(SyntaxKind.NewKeywordToken);
        var typeNameExpression = ParseNameExpression();
        var arguments = ParseCommaSeparatedSyntaxList(ParseArgument);

        var namedArguments = arguments.Items.Where(p => p.IsNamedArgument);
        if (namedArguments.Any() && namedArguments.Count() != arguments.Items.Length)
        {
            ReportDiagnostic(
                new Diagnostic()
                {
                    Message = "Either all or none of the arguments should be named arguments",
                    Level = DiagnosticLevel.Error,
                    TextLocation = arguments.OpenParenthesisToken.GetTextLocation(),
                    ErrorCode = ErrorCode.MixedPositionalAndNamedArguments
                });
        }

        return new NewExpression()
        {
            SyntaxTree = syntaxTree,
            NewKeywordToken = newKeywordToken,
            TypeNameExpression = typeNameExpression,
            Arguments = arguments
        };
    }
}
