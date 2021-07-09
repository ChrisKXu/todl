using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.Evaluation;
using Xunit;

namespace Todl.Compiler.Tests
{
    public sealed class EvaluatorTests
    {
        [Theory]
        [InlineData("10", 10)]
        [InlineData("true", true)]
        [InlineData("1 + 2", 3)]
        [InlineData("(1 + 2) * 3 + 4", 13)]
        [InlineData("true && false", false)]
        [InlineData("false || true", true)]
        public void TestEvaluationPositive(string input, object expectedOutput)
        {
            var sourceText = SourceText.FromString(input);
            var evaluator = new Evaluator();

            var result = evaluator.Evaluate(sourceText);
            result.DiagnosticsOutput.Should().BeEmpty();
            result.EvaluationOutput.Should().Be(expectedOutput);
        }
    }
}
