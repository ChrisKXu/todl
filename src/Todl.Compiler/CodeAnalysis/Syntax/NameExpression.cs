using System.Collections.Generic;
using System.Text;
using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public sealed class NameExpression : Expression
    {
        public IReadOnlyList<SyntaxToken> SyntaxTokens { get; internal init; }

        public override TextSpan Text => TextSpan.FromTextSpans(SyntaxTokens[0].Text, SyntaxTokens[^1].Text);

        public bool IsSimpleName => SyntaxTokens.Count == 1;
    }

    public sealed partial class Parser
    {
        private NameExpression ParseNameExpression()
        {
            if (SyntaxFacts.BuiltInTypes.Contains(Current.Kind))
            {
                return new NameExpression()
                {
                    SyntaxTree = syntaxTree,
                    SyntaxTokens = new List<SyntaxToken>()
                    {
                        ExpectToken(Current.Kind)
                    }
                };
            }

            var syntaxTokens = new List<SyntaxToken>
            {
                ExpectToken(SyntaxKind.IdentifierToken)
            };

            var builder = new StringBuilder(syntaxTokens[0].Text.ToString());

            while (Current.Kind == SyntaxKind.DotToken && Peak.Kind == SyntaxKind.IdentifierToken)
            {
                if (!syntaxTree.ClrTypeCache.Namespaces.Contains(builder.ToString()))
                {
                    break;
                }

                syntaxTokens.Add(ExpectToken(SyntaxKind.DotToken));

                var identifierToken = ExpectToken(SyntaxKind.IdentifierToken);
                syntaxTokens.Add(identifierToken);
                builder.Append($".{identifierToken.Text}");
            }

            return new NameExpression()
            {
                SyntaxTree = syntaxTree,
                SyntaxTokens = syntaxTokens
            };
        }
    }
}
