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
        var builtInTypes = f.SyntaxNode.SyntaxTree.ClrTypeCache.BuiltInTypes;
        if (!f.ReturnType.Equals(builtInTypes.Void)
            && !f.ReturnType.Equals(builtInTypes.Int32))
        {
            return false;
        }

        // entry point function should either have no parameters or exactly one parameter of type string[]
        var parameters = f.FunctionSymbol.Parameters.ToList();
        if (parameters.Count > 2)
        {
            return false;
        }

        if (parameters.Count == 1)
        {
            var p = parameters[0];
            if (p.Type is not ClrTypeSymbol clrTypeSymbol)
            {
                return false;
            }

            return clrTypeSymbol.ClrType.IsArray && clrTypeSymbol.ClrType.GetElementType().Equals(builtInTypes.String.ClrType);
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
