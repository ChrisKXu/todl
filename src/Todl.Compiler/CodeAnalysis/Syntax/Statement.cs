using System;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public abstract class Statement : SyntaxNode
    {
        protected Statement(SyntaxTree syntaxTree) : base(syntaxTree)
        {
        }
    }

    public sealed partial class Parser
    {
        internal Statement ParseStatement()
        {
            return Current.Kind switch
            {
                SyntaxKind.OpenBraceToken => this.ParseBlockStatement(),
                SyntaxKind.ConstKeywordToken or SyntaxKind.LetKeywordToken => this.ParseVariableDeclarationStatement(),
                _ => this.ParseExpressionStatement()
            };
        }
    }
}
