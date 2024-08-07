﻿using System.Collections.Generic;
using System.Collections.Immutable;
using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax;

public sealed class AssignmentExpression : Expression
{
    public Expression Left { get; internal init; }
    public SyntaxToken AssignmentOperator { get; internal init; }
    public Expression Right { get; internal init; }

    public override TextSpan Text => TextSpan.FromTextSpans(Left.Text, Right.Text);

    public static readonly IReadOnlySet<SyntaxKind> AssignmentOperators
        = ImmutableHashSet.CreateRange(
        [
            SyntaxKind.EqualsToken,
            SyntaxKind.PlusEqualsToken,
            SyntaxKind.MinusEqualsToken,
            SyntaxKind.StarEqualsToken,
            SyntaxKind.SlashEqualsToken
        ]);
}

public sealed partial class Parser
{
    private AssignmentExpression ParseAssignmentExpression(Expression left)
        => new()
        {
            SyntaxTree = syntaxTree,
            Left = left,
            AssignmentOperator = ExpectToken(Current.Kind),
            Right = ParseExpression()
        };
}
