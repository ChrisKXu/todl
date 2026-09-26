using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.CodeGeneration;

namespace Todl.Compiler.Tests.CodeGeneration;

internal sealed class TestEmitter : Emitter.InstructionEmitter
{
    public override AssemblyDefinition AssemblyDefinition { get; }
    public override Compilation Compilation { get; }
    public override BoundTodlTypeDefinition BoundTodlTypeDefinition { get; }
    public override TypeDefinition TypeDefinition { get; }
    public override ILProcessor ILProcessor { get; }

    public MethodDefinition MethodDefinition { get; }

    public TestEmitter()
        : base(null)
    {
        var assemblyNameDefinition = new AssemblyNameDefinition("test", new Version(1, 0));
        AssemblyDefinition = AssemblyDefinition.CreateAssembly(assemblyNameDefinition, "default", ModuleKind.Console);
        Compilation = new Compilation("test", new Version(1, 0), Array.Empty<SourceText>(), TestDefaults.DefaultClrTypeCache);

        var attributes = MethodAttributes.Static;
        attributes |= MethodAttributes.Public;

        MethodDefinition = new MethodDefinition(
            name: "test",
            attributes: attributes,
            returnType: AssemblyDefinition.MainModule.TypeSystem.Void);

        ILProcessor = MethodDefinition.Body.GetILProcessor();

        MethodDefinition.Body.SimplifyMacros();
    }

    public void Emit()
    {
        MethodDefinition.Body.OptimizeMacros();
    }
}
