using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public sealed class Argument : SyntaxNode
    {
        public Argument(SyntaxTree syntaxTree) : base(syntaxTree) { }

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

    public sealed class ArgumentsList : SyntaxNode, IReadOnlyList<Argument>
    {
        public SyntaxToken OpenParenthesisToken { get; internal init; }
        public IReadOnlyList<Argument> Arguments { get; internal init; }
        public SyntaxToken CloseParenthesisToken { get; internal init; }

        public int Count => Arguments.Count;
        public Argument this[int index] => Arguments[index];

        public ArgumentsList(SyntaxTree syntaxTree) : base(syntaxTree) { }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return OpenParenthesisToken;

            foreach (var argument in Arguments)
            {
                yield return argument;
            }

            yield return CloseParenthesisToken;
        }

        public IEnumerator<Argument> GetEnumerator() => Arguments.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Arguments.GetEnumerator();
    }

    public sealed partial class Parser
    {
        private ArgumentsList ParseArgumentsList()
        {
            var openParenthesisToken = ExpectToken(SyntaxKind.OpenParenthesisToken);
            var arguments = new List<Argument>();

            while (Current.Kind != SyntaxKind.CloseParenthesisToken)
            {
                SyntaxToken commaToken = null;

                if (Current.Kind == SyntaxKind.CommaToken && arguments.Any())
                {
                    commaToken = ExpectToken(SyntaxKind.CommaToken);
                }

                if (Current.Kind == SyntaxKind.IdentifierToken && Peak.Kind == SyntaxKind.ColonToken)
                {
                    arguments.Add(new Argument(syntaxTree)
                    {
                        CommaToken = commaToken,
                        Identifier = ExpectToken(SyntaxKind.IdentifierToken),
                        ColonToken = ExpectToken(SyntaxKind.ColonToken),
                        Expression = ParseExpression()
                    });
                }
                else
                {
                    arguments.Add(new Argument(syntaxTree)
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

            return new ArgumentsList(syntaxTree)
            {
                OpenParenthesisToken = openParenthesisToken,
                Arguments = arguments,
                CloseParenthesisToken = closeParenthesisToken
            };
        }
    }
}
