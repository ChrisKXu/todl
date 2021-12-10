using System;
using System.Collections.Generic;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Symbols;

public sealed class FunctionSymbol : Symbol
{
    public FunctionDeclarationMember FunctionDeclarationMember { get; internal init; }
    public IReadOnlyDictionary<string, VariableSymbol> Parameters { get; internal init; }

    public IEnumerable<string> OrderedParameterNames
        => FunctionDeclarationMember.Parameters.Items.Select(p => p.Identifier.Text.ToString());
    public override string Name => FunctionDeclarationMember.Name.Text.ToString();
    public TypeSymbol ReturnType
        => ClrTypeSymbol.MapClrType(FunctionDeclarationMember.SyntaxTree.ClrTypeCacheView.ResolveType(FunctionDeclarationMember.ReturnType));

    public static FunctionSymbol FromFunctionDeclarationMember(FunctionDeclarationMember functionDeclarationMember)
    {
        var parameters = functionDeclarationMember
            .Parameters
            .Items
            .Select(p => new VariableSymbol(
                name: p.Identifier.Text.ToString(),
                readOnly: true,
                type: ClrTypeSymbol.MapClrType(functionDeclarationMember.SyntaxTree.ClrTypeCacheView.ResolveType(p.ParameterType))))
            .ToDictionary(v => v.Name, v => v);

        return new()
        {
            FunctionDeclarationMember = functionDeclarationMember,
            Parameters = parameters
        };
    }

    public override bool Equals(Symbol other)
        => other is FunctionSymbol functionSymbol
        && FunctionDeclarationMember.Equals(functionSymbol.FunctionDeclarationMember);

    public override int GetHashCode()
        => HashCode.Combine(FunctionDeclarationMember);

    public bool Match(string name, IReadOnlyDictionary<string, TypeSymbol> namedArguments)
    {
        if (!Name.Equals(name))
        {
            return false;
        }

        return Parameters
            .ToDictionary(p => p.Key, p => p.Value.Type)
            .SequenceEqual(namedArguments);
    }

    public bool Match(string name, IEnumerable<TypeSymbol> positionalArguments)
    {
        if (OrderedParameterNames.Count() != positionalArguments.Count())
        {
            return false;
        }

        var namedArguments = OrderedParameterNames
            .Zip(positionalArguments)
            .ToDictionary(p => p.First, p => p.Second);

        return Match(name, namedArguments);
    }
}
