using System.Linq;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;

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
            var functionBinder = CreateFunctionBinder();
            var functionSymbol = Scope.LookupFunctionSymbol(functionDeclarationMember);

            foreach (var parameter in functionSymbol.Parameters)
            {
                functionBinder.Scope.DeclareVariable(parameter);
            }

            return new()
            {
                SyntaxNode = functionDeclarationMember,
                FunctionScope = functionBinder.Scope,
                Body = functionBinder.BindBlockStatementInScope(functionDeclarationMember.Body),
                FunctionSymbol = functionSymbol
            };
        }
    }
}
