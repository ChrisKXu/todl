using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Todl.Compiler.SourceGenerators;

[Generator]
public sealed class BoundNodeFactorySourceGenerator : IIncrementalGenerator
{
    private const string BoundNodeFactoryClassName = "BoundNodeFactory";
    private const string BoundNodeNamespace = "Todl.Compiler.CodeAnalysis.Binding";
    private const string BoundNodeTypeName = $"{BoundNodeNamespace}.BoundNode";
    private const string BoundNodeFactoryGeneratedSourceFileName = $"{BoundNodeFactoryClassName}_generated.cs";

    private static readonly IReadOnlyList<string> defaultNamespaces = new List<string>()
    {
        "System",
        "System.CodeDom.Compiler",
        "System.Collections.Generic",
        "System.Reflection",
        "Todl.Compiler.CodeAnalysis.Symbols",
        "Todl.Compiler.CodeAnalysis.Syntax",
        "Todl.Compiler.Diagnostics"
    };

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var boundNodeClasses = context.SyntaxProvider.CreateSyntaxProvider(
            static (syntaxNode, _) => syntaxNode is ClassDeclarationSyntax classDeclarationSyntax,
            Transform)
            .Where(m => m is not null);

        context.RegisterSourceOutput(
            context.CompilationProvider.Combine(boundNodeClasses.Collect()),
            static (context, values) => Generate(context, values.Left, values.Right));
    }

    static ClassDeclarationSyntax Transform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var semanticModel = context.SemanticModel;
        var symbolInfo = semanticModel.GetDeclaredSymbol(context.Node) as INamedTypeSymbol;

        if (symbolInfo.Name.Contains("Bound") && !symbolInfo.IsAbstract)
        {
            return context.Node as ClassDeclarationSyntax;
        }

        return null;
    }

    static void Generate(
        SourceProductionContext context,
        Compilation compilation,
        IReadOnlyList<ClassDeclarationSyntax> boundNodeClasses)
    {
        try
        {
            using var stringWriter = new StringWriter();
            using var indentedTextWriter = new IndentedTextWriter(stringWriter);

            // write usings
            foreach (var ns in defaultNamespaces)
            {
                indentedTextWriter.WriteLine($"using {ns};");
            }
            indentedTextWriter.WriteLine();

            indentedTextWriter.WriteLine($"namespace {BoundNodeNamespace};");
            indentedTextWriter.WriteLine();
            indentedTextWriter.WriteLine("[" + nameof(GeneratedCodeAttribute) + "(\"Todl.Compiler.SourceGenerators.BoundNodeFactorySourceGenerator\", \"1.0.0.0\")" + "]");
            indentedTextWriter.WriteLine($"public static class {BoundNodeFactoryClassName}");
            indentedTextWriter.BeginCurlyBrace();
            WriteAllCreateMethods(compilation, boundNodeClasses, indentedTextWriter);
            indentedTextWriter.EndCurlyBrace();

            context.AddSource(BoundNodeFactoryGeneratedSourceFileName, stringWriter.ToString());
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

    static void WriteAllCreateMethods(
        Compilation compilation,
        IReadOnlyList<ClassDeclarationSyntax> boundNodeClasses,
        IndentedTextWriter writer)
    {
        var boundNodeType = compilation.GetTypeByMetadataName(BoundNodeTypeName);

        var allBoundNodeTypes = boundNodeClasses
            .Select(s => compilation.GetSemanticModel(s.SyntaxTree).GetDeclaredSymbol(s))
            .Where(t => t.IsDerivedFrom(boundNodeType));

        foreach (var typeSymbol in allBoundNodeTypes)
        {
            WriteCreateMethod(typeSymbol, boundNodeType, writer);
        }
    }

    static void WriteCreateMethod(INamedTypeSymbol typeSymbol, INamedTypeSymbol boundNodeType, IndentedTextWriter writer)
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

        foreach (var p in properties)
        {
            if (p.Type.IsDerivedFrom(boundNodeType))
            {
                writer.WriteLine($"diagnosticBuilder.Add({p.CamelCasedName()});");
            }
            else if (p.Type is INamedTypeSymbol t
                && t.IsGenericType
                && t.TypeArguments.Any(t => t.IsDerivedFrom(boundNodeType)))
            {
                writer.WriteLine($"diagnosticBuilder.AddRange({p.CamelCasedName()});");
            }
        }

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
