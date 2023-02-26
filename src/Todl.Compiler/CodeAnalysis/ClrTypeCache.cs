﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis;

public sealed class ClrTypeCache
{
    private readonly HashSet<string> loadedNamespaces = new();

    public IReadOnlySet<Assembly> Assemblies { get; }
    public Assembly CoreAssembly { get; } // the assembly that contains object, bool, int, etc...
    public IReadOnlySet<ClrTypeSymbol> Types { get; }
    public IReadOnlySet<string> Namespaces => loadedNamespaces;

    public BuiltInTypes BuiltInTypes { get; }

    private static readonly IReadOnlyDictionary<string, SpecialType> BuiltInTypeNames
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
        };

    private ClrTypeCache(IEnumerable<Assembly> assemblies, Assembly coreAssembly)
    {
        Assemblies = assemblies.ToHashSet();
        CoreAssembly = coreAssembly;

        Types = assemblies
            .SelectMany(a => a.GetExportedTypes())
            .Where(t => !t.IsGenericType) // TODO: support generic type
            .Where(t => !BuiltInTypeNames.ContainsKey(t.FullName))
            .Select(t => new ClrTypeSymbol(t))
            .ToHashSet();

        BuiltInTypes = new BuiltInTypes(this);

        PopulateNamespaces();
    }

    public ClrTypeSymbol Resolve(string name)
    {
        if (BuiltInTypeNames.ContainsKey(name))
        {
            return ResolveSpecialType(BuiltInTypeNames[name]);
        }

        // TODO: obviously we need to optimize this
        return Types.FirstOrDefault(t => t.Name == name);
    }

    public ClrTypeSymbol ResolveSpecialType(SpecialType specialType)
    {
        var type = specialType switch
        {
            SpecialType.ClrBoolean => CoreAssembly.GetType(typeof(bool).FullName),
            SpecialType.ClrObject => CoreAssembly.GetType(typeof(object).FullName),
            SpecialType.ClrVoid => CoreAssembly.GetType(typeof(void).FullName),
            SpecialType.ClrString => CoreAssembly.GetType(typeof(string).FullName),
            SpecialType.ClrByte => CoreAssembly.GetType(typeof(byte).FullName),
            SpecialType.ClrChar => CoreAssembly.GetType(typeof(char).FullName),
            SpecialType.ClrInt32 => CoreAssembly.GetType(typeof(int).FullName),
            SpecialType.ClrUInt32 => CoreAssembly.GetType(typeof(uint).FullName),
            SpecialType.ClrInt64 => CoreAssembly.GetType(typeof(long).FullName),
            SpecialType.ClrUInt64 => CoreAssembly.GetType(typeof(ulong).FullName),
            SpecialType.ClrDouble => CoreAssembly.GetType(typeof(double).FullName),
            SpecialType.ClrFloat => CoreAssembly.GetType(typeof(float).FullName),
            _ => null
        };

        return new(type, specialType);
    }

    public ClrTypeCacheView CreateView(IEnumerable<ImportDirective> importDirectives)
        => new(this, importDirectives);

    public static ClrTypeCache FromAssemblies(IEnumerable<Assembly> assemblies, Assembly coreAssembly)
        => new(assemblies, coreAssembly);

    /// <summary>
    /// Populating loadedNamespaces with a full list of namespaces
    /// e.g.
    /// when input is "System.Collections.Generic"
    /// the result should be
    /// {
    ///     "System",
    ///     "System.Collections",
    ///     "System.Collections.Generic"
    /// }
    /// </summary>
    private void PopulateNamespaces()
    {
        var namespaces = Types
            .Where(t => !string.IsNullOrEmpty(t.Namespace))
            .Select(t => t.Namespace);

        foreach (var n in namespaces)
        {
            var position = -1;
            while ((position = n.IndexOf('.', position + 1)) != -1)
            {
                loadedNamespaces.Add(n[..position]);
            }
            loadedNamespaces.Add(n);
        }
    }
}
