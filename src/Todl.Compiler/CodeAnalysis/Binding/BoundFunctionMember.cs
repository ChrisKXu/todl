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
            var functionSymbol = Scope.LookupFunctionSymbol(functionDeclarationMember);
            var functionBinder = CreateFunctionBinder(functionSymbol);

            var duplicate = functionSymbol
                .Parameters
                .GroupBy(p => p.Name)
                .FirstOrDefault(g => g.Count() > 1);

            if (duplicate is not null)
            {
                diagnosticBuilder.Add(new Diagnostic()
                {
                    Message = $"Parameter '{duplicate.First().Name}' is a duplicate",
                    ErrorCode = ErrorCode.DuplicateParameterName,
                    Level = DiagnosticLevel.Error,
                    TextLocation = functionDeclarationMember.Name.GetTextLocation()
                });
            }

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
