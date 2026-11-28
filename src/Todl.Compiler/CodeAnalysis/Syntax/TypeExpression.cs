using System.Collections.Immutable;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Syntax;

public sealed class TypeExpression : Expression
{
    public NameExpression BaseTypeExpression { get; internal init; }
    public SyntaxToken? QuestionToken { get; internal init; }
    public ImmutableArray<ArrayRankSpecifier> ArrayRankSpecifiers { get; internal init; }

    public bool IsNullableType => QuestionToken is not null;
    public bool IsArrayType => ArrayRankSpecifiers.Any();

    public override TextSpan Text
        => IsArrayType
            ? TextSpan.FromBounds(BaseTypeExpression.Text.Start, ArrayRankSpecifiers[^1].CloseBracketToken.Span.End)
            : IsNullableType
                ? TextSpan.FromBounds(BaseTypeExpression.Text.Start, QuestionToken.Value.Span.End)
                : BaseTypeExpression.Text;
}

public sealed class ArrayRankSpecifier : SyntaxNode
{
    public SyntaxToken OpenBracketToken { get; internal set; }
    public SyntaxToken CloseBracketToken { get; internal set; }

    public override TextSpan Text => TextSpan.FromBounds(OpenBracketToken.Span.Start, CloseBracketToken.Span.End);
}

public sealed partial class Parser
{
    private TypeExpression ParseTypeExpression()
    {
        var nameExpression = ParseNameExpression();
        var questionToken = ParseOptionalQuestionToken();
        var arrayRankSpecifiers = ImmutableArray.CreateBuilder<ArrayRankSpecifier>();

        while (Current.Kind == SyntaxKind.OpenBracketToken)
        {
            arrayRankSpecifiers.Add(ParseArrayRankSpecifier());
        }

        while (arrayRankSpecifiers.Count > 0 && Current.Kind == SyntaxKind.QuestionToken)
        {
            var misplacedQuestionToken = NextToken();
            ReportDiagnostic(new()
            {
                Message = "Array types cannot be made nullable; place '?' before the rank specifiers to make the elements nullable (e.g. 'int?[]').",
                ErrorCode = ErrorCode.InvalidNullableType,
                Level = DiagnosticLevel.Error,
                TextLocation = new(syntaxTree.SourceText, misplacedQuestionToken.Span)
            });
        }

        return new()
        {
            SyntaxTree = syntaxTree,
            BaseTypeExpression = nameExpression,
            QuestionToken = questionToken,
            ArrayRankSpecifiers = arrayRankSpecifiers.ToImmutable()
        };
    }

    // A nullable type takes a single '?'; any further ones are reported and dropped.
    private SyntaxToken? ParseOptionalQuestionToken()
    {
        if (Current.Kind != SyntaxKind.QuestionToken)
        {
            return null;
        }

        var questionToken = NextToken();

        while (Current.Kind == SyntaxKind.QuestionToken)
        {
            var extraQuestionToken = NextToken();
            ReportDiagnostic(new()
            {
                Message = "A nullable type cannot be nested: '?' can be applied to a type only once.",
                ErrorCode = ErrorCode.InvalidNullableType,
                Level = DiagnosticLevel.Error,
                TextLocation = new(syntaxTree.SourceText, extraQuestionToken.Span)
            });
        }

        return questionToken;
    }

    private ArrayRankSpecifier ParseArrayRankSpecifier()
        => new()
        {
            SyntaxTree = syntaxTree,
            OpenBracketToken = ExpectToken(SyntaxKind.OpenBracketToken),
            CloseBracketToken = ExpectToken(SyntaxKind.CloseBracketToken)
        };
}
