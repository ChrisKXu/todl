using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Todl.Compiler.CodeAnalysis;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Symbols;

namespace Todl.Compiler.CodeGeneration;

internal sealed partial class Emitter
{
    private readonly Compilation compilation;
    private readonly AssemblyDefinition assemblyDefinition;
    private readonly Dictionary<FunctionSymbol, MethodDefinition> methodReferences = new();
    private readonly Dictionary<LocalVariableSymbol, VariableDefinition> variables = new();

    private BuiltInTypes BuiltInTypes => compilation.ClrTypeCache.BuiltInTypes;

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
            baseType: ResolveTypeReference(BuiltInTypes.Object));

        assemblyDefinition.MainModule.Types.Add(entryPointType);

        var functionMembers = boundEntryPointTypeDefinition.BoundMembers.OfType<BoundFunctionMember>();

        // Emit function reference first
        foreach (var functionMember in functionMembers)
        {
            var methodDefinition = EmitFunctionMemberReference(functionMember);
            entryPointType.Methods.Add(methodDefinition);
            methodReferences[functionMember.FunctionSymbol] = methodDefinition;

            if (functionMember == boundEntryPointTypeDefinition.EntryPointFunctionMember)
            {
                assemblyDefinition.EntryPoint = methodDefinition;
            }
        }

        // Emit function body
        foreach (var functionMember in functionMembers)
        {
            EmitFunctionMember(methodReferences[functionMember.FunctionSymbol], functionMember);
        }
    }

    private MethodDefinition EmitFunctionMemberReference(BoundFunctionMember functionMember)
    {
        var attributes = MethodAttributes.Static;
        attributes |= functionMember.IsPublic ? MethodAttributes.Public : MethodAttributes.Private;

        var methodDefinition = new MethodDefinition(
            name: functionMember.FunctionSymbol.Name,
            attributes: attributes,
            returnType: ResolveTypeReference(functionMember.ReturnType as ClrTypeSymbol));

        foreach (var parameter in functionMember.FunctionSymbol.Parameters)
        {
            methodDefinition.Parameters.Add(new ParameterDefinition(
                name: parameter.Name,
                attributes: ParameterAttributes.None,
                parameterType: ResolveTypeReference(parameter.Type as ClrTypeSymbol)));
        }

        return methodDefinition;
    }

    private void EmitFunctionMember(MethodDefinition methodDefinition, BoundFunctionMember functionMember)
    {
        EmitStatement(methodDefinition.Body, functionMember.Body);
    }

    private TypeReference ResolveTypeReference(ClrTypeSymbol clrTypeSymbol)
    {
        if (clrTypeSymbol.Equals(BuiltInTypes.Void))
        {
            return assemblyDefinition.MainModule.TypeSystem.Void;
        }

        if (clrTypeSymbol.Equals(BuiltInTypes.Int32))
        {
            return assemblyDefinition.MainModule.TypeSystem.Int32;
        }

        if (clrTypeSymbol.Equals(BuiltInTypes.String))
        {
            return assemblyDefinition.MainModule.TypeSystem.String;
        }

        return assemblyDefinition.MainModule.ImportReference(clrTypeSymbol.ClrType);
    }

    private MethodReference ResolveMethodReference(BoundClrFunctionCallExpression boundClrFunctionCallExpression)
    {
        var methodReference = assemblyDefinition.MainModule.ImportReference(boundClrFunctionCallExpression.MethodInfo);
        methodReference.ReturnType = ResolveTypeReference(boundClrFunctionCallExpression.ResultType as ClrTypeSymbol);

        for (var i = 0; i != methodReference.Parameters.Count; ++i)
        {
            methodReference.Parameters[i].ParameterType
                = ResolveTypeReference(boundClrFunctionCallExpression.BoundArguments[i].ResultType as ClrTypeSymbol);
        }

        return methodReference;
    }
}
