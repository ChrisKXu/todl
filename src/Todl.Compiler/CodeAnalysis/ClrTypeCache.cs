using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis;

public sealed class ClrTypeCache
{
    public ImmutableArray<Assembly> Assemblies { get; }
    public Assembly CoreAssembly { get; } // the assembly that contains object, bool, int, etc...

    public BuiltInTypes BuiltInTypes { get; }

    // Lazy cache: full type name -> ClrTypeSymbol
    private readonly ConcurrentDictionary<string, ClrTypeSymbol> typeCache = new();

    // Lazy namespace -> types mapping (only populated when needed for wildcard imports)
    private readonly ConcurrentDictionary<string, ImmutableArray<ClrTypeSymbol>> namespaceTypes = new();

    private static readonly ImmutableDictionary<string, SpecialType> builtInTypeNames
        = new Dictionary<string, SpecialType>()
        {
            { "bool", SpecialType.ClrBoolean },
            { typeof(bool).FullName, SpecialType.ClrBoolean },
            { "byte", SpecialType.ClrByte },
            { typeof(byte).FullName, SpecialType.ClrByte },
            { "char", SpecialType.ClrChar },
            { typeof(char).FullName, SpecialType.ClrChar },
            { "int", SpecialType.ClrInt32 },
            { typeof(int).FullName, SpecialType.ClrInt32 },
            { "uint", SpecialType.ClrUInt32 },
            { typeof(uint).FullName, SpecialType.ClrUInt32 },
            { "long", SpecialType.ClrInt64 },
            { typeof(long).FullName, SpecialType.ClrInt64 },
            { "ulong", SpecialType.ClrUInt64 },
            { typeof(ulong).FullName, SpecialType.ClrUInt64 },
            { "string", SpecialType.ClrString },
            { typeof(string).FullName, SpecialType.ClrString },
            { "void", SpecialType.ClrVoid },
            { typeof(void).FullName, SpecialType.ClrVoid },
            { "object", SpecialType.ClrObject },
            { typeof(object).FullName, SpecialType.ClrObject },
            { "float", SpecialType.ClrFloat },
            { typeof(float).FullName, SpecialType.ClrFloat },
            { "double", SpecialType.ClrDouble },
            { typeof(double).FullName, SpecialType.ClrDouble }
        }.ToImmutableDictionary();

    private ClrTypeCache(IEnumerable<Assembly> assemblies, Assembly coreAssembly)
    {
        Assemblies = assemblies.ToImmutableArray();
        CoreAssembly = coreAssembly;
        BuiltInTypes = new BuiltInTypes(this);
        // No eager type loading - types resolved on demand
    }

    public ClrTypeSymbol Resolve(string name)
    {
        // 1. Check built-in types first (fast path)
        if (builtInTypeNames.TryGetValue(name, out var specialType))
        {
            return ResolveSpecialType(specialType);
        }

        // 2. Check cache
        if (typeCache.TryGetValue(name, out var cached))
        {
            return cached;
        }

        // 3. Try direct assembly lookup (O(1) via CLR metadata)
        foreach (var assembly in Assemblies)
        {
            var type = assembly.GetType(name);
            if (type != null && !type.IsGenericType)
            {
                var symbol = new ClrTypeSymbol(type);
                // Thread-safe cache population
                return typeCache.GetOrAdd(name, symbol);
            }
        }

        // 4. Not found
        return null;
    }

    public ClrTypeSymbol Resolve(Type type)
    {
        if (type == null) return null;

        // Check built-in types
        if (builtInTypeNames.TryGetValue(type.FullName, out var specialType))
        {
            return ResolveSpecialType(specialType);
        }

        // Check cache by full name
        var fullName = type.FullName;
        if (typeCache.TryGetValue(fullName, out var cached))
        {
            return cached;
        }

        // Create and cache
        if (!type.IsGenericType)
        {
            var symbol = new ClrTypeSymbol(type);
            return typeCache.GetOrAdd(fullName, symbol);
        }

        return null;
    }

    public ClrTypeSymbol ResolveSpecialType(SpecialType specialType)
    {
        var type = specialType switch
        {
            SpecialType.ClrBoolean => CoreAssembly.GetType(typeof(bool).FullName),
            SpecialType.ClrObject => CoreAssembly.GetType(typeof(object).FullName),
            SpecialType.ClrVoid => CoreAssembly.GetType(typeof(void).FullName),
            SpecialType.ClrString => CoreAssembly.GetType(typeof(string).FullName),
            SpecialType.ClrSByte => CoreAssembly.GetType(typeof(sbyte).FullName),
            SpecialType.ClrByte => CoreAssembly.GetType(typeof(byte).FullName),
            SpecialType.ClrInt16 => CoreAssembly.GetType(typeof(short).FullName),
            SpecialType.ClrUInt16 => CoreAssembly.GetType(typeof(ushort).FullName),
            SpecialType.ClrChar => CoreAssembly.GetType(typeof(char).FullName),
            SpecialType.ClrInt32 => CoreAssembly.GetType(typeof(int).FullName),
            SpecialType.ClrUInt32 => CoreAssembly.GetType(typeof(uint).FullName),
            SpecialType.ClrInt64 => CoreAssembly.GetType(typeof(long).FullName),
            SpecialType.ClrUInt64 => CoreAssembly.GetType(typeof(ulong).FullName),
            SpecialType.ClrDouble => CoreAssembly.GetType(typeof(double).FullName),
            SpecialType.ClrFloat => CoreAssembly.GetType(typeof(float).FullName),
            _ => throw new NotSupportedException($"Special type {specialType} is not supported")
        };

        return new(type, specialType);
    }

    /// <summary>
    /// Gets all types in a namespace (lazy, cached).
    /// Used for wildcard imports.
    /// </summary>
    public ImmutableArray<ClrTypeSymbol> GetTypesInNamespace(string namespaceName)
    {
        return namespaceTypes.GetOrAdd(namespaceName, ns =>
        {
            var builder = ImmutableArray.CreateBuilder<ClrTypeSymbol>();
            foreach (var assembly in Assemblies)
            {
                foreach (var type in assembly.GetExportedTypes())
                {
                    if (type.Namespace == ns && !type.IsGenericType)
                    {
                        var symbol = Resolve(type);
                        if (symbol != null)
                        {
                            builder.Add(symbol);
                        }
                    }
                }
            }
            return builder.ToImmutable();
        });
    }

    public ClrTypeCacheView CreateView(IEnumerable<ImportDirective> importDirectives)
        => new(this, importDirectives);

    public static ClrTypeCache FromAssemblies(IEnumerable<Assembly> assemblies, Assembly coreAssembly)
        => new(assemblies, coreAssembly);
}
