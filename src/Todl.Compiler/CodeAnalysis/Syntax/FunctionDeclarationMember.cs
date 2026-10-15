using System.Collections.Immutable;
using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax;

public sealed class Parameter : SyntaxNode
{
    public TypeExpression ParameterType { get; internal init; }
    public SyntaxToken Identifier { get; internal init; }

    public override TextSpan Text
        => TextSpan.FromBounds(ParameterType.Text.Start, Identifier.Span.End);
}

public sealed class FunctionDeclarationMember : Member
{
    public TypeExpression ReturnType { get; internal init; }
    public SyntaxToken Name { get; internal init; }
    public CommaSeparatedSyntaxList<Parameter> Parameters { get; internal init; }
    public BlockStatement Body { get; internal init; }
    public ImmutableArray<ExceptClause> ExceptClauses { get; internal init; }
    public FinallyClause FinallyClause { get; internal init; }

    public bool HasHandlerClauses => !ExceptClauses.IsEmpty || FinallyClause is not null;

    public override TextSpan Text
        => TextSpan.FromBounds(ReturnType.Text.Start, HandlerClauses.GetEnd(Body, ExceptClauses, FinallyClause));
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
        var returnType = ParseTypeExpression();
        var name = ExpectToken(SyntaxKind.IdentifierToken);
        var parameters = ParseCommaSeparatedSyntaxList(ParseParemeter);
        var body = ParseBlockStatement();
        var (exceptClauses, finallyClause) = ParseHandlerClauses();

        return new FunctionDeclarationMember()
        {
            SyntaxTree = syntaxTree,
            ReturnType = returnType,
            Name = name,
            Parameters = parameters,
            Body = body,
            ExceptClauses = exceptClauses,
            FinallyClause = finallyClause
        };
    }
}
