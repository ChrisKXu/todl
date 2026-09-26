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

        // this helps to keep track of header and exit blocks for a given loop
        private readonly Dictionary<BoundLoopContext, (BasicBlock Header, BasicBlock Exit)> loopBlocks = new();

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

        public override BoundNode VisitBoundVariableDeclarationStatement(BoundVariableDeclarationStatement boundVariableDeclarationStatement)
        {
            // BoundTreeWalker's own override only recurses into the initializer and never
            // reaches DefaultVisit, so declarations would otherwise be invisible to the CFG.
            current.Statements.Add(boundVariableDeclarationStatement);
            return boundVariableDeclarationStatement;
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

            // Merge block - only connect branches with live, non-terminal flow. The merge
            // is kept even if it stays empty here: StartNewBlock/Build preserve any block
            // that already has an incoming edge instead of silently dropping it.
            current = new BasicBlock();
            ConnectToMerge(consequenceEnd, current);
            ConnectToMerge(alternativeEnd, current);

            return boundConditionalStatement;
        }

        private BasicBlock VisitBranch(BoundStatement branch, BasicBlock from)
        {
            current = new BasicBlock();
            Connect(from, current);
            Visit(branch);

            var end = current;
            if (ShouldPreserve(end))
            {
                blocks.Add(end);
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
            // Close off whatever preceded the loop as its own block, distinct from the
            // header, so a `continue` back edge never re-executes pre-loop code.
            var preHeader = current;
            if (!preHeader.Statements.Any())
            {
                preHeader.Statements.Add(new BoundNoOpStatement());
            }
            blocks.Add(preHeader);

            // The header is a synthetic branch point standing in for condition evaluation:
            // it either enters the body or falls through to the exit. It is always reachable
            // (from preHeader, or later from the body's back edge), so it is always kept.
            var header = new BasicBlock();
            header.Statements.Add(new BoundNoOpStatement());
            blocks.Add(header);
            Connect(preHeader, header);

            // Constant folding runs after control flow analysis, so a literal condition is
            // only visible here as a direct BoundConstant. When it is, drop the edge that a
            // constant condition makes impossible instead of always modeling both outcomes.
            var constantCondition = EvaluateConstantCondition(boundLoopStatement);

            // The exit represents "after the loop". Unlike header it is only kept if
            // something actually reaches it (fallthrough or a break); a `while true` loop
            // with no break has no textual code after it and should not manufacture one.
            var exit = new BasicBlock();
            if (constantCondition != true)
            {
                Connect(header, exit);
            }

            // Registered before visiting the body so nested break/continue statements -
            // including ones belonging to this exact loop - resolve correctly.
            loopBlocks[boundLoopStatement.BoundLoopContext] = (header, exit);

            current = new BasicBlock();
            if (constantCondition != false)
            {
                Connect(header, current);
            }

            Visit(boundLoopStatement.Body);

            var bodyEnd = current;
            if (ShouldPreserve(bodyEnd))
            {
                blocks.Add(bodyEnd);
                if (!bodyEnd.IsTerminal)
                {
                    // Falling off the end of the body re-checks the condition. This is the
                    // loop's back edge; return/break/continue each wire their own exit and
                    // leave `current` as a fresh, empty, unconnected block instead.
                    Connect(bodyEnd, header);
                }
            }

            current = exit;

            return boundLoopStatement;
        }

        public override BoundNode VisitBoundBreakStatement(BoundBreakStatement boundBreakStatement)
        {
            current.Statements.Add(boundBreakStatement);

            if (boundBreakStatement.BoundLoopContext is null)
            {
                // Already reported as NoEnclosingLoop during binding; nothing to wire.
                return boundBreakStatement;
            }

            var (_, exit) = loopBlocks[boundBreakStatement.BoundLoopContext];
            StartNewBlock(exit);

            return boundBreakStatement;
        }

        public override BoundNode VisitBoundContinueStatement(BoundContinueStatement boundContinueStatement)
        {
            current.Statements.Add(boundContinueStatement);

            if (boundContinueStatement.BoundLoopContext is null)
            {
                // Already reported as NoEnclosingLoop during binding; nothing to wire.
                return boundContinueStatement;
            }

            var (header, _) = loopBlocks[boundContinueStatement.BoundLoopContext];

            // continue re-checks the condition, so it targets the header directly - not the
            // loop's exit. FlushCurrentBlock (unlike StartNewBlock) adds no implicit edge,
            // since the edge above is already the block's one and only outgoing edge.
            Connect(current, header);
            FlushCurrentBlock();

            return boundContinueStatement;
        }

        public override BoundNode VisitBoundExpressionStatement(BoundExpressionStatement boundExpressionStatement)
        {
            current.Statements.Add(boundExpressionStatement);
            return boundExpressionStatement;
        }

        /// <summary>
        /// Returns the loop's condition as a constant boolean, accounting for `until`
        /// negation, or null when it is not known to be constant at this point.
        /// </summary>
        private static bool? EvaluateConstantCondition(BoundLoopStatement boundLoopStatement)
        {
            if (boundLoopStatement.Condition is not BoundConstant { Value: ConstantBooleanValue constantBooleanValue })
            {
                return null;
            }

            return boundLoopStatement.ConditionNegated
                ? !constantBooleanValue.BooleanValue
                : constantBooleanValue.BooleanValue;
        }

        // A block must be kept once it is wired into the graph (has an incoming edge) or
        // holds real statements. Otherwise it is a scratch object nothing ever reaches -
        // e.g. the fresh block created right after a return/break/continue when no further
        // statements follow - and can be safely discarded instead of becoming an orphan
        // that Branches references but Blocks does not contain.
        private static bool ShouldPreserve(BasicBlock block)
            => block.Statements.Any() || block.Incoming.Any();

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
            if (!ShouldPreserve(current))
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

        /// <summary>
        /// Flushes `current` into the graph without adding an implicit outgoing edge, for
        /// statements (namely `continue`) that already wired their own outgoing edge.
        /// </summary>
        private void FlushCurrentBlock()
        {
            if (!ShouldPreserve(current))
            {
                return;
            }

            blocks.Add(current);
            current = new BasicBlock();
        }

        public ControlFlowGraph Build()
        {
            if (blocks.LastOrDefault() != current && ShouldPreserve(current))
            {
                blocks.Add(current);
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

            return Statements[0].SyntaxNode.GetText();
        }
    }

    [DebuggerDisplay("{From} ==> {To}")]
    internal sealed record BasicBlockBranch(BasicBlock From, BasicBlock To);
}
