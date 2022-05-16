using System.Collections.Generic;
using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public sealed class Parameter : SyntaxNode
    {
        public TypeExpression ParameterType { get; internal init; }
        public SyntaxToken Identifier { get; internal init; }

        public override TextSpan Text
            => TextSpan.FromTextSpans(ParameterType.Text, Identifier.Text);
    }

    public sealed class FunctionDeclarationMember : Member
    {
        public NameExpression ReturnType { get; internal init; }
        public SyntaxToken Name { get; internal init; }
        public CommaSeparatedSyntaxList<Parameter> Parameters { get; internal init; }
        public BlockStatement Body { get; internal init; }

        public override TextSpan Text => TextSpan.FromTextSpans(ReturnType.Text, Body.Text);
    }

    public sealed partial class Parser
    {
        private Parameter ParseParemeter()
        {
            return new()
            {
                SyntaxTree = syntaxTree,
                ParameterType = ParseTypeExpression(),
                Identifier = ExpectToken(SyntaxKind.IdentifierToken)
            };
        }

        private FunctionDeclarationMember ParseFunctionDeclarationMember()
        {
            var returnType = ParseNameExpression();
            var name = ExpectToken(SyntaxKind.IdentifierToken);
            var parameters = ParseCommaSeparatedSyntaxList(ParseParemeter);
            var body = ParseBlockStatement();

            return new FunctionDeclarationMember()
            {
                SyntaxTree = syntaxTree,
                ReturnType = returnType,
                Name = name,
                Parameters = parameters,
                Body = body
            };
        }
    }
}
