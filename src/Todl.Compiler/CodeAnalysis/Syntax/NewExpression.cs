using System.Collections.Generic;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public sealed class NewExpression : Expression
    {
        public SyntaxToken NewKeywordToken { get; internal init; }
        public NameExpression TypeNameExpression { get; internal init; }
        public ArgumentsList Arguments { get; internal init; }

        public NewExpression(SyntaxTree syntaxTree) : base(syntaxTree) { }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return NewKeywordToken;
            yield return TypeNameExpression;
            yield return Arguments;
        }
    }

    public sealed partial class Parser
    {
        private NewExpression ParseNewExpression()
        {
            return new NewExpression(syntaxTree)
            {
                NewKeywordToken = ExpectToken(SyntaxKind.NewKeywordToken),
                TypeNameExpression = ParseNameExpression(),
                Arguments = ParseArgumentsList()
            };
        }
    }
}
