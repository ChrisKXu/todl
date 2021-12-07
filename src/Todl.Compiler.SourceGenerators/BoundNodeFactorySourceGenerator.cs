using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Todl.Compiler.SourceGenerators;

[Generator]
public sealed class BoundNodeFactorySourceGenerator : ISourceGenerator
{
    private const string BoundNodeFactoryClassName = "BoundNodeFactory";
    private const string BoundNodeNamespace = "Todl.Compiler.CodeAnalysis.Binding";
    private const string BoundNodeTypeName = $"{BoundNodeNamespace}.BoundNode";

    public void Initialize(GeneratorInitializationContext context)
    {
        // do nothing
    }

    public void Execute(GeneratorExecutionContext context)
    {
        try
        {
            using var stringWriter = new StringWriter();
            using var indentedTextWriter = new IndentedTextWriter(stringWriter);

            // write usings
            var usings = new[]
            {
                "System",
                "System.Collections.Generic",
                "System.Reflection",
                "Todl.Compiler.CodeAnalysis.Symbols",
                "Todl.Compiler.CodeAnalysis.Syntax",
                "Todl.Compiler.Diagnostics"
            };

            foreach (var u in usings)
            {
                indentedTextWriter.WriteLine($"using {u};");
            }
            indentedTextWriter.WriteLine();

            indentedTextWriter.WriteLine($"namespace {BoundNodeNamespace};");
            indentedTextWriter.WriteLine();
            indentedTextWriter.WriteLine($"public static class {BoundNodeFactoryClassName}");
            indentedTextWriter.BeginCurlyBrace();
            WriteAllCreateMethods(context, indentedTextWriter);
            indentedTextWriter.EndCurlyBrace();

            context.AddSource($"{BoundNodeFactoryClassName}_generated.cs", stringWriter.ToString());
        }
        catch (Exception ex)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    descriptor: new DiagnosticDescriptor(
                        id: "TODL000",
                        title: "ABCD",
                        messageFormat: ex.Message + ex.StackTrace,
                        category: "Todl.Compiler.SourceGenerators",
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true,
                        description: ex.ToString()),
                    location: null));
        }
    }

    private void WriteAllCreateMethods(GeneratorExecutionContext context, IndentedTextWriter writer)
    {
        var compilation = context.Compilation;
        var boundNodeType = compilation.GetTypeByMetadataName(BoundNodeTypeName);
        var boundNodeNamespace = boundNodeType.ContainingNamespace;
        var allBoundNodeTypes = boundNodeNamespace
            .GetTypeMembers()
            .Where(t => !t.IsAbstract && t.IsDerivedFrom(boundNodeType));

        foreach (var typeSymbol in allBoundNodeTypes)
        {
            WriteCreateMethod(typeSymbol, writer);
        }
    }

    private void WriteCreateMethod(INamedTypeSymbol typeSymbol, IndentedTextWriter writer)
    {
        var properties = typeSymbol
            .GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => !p.IsReadOnly);

        writer.WriteLine($"public static {typeSymbol.Name} Create{typeSymbol.Name}(");
        ++writer.Indent;
        writer.WriteLine("SyntaxNode syntaxNode,");

        foreach (var p in properties)
        {
            writer.WriteLine($"{p.GetPropertyTypeName()} {p.CamelCasedName()},");
        }

        writer.WriteLine("DiagnosticBag.Builder diagnosticBuilder = null)");
        --writer.Indent;
        writer.BeginCurlyBrace();
        writer.WriteLine("diagnosticBuilder ??= new();");
        writer.WriteLine();
        writer.WriteLine("return new()");
        writer.BeginCurlyBrace();
        writer.WriteLine("SyntaxNode = syntaxNode,");

        foreach (var p in properties)
        {
            writer.WriteLine($"{p.Name} = {p.CamelCasedName()},");
        }

        writer.WriteLine("DiagnosticBuilder = diagnosticBuilder");
        writer.EndCurlyBrace(appendEndingSemilcolon: true);
        writer.EndCurlyBrace();
        writer.WriteLine();
    }
}
