using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Symbols;

/// <summary>
/// The variable declared by a typed except clause; typed exactly as the clause and read-only.
/// </summary>
internal sealed class ExceptionVariableSymbol : VariableSymbol
{
    public ExceptClause ExceptClause { get; }

    public override string Name => ExceptClause.IdentifierToken.Value.Text.ToString();
    public override bool ReadOnly => true;
    public override TypeSymbol Type { get; }

    public ExceptionVariableSymbol(ExceptClause exceptClause, TypeSymbol type)
    {
        ExceptClause = exceptClause;
        Type = type;
    }
}
