using System;
using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    public abstract class BoundStatement : BoundNode { }

    public partial class Binder
    {
        public BoundStatement BindStatement(Statement statement)
            => statement switch
            {
                ExpressionStatement expressionStatement => BindExpressionStatement(expressionStatement),
                BlockStatement blockStatement => BindBlockStatement(blockStatement),
                VariableDeclarationStatement variableDeclarationStatement => BindVariableDeclarationStatement(variableDeclarationStatement),
                ReturnStatement returnStatement => BindReturnStatement(returnStatement),
                _ => throw new NotSupportedException() // keep compiler happy, this shouldn't happen as guarded by test cases
            };
    }
}
