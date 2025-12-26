using System;
using System.Collections.Immutable;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Syntax;

/// <summary>
/// This class implements a recursive descent parser for the todl language
/// </summary>
public sealed partial class Parser
{
    private readonly SyntaxTree syntaxTree;
    private readonly DiagnosticBag.Builder diagnosticBuilder;
    private readonly ImmutableArray<Directive>.Builder directives
        = ImmutableArray.CreateBuilder<Directive>();
    private readonly ImmutableArray<Member>.Builder members
        = ImmutableArray.CreateBuilder<Member>();

    private int position = 0;

    private ImmutableArray<SyntaxToken> SyntaxTokens => syntaxTree.SyntaxTokens;

    private SyntaxToken Current => Seek(0);
    private SyntaxToken Peak => Seek(1);

    public ImmutableArray<Directive> Directives => directives.ToImmutable();
    public ImmutableArray<Member> Members => members.ToImmutable();

    private SyntaxToken Seek(int offset)
    {
        var index = position + offset;
        if (index >= SyntaxTokens.Length)
        {
            return SyntaxTokens[^1];
        }

        return SyntaxTokens[index];
    }

    private SyntaxToken NextToken()
    {
        var current = this.Current;
        ++this.position;
        return current;
    }

    private SyntaxToken ExpectToken(SyntaxKind syntaxKind)
    {
        if (Current.Kind == syntaxKind)
        {
            return NextToken();
        }

        return ReportUnexpectedToken(syntaxKind);
    }

    private SyntaxToken ExpectUntil(SyntaxKind syntaxKind, Action action)
    {
        while (Current.Kind != syntaxKind
            && Current.Kind != SyntaxKind.EndOfFileToken
            && Current.Kind != SyntaxKind.BadToken)
        {
            var oldPosition = position;

            action();

            // if the action hasn't taken any tokens it's unlikely that it will ever do so
            // in the following rounds, this could cause an infinite loop.
            if (position == oldPosition)
            {
                break;
            }
        }

        return ExpectToken(syntaxKind);
    }

    internal Parser(SyntaxTree syntaxTree, DiagnosticBag.Builder diagnosticBuilder)
    {
        this.syntaxTree = syntaxTree;
        this.diagnosticBuilder = diagnosticBuilder;
    }

    public void Parse()
    {
        while (Current.Kind == SyntaxKind.ImportKeywordToken)
        {
            directives.Add(ParseDirective());
        }

        while (Current.Kind != SyntaxKind.EndOfFileToken
            && Current.Kind != SyntaxKind.BadToken)
        {
            members.Add(ParseMember());
        }
    }

    private Expression ParsePrimaryExpression()
    {
        Expression baseExpression;

        switch (Current.Kind)
        {
            case SyntaxKind.NumberToken:
            case SyntaxKind.StringToken:
            case SyntaxKind.TrueKeywordToken:
            case SyntaxKind.FalseKeywordToken:
                baseExpression = new LiteralExpression()
                {
                    SyntaxTree = syntaxTree,
                    LiteralToken = ExpectToken(Current.Kind)
                };
                break;
            case SyntaxKind.OpenParenthesisToken:
                baseExpression = this.ParseParethesizedExpression();
                break;
            case SyntaxKind.NewKeywordToken:
                baseExpression = ParseNewExpression();
                break;
            case SyntaxKind.IdentifierToken:
            default:
                baseExpression = ParseNameExpression();
                break;
        }

        while (true)
        {
            if (AssignmentExpression.AssignmentOperators.Contains(Current.Kind))
            {
                baseExpression = ParseAssignmentExpression(baseExpression);
            }
            else if (Current.Kind == SyntaxKind.DotToken && Peak.Kind == SyntaxKind.IdentifierToken)
            {
                baseExpression = new MemberAccessExpression()
                {
                    SyntaxTree = syntaxTree,
                    BaseExpression = baseExpression,
                    DotToken = ExpectToken(SyntaxKind.DotToken),
                    MemberIdentifierToken = ExpectToken(SyntaxKind.IdentifierToken)
                };
            }
            else if (Current.Kind == SyntaxKind.OpenParenthesisToken)
            {
                baseExpression = ParseFunctionCallExpression(baseExpression);
                break;
            }
            else
            {
                break;
            }
        }

        return baseExpression;
    }

    private SyntaxToken ReportUnexpectedToken(SyntaxKind expectedSyntaxKind)
    {
        // return a fake syntax token of the expected kind, with a text span at the current location with 0 length 
        return new()
        {
            Kind = expectedSyntaxKind,
            Text = syntaxTree.SourceText.GetTextSpan(Current.Text.Start, 0),
            LeadingTrivia = ImmutableArray<SyntaxTrivia>.Empty,
            TrailingTrivia = ImmutableArray<SyntaxTrivia>.Empty,
            Missing = true,
            ErrorCode = ErrorCode.UnexpectedToken
        };
    }

    private void ReportDiagnostic(Diagnostic diagnostic)
        => diagnosticBuilder.Add(diagnostic);
}
