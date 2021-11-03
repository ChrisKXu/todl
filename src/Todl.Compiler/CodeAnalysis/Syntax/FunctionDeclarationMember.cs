using System.Collections.Generic;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public sealed class Parameter : SyntaxNode
    {
        public SyntaxToken CommaToken { get; internal init; }
        public NameExpression ParameterType { get; internal init; }
        public SyntaxToken Identifier { get; internal init; }

        public Parameter(SyntaxTree syntaxTree) : base(syntaxTree) { }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            if (CommaToken != null)
            {
                yield return CommaToken;
            }

            yield return ParameterType;
            yield return Identifier;
        }
    }

    public sealed class ParameterList : SyntaxNode
    {
        public SyntaxToken OpenParenthesisToken { get; internal init; }
        public IReadOnlyList<Parameter> Parameters { get; internal init; }
        public SyntaxToken CloseParenthesisToken { get; internal init; }

        public ParameterList(SyntaxTree syntaxTree) : base(syntaxTree) { }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return OpenParenthesisToken;

            foreach (var parameter in Parameters)
            {
                yield return parameter;
            }

            yield return CloseParenthesisToken;
        }
    }

    public sealed class FunctionDeclarationMember : Member
    {
        public NameExpression ReturnType { get; internal init; }
        public SyntaxToken Name { get; internal init; }
        public ParameterList ParameterList { get; internal init; }
        public BlockStatement Body { get; internal init; }

        public FunctionDeclarationMember(SyntaxTree syntaxTree) : base(syntaxTree) { }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return ReturnType;
            yield return Name;
            yield return ParameterList;
            yield return Body;
        }
    }

    public sealed partial class Parser
    {
        private ParameterList ParseParameterList()
        {
            var openParenthesisToken = ExpectToken(SyntaxKind.OpenParenthesisToken);
            var parameters = new List<Parameter>();
            var closeParenthesisToken = ExpectToken(SyntaxKind.CloseParenthesisToken);

            return new ParameterList(syntaxTree)
            {
                OpenParenthesisToken = openParenthesisToken,
                Parameters = parameters,
                CloseParenthesisToken = closeParenthesisToken
            };
        }

        private FunctionDeclarationMember ParseFunctionDeclarationMember()
        {
            var returnType = ParseNameExpression();
            var name = ExpectToken(SyntaxKind.IdentifierToken);
            var parameterList = ParseParameterList();
            var body = ParseBlockStatement();

            return new FunctionDeclarationMember(syntaxTree)
            {
                ReturnType = returnType,
                Name = name,
                ParameterList = parameterList,
                Body = body
            };
        }
    }
}
