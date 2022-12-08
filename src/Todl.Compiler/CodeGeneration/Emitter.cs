using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Todl.Compiler.CodeAnalysis;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Symbols;

namespace Todl.Compiler.CodeGeneration;

internal partial class Emitter
{
    private readonly Dictionary<FunctionSymbol, MethodDefinition> methodReferences = new();
    private readonly Dictionary<LocalVariableSymbol, VariableDefinition> variables = new();

    private BuiltInTypes BuiltInTypes => Compilation.ClrTypeCache.BuiltInTypes;

    public Emitter Parent { get; private init; }

    public virtual Compilation Compilation
        => Parent?.Compilation;

    public virtual AssemblyDefinition AssemblyDefinition
        => Parent?.AssemblyDefinition;

    public AssemblyDefinition Emit()
    {
        var entryPointType = EmitEntryPointType(Compilation.MainModule.EntryPointType);

        AssemblyDefinition.MainModule.Types.Add(entryPointType);

        return AssemblyDefinition;
    }

    private TypeReference ResolveTypeReference(ClrTypeSymbol clrTypeSymbol)
    {
        if (clrTypeSymbol.Equals(BuiltInTypes.Void))
        {
            return AssemblyDefinition.MainModule.TypeSystem.Void;
        }

        if (clrTypeSymbol.Equals(BuiltInTypes.Int32))
        {
            return AssemblyDefinition.MainModule.TypeSystem.Int32;
        }

        if (clrTypeSymbol.Equals(BuiltInTypes.String))
        {
            return AssemblyDefinition.MainModule.TypeSystem.String;
        }

        return AssemblyDefinition.MainModule.ImportReference(clrTypeSymbol.ClrType);
    }

    private MethodReference ResolveMethodReference(BoundClrFunctionCallExpression boundClrFunctionCallExpression)
    {
        var methodReference = AssemblyDefinition.MainModule.ImportReference(boundClrFunctionCallExpression.MethodInfo);
        methodReference.ReturnType = ResolveTypeReference(boundClrFunctionCallExpression.ResultType as ClrTypeSymbol);

        for (var i = 0; i != methodReference.Parameters.Count; ++i)
        {
            methodReference.Parameters[i].ParameterType
                = ResolveTypeReference(boundClrFunctionCallExpression.BoundArguments[i].ResultType as ClrTypeSymbol);
        }

        return methodReference;
    }

    public static AssemblyEmitter CreateAssemblyEmitter(Compilation compilation)
        => new(compilation);

    internal sealed class AssemblyEmitter : Emitter
    {
        private readonly Compilation compilation;
        private readonly AssemblyDefinition assemblyDefinition;

        internal AssemblyEmitter(Compilation compilation)
        {
            this.compilation = compilation;

            var assemblyName = new AssemblyNameDefinition(compilation.AssemblyName, compilation.Version);
            assemblyDefinition = AssemblyDefinition.CreateAssembly(assemblyName, compilation.AssemblyName, ModuleKind.Console);
        }

        public override Compilation Compilation => compilation;
        public override AssemblyDefinition AssemblyDefinition => assemblyDefinition;
    }
}
