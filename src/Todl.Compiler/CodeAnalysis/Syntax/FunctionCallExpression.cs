using System.Collections.Generic;
using System.Linq;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public sealed class FunctionCallExpression : Expression
    {
        public FunctionCallExpression(SyntaxTree syntaxTree) : base(syntaxTree) { }

        public Expression BaseExpression { get; internal init; }
        public ArgumentsList Arguments { get; internal init; }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return BaseExpression;
            yield return Arguments;
        }
    }

    public sealed partial class Parser
    {
        private FunctionCallExpression ParseFunctionCallExpression(Expression baseExpression)
        {
            var argumentsList = ParseArgumentsList();

            return new FunctionCallExpression(syntaxTree)
            {
                BaseExpression = baseExpression,
                Arguments = argumentsList
            };
        }
    }
}
