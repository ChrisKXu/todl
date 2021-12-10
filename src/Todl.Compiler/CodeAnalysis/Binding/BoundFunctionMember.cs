using System.Linq;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    public sealed class BoundFunctionMember : BoundMember
    {
        public BoundScope FunctionScope { get; internal init; }
        public BoundBlockStatement Body { get; internal init; }
        public FunctionSymbol FunctionSymbol { get; internal init; }

        public TypeSymbol ReturnType => FunctionSymbol.ReturnType;
    }

    public partial class Binder
    {
        private BoundFunctionMember BindFunctionDeclarationMember(FunctionDeclarationMember functionDeclarationMember)
        {
            var diagnosticBuilder = new DiagnosticBag.Builder();

            var clrTypeCacheView = functionDeclarationMember.SyntaxTree.ClrTypeCacheView;
            var namedArguments = functionDeclarationMember
                .Parameters
                .Items
                .ToDictionary(
                    p => p.Identifier.Text.ToString(),
                    p => ClrTypeSymbol.MapClrType(clrTypeCacheView.ResolveType(p.ParameterType)));

            var functionSymbol = Scope.LookupFunctionSymbol(
                name: functionDeclarationMember.Name.Text.ToString(),
                namedArguments: namedArguments);

            if (functionSymbol.FunctionDeclarationMember != functionDeclarationMember)
            {
                diagnosticBuilder.Add(new Diagnostic()
                {
                    Message = "Ambiguous function declaration. Multiple functions with the same name and parameters set are declared within the same scope.",
                    ErrorCode = ErrorCode.AmbiguousFunctionDeclaration,
                    TextLocation = functionDeclarationMember.Name.Text.GetTextLocation(),
                    Level = DiagnosticLevel.Error
                });
            }

            var functionBinder = CreateFunctionBinder(functionSymbol);

            foreach (var parameter in functionSymbol.Parameters)
            {
                functionBinder.Scope.DeclareVariable(parameter);
            }

            return BoundNodeFactory.CreateBoundFunctionMember(
                syntaxNode: functionDeclarationMember,
                functionScope: functionBinder.Scope,
                body: functionBinder.BindBlockStatementInScope(functionDeclarationMember.Body),
                functionSymbol: functionSymbol,
                diagnosticBuilder: diagnosticBuilder);
        }
    }
}
