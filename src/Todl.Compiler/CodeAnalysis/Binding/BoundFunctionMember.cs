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

    public sealed partial class Binder
    {
        private BoundFunctionMember BindFunctionDeclarationMember(BoundScope parentScope, FunctionDeclarationMember functionDeclarationMember)
        {
            var functionScope = parentScope.CreateChildScope(BoundScopeKind.Function);

            foreach (var parameter in functionDeclarationMember.Parameters.Items)
            {
                // declaring parameters as readonly variables in function
                functionScope.DeclareVariable(new VariableSymbol(
                    name: parameter.Identifier.Text.ToString(),
                    readOnly: true,
                    type: ClrTypeSymbol.MapClrType(functionDeclarationMember.SyntaxTree.ClrTypeCacheView.ResolveType(parameter.ParameterType))));
            }

            return new BoundFunctionMember()
            {
                FunctionScope = functionScope,
                Body = BindBlockStatement(functionScope, functionDeclarationMember.Body),
                ReturnType = ClrTypeSymbol.MapClrType(functionDeclarationMember.SyntaxTree.ClrTypeCacheView.ResolveType(functionDeclarationMember.ReturnType))
            };
        }
    }
}
