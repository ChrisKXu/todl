using System.Linq;
using Mono.Cecil;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Symbols;

namespace Todl.Compiler.CodeGeneration;

internal partial class Emitter
{
    public TypeDefinition EmitEntryPointType(BoundEntryPointTypeDefinition boundEntryPointTypeDefinition)
    {
        var entryPointType = new TypeDefinition(
            @namespace: Compilation.AssemblyName,
            name: boundEntryPointTypeDefinition.Name,
            attributes: TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.Abstract,
            baseType: ResolveTypeReference(BuiltInTypes.Object));

        var functionMembers = boundEntryPointTypeDefinition.BoundMembers.OfType<BoundFunctionMember>();

        // Emit function reference first
        foreach (var functionMember in functionMembers)
        {
            var methodDefinition = EmitFunctionMemberReference(functionMember);
            entryPointType.Methods.Add(methodDefinition);
            methodReferences[functionMember.FunctionSymbol] = methodDefinition;

            if (functionMember == boundEntryPointTypeDefinition.EntryPointFunctionMember)
            {
                AssemblyDefinition.EntryPoint = methodDefinition;
            }
        }

        // Emit function body
        foreach (var functionMember in functionMembers)
        {
            EmitFunctionMember(methodReferences[functionMember.FunctionSymbol], functionMember);
        }

        return entryPointType;
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
}
