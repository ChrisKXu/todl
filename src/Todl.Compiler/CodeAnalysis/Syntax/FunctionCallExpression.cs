using System.Collections.Generic;
using System.Linq;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public sealed class FunctionCallExpression : Expression
    {
        public FunctionCallExpression(SyntaxTree syntaxTree) : base(syntaxTree) { }

        public Expression BaseExpression { get; internal init; }
        public SyntaxToken DotToken { get; internal init; }
        public SyntaxToken NameToken { get; internal init; }
        public ArgumentsList Arguments { get; internal init; }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            if (BaseExpression != null && DotToken != null)
            {
                yield return BaseExpression;
                yield return DotToken;
            }
            yield return NameToken;
            yield return Arguments;
        }
    }
}
