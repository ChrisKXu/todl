using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Todl.Compiler.CodeAnalysis.Symbols;

namespace Todl.Compiler.CodeGeneration;

internal partial class Emitter
{
    public Emitter Parent { get; protected init; }

    public virtual Compilation Compilation
        => Parent?.Compilation;

    public virtual AssemblyDefinition AssemblyDefinition
        => Parent?.AssemblyDefinition;

    public virtual BoundTodlTypeDefinition BoundTodlTypeDefinition
        => Parent?.BoundTodlTypeDefinition;

    public virtual TypeDefinition TypeDefinition
        => Parent?.TypeDefinition;

    public virtual IDictionary<VariableSymbol, VariableDefinition> Variables
        => Parent?.Variables;

    public virtual IDictionary<ParameterSymbol, ParameterDefinition> Parameters
        => Parent?.Parameters;

    private TypeReference ResolveTypeReference(ClrTypeSymbol clrTypeSymbol)
    {
        var typeSystem = AssemblyDefinition.MainModule.TypeSystem;

        return clrTypeSymbol.SpecialType switch
        {
            SpecialType.ClrVoid => typeSystem.Void,
            SpecialType.ClrBoolean => typeSystem.Boolean,
            SpecialType.ClrByte => typeSystem.Byte,
            SpecialType.ClrObject => typeSystem.Object,
            SpecialType.ClrChar => typeSystem.Char,
            SpecialType.ClrString => typeSystem.String,
            SpecialType.ClrInt32 => typeSystem.Int32,
            SpecialType.ClrUInt32 => typeSystem.UInt32,
            SpecialType.ClrInt64 => typeSystem.Int64,
            SpecialType.ClrUInt64 => typeSystem.UInt64,
            SpecialType.ClrFloat => typeSystem.Single,
            SpecialType.ClrDouble => typeSystem.Double,
            _ => ResolveComplexTypeReference(clrTypeSymbol.ClrType)
        };
    }

    // MetadataLoadContext-sourced primitives import as CLASS<TypeRef> instead of intrinsic
    // element types, which the runtime rejects; route composite constituents back through
    // ResolveTypeReference so primitives hit the SpecialType fast path.
    private TypeReference ResolveComplexTypeReference(Type clrType)
    {
        if (clrType.IsArray)
        {
            var elementType = ResolveTypeReference(Compilation.ClrTypeCache.Resolve(clrType.GetElementType()));
            return clrType.IsSZArray ? new ArrayType(elementType) : new ArrayType(elementType, clrType.GetArrayRank());
        }

        if (clrType.IsConstructedGenericType)
        {
            var openTypeReference = AssemblyDefinition.MainModule.ImportReference(clrType.GetGenericTypeDefinition());
            var genericInstanceType = new GenericInstanceType(openTypeReference);

            foreach (var typeArgument in clrType.GetGenericArguments())
            {
                genericInstanceType.GenericArguments.Add(ResolveTypeReference(Compilation.ClrTypeCache.Resolve(typeArgument)));
            }

            return genericInstanceType;
        }

        return AssemblyDefinition.MainModule.ImportReference(clrType);
    }

    private MethodReference ResolveMethodReference(BoundClrFunctionCallExpression boundClrFunctionCallExpression)
    {
        var methodInfo = boundClrFunctionCallExpression.MethodInfo;
        var methodReference = AssemblyDefinition.MainModule.ImportReference(methodInfo);
        methodReference.ReturnType = ResolveTypeReference(boundClrFunctionCallExpression.ResultType as ClrTypeSymbol);

        var parameters = methodInfo.GetParameters();
        for (var i = 0; i != methodReference.Parameters.Count; ++i)
        {
            methodReference.Parameters[i].ParameterType
                = ResolveTypeReference(Compilation.ClrTypeCache.Resolve(parameters[i].ParameterType));
        }

        return methodReference;
    }

    private MethodReference ResolveMethodReference(BoundObjectCreationExpression boundObjectCreationExpression)
    {
        var constructorInfo = boundObjectCreationExpression.ConstructorInfo;
        var methodReference = AssemblyDefinition.MainModule.ImportReference(constructorInfo);

        var parameters = constructorInfo.GetParameters();
        for (var i = 0; i != methodReference.Parameters.Count; ++i)
        {
            methodReference.Parameters[i].ParameterType
                = ResolveTypeReference(Compilation.ClrTypeCache.Resolve(parameters[i].ParameterType));
        }

        return methodReference;
    }

    protected virtual MethodReference ResolveMethodReference(BoundTodlFunctionCallExpression boundTodlFunctionCallExpression)
    {
        return Parent.ResolveMethodReference(boundTodlFunctionCallExpression);
    }

    public static AssemblyEmitter CreateAssemblyEmitter(Compilation compilation)
        => new(compilation);

    public TypeEmitter CreateTypeEmitter(BoundTodlTypeDefinition boundTodlTypeDefinition)
        => new(this, boundTodlTypeDefinition);

    internal sealed class AssemblyEmitter : Emitter
    {
        private readonly Compilation compilation;
        private readonly AssemblyDefinition assemblyDefinition;

        internal AssemblyEmitter(Compilation compilation)
        {
            this.compilation = compilation;

            var assemblyName = new AssemblyNameDefinition(compilation.AssemblyName, compilation.Version);
            var moduleParameters = new ModuleParameters
            {
                Kind = ModuleKind.Console,
                AssemblyResolver = CreateAssemblyResolver(compilation)
            };
            assemblyDefinition = AssemblyDefinition.CreateAssembly(assemblyName, compilation.AssemblyName, moduleParameters);
        }

