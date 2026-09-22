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
                return TextSpan.FromBounds(Identifier.Value.Span.Start, Expression.Text.End);
            }

            return Expression.Text;
        }
    }
}

public sealed class FunctionCallExpression : Expression
{
    public Expression Expression { get; internal init; }
    public CommaSeparatedSyntaxList<Argument> Arguments { get; internal init; }

    public override TextSpan Text
        => TextSpan.FromBounds(Expression.Text.Start, Arguments.Text.End);
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
        var arguments = ParseCommaSeparatedSyntaxList(ParseArgument);

        var namedArguments = arguments.Items.Where(p => p.IsNamedArgument);
        if (namedArguments.Any() && namedArguments.Count() != arguments.Items.Length)
        {
            ReportDiagnostic(
                new Diagnostic()
                {
                    Message = "Either all or none of the arguments should be named arguments",
                    TextLocation = arguments.GetTextLocation(arguments.OpenParenthesisToken.Span),
                    ErrorCode = ErrorCode.MixedPositionalAndNamedArguments
                });
        }

        return new()
        {
            SyntaxTree = syntaxTree,
            Expression = baseExpression,
            Arguments = arguments
        };
    }
}
