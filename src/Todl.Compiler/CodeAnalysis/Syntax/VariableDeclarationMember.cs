using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public sealed class VariableDeclarationMember : Member
    {
        public VariableDeclarationStatement VariableDeclarationStatement { get; init; }

        public VariableDeclarationMember(SyntaxTree syntaxTree) : base(syntaxTree) { }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return VariableDeclarationStatement;
        }
    }

    public sealed partial class Parser
    {
        private VariableDeclarationMember ParseVariableDeclarationMember()
            => new VariableDeclarationMember(syntaxTree)
            {
                VariableDeclarationStatement = ParseVariableDeclarationStatement()
            };
    }
}
