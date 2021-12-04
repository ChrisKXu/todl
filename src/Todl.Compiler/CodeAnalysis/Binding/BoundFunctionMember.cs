using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    public sealed class BoundFunctionMember : BoundMember
    {
        public BoundScope FunctionScope { get; internal init; }
        public BoundBlockStatement Body { get; internal init; }
        public TypeSymbol ReturnType { get; internal init; }
    }

    public partial class Binder
    {
        private BoundFunctionMember BindFunctionDeclarationMember(FunctionDeclarationMember functionDeclarationMember)
        {
            var functionBinder = CreateFunctionBinder();

            foreach (var parameter in functionDeclarationMember.Parameters.Items)
            {
                // declaring parameters as readonly variables in function
                functionBinder.Scope.DeclareVariable(new VariableSymbol(
                    name: parameter.Identifier.Text.ToString(),
                    readOnly: true,
                    type: ClrTypeSymbol.MapClrType(functionDeclarationMember.SyntaxTree.ClrTypeCacheView.ResolveType(parameter.ParameterType))));
            }

            return new()
            {
                SyntaxNode = functionDeclarationMember,
                FunctionScope = functionBinder.Scope,
                Body = functionBinder.BindBlockStatementInScope(functionDeclarationMember.Body),
                ReturnType = ClrTypeSymbol.MapClrType(functionDeclarationMember.SyntaxTree.ClrTypeCacheView.ResolveType(functionDeclarationMember.ReturnType))
            };
        }
    }
}
