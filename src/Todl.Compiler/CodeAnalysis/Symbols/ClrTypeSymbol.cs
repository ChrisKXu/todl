using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Todl.Compiler.CodeAnalysis.Symbols;

public sealed class ClrTypeSymbol : TypeSymbol
{
    public override bool IsNative => true;
    public override bool IsArray => ClrType.IsArray;
    public override string Name => ClrType.FullName;
    public override bool IsReferenceType => !ClrType.IsValueType;
    public string Namespace => ClrType.Namespace;

    public Type ClrType { get; }

    public override SpecialType SpecialType { get; }

    internal ClrTypeSymbol(Type clrType)
        : this(clrType, SpecialType.None)
    {

    }

    internal ClrTypeSymbol(Type clrType, SpecialType specialType)
    {
        ClrType = clrType;
        SpecialType = specialType;
    }

    public override bool Equals(Symbol other)
    {
        if (other is ClrTypeSymbol clrTypeSymbol)
        {
            return ClrType.Equals(clrTypeSymbol.ClrType);
        }

        return false;
    }

    public MemberInfo ResolveMember(ReadOnlyMemory<char> name)
    {
        return ClrType.GetMember(name.ToString()).FirstOrDefault();
    }

    public IEnumerable<PropertyInfo> ResolveIndexerCandidates()
    {
        var defaultMemberAttributeData = CustomAttributeData.GetCustomAttributes(ClrType)
            .FirstOrDefault(a => a.AttributeType.FullName == typeof(DefaultMemberAttribute).FullName);

        if (defaultMemberAttributeData is null)
        {
            return Enumerable.Empty<PropertyInfo>();
        }

        var indexerName = (string)defaultMemberAttributeData.ConstructorArguments[0].Value;

        return ClrType
            .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(p => p.Name == indexerName && p.GetIndexParameters().Length == 1);
    }

    public override int GetHashCode()
        => ClrType.GetHashCode();

    public override string ToString() => Name;
}
