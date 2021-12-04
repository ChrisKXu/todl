using System;
using System.Collections.Generic;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Symbols;

public sealed class FunctionSymbol : Symbol
{
    internal FunctionSymbol(
        FunctionDeclarationMember functionDeclarationMember,
        IEnumerable<VariableSymbol> parameters)
    {
        FunctionDeclarationMember = functionDeclarationMember;
        Parameters = (parameters ?? Enumerable.Empty<VariableSymbol>()).ToHashSet();
    }

    public FunctionDeclarationMember FunctionDeclarationMember { get; }
    public IReadOnlySet<VariableSymbol> Parameters { get; }

    public override string Name => FunctionDeclarationMember.Name.ToString();
    public TypeSymbol ReturnType
        => ClrTypeSymbol.MapClrType(FunctionDeclarationMember.SyntaxTree.ClrTypeCacheView.ResolveType(FunctionDeclarationMember.ReturnType));

    public static FunctionSymbol FromFunctionDeclarationMember(FunctionDeclarationMember functionDeclarationMember)
    {
        var parameters = functionDeclarationMember.Parameters.Items.Select(p => new VariableSymbol(
            name: p.Identifier.Text.ToString(),
            readOnly: true,
            type: ClrTypeSymbol.MapClrType(functionDeclarationMember.SyntaxTree.ClrTypeCacheView.ResolveType(p.ParameterType))));

        return new(functionDeclarationMember, parameters);
    }

    public override bool Equals(Symbol other)
    {
        if (other is FunctionSymbol functionSymbol)
        {
            return FunctionDeclarationMember.Equals(functionSymbol.FunctionDeclarationMember)
                && Parameters.SetEquals(functionSymbol.Parameters);
        }

        return false;
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(FunctionDeclarationMember);

        foreach (var p in Parameters)
        {
            hashCode.Add(p);
        }

        return hashCode.ToHashCode();
    }
}
