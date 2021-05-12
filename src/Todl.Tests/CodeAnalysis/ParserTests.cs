using FluentAssertions;
using Todl.CodeAnalysis;
using Xunit;

namespace Todl.Tests.CodeAnalysis
{
    public class ParserTests
    {
        [Fact]
        public void BasicParserTests()
        {
            var sourceText = SourceText.FromString("1 + 2 + 3");
            var syntaxTree = SyntaxTree.Parse(sourceText);
            syntaxTree.Should().NotBeNull();

            var sourceText2 = SourceText.FromString("1 + 2 * 3");
            var syntaxTree2 = SyntaxTree.Parse(sourceText2);
            syntaxTree2.Should().NotBeNull();
        }
    }
}