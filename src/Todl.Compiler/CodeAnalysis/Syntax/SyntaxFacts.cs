using System.Collections.Generic;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    static class SyntaxFacts
    {
        public static readonly IReadOnlyDictionary<SyntaxKind, int> UnaryOperatorPrecedence = new Dictionary<SyntaxKind, int>()
        {
            { SyntaxKind.PlusToken, 6 },
            { SyntaxKind.PlusPlusToken, 6 },
            { SyntaxKind.MinusToken, 6 },
            { SyntaxKind.MinusMinusToken, 6 },
            { SyntaxKind.BangToken, 6 },
        };

        public static readonly IReadOnlyDictionary<SyntaxKind, int> BinaryOperatorPrecedence = new Dictionary<SyntaxKind, int>()
        {
            { SyntaxKind.StarToken, 5 },
            { SyntaxKind.SlashToken, 5 },

            { SyntaxKind.PlusToken, 4 },
            { SyntaxKind.MinusToken, 4 },

            { SyntaxKind.EqualsEqualsToken, 3 },
            { SyntaxKind.BangEqualsToken, 3 },

            { SyntaxKind.AmpersandAmpersandToken, 2 },
            { SyntaxKind.AmpersandToken, 2 },

            { SyntaxKind.PipePipeToken, 1 },
            { SyntaxKind.PipeToken, 1 }
        };

        public static readonly IReadOnlyDictionary<string, SyntaxKind> KeywordMap = new Dictionary<string, SyntaxKind>()
        {
            { "true", SyntaxKind.TrueKeywordToken },
            { "false", SyntaxKind.FalseKeywordToken },

            { "const", SyntaxKind.ConstKeywordToken },
            { "let", SyntaxKind.LetKeywordToken }
        };
    }
}
