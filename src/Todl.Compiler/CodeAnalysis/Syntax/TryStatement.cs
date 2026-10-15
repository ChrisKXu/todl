using System.Collections.Immutable;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Syntax;

public sealed class TryStatement : Statement
{
    public SyntaxToken TryKeywordToken { get; internal init; }
    public BlockStatement BlockStatement { get; internal init; }
    public ImmutableArray<ExceptClause> ExceptClauses { get; internal init; }
    public FinallyClause FinallyClause { get; internal init; }

    public override TextSpan Text
        => TextSpan.FromBounds(
            TryKeywordToken.Span.Start,
            HandlerClauses.GetEnd(BlockStatement, ExceptClauses, FinallyClause));
}

public sealed class ExceptClause : SyntaxNode
{
    public SyntaxToken ExceptKeywordToken { get; internal init; }
    public TypeExpression TypeExpression { get; internal init; }
    public SyntaxToken? IdentifierToken { get; internal init; }
    public BlockStatement BlockStatement { get; internal init; }

    public bool IsGeneralClause => TypeExpression is null;

    public override TextSpan Text
        => TextSpan.FromBounds(ExceptKeywordToken.Span.Start, BlockStatement.Text.End);
}

public sealed class FinallyClause : SyntaxNode
{
    public SyntaxToken FinallyKeywordToken { get; internal init; }
    public BlockStatement BlockStatement { get; internal init; }

    public override TextSpan Text
        => TextSpan.FromBounds(FinallyKeywordToken.Span.Start, BlockStatement.Text.End);
}

internal static class HandlerClauses
{
    public static int GetEnd(
        BlockStatement blockStatement,
        ImmutableArray<ExceptClause> exceptClauses,
        FinallyClause finallyClause)
    {
        if (finallyClause is not null)
        {
            return finallyClause.Text.End;
        }

        return exceptClauses.IsEmpty ? blockStatement.Text.End : exceptClauses[^1].Text.End;
    }
}

public sealed partial class Parser
{
    private TryStatement ParseTryStatement()
    {
        var tryKeywordToken = ExpectToken(SyntaxKind.TryKeywordToken);
        var blockStatement = ParseBlockStatement();
        var (exceptClauses, finallyClause) = ParseHandlerClauses();

        if (exceptClauses.IsEmpty && finallyClause is null)
        {
            ReportDiagnostic(new Diagnostic()
            {
                Message = "A try statement requires at least one except or finally clause",
                ErrorCode = ErrorCode.UnexpectedToken,
                Level = DiagnosticLevel.Error,
                TextLocation = new TextLocation(syntaxTree.SourceText, tryKeywordToken.Span)
            });
        }

        return new()
        {
            SyntaxTree = syntaxTree,
            TryKeywordToken = tryKeywordToken,
            BlockStatement = blockStatement,
            ExceptClauses = exceptClauses,
            FinallyClause = finallyClause
        };
    }

    // Misplaced clauses are reported and consumed to avoid cascading errors.
    private (ImmutableArray<ExceptClause> ExceptClauses, FinallyClause FinallyClause) ParseHandlerClauses()
    {
        var exceptClauses = ImmutableArray.CreateBuilder<ExceptClause>();
        FinallyClause finallyClause = null;

        while (Current.Kind == SyntaxKind.ExceptKeywordToken || Current.Kind == SyntaxKind.FinallyKeywordToken)
        {
            if (Current.Kind == SyntaxKind.ExceptKeywordToken)
            {
                var exceptClause = ParseExceptClause();
                if (finallyClause is not null)
                {
                    ReportMisplacedHandlerClause("An except clause must precede the finally clause", exceptClause.ExceptKeywordToken);
                }

                exceptClauses.Add(exceptClause);
            }
            else
            {
                var parsedFinallyClause = ParseFinallyClause();
                if (finallyClause is not null)
                {
                    ReportMisplacedHandlerClause("At most one finally clause is allowed", parsedFinallyClause.FinallyKeywordToken);
                }
                else
                {
                    finallyClause = parsedFinallyClause;
                }
            }
        }

        return (exceptClauses.ToImmutable(), finallyClause);
    }

    private ExceptClause ParseExceptClause()
    {
        var exceptKeywordToken = ExpectToken(SyntaxKind.ExceptKeywordToken);
        TypeExpression typeExpression = null;
        SyntaxToken? identifierToken = null;

        // after 'except', '{' means a general clause; anything else starts a type expression
        if (Current.Kind != SyntaxKind.OpenBraceToken)
        {
            typeExpression = ParseTypeExpression();

            if (Current.Kind == SyntaxKind.IdentifierToken)
            {
                identifierToken = ExpectToken(SyntaxKind.IdentifierToken);
            }
        }

        return new()
        {
            SyntaxTree = syntaxTree,
            ExceptKeywordToken = exceptKeywordToken,
            TypeExpression = typeExpression,
            IdentifierToken = identifierToken,
            BlockStatement = ParseBlockStatement()
        };
    }

    private FinallyClause ParseFinallyClause()
        => new()
        {
            SyntaxTree = syntaxTree,
            FinallyKeywordToken = ExpectToken(SyntaxKind.FinallyKeywordToken),
            BlockStatement = ParseBlockStatement()
        };

    private void ReportMisplacedHandlerClause(string message, SyntaxToken keywordToken)
        => ReportDiagnostic(new Diagnostic()
        {
            Message = message,
            ErrorCode = ErrorCode.UnexpectedToken,
            Level = DiagnosticLevel.Error,
            TextLocation = new TextLocation(syntaxTree.SourceText, keywordToken.Span)
        });
}
