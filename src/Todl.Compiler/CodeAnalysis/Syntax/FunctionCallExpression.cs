using System.Diagnostics;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Syntax;

public sealed class Argument : SyntaxNode
{
    public SyntaxToken? Identifier { get; internal init; }
    public SyntaxToken? ColonToken { get; internal init; }
    public Expression Expression { get; internal init; }

    public bool IsNamedArgument => Identifier.HasValue;

    public override TextSpan Text
    {
        get
        {
            if (IsNamedArgument)
            {
                return TextSpan.FromTextSpans(Identifier.Value.Text, Expression.Text);
            }

            return Expression.Text;
        }
    }
}

public sealed class FunctionCallExpression : Expression
{
    public Expression BaseExpression { get; internal init; }
    public SyntaxToken DotToken { get; internal init; }
    public SyntaxToken NameToken { get; internal init; }
    public CommaSeparatedSyntaxList<Argument> Arguments { get; internal init; }

    public override TextSpan Text
    {
        get
        {
            if (BaseExpression != null)
            {
                return TextSpan.FromTextSpans(BaseExpression.Text, Arguments.Text);
            }

            return TextSpan.FromTextSpans(NameToken.Text, Arguments.Text);
        }
    }
}

public sealed partial class Parser
{
    private Argument ParseArgument()
    {
        if (Current.Kind == SyntaxKind.IdentifierToken && Peak.Kind == SyntaxKind.ColonToken)
        {
            return new()
            {
                SyntaxTree = syntaxTree,
                Identifier = ExpectToken(SyntaxKind.IdentifierToken),
                ColonToken = ExpectToken(SyntaxKind.ColonToken),
                Expression = ParseExpression()
            };
        }

        return new()
        {
            SyntaxTree = syntaxTree,
            Expression = ParseExpression()
        };
    }

    private FunctionCallExpression ParseFunctionCallExpression(Expression baseExpression)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var arguments = ParseCommaSeparatedSyntaxList(ParseArgument);

        var namedArguments = arguments.Items.Where(p => p.IsNamedArgument);
        if (namedArguments.Any() && namedArguments.Count() != arguments.Items.Length)
        {
            diagnosticBuilder.Add(
                new Diagnostic()
                {
                    Message = "Either all or none of the arguments should be named arguments",
                    Level = DiagnosticLevel.Error,
                    TextLocation = arguments.OpenParenthesisToken.GetTextLocation(),
                    ErrorCode = ErrorCode.MixedPositionalAndNamedArguments
                });
        }

        if (baseExpression is MemberAccessExpression memberAccessExpression)
        {
            return new FunctionCallExpression()
            {
                SyntaxTree = syntaxTree,
                BaseExpression = memberAccessExpression.BaseExpression,
                DotToken = memberAccessExpression.DotToken,
                NameToken = memberAccessExpression.MemberIdentifierToken,
                Arguments = arguments,
                DiagnosticBuilder = diagnosticBuilder
            };
        }

        Debug.Assert(baseExpression is NameExpression nameExpression && nameExpression.IsSimpleName);

        return new()
        {
            SyntaxTree = syntaxTree,
            NameToken = (baseExpression as NameExpression).SyntaxTokens[0],
            Arguments = arguments,
            DiagnosticBuilder = diagnosticBuilder
        };
    }
}
