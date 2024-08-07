﻿using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax;

public class LiteralExpression : Expression
{
    public SyntaxToken LiteralToken { get; internal init; }

    public override TextSpan Text => LiteralToken.Text;
}
