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
    private readonly TypeDefinition entryPointType;

    internal Emitter(Compilation compilation)
    {
        this.compilation = compilation;

        var assemblyName = new AssemblyNameDefinition(compilation.AssemblyName, compilation.Version);
        assemblyDefinition = AssemblyDefinition.CreateAssembly(assemblyName, compilation.AssemblyName, ModuleKind.Console);

        entryPointType = new TypeDefinition(
            @namespace: compilation.AssemblyName,
            name: "_Todl_Generated_EntryPoint",
            attributes: TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.Abstract,
            baseType: ResolveClrType(compilation.ClrTypeCache.BuiltInTypes.Object));

        assemblyDefinition.MainModule.Types.Add(entryPointType);
    }

    public AssemblyDefinition Emit()
    {
        EmitModule(compilation.MainModule);

        return assemblyDefinition;
    }

    public void EmitModule(BoundModule module)
    {
        foreach (var functionMember in module.BoundMembers.OfType<BoundFunctionMember>())
        {
            var methodDefinition = EmitFunctionMember(functionMember);
            if (functionMember == module.EntryPoint)
            {
                assemblyDefinition.EntryPoint = methodDefinition;
            }
        }
    }

    public MethodDefinition EmitFunctionMember(BoundFunctionMember functionMember)
    {
        var methodDefinition = new MethodDefinition(
            name: functionMember.FunctionSymbol.Name,
            attributes: MethodAttributes.Static | MethodAttributes.Public,
            returnType: ResolveClrType(functionMember.ReturnType as ClrTypeSymbol));

        methodDefinition.Body.GetILProcessor().Emit(OpCodes.Nop);
        methodDefinition.Body.GetILProcessor().Emit(OpCodes.Ret);
        entryPointType.Methods.Add(methodDefinition);

        return methodDefinition;
    }

    private TypeReference ResolveClrType(ClrTypeSymbol clrTypeSymbol)
    {
        return assemblyDefinition.MainModule.ImportReference(clrTypeSymbol.ClrType);
    }
}
