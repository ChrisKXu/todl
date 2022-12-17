using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Todl.Compiler.CodeAnalysis;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Symbols;

namespace Todl.Compiler.CodeGeneration;

internal partial class Emitter
{
    private readonly Dictionary<LocalVariableSymbol, VariableDefinition> variables = new();

    private BuiltInTypes BuiltInTypes => Compilation.ClrTypeCache.BuiltInTypes;

    public Emitter Parent { get; protected init; }

    public virtual Compilation Compilation
        => Parent?.Compilation;

    public virtual AssemblyDefinition AssemblyDefinition
        => Parent?.AssemblyDefinition;

    public virtual BoundTodlTypeDefinition BoundTodlTypeDefinition
        => Parent?.BoundTodlTypeDefinition;

    public virtual TypeDefinition TypeDefinition
        => Parent?.TypeDefinition;

    public virtual BoundFunctionMember BoundFunctionMember
        => Parent?.BoundFunctionMember;

    public virtual MethodDefinition MethodDefinition
        => Parent?.MethodDefinition;

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

    protected virtual MethodReference ResolveMethodReference(BoundTodlFunctionCallExpression boundTodlFunctionCallExpression)
    {
        return Parent.ResolveMethodReference(boundTodlFunctionCallExpression);
    }

    public static AssemblyEmitter CreateAssemblyEmitter(Compilation compilation)
        => new(compilation);

    public TypeEmitter CreateTypeEmitter(BoundTodlTypeDefinition boundTodlTypeDefinition)
        => new(this, boundTodlTypeDefinition);

    public FunctionEmitter CreateFunctionEmitter(BoundFunctionMember boundFunctionMember)
        => new(this, boundFunctionMember);

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

        public AssemblyDefinition Emit()
        {
            var typeEmitter = CreateTypeEmitter(Compilation.MainModule.EntryPointType);
            var entryPointType = typeEmitter.Emit();
            AssemblyDefinition.MainModule.Types.Add(entryPointType);
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
                baseType: ResolveTypeReference(BuiltInTypes.Object));
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

        protected override MethodReference ResolveMethodReference(BoundTodlFunctionCallExpression boundTodlFunctionCallExpression)
            => methodReferences[boundTodlFunctionCallExpression.FunctionSymbol];
    }

    internal sealed partial class FunctionEmitter : Emitter
    {
        private readonly BoundFunctionMember boundFunctionMember;
        private readonly MethodDefinition methodDefinition;

        internal FunctionEmitter(Emitter parent, BoundFunctionMember boundFunctionMember)
        {
            Parent = parent;
            this.boundFunctionMember = boundFunctionMember;

            var attributes = MethodAttributes.Static;
            attributes |= boundFunctionMember.IsPublic ? MethodAttributes.Public : MethodAttributes.Private;

            methodDefinition = new MethodDefinition(
                name: boundFunctionMember.FunctionSymbol.Name,
                attributes: attributes,
                returnType: ResolveTypeReference(boundFunctionMember.ReturnType as ClrTypeSymbol));

            foreach (var parameter in BoundFunctionMember.FunctionSymbol.Parameters)
            {
                methodDefinition.Parameters.Add(new(
                    name: parameter.Name,
                    attributes: ParameterAttributes.None,
                    parameterType: ResolveTypeReference(parameter.Type as ClrTypeSymbol)));
            }
        }

        public override BoundFunctionMember BoundFunctionMember => boundFunctionMember;
        public override MethodDefinition MethodDefinition => methodDefinition;

        public MethodDefinition Emit()
        {
            EmitStatement(MethodDefinition.Body, BoundFunctionMember.Body);
            return MethodDefinition;
        }
    }
}
