using System.Linq;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree
{
    [BoundNode]
    public sealed class BoundFunctionMember : BoundMember
    {
        public BoundScope FunctionScope { get; internal init; }
        public BoundBlockStatement Body { get; internal init; }
        public FunctionSymbol FunctionSymbol { get; internal init; }

        public TypeSymbol ReturnType => FunctionSymbol.ReturnType;
        public bool IsPublic => FunctionSymbol.IsPublic;
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

            var body = functionBinder.BindBlockStatementInScope(functionDeclarationMember.Body);
            if (functionSymbol.ReturnType.SpecialType == SpecialType.ClrVoid)
            {
                if (!body.Statements.Any() || body.Statements[^1] is not BoundReturnStatement)
                {
                    var returnStatement = BoundNodeFactory.CreateBoundReturnStatement(
                        syntaxNode: null,
                        boundReturnValueExpression: null,
                        diagnosticBuilder: diagnosticBuilder);

                    body = BoundNodeFactory.CreateBoundBlockStatement(
                        syntaxNode: body.SyntaxNode,
                        scope: body.Scope,
                        statements: body.Statements.Append(returnStatement).ToList());
                }
            }

            return BoundNodeFactory.CreateBoundFunctionMember(
                syntaxNode: functionDeclarationMember,
                functionScope: functionBinder.Scope,
                body: body,
                functionSymbol: functionSymbol,
                diagnosticBuilder: diagnosticBuilder);
        }
    }
}
