using System.Collections.Generic;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding;

public sealed class BoundEntryPointTypeDefinition : BoundTodlTypeDefinition
{
    public const string EntryPointFunctionName = "Main";
    public const string GeneratedTypeName = "_Todl_Generated_EntryPoint_";

    public override bool IsGeneratedType => true;
    public override bool IsStatic => true;

    public BoundFunctionMember EntryPointFunctionMember
        => Functions.FirstOrDefault(IsEntryPointCandidate);

    private bool IsEntryPointCandidate(BoundFunctionMember f)
    {
        // name should be "Main"
        if (f.FunctionSymbol.Name != EntryPointFunctionName)
        {
            return false;
        }

        // entry point function should either return "void" or "int"
        if (!f.ReturnType.Equals(f.SyntaxNode.SyntaxTree.ClrTypeCache.BuiltInTypes.Void)
            && !f.ReturnType.Equals(f.SyntaxNode.SyntaxTree.ClrTypeCache.BuiltInTypes.Int32))
        {
            return false;
        }

        if (f.FunctionSymbol.Parameters.Any())
        {
            return false;
        }

        return true;
    }
}

public partial class Binder
{
    public BoundEntryPointTypeDefinition BindEntryPointTypeDefinition(IEnumerable<SyntaxTree> syntaxTrees)
    {
        var typeBinder = CreateTypeBinder();
        var diagnosticBuilder = new DiagnosticBag.Builder();

        var members = syntaxTrees.SelectMany(tree => tree.Members);
        foreach (var functionDeclarationMember in members.OfType<FunctionDeclarationMember>())
        {
            var function = FunctionSymbol.FromFunctionDeclarationMember(functionDeclarationMember);
            if (typeBinder.Scope.DeclareFunction(function) != function)
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

        var boundMembers = members.Select(m => typeBinder.BindMember(m));
        diagnosticBuilder.AddRange(boundMembers);

        // TODO: Use BoundNodeFactory to create BoundEntryPointTypeDefinition
        return new()
        {
            SyntaxNode = null,
            BoundMembers = boundMembers.ToList(),
            DiagnosticBuilder = diagnosticBuilder
        };
    }
}
