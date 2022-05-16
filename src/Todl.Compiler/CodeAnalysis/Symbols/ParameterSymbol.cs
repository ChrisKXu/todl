using System;
using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Symbols;

public sealed class ParameterSymbol : VariableSymbol
{
    public Parameter Parameter { get; internal init; }

    public override string Name => Parameter.Identifier.Text.ToString();
    public override bool ReadOnly => false;

    public override TypeSymbol Type
        => Parameter.SyntaxTree.ClrTypeCacheView.ResolveType(Parameter.ParameterType);

    public override bool Equals(Symbol other)
        => other is ParameterSymbol parameterSymbol
        && Name.Equals(parameterSymbol.Name)
        && Type.Equals(parameterSymbol.Type);

    public override int GetHashCode() => HashCode.Combine(Name, Type);
}
