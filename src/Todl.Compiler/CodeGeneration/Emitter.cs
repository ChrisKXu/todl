using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Todl.Compiler.CodeAnalysis;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Symbols;

namespace Todl.Compiler.CodeGeneration;

internal sealed class Emitter
{
    private readonly Compilation compilation;
    private readonly AssemblyDefinition assemblyDefinition;

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
            baseType: ResolveClrType(BuiltInTypes.Object));

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
        var attributes = MethodAttributes.Static;
        attributes |= functionMember.IsPublic ? MethodAttributes.Public : MethodAttributes.Private;

        var methodDefinition = new MethodDefinition(
            name: functionMember.FunctionSymbol.Name,
            attributes: attributes,
            returnType: ResolveClrType(functionMember.ReturnType as ClrTypeSymbol));

        foreach (var statement in functionMember.Body.Statements)
        {
            EmitStatement(methodDefinition.Body, statement);
        }

        return methodDefinition;
    }

    private void EmitStatement(MethodBody methodBody, BoundStatement boundStatement)
    {
        switch (boundStatement)
        {
            case BoundReturnStatement boundReturnStatement:
                EmitReturnStatement(methodBody, boundReturnStatement);
                return;
            default:
                return;
        }
    }

    private void EmitReturnStatement(MethodBody methodBody, BoundReturnStatement boundReturnStatement)
    {
        if (boundReturnStatement.BoundReturnValueExpression is not null)
        {
            EmitExpression(methodBody, boundReturnStatement.BoundReturnValueExpression);
        }

        methodBody.GetILProcessor().Emit(OpCodes.Ret);
    }

    private void EmitExpression(MethodBody methodBody, BoundExpression boundExpression)
    {
        switch (boundExpression)
        {
            case BoundConstant boundConstant:
                EmitConstant(methodBody, boundConstant);
                return;
            default:
                return;
        }
    }

    private void EmitConstant(MethodBody methodBody, BoundConstant boundConstant)
    {
        if (boundConstant.ResultType.Equals(BuiltInTypes.Int32))
        {
            methodBody.GetILProcessor().Emit(OpCodes.Ldc_I4, (int)boundConstant.Value);
        }
        else if (boundConstant.ResultType.Equals(BuiltInTypes.String))
        {
            methodBody.GetILProcessor().Emit(OpCodes.Ldstr, (string)boundConstant.Value);
        }
    }

    private TypeReference ResolveClrType(ClrTypeSymbol clrTypeSymbol)
    {
        if (clrTypeSymbol.Equals(BuiltInTypes.Void))
        {
            return assemblyDefinition.MainModule.TypeSystem.Void;
        }
        else if (clrTypeSymbol.Equals(BuiltInTypes.Int32))
        {
            return assemblyDefinition.MainModule.TypeSystem.Int32;
        }

        return assemblyDefinition.MainModule.ImportReference(clrTypeSymbol.ClrType);
    }
}
