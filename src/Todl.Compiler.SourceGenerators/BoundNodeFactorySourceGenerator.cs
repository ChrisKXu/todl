using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Todl.Compiler.SourceGenerators;

[Generator]
public sealed class BoundNodeFactorySourceGenerator : IIncrementalGenerator
{
    private const string BoundNodeFactoryClassName = "BoundNodeFactory";
    private const string BoundNodeNamespace = "Todl.Compiler.CodeAnalysis.Binding";
    private const string BoundNodeTypeName = $"{BoundNodeNamespace}.BoundNode";
    private const string BoundNodeFactoryGeneratedSourceFileName = $"{BoundNodeFactoryClassName}_generated.cs";

    private static readonly SyntaxList<UsingDirectiveSyntax> defaultNamespaces = List(new List<string>()
    {
        "System",
        "System.CodeDom.Compiler",
        "System.Collections.Generic",
        "System.Reflection",
        "Todl.Compiler.CodeAnalysis.Symbols",
        "Todl.Compiler.CodeAnalysis.Syntax",
        "Todl.Compiler.Diagnostics"
    }.Select(n => UsingDirective(ParseName(n))));

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

            var compilationUnit = CompilationUnit()
                .WithUsings(defaultNamespaces)
                .WithMembers(List(
                    new MemberDeclarationSyntax[]
                    {
                        FileScopedNamespaceDeclaration(ParseName(BoundNodeNamespace)),
                        WriteBoundNodeFactory(compilation, boundNodeClasses)
                    }))
                .NormalizeWhitespace();

            context.AddSource(BoundNodeFactoryGeneratedSourceFileName, compilationUnit.GetText(Encoding.UTF8));
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

    static ClassDeclarationSyntax WriteBoundNodeFactory(Compilation compilation, IReadOnlyList<ClassDeclarationSyntax> boundNodeClasses)
    {
        var attributes = AttributeList(
            SingletonSeparatedList(
                Attribute(ParseName(nameof(GeneratedCodeAttribute)))
                    .WithArgumentList(
                        AttributeArgumentList(
                            SeparatedList<AttributeArgumentSyntax>(
                                new SyntaxNodeOrToken[]
                                {
                                    AttributeArgument(
                                        LiteralExpression(
                                            SyntaxKind.StringLiteralExpression,
                                            Literal(typeof(BoundNodeFactorySourceGenerator).FullName))),
                                    Token(SyntaxKind.CommaToken),
                                    AttributeArgument(
                                        LiteralExpression(
                                            SyntaxKind.StringLiteralExpression,
                                            Literal("1.0.0.0")))
                                })))));

        var boundNodeType = compilation.GetTypeByMetadataName(BoundNodeTypeName);
        var members = boundNodeClasses
            .Select(s => compilation.GetSemanticModel(s.SyntaxTree).GetDeclaredSymbol(s))
            .Where(t => t.IsDerivedFrom(boundNodeType))
            .Select(t => WriteCreateMethod(t, boundNodeType));

        return ClassDeclaration(BoundNodeFactoryClassName)
            .WithMembers(List(members))
            .WithAttributeLists(SingletonList(attributes))
            .WithModifiers(
                TokenList(
                    new[] { Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword) }));
    }

    static MemberDeclarationSyntax WriteCreateMethod(INamedTypeSymbol returnType, INamedTypeSymbol boundNodeType)
    {
        var properties = returnType
            .GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => !p.IsReadOnly);

        var parameters = new List<ParameterSyntax>()
        {
            Parameter(Identifier("syntaxNode")).WithType(ParseTypeName("SyntaxNode"))
        };

        parameters.AddRange(properties.Select(p =>
            Parameter(Identifier(p.CamelCasedName()))
                .WithType(ParseTypeName(p.GetPropertyTypeName()))));

        parameters.Add(Parameter(Identifier("diagnosticBuilder"))
            .WithType(ParseTypeName("DiagnosticBag.Builder"))
            .WithDefault(EqualsValueClause(
                LiteralExpression(SyntaxKind.NullLiteralExpression))));

        var statements = new List<StatementSyntax>()
        {
            // equivalent to "diagnosticBuilder ??= new();"
            ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.CoalesceAssignmentExpression,
                    IdentifierName("diagnosticBuilder"),
                    ImplicitObjectCreationExpression()))
        };

        statements.AddRange(
            properties
                .Where(p => p.Type.IsDerivedFrom(boundNodeType))
                .Select(p => ExpressionStatement(
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("diagnosticBuilder"),
                            IdentifierName("Add")))
                    .WithArgumentList(ArgumentList(SingletonSeparatedList(
                        Argument(IdentifierName(p.CamelCasedName()))))))));

        statements.AddRange(
            properties
                .Where(p => p.Type is INamedTypeSymbol t
                    && t.IsGenericType
                    && t.TypeArguments.Any(t => t.IsDerivedFrom(boundNodeType)))
                .Select(p => ExpressionStatement(
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("diagnosticBuilder"),
                            IdentifierName("AddRange")))
                    .WithArgumentList(ArgumentList(SingletonSeparatedList(
                        Argument(IdentifierName(p.CamelCasedName()))))))));

        var initializerAssignmentExpressions = new List<ExpressionSyntax>()
        {
            AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                IdentifierName("SyntaxNode"),
                IdentifierName("syntaxNode")),

            AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                IdentifierName("DiagnosticBuilder"),
                IdentifierName("diagnosticBuilder"))
        };

        initializerAssignmentExpressions.AddRange(
            properties.Select(p => AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                IdentifierName(p.Name),
                IdentifierName(p.CamelCasedName()))));

        statements.Add(ReturnStatement(
            ImplicitObjectCreationExpression()
                .WithInitializer(InitializerExpression(
                    SyntaxKind.ObjectInitializerExpression,
                    SeparatedList(initializerAssignmentExpressions)))));

        return MethodDeclaration(ParseTypeName(returnType.Name), $"Create{returnType.Name}")
            .WithModifiers(
                TokenList(
                    new[] { Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword) }))
            .WithParameterList(ParameterList(SeparatedList(parameters)))
            .WithBody(Block(List(statements)));
    }
}
