﻿using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax;

public sealed class ExpressionStatement : Statement
{
    public Expression Expression { get; internal init; }
    public SyntaxToken SemicolonToken { get; internal init; }

    public override TextSpan Text
        => TextSpan.FromTextSpans(Expression.Text, SemicolonToken.Text);
}

public sealed partial class Parser
{
    private ExpressionStatement ParseExpressionStatement()
        => new()
        {
            SyntaxTree = syntaxTree,
            Expression = ParseExpression(),
            SemicolonToken = ExpectToken(SyntaxKind.SemicolonToken)
        };
}
