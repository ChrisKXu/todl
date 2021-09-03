using System.Collections.Generic;
using System.Linq;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public sealed class FunctionCallExpression : Expression
    {
        public FunctionCallExpression(SyntaxTree syntaxTree)
            : base(syntaxTree)
        {
        }

        public Expression BaseExpression { get; internal init; }
        public SyntaxToken OpenParenthesisToken { get; internal init; }
        public IReadOnlyList<Argument> Arguments { get; internal init; }
        public SyntaxToken CloseParenthesisToken { get; internal init; }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return BaseExpression;
            yield return OpenParenthesisToken;

            if (Arguments.Any())
            {
                foreach (var argument in Arguments)
                {
                    yield return argument;
                }
            }

            yield return CloseParenthesisToken;
        }

        public sealed class Argument : SyntaxNode
        {
            public Argument(SyntaxTree syntaxTree)
                : base(syntaxTree)
            {
            }

            // leading comma token
            public SyntaxToken CommaToken { get; internal init; }
            public SyntaxToken Identifier { get; internal init; }
            public SyntaxToken ColonToken { get; internal init; }
            public Expression Expression { get; internal init; }

            public bool IsNamedArgument => Identifier != null;

            public override IEnumerable<SyntaxNode> GetChildren()
            {
                if (CommaToken != null)
                {
                    yield return CommaToken;
                }

                if (IsNamedArgument)
                {
                    yield return Identifier;
                    yield return ColonToken;
                }

                yield return Expression;
            }
        }
    }

    public sealed partial class Parser
    {
        private FunctionCallExpression ParseFunctionCallExpression(Expression baseExpression)
        {
            var openParenthesisToken = ExpectToken(SyntaxKind.OpenParenthesisToken);
            var arguments = new List<FunctionCallExpression.Argument>();

            while (Current.Kind != SyntaxKind.CloseParenthesisToken)
            {
                SyntaxToken commaToken = null;

                if (Current.Kind == SyntaxKind.CommaToken && arguments.Any())
                {
                    commaToken = ExpectToken(SyntaxKind.CommaToken);
                }

                if (Current.Kind == SyntaxKind.IdentifierToken && Peak.Kind == SyntaxKind.ColonToken)
                {
                    arguments.Add(new FunctionCallExpression.Argument(syntaxTree)
                    {
                        CommaToken = commaToken,
                        Identifier = ExpectToken(SyntaxKind.IdentifierToken),
                        ColonToken = ExpectToken(SyntaxKind.ColonToken),
                        Expression = ParseExpression()
                    });
                }
                else
                {
                    arguments.Add(new FunctionCallExpression.Argument(syntaxTree)
                    {
                        CommaToken = commaToken,
                        Expression = ParseExpression()
                    });
                }
            }

            var closeParenthesisToken = ExpectToken(SyntaxKind.CloseParenthesisToken);

            var namedArguments = arguments.Where(p => p.IsNamedArgument);
            if (namedArguments.Any() && namedArguments.Count() != arguments.Count)
            {
                diagnostics.Add(
                    new Diagnostic()
                    {
                        Message = "Either all or none of the arguments should be named arguments",
                        Level = DiagnosticLevel.Error,
                        TextLocation = openParenthesisToken.GetTextLocation(),
                        ErrorCode = ErrorCode.MixedPositionalAndNamedArguments
                    });
            }

            return new FunctionCallExpression(syntaxTree)
            {
                BaseExpression = baseExpression,
                OpenParenthesisToken = openParenthesisToken,
                Arguments = arguments,
                CloseParenthesisToken = closeParenthesisToken
            };
        }
    }
}
