using System.CodeDom.Compiler;
using System.IO;
using Microsoft.CodeAnalysis;

namespace Todl.Compiler.SourceGenerators;

[Generator]
public sealed class BoundNodeFactorySourceGenerator : ISourceGenerator
{
    private const string BoundNodeFactoryClassName = "BoundNodeFactory";

    public void Initialize(GeneratorInitializationContext context)
    {
        // do nothing
    }

    public void Execute(GeneratorExecutionContext context)
    {
        using var stringWriter = new StringWriter();
        using var indentedTextWriter = new IndentedTextWriter(stringWriter);

        indentedTextWriter.WriteLine("namespace Todl.Compiler.CodeAnalysis.Binding;");
        indentedTextWriter.WriteLine();
        indentedTextWriter.WriteLine($"public static class {BoundNodeFactoryClassName}");
        indentedTextWriter.WriteLine("{");
        ++indentedTextWriter.Indent;
        WriteAllMethods(context, indentedTextWriter);
        --indentedTextWriter.Indent;
        indentedTextWriter.WriteLine("}");

        context.AddSource($"{BoundNodeFactoryClassName}_generated.cs", stringWriter.ToString());
    }

    public void WriteAllMethods(GeneratorExecutionContext context, IndentedTextWriter writer)
    {
        var compilation = context.Compilation;
    }
}
