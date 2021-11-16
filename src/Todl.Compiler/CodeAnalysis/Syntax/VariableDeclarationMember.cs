using System.Collections.Generic;
using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public sealed class VariableDeclarationMember : Member
    {
        public VariableDeclarationStatement VariableDeclarationStatement { get; init; }

        public override TextSpan Text => VariableDeclarationStatement.Text;

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return VariableDeclarationStatement;
        }
    }

    public sealed partial class Parser
    {
        private VariableDeclarationMember ParseVariableDeclarationMember()
            => new()
            {
                SyntaxTree = syntaxTree,
                VariableDeclarationStatement = ParseVariableDeclarationStatement()
            };
    }
}
