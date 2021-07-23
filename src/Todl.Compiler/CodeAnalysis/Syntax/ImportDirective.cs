using System;
using System.Collections.Generic;
using System.Linq;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public sealed class ImportDirective : Directive
    {
        public SyntaxToken ImportKeywordToken { get; internal init; }
        public SyntaxToken StarToken { get; internal init; }
        public SyntaxToken OpenBraceToken { get; internal init; }
        public IReadOnlyList<SyntaxToken> ImportedTokens { get; internal init; }
        public SyntaxToken CloseBraceToken { get; internal init; }
        public SyntaxToken FromKeywordToken { get; internal init; }
        public Expression NamespaceExpression { get; internal init; }
        public SyntaxToken SemicolonToken { get; internal init; }

        public bool ImportAll => StarToken != null;

        public string Namespace
        {
            get
            {
                return NamespaceExpression switch
                {
                    NameExpression nameExpression => nameExpression.IdentifierToken.Text.ToString(),
                    MemberAccessExpression memberAccessExpression => memberAccessExpression.QualifiedName,
                    _ => throw new NotSupportedException()
                };
            }
        }

        public IEnumerable<string> ImportedNames
        {
            get
            {
                if (ImportedTokens == null)
                {
                    return Array.Empty<string>();
                }

                return ImportedTokens
                    .Where(token => token.Kind == SyntaxKind.IdentifierToken)
                    .Select(token => token.Text.ToString());
            }
        }

        public ImportDirective(SyntaxTree syntaxTree) : base(syntaxTree) { }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return ImportKeywordToken;
            yield return FromKeywordToken;
            yield return SemicolonToken;
        }
    }

    public sealed partial class Parser
    {
        // Supported import directive forms
        // 1. import { Console } from System;
        // 2. import { List, Dictionary, LinkedList } from System.Collections.Generic;
        // 3. import * from System.Threading.Tasks;
        private ImportDirective ParseImportDirective()
        {
            var importKeyword = ExpectToken(SyntaxKind.ImportKeywordToken);
            SyntaxToken starToken = null, openBraceToken = null, closeBraceToken = null;
            List<SyntaxToken> importedTokens = null;

            if (Current.Kind == SyntaxKind.StarToken)
            {
                starToken = ExpectToken(SyntaxKind.StarToken);
            }
            else
            {
                importedTokens = new List<SyntaxToken>();
                openBraceToken = ExpectToken(SyntaxKind.OpenBraceToken);

                while (Current.Kind == SyntaxKind.IdentifierToken || Current.Kind == SyntaxKind.CommaToken)
                {
                    importedTokens.Add(ExpectToken(Current.Kind));
                }

                closeBraceToken = ExpectToken(SyntaxKind.CloseBraceToken);
            }

            var fromKeyword = ExpectToken(SyntaxKind.FromKeywordToken);
            var namespaceExpression = ParseExpression();
            var semicolonToken = ExpectToken(SyntaxKind.SemicolonToken);

            return new ImportDirective(syntaxTree)
            {
                ImportKeywordToken = importKeyword,
                StarToken = starToken,
                OpenBraceToken = openBraceToken,
                ImportedTokens = importedTokens,
                CloseBraceToken = closeBraceToken,
                FromKeywordToken = fromKeyword,
                NamespaceExpression = namespaceExpression,
                SemicolonToken = semicolonToken
            };
        }
    }
}