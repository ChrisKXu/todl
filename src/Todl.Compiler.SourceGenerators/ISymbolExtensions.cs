using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Todl.Compiler.SourceGenerators;

public static class ISymbolExtensions
{
    public static bool IsDerivedFrom(this ITypeSymbol type, INamedTypeSymbol baseType)
    {
        var current = type;

        while (current != null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
                return true;

            current = current.BaseType;
        }

        return false;
    }

    public static string CamelCasedName(this ISymbol symbol)
    {
        var name = symbol.Name;

        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        // "operator" is a c# keyword so we need to special handle it
        if (name.Equals("Operator"))
        {
            return "@operator";
        }

        var charArray = name.ToCharArray();
        charArray[0] = char.ToLowerInvariant(charArray[0]);
        return new string(charArray);
    }

    public static string GetPropertyTypeName(this IPropertySymbol property)
    {
        var propertyType = property.Type as INamedTypeSymbol;

        if (propertyType.ContainingType is not null)
        {
            return $"{propertyType.ContainingType.Name}.{propertyType.Name}";
        }

        if (propertyType.IsGenericType)
        {
            var commaSeparatedList = string.Join(",", propertyType.TypeArguments.Select(t => t.Name));
            return $"{propertyType.Name}<{commaSeparatedList}>";
        }

        return propertyType.Name;
    }
}
