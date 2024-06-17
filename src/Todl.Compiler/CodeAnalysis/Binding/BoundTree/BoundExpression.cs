using System;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

public abstract class BoundExpression : BoundNode
{
    public virtual TypeSymbol ResultType { get; }
    public virtual bool LValue => false;
    public virtual bool Constant => false;
    public virtual bool ReadOnly => true;
}

public partial class Binder
{
    internal BoundExpression BindExpression(Expression expression)
        => expression switch
        {
            LiteralExpression literalExpression => BindLiteralExpression(literalExpression),
            BinaryExpression binaryExpression => BindBinaryExpression(binaryExpression),
            UnaryExpression unaryExpression => BindUnaryExpression(unaryExpression),
            ParethesizedExpression parethesizedExpression => BindExpression(parethesizedExpression.InnerExpression),
            AssignmentExpression assignmentExpression => BindAssignmentExpression(assignmentExpression),
            NameExpression nameExpression => BindNameExpression(nameExpression),
            MemberAccessExpression memberAccessExpression => BindMemberAccessExpression(memberAccessExpression),
            FunctionCallExpression functionCallExpression => BindFunctionCallExpression(functionCallExpression),
            NewExpression newExpression => BindNewExpression(newExpression),
            _ => throw new NotSupportedException() // keep compiler happy, this shouldn't happen as guarded by test cases
        };
}
