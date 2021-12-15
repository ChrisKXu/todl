using System;
using System.Collections.Generic;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Binding.ControlFlowAnalysis;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding;

public sealed class BoundModule : IDiagnosable
{
    private readonly Binder binder;
    private readonly List<BoundMember> boundMembers = new();
    private readonly DiagnosticBag.Builder diagnosticBuilder = new();
    private readonly List<BoundNodeVisitor> boundNodeVisitors = new();

    public IReadOnlyList<SyntaxTree> SyntaxTrees { get; private init; }
    public IReadOnlyList<BoundMember> BoundMembers => boundMembers;

    // make ctor private
    private BoundModule()
    {
        binder = Binder.CreateModuleBinder();
        boundNodeVisitors.Add(new ControlFlowAnalyzer(diagnosticBuilder));
    }

    private void BindSyntaxTrees()
    {
        var members = SyntaxTrees.SelectMany(tree => tree.Members);
        foreach (var functionDeclarationMember in members.OfType<FunctionDeclarationMember>())
        {
            var function = FunctionSymbol.FromFunctionDeclarationMember(functionDeclarationMember);
            if (binder.Scope.DeclareFunction(function) != function)
            {
                diagnosticBuilder.Add(new Diagnostic()
                {
                    Message = "Ambiguous function declaration. Multiple functions with the same name and parameters set are declared within the same scope.",
                    ErrorCode = ErrorCode.AmbiguousFunctionDeclaration,
                    TextLocation = functionDeclarationMember.Name.Text.GetTextLocation(),
                    Level = DiagnosticLevel.Error
                });
            }
        }

        boundMembers.AddRange(members.Select(m => binder.BindMember(m)));

        foreach (var visitor in boundNodeVisitors)
        {
            boundMembers.ForEach(m => visitor.VisitMember(m));
        }
    }

    public static BoundModule Create(
        IReadOnlyList<SyntaxTree> syntaxTrees)
    {
        syntaxTrees ??= Array.Empty<SyntaxTree>();

        var boundModule = new BoundModule()
        {
            SyntaxTrees = syntaxTrees
        };

        boundModule.BindSyntaxTrees();

        return boundModule;
    }

    public IEnumerable<Diagnostic> GetDiagnostics()
    {
        diagnosticBuilder.AddRange(BoundMembers);
        return diagnosticBuilder.Build();
    }
}
