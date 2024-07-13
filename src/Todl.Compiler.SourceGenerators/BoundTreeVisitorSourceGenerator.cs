using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Todl.Compiler.SourceGenerators;

[Generator]
internal sealed class BoundTreeVisitorSourceGenerator : IIncrementalGenerator
{
    private const string BoundTreeVisitorNamespace = "Todl.Compiler.CodeAnalysis.Binding.BoundTree";
    private const string BoundTreeVisitorClassName = "BoundTreeVisitor";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var pipeline = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: $"{BoundTreeVisitorNamespace}.BoundNodeAttribute",
            predicate: static (syntaxNode, _) => syntaxNode is ClassDeclarationSyntax,
            transform: static (context, _) => context.TargetSymbol);

        context.RegisterSourceOutput(pipeline, GenerateBoundTreeVisitorMethods);
    }

    static void GenerateBoundTreeVisitorMethods(SourceProductionContext context, ISymbol symbol)
    {
        try
        {
            var className = symbol.Name;
            var camelCaseClassName = symbol.CamelCasedName();

            var sourceText = SourceText.From($$"""
                namespace {{BoundTreeVisitorNamespace}};

                internal abstract partial class {{BoundTreeVisitorClassName}}
                {
                    [System.CodeDom.Compiler.GeneratedCode("{{nameof(BoundTreeVisitorSourceGenerator)}}", "1.0.0.0")]
                    public virtual BoundNode Visit{{className}}({{className}} {{camelCaseClassName}}) => DefaultVisit({{camelCaseClassName}});
                }
                """, Encoding.UTF8);

            context.AddSource($"{BoundTreeVisitorClassName}.{className}.g.cs", sourceText);
        }
        catch (Exception ex)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    descriptor: new DiagnosticDescriptor(
                        id: "TODL000",
                        title: "Error occurred while generating BoundTreeVisitor",
                        messageFormat: ex.Message + ex.StackTrace,
                        category: typeof(BoundTreeVisitorSourceGenerator).FullName,
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true,
                        description: ex.Message + ex.StackTrace),
                    location: null));
        }
    }
}
