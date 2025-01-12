using System.Collections.Immutable;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;

namespace Todl.Compiler.CodeAnalysis.Binding;

internal abstract class BoundTreeRewriter : BoundTreeVisitor
{
    public override BoundNode VisitBoundBlockStatement(BoundBlockStatement boundBlockStatement)
    {
        var statements = VisitList(boundBlockStatement.Statements);
        return statements == boundBlockStatement.Statements
            ? boundBlockStatement
            : BoundNodeFactory.CreateBoundBlockStatement(boundBlockStatement.SyntaxNode, boundBlockStatement.Scope, statements);
    }

    public ImmutableArray<TBoundNode> VisitList<TBoundNode>(ImmutableArray<TBoundNode> nodes)
        where TBoundNode : BoundNode
    {
        var builder = ImmutableArray.CreateBuilder<TBoundNode>(nodes.Length);
        var modified = false;

        foreach (var node in nodes)
        {
            var newNode = (TBoundNode)Visit(node);
            modified |= newNode != node;
            if (newNode is not null)
            {
                builder.Add(newNode);
            }
        }

        return modified ? builder.ToImmutable() : nodes;
    }
}
