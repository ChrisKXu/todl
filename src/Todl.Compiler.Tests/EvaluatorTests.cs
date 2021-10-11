using System.Collections.Generic;
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
        [InlineData("\"abc\".Length", 3)]
        [InlineData("System.Int32.MaxValue", int.MaxValue)]
        [InlineData("10.ToString()", "10")]
        [InlineData("System.Math.Max(20, System.Int32.MaxValue)", int.MaxValue)]
        [InlineData("System.Math.Max(val1: 20, val2: System.Int32.MaxValue)", int.MaxValue)]
        [InlineData("int.MinValue", int.MinValue)]
        [InlineData("string.Format(\"abc{0}\", \"de\")", "abcde")]
        [MemberData(nameof(GetTestEvaluationPositiveMemberData))]
        public void TestEvaluationPositive(string input, object expectedOutput)
        {
            var sourceText = SourceText.FromString(input);
            var evaluator = new Evaluator();

            var result = evaluator.Evaluate(sourceText);
            result.DiagnosticsOutput.Should().BeEmpty();
            result.EvaluationOutput.Should().BeEquivalentTo(expectedOutput);
        }

        public static IEnumerable<object[]> GetTestEvaluationPositiveMemberData()
            => new object[][]
            {
                new object[] { "bool.TrueString", bool.TrueString },
                new object[] { "string.Empty", string.Empty },
                new object[] { "new System.Exception()", new System.Exception() },
                new object[] { "new System.Exception(\"This is an exception\")", new System.Exception("This is an exception") },
                new object[] { "new System.Exception(message: \"This is an exception\")", new System.Exception("This is an exception") }
            };
    }
}
