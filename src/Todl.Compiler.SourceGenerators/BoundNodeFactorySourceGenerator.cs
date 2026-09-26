using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Todl.Compiler.SourceGenerators;

[Generator]
internal sealed class BoundNodeFactorySourceGenerator : IIncrementalGenerator
{
    private const string BoundNodeFactoryNamespace = "Todl.Compiler.CodeAnalysis.Binding.BoundTree";
    private const string BoundNodeFactoryClassName = "BoundNodeFactory";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var pipeline = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: $"{BoundNodeFactoryNamespace}.BoundNodeAttribute",
            predicate: static (syntaxNode, _) => syntaxNode is ClassDeclarationSyntax,
            transform: static (context, _) => new BoundNodeMetadata(context));

        context.RegisterSourceOutput(pipeline, GenerateBoundNodeFactoryMethods);
    }

    static void GenerateBoundNodeFactoryMethods(SourceProductionContext context, BoundNodeMetadata boundNodeMetadata)
    {
        try
        {
            var className = boundNodeMetadata.ClassName;

            var sourceText = SourceText.From($$"""
                using System;
                using System.CodeDom.Compiler;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                using System.Reflection;
                using Todl.Compiler.CodeAnalysis.Symbols;
                using Todl.Compiler.CodeAnalysis.Syntax;
                using Todl.Compiler.Diagnostics;

                namespace {{BoundNodeFactoryNamespace}};

                internal sealed partial class {{BoundNodeFactoryClassName}}
                {
                    [GeneratedCode("{{nameof(BoundNodeFactorySourceGenerator)}}", "1.0.0.0")]
                    internal static {{className}} Create{{className}}(
                        {{boundNodeMetadata.WriteParameters()}})
                    {
                        return new {{className}}()
                        {
                            {{boundNodeMetadata.WriteInitializers()}}
                        };
                    }
                }
                """, Encoding.UTF8);

            context.AddSource($"{BoundNodeFactoryClassName}.{boundNodeMetadata.ClassName}.g.cs", sourceText);
        }
        catch (Exception ex)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    descriptor: new DiagnosticDescriptor(
                        id: "TODL000",
                        title: "Error occurred while generating BoundNodeFactory",
                        messageFormat: ex.Message + ex.StackTrace,
                        category: typeof(BoundNodeFactorySourceGenerator).FullName,
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true,
                        description: ex.ToString()),
                    location: null));
        }
    }

    private sealed class BoundNodeMetadata
    {
        private const string BoundNodeTypeName = $"{BoundNodeFactoryNamespace}.BoundNode";

        private readonly GeneratorAttributeSyntaxContext context;

        public string ClassName => context.TargetSymbol.Name;

        public IEnumerable<(string Name, IPropertySymbol Property, bool IsOwn)> Properties { get; }

        public BoundNodeMetadata(GeneratorAttributeSyntaxContext context)
        {
            this.context = context;

            var boundNodeClass = context.TargetSymbol as INamedTypeSymbol;
            var boundNodeType = context.SemanticModel.Compilation.GetTypeByMetadataName(BoundNodeTypeName);

            // Walk own type then base types (stopping before BoundNode); own properties are required, inherited ones default to unset.
            var seenNames = new HashSet<string>();
            var properties = new List<(string, IPropertySymbol, bool)>();
            var isOwn = true;

            for (var type = boundNodeClass; type is not null && !SymbolEqualityComparer.Default.Equals(type, boundNodeType); type = type.BaseType)
            {
                foreach (var property in type.GetMembers().OfType<IPropertySymbol>().Where(p => !p.IsReadOnly))
                {
                    if (seenNames.Add(property.Name))
                    {
                        properties.Add((property.GetPropertyTypeName(), property, isOwn));
                    }
                }

                isOwn = false;
            }

            Properties = properties;
        }

        public string WriteParameters()
        {
            var properties = Properties
                .Select(p => p.IsOwn
                    ? $"{p.Name} {p.Property.CamelCasedName()}"
                    : $"{p.Name} {p.Property.CamelCasedName()} = default")
                .ToList();
            properties.Insert(0, "SyntaxNode syntaxNode");
            return string.Join(",\n", properties);
        }

        public string WriteInitializers()
        {
            var properties = Properties.Select(p => $"{p.Property.Name} = {p.Property.CamelCasedName()}").ToList();
            properties.Insert(0, "SyntaxNode = syntaxNode");
            return string.Join(",\n", properties);
        }
    }
}
