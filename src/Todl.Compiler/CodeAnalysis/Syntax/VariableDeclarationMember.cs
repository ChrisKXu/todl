using System.Collections.Generic;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public sealed class VariableDeclarationMember : Member
    {
        public VariableDeclarationStatement VariableDeclarationStatement { get; init; }

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
