namespace Todl.Compiler.Diagnostics
{
    public enum ErrorCode
    {
        Invalid,

        // LexerErrors
        UnrecognizedToken,
        UnexpectedEndOfFile,

        // ParserErrors
        UnexpectedToken,
        MixedPositionalAndNamedArguments,
        IfUnlessKeywordMismatch,
        DuplicateBareElseClauses,
        MisplacedBareElseClauses,

        // BinderErrors
        UndeclaredVariable,
        ReadOnlyVariable,
        TypeMismatch,
        NotAnLValue,
        UnsupportedOperator,
        UnsupportedLiteral,
        NoMatchingCandidate,
        MemberNotFound,
        MemberNotAccessible,
        TypeNotFound,
        UnexpectedStatement,
        AmbiguousFunctionDeclaration,
        NotAllPathsReturn,
        UnreachableCode,
        DuplicateParameterName,
        MissingEntryPoint,
        NoEnclosingLoop
    }
}
