using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;

namespace Todl.Compiler.CodeAnalysis.Binding.ControlFlowAnalysis;

internal sealed class ControlFlowGraph
{
    public BasicBlock StartBlock => Blocks.First();
    public BasicBlock EndBlock => Blocks.Last();
    public ImmutableArray<BasicBlock> Blocks { get; private init; }
    public ImmutableArray<BasicBlockBranch> Branches { get; private init; }

    internal static ControlFlowGraph Create(BoundFunctionMember boundFunctionMember)
    {
        var builder = new Builder();
        boundFunctionMember.Accept(builder);

        return builder.Build();
    }

    private sealed class Builder : BoundTreeWalker
    {
        private readonly ImmutableArray<BasicBlock>.Builder blocks = ImmutableArray.CreateBuilder<BasicBlock>();
        private readonly ImmutableArray<BasicBlockBranch>.Builder branches = ImmutableArray.CreateBuilder<BasicBlockBranch>();
        private readonly BasicBlock startBlock = new();
        private readonly BasicBlock endBlock = new();

        // this helps to keep track of begin and end blocks for a given loop
        private readonly Dictionary<BoundLoopContext, (BasicBlock, BasicBlock)> loopBlocks = new();

        private BasicBlock current = new();

        public override BoundNode DefaultVisit(BoundNode node)
        {
            if (node is BoundStatement boundStatement)
            {
                current.Statements.Add(boundStatement);
            }

            return base.DefaultVisit(node);
        }

        public override BoundNode VisitBoundReturnStatement(BoundReturnStatement boundReturnStatement)
        {
            current.Statements.Add(boundReturnStatement);
            StartNewBlock(endBlock);

            return boundReturnStatement;
        }

        public override BoundNode VisitBoundBlockStatement(BoundBlockStatement boundBlockStatement)
        {
            if (boundBlockStatement.Statements.IsEmpty)
            {
                current.Statements.Add(new BoundNoOpStatement());
                return boundBlockStatement;
            }

            return base.VisitBoundBlockStatement(boundBlockStatement);
        }

        public override BoundNode VisitBoundConditionalStatement(BoundConditionalStatement boundConditionalStatement)
        {
            var begin = current;

            // Ensure begin is a proper block so branches can't absorb it
            if (!begin.Statements.Any())
            {
                begin.Statements.Add(new BoundNoOpStatement());
            }
            blocks.Add(begin);

            var consequenceEnd = VisitBranch(boundConditionalStatement.Consequence, begin);
            var alternativeEnd = VisitBranch(boundConditionalStatement.Alternative, begin);

            // Merge block — only connect branches with live, non-terminal flow
            current = new BasicBlock();
            ConnectToMerge(consequenceEnd, current);
            ConnectToMerge(alternativeEnd, current);

            if (boundConditionalStatement.Consequence is BoundNoOpStatement || boundConditionalStatement.Alternative is BoundNoOpStatement)
            {
                current.Statements.Add(new BoundNoOpStatement());
            }

            return boundConditionalStatement;
        }

        private BasicBlock VisitBranch(BoundStatement branch, BasicBlock from)
        {
            current = new BasicBlock();
            Connect(from, current);
            Visit(branch);
            var end = current;
            if (end.Statements.Any())
            {
                blocks.Add(end);
                if (end.IsTerminal)
                {
                    Connect(end, endBlock);
                }
            }
            return end;
        }

        private void ConnectToMerge(BasicBlock branchEnd, BasicBlock merge)
        {
            if (branchEnd.Incoming.Any() && !branchEnd.IsTerminal)
            {
                Connect(branchEnd, merge);
            }
        }

        public override BoundNode VisitBoundLoopStatement(BoundLoopStatement boundLoopStatement)
        {
            var begin = current;

            StartNewBlock(endBlock);
            Connect(begin, current);

            var end = new BasicBlock();
            Connect(begin, end);
            loopBlocks[boundLoopStatement.BoundLoopContext] = (begin, end);

            Visit(boundLoopStatement.Body);
            var body = current;

            StartNewBlock(end);
            Connect(body, current);
            Connect(begin, current);

            current = end;

            return boundLoopStatement;
        }

        public override BoundNode VisitBoundBreakStatement(BoundBreakStatement boundBreakStatement)
        {
            current.Statements.Add(boundBreakStatement);
            var (_, end) = loopBlocks[boundBreakStatement.BoundLoopContext];

            StartNewBlock(end);

            return boundBreakStatement;
        }

        public override BoundNode VisitBoundContinueStatement(BoundContinueStatement boundContinueStatement)
        {
            current.Statements.Add(boundContinueStatement);
            var (begin, end) = loopBlocks[boundContinueStatement.BoundLoopContext];

            Connect(current, begin);
            StartNewBlock(end);

            return boundContinueStatement;
        }

        public override BoundNode VisitBoundExpressionStatement(BoundExpressionStatement boundExpressionStatement)
        {
            current.Statements.Add(boundExpressionStatement);
            return boundExpressionStatement;
        }

        private void Connect(BasicBlock from, BasicBlock to)
        {
            if (from == to)
            {
                return;
            }

            var branch = new BasicBlockBranch(from, to);
            branches.Add(branch);
            from.Outgoing.Add(branch);
            to.Incoming.Add(branch);
        }

        private void StartNewBlock(BasicBlock end)
        {
            if (!current.Statements.Any())
            {
                return;
            }

            blocks.Add(current);
            var next = new BasicBlock();

            if (current.IsTerminal)
            {
                Connect(current, end);
            }
            else
            {
                Connect(current, next);
            }

            current = next;
        }

        public ControlFlowGraph Build()
        {
            if (blocks.LastOrDefault() != current)
            {
                StartNewBlock(endBlock);
            }

            blocks.Insert(0, startBlock);
            blocks.Add(endBlock);

            Connect(startBlock, blocks[1]);

            if (blocks.Count > 2 && !blocks[^2].IsTerminal)
            {
                Connect(blocks[^2], endBlock);
            }

            return new()
            {
                Blocks = blocks.ToImmutable(),
                Branches = branches.ToImmutable()
            };
        }
    }

    [DebuggerDisplay("{GetDebuggerDisplay()}")]
    internal sealed class BasicBlock
    {
        public List<BoundStatement> Statements { get; } = new();
        public List<BasicBlockBranch> Incoming { get; } = new();
        public List<BasicBlockBranch> Outgoing { get; } = new();

        public bool IsTerminal
        {
            get
            {
                if (!Statements.Any())
                    return false;

                var last = Statements[^1];
                return last is BoundReturnStatement
                    || last is BoundBreakStatement
                    || last is BoundContinueStatement;
            }
        }

        public bool IsReturn
        {
            get
            {
                if (!Statements.Any())
                    return false;

                var last = Statements[^1];
                return last is BoundReturnStatement;
            }
        }

        public bool Reachable => Incoming.Any();

        public string GetDebuggerDisplay()
        {
            if (!Statements.Any())
            {
                return "[Empty]";
            }

            return Statements[0].SyntaxNode.Text.ToString();
        }
    }

    [DebuggerDisplay("{From} ==> {To}")]
    internal sealed record BasicBlockBranch(BasicBlock From, BasicBlock To);
}
