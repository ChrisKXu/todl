using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public sealed class Argument : SyntaxNode
    {
        public SyntaxToken Identifier { get; internal init; }
        public SyntaxToken ColonToken { get; internal init; }
        public Expression Expression { get; internal init; }

        public bool IsNamedArgument => Identifier != null;

        public override TextSpan Text
        {
            get
            {
                if (IsNamedArgument)
                {
                    return TextSpan.FromTextSpans(Identifier.Text, Expression.Text);
                }

                return Expression.Text;
            }
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            if (IsNamedArgument)
            {
                yield return Identifier;
                yield return ColonToken;
            }

            yield return Expression;
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

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            if (BaseExpression != null && DotToken != null)
            {
                yield return BaseExpression;
                yield return DotToken;
            }
            yield return NameToken;
            yield return Arguments;
        }
    }

    public sealed partial class Parser
    {
        private Argument ParseArgument()
        {
            if (Current.Kind == SyntaxKind.IdentifierToken && Peak.Kind == SyntaxKind.ColonToken)
            {
                return new Argument()
                {
                    SyntaxTree = syntaxTree,
                    Identifier = ExpectToken(SyntaxKind.IdentifierToken),
                    ColonToken = ExpectToken(SyntaxKind.ColonToken),
                    Expression = ParseExpression()
                };
            }

            return new Argument()
            {
                SyntaxTree = syntaxTree,
                Expression = ParseExpression()
            };
        }

        private FunctionCallExpression ParseFunctionCallExpression(Expression baseExpression)
        {
            var arguments = ParseCommaSeparatedSyntaxList(ParseArgument);

            var namedArguments = arguments.Items.Where(p => p.IsNamedArgument);
            if (namedArguments.Any() && namedArguments.Count() != arguments.Items.Count)
            {
                diagnostics.Add(
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
                    Arguments = arguments
                };
            }

            Debug.Assert(baseExpression is NameExpression nameExpression && nameExpression.IsSimpleName);

            return new FunctionCallExpression()
            {
                SyntaxTree = syntaxTree,
                NameToken = (baseExpression as NameExpression).SyntaxTokens[0],
                Arguments = arguments
            };
        }
    }
}
