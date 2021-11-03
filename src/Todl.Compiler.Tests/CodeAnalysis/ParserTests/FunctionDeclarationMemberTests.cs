using Todl.Compiler.CodeAnalysis.Syntax;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis
{
    public partial class ParserTests
    {
        [Theory]
        [InlineData("int Function(){}")]
        [InlineData("System.Uri Function(){}")]
        public void ParseFunctionDeclarationMemberBasic(string inputText)
        {
            var function = ParseMember<FunctionDeclarationMember>(inputText);
            function.Should().NotBeNull();
        }
    }
}
