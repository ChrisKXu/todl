using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.CodeAnalysis.Text;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis
{
    public sealed class BinderTests
    {
        [Fact]
        public void TestBindBinaryExpression()
        {
            var syntaxTree = new SyntaxTree(SourceText.FromString("1 + 2 + 3"));
            var parser = new Parser(syntaxTree);
            parser.Lex();

            var binaryExpression = parser.ParseBinaryExpression() as BinaryExpression;
            var binder = new Binder();

            var boundBinaryExpression = binder.BindExpression(binaryExpression) as BoundBinaryExpression;
            boundBinaryExpression.Should().NotBeNull();
            boundBinaryExpression.Operator.BoundBinaryOperatorKind.Should().Be(BoundBinaryExpression.BoundBinaryOperatorKind.NumericAddition);
            (boundBinaryExpression.Right as BoundConstant).Value.Should().Be(3);

            var left = boundBinaryExpression.Left as BoundBinaryExpression;
            left.Should().NotBeNull();
            (left.Left as BoundConstant).Value.Should().Be(1);
            (left.Right as BoundConstant).Value.Should().Be(2);
            left.Operator.BoundBinaryOperatorKind.Should().Be(BoundBinaryExpression.BoundBinaryOperatorKind.NumericAddition);
        }
    }
}
