using System;
using System.Collections.Generic;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Symbols;

public sealed class FunctionSymbol : Symbol
{
    public FunctionDeclarationMember FunctionDeclarationMember { get; internal init; }
    public IEnumerable<ParameterSymbol> Parameters { get; internal init; }

    public IEnumerable<string> OrderedParameterNames
        => FunctionDeclarationMember.Parameters.Items.Select(p => p.Identifier.Text.ToString());
    public override string Name => FunctionDeclarationMember.Name.Text.ToString();
    public TypeSymbol ReturnType
        => FunctionDeclarationMember.SyntaxTree.ClrTypeCacheView.ResolveType(FunctionDeclarationMember.ReturnType);

    public static FunctionSymbol FromFunctionDeclarationMember(FunctionDeclarationMember functionDeclarationMember)
    {
        var parameters = functionDeclarationMember
            .Parameters
            .Items
            .Select(p => new ParameterSymbol() { Parameter = p });

        return new()
        {
            FunctionDeclarationMember = functionDeclarationMember,
            Parameters = parameters
        };
    }

    public override bool Equals(Symbol other)
        => other is FunctionSymbol functionSymbol
        && FunctionDeclarationMember == functionSymbol.FunctionDeclarationMember;

    public override int GetHashCode()
        => HashCode.Combine(FunctionDeclarationMember);

    public bool Match(string name, IReadOnlyDictionary<string, TypeSymbol> namedArguments)
    {
        if (!Name.Equals(name))
        {
            return false;
        }

        return Parameters
            .Select(p => KeyValuePair.Create(p.Name, p.Type))
            .ToHashSet()
            .SetEquals(namedArguments.ToHashSet());
    }

    public bool Match(string name, IEnumerable<TypeSymbol> positionalArguments)
    {
        if (!Name.Equals(name))
        {
            return false;
        }

        return Parameters.Select(p => p.Type).SequenceEqual(positionalArguments);
    }

    public bool IsAmbigousWith(FunctionSymbol other)
    {
        // one can't be ambiguous with itself
        if (Equals(other))
        {
            return false;
        }

        if (Name != other.Name)
        {
            return false;
        }

        // func(int a, string b) is ambigous with func(string b, int a)
        if (Parameters.ToHashSet().SetEquals(other.Parameters))
        {
            return true;
        }

        // func(int a, string b) is ambiguous with func(int b, string a)
        if (Parameters.Select(p => p.Type).SequenceEqual(other.Parameters.Select(p => p.Type)))
        {
            return true;
        }

        return false;
    }
}
