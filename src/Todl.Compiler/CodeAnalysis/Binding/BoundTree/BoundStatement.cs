using System;
using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

internal abstract class BoundStatement : BoundNode { }

public partial class Binder
{
    internal BoundStatement BindStatement(Statement statement)
        => statement switch
        {
            ExpressionStatement expressionStatement => BindExpressionStatement(expressionStatement),
            BlockStatement blockStatement => BindBlockStatement(blockStatement),
            VariableDeclarationStatement variableDeclarationStatement => BindVariableDeclarationStatement(variableDeclarationStatement),
            ReturnStatement returnStatement => BindReturnStatement(returnStatement),
            IfUnlessStatement ifUnlessStatement => BindIfUnlessStatement(ifUnlessStatement),
            BreakStatement breakStatement => BindBreakStatement(breakStatement),
            ContinueStatement continueStatement => BindContinueStatement(continueStatement),
            WhileUntilStatement whileUntilStatement => BindWhileUntilStatement(whileUntilStatement),
            _ => throw new NotSupportedException() // keep compiler happy, this shouldn't happen as guarded by test cases
        };
}
