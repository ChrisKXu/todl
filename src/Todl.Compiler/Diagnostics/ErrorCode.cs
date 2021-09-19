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

        // BinderErrors
        UndeclaredVariable,
        ReadOnlyVariable,
        TypeMismatch,
        NotAnLValue,
        UnsupportedOperator,
        UnsupportedLiteral,
        NoMatchingCandidate,
        MemberNotFound,
        TypeNotFound
    }
}
