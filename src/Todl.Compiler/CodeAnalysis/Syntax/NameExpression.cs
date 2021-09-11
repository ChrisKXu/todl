using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public sealed class NameExpression : Expression
    {
        public IReadOnlyList<SyntaxToken> SyntaxTokens { get; internal init; }

        public TextSpan QualifiedName
        {
            get
            {
                if (IsSimpleName)
                {
                    return SyntaxTokens[0].Text;
                }

                var start = SyntaxTokens[0].Text.Start;
                var endText = SyntaxTokens[SyntaxTokens.Count - 1].Text;
                var length = endText.Start + endText.Length - start;

                return SyntaxTree.SourceText.GetTextSpan(start, length);
            }
        }

        public bool IsSimpleName => SyntaxTokens.Count == 1;

        public NameExpression(SyntaxTree syntaxTree) : base(syntaxTree) { }

        public override IEnumerable<SyntaxNode> GetChildren() => SyntaxTokens;
    }

    public sealed partial class Parser
    {
        private NameExpression ParseNameExpression()
        {
            var syntaxTokens = new List<SyntaxToken>
            {
                ExpectToken(SyntaxKind.IdentifierToken)
            };

            var builder = new StringBuilder(syntaxTokens[0].Text.ToString());

            while (Current.Kind == SyntaxKind.DotToken && Peak.Kind == SyntaxKind.IdentifierToken)
            {
                if (!loadedNamespaces.Contains(builder.ToString()))
                {
                    break;
                }

                syntaxTokens.Add(ExpectToken(SyntaxKind.DotToken));

                var identifierToken = ExpectToken(SyntaxKind.IdentifierToken);
                syntaxTokens.Add(identifierToken);
                builder.Append($".{identifierToken.Text}");
            }

            return new NameExpression(syntaxTree)
            {
                SyntaxTokens = syntaxTokens
            };
        }
    }
}
