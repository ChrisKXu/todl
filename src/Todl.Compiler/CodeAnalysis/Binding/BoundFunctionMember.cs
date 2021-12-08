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
            var functionSymbol = Scope.LookupFunctionSymbol(functionDeclarationMember);
            var functionBinder = CreateFunctionBinder(functionSymbol);

            foreach (var parameter in functionSymbol.Parameters)
            {
                functionBinder.Scope.DeclareVariable(parameter);
            }

            return BoundNodeFactory.CreateBoundFunctionMember(
                syntaxNode: functionDeclarationMember,
                functionScope: functionBinder.Scope,
                body: functionBinder.BindBlockStatementInScope(functionDeclarationMember.Body),
                functionSymbol: functionSymbol);
        }
    }
}
