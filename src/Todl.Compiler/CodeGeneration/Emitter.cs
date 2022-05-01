using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Symbols;

namespace Todl.Compiler.CodeGeneration;

internal class Emitter
{
    private readonly Compilation compilation;
    private readonly AssemblyDefinition assemblyDefinition;

    internal Emitter(Compilation compilation)
    {
        this.compilation = compilation;

        var assemblyName = new AssemblyNameDefinition(compilation.AssemblyName, compilation.Version);
        assemblyDefinition = AssemblyDefinition.CreateAssembly(assemblyName, compilation.AssemblyName, ModuleKind.Console);
    }

    public AssemblyDefinition Emit()
    {
        EmitEntryPointType(compilation.MainModule.EntryPointType);

        return assemblyDefinition;
    }

    public void EmitEntryPointType(BoundEntryPointTypeDefinition boundEntryPointTypeDefinition)
    {
        var entryPointType = new TypeDefinition(
            @namespace: compilation.AssemblyName,
            name: boundEntryPointTypeDefinition.Name,
            attributes: TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.Abstract,
            baseType: ResolveClrType(compilation.ClrTypeCache.BuiltInTypes.Object));

        assemblyDefinition.MainModule.Types.Add(entryPointType);

        foreach (var functionMember in boundEntryPointTypeDefinition.BoundMembers.OfType<BoundFunctionMember>())
        {
            var methodDefinition = EmitFunctionMember(functionMember);
            entryPointType.Methods.Add(methodDefinition);

            if (functionMember == boundEntryPointTypeDefinition.EntryPointFunctionMember)
            {
                assemblyDefinition.EntryPoint = methodDefinition;
            }
        }
    }

    public MethodDefinition EmitFunctionMember(BoundFunctionMember functionMember)
    {
        var methodDefinition = new MethodDefinition(
            name: functionMember.FunctionSymbol.Name,
            attributes: MethodAttributes.Static | MethodAttributes.Private,
            returnType: assemblyDefinition.MainModule.TypeSystem.Void);

        methodDefinition.Body.GetILProcessor().Emit(OpCodes.Nop);
        methodDefinition.Body.GetILProcessor().Emit(OpCodes.Ret);

        return methodDefinition;
    }

    private TypeReference ResolveClrType(ClrTypeSymbol clrTypeSymbol)
    {
        return assemblyDefinition.MainModule.ImportReference(clrTypeSymbol.ClrType);
    }
}