        private static DefaultAssemblyResolver CreateAssemblyResolver(Compilation compilation)
        {
            var resolver = new DefaultAssemblyResolver();
            var directories = compilation.ClrTypeCache.Assemblies
                .Select(assembly => Path.GetDirectoryName(assembly.Location))
                .Where(directory => !string.IsNullOrEmpty(directory))
                .Distinct();

            foreach (var directory in directories)
            {
                resolver.AddSearchDirectory(directory);
            }

            return resolver;
        }

        public override Compilation Compilation => compilation;
        public override AssemblyDefinition AssemblyDefinition => assemblyDefinition;

        public AssemblyDefinition Emit()
        {
            foreach (var boundTodlTypeDefinition in Compilation.MainModule.Types)
            {
                var typeEmitter = CreateTypeEmitter(boundTodlTypeDefinition);
                var typeDefinition = typeEmitter.Emit();
                AssemblyDefinition.MainModule.Types.Add(typeDefinition);
            }

            return AssemblyDefinition;
        }
    }

    internal sealed class TypeEmitter : Emitter
    {
        private readonly Dictionary<FunctionSymbol, MethodDefinition> methodReferences = new();
        private readonly BoundTodlTypeDefinition boundTodlTypeDefinition;
        private readonly TypeDefinition typeDefinition;

        internal TypeEmitter(Emitter parent, BoundTodlTypeDefinition boundTodlTypeDefinition)
        {
            Parent = parent;
            this.boundTodlTypeDefinition = boundTodlTypeDefinition;

            typeDefinition = new TypeDefinition(
                @namespace: Compilation.AssemblyName,
                name: boundTodlTypeDefinition.Name,
                attributes: TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.Abstract,
                baseType: AssemblyDefinition.MainModule.TypeSystem.Object);
        }

        public override BoundTodlTypeDefinition BoundTodlTypeDefinition => boundTodlTypeDefinition;
        public override TypeDefinition TypeDefinition => typeDefinition;

        public TypeDefinition Emit()
        {
            var functionMembers = BoundTodlTypeDefinition.BoundMembers.OfType<BoundFunctionMember>();
            var functionEmitters = new List<FunctionEmitter>();

            // Emit function reference first
            foreach (var functionMember in functionMembers)
            {
                var functionEmitter = CreateFunctionEmitter(functionMember);
                var methodDefinition = functionEmitter.MethodDefinition;
                functionEmitters.Add(functionEmitter);
                TypeDefinition.Methods.Add(methodDefinition);
                methodReferences[functionMember.FunctionSymbol] = methodDefinition;

                if (BoundTodlTypeDefinition is BoundEntryPointTypeDefinition entryPointType
                    && functionMember == entryPointType.EntryPointFunctionMember)
                {
                    AssemblyDefinition.EntryPoint = methodDefinition;
                }
            }

            // Emit function body
            functionEmitters.ForEach(e => e.Emit());

            return TypeDefinition;
        }

        public FunctionEmitter CreateFunctionEmitter(BoundFunctionMember boundFunctionMember)
            => new(this, boundFunctionMember);

        protected override MethodReference ResolveMethodReference(BoundTodlFunctionCallExpression boundTodlFunctionCallExpression)
            => methodReferences[boundTodlFunctionCallExpression.FunctionSymbol];
    }

    internal abstract partial class InstructionEmitter : Emitter
    {
        public abstract ILProcessor ILProcessor { get; }

        public override IDictionary<VariableSymbol, VariableDefinition> Variables { get; }
            = new Dictionary<VariableSymbol, VariableDefinition>();

        internal InstructionEmitter(Emitter parent)
        {
            Parent = parent;
        }
    }

    internal sealed partial class FunctionEmitter : InstructionEmitter
    {
        private readonly BoundFunctionMember boundFunctionMember;
        private readonly MethodDefinition methodDefinition;
        private readonly ILProcessor ilProcessor;

        internal FunctionEmitter(Emitter parent, BoundFunctionMember boundFunctionMember)
            : base(parent)
        {
            Parent = parent;
            this.boundFunctionMember = boundFunctionMember;

            var attributes = MethodAttributes.Static;
            attributes |= boundFunctionMember.IsPublic ? MethodAttributes.Public : MethodAttributes.Private;

            methodDefinition = new MethodDefinition(
                name: boundFunctionMember.FunctionSymbol.Name,
                attributes: attributes,
                returnType: ResolveTypeReference(boundFunctionMember.ReturnType as ClrTypeSymbol));

            foreach (var parameter in boundFunctionMember.FunctionSymbol.Parameters)
            {
                var paremeterDefinition = new ParameterDefinition(
                    name: parameter.Name,
                    attributes: ParameterAttributes.None,
                    parameterType: ResolveTypeReference(parameter.Type as ClrTypeSymbol));

                methodDefinition.Parameters.Add(paremeterDefinition);
                Parameters[parameter] = paremeterDefinition;
            }

            ilProcessor = methodDefinition.Body.GetILProcessor();
        }

        public MethodDefinition MethodDefinition => methodDefinition;
        public override ILProcessor ILProcessor => ilProcessor;
        public override IDictionary<ParameterSymbol, ParameterDefinition> Parameters { get; }
            = new Dictionary<ParameterSymbol, ParameterDefinition>();

        public MethodDefinition Emit()
        {
            EmitStatement(boundFunctionMember.Body);
            return methodDefinition;
        }
    }
}
