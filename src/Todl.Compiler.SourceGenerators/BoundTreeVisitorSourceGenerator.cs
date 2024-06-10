using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Todl.Compiler.SourceGenerators;

[Generator]
internal class BoundTreeVisitorSourceGenerator : IIncrementalGenerator
{
    private const string BoundNodeFactoryClassName = "BoundTreeVisitor";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var boundNodeClasses = context.SyntaxProvider.CreateSyntaxProvider(
            static (syntaxNode, _) => syntaxNode is ClassDeclarationSyntax classDeclarationSyntax,
            Transform)
            .Where(m => m is not null);
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
}
