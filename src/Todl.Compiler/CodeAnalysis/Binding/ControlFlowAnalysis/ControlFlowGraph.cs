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

        // header and exit blocks of each loop, plus the number of finally regions active at loop entry
        private readonly Dictionary<BoundLoopContext, (BasicBlock Header, BasicBlock Exit, int FinallyDepth)> loopBlocks = new();

        // finally regions of the enclosing try statements, innermost last
        private readonly List<FinallyRegion> finallyRegions = new();

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
            Jump(endBlock, finallyDepth: 0);

            return boundReturnStatement;
        }

        // except blocks are reached from the try entry, so a throw only needs to leave the function
        public override BoundNode VisitBoundThrowStatement(BoundThrowStatement boundThrowStatement)
        {
            current.Statements.Add(boundThrowStatement);
            Jump(endBlock, finallyDepth: 0);

            return boundThrowStatement;
        }

        public override BoundNode VisitBoundTryStatement(BoundTryStatement boundTryStatement)
        {
            // Closed off as its own block so the except and finally edges leave from the try entry.
            var begin = current;
            if (!begin.Statements.Any())
            {
                begin.Statements.Add(new BoundNoOpStatement());
            }
            begin.OriginatingStatement = boundTryStatement;
            blocks.Add(begin);

            var after = new BasicBlock();
            FinallyRegion region = null;
            if (boundTryStatement.FinallyBlock is not null)
            {
                region = new FinallyRegion();
                finallyRegions.Add(region);

                // any point may throw, so the finally block is always reachable
                RouteThroughFinally(begin, ActiveFinallyRegions(finallyDepth: 0), target: null);
            }

            var normalEnds = new List<BasicBlock> { VisitBranch(boundTryStatement.Body, begin, connect: true) };
            foreach (var exceptClause in boundTryStatement.ExceptClauses)
            {
                normalEnds.Add(VisitBranch(exceptClause.Body, begin, connect: true));
            }

            if (region is not null)
            {
                finallyRegions.RemoveAt(finallyRegions.Count - 1);
            }

            foreach (var end in normalEnds.Where(end => end.Incoming.Any() && !end.IsTerminal))
            {
                if (region is null)
                {
                    Connect(end, after);
                }
                else
                {
                    RouteThroughFinally(end, new[] { region }, after);
                }
            }

            if (region is not null)
            {
                VisitFinallyBlock(boundTryStatement.FinallyBlock, region);
            }

            current = after;

            return boundTryStatement;
        }

        private void VisitFinallyBlock(BoundBlockStatement finallyBlock, FinallyRegion region)
        {
            current = region.Entry;
            Visit(finallyBlock);

            var finallyEnd = current;
            if (!ShouldPreserve(finallyEnd))
            {
                return;
            }

            blocks.Add(finallyEnd);
            if (!finallyEnd.Incoming.Any() || finallyEnd.IsTerminal)
            {
                return;
            }

            region.End = finallyEnd;

            // a jump continues past this finally block only if it and every finally block it ran before this one completes
            foreach (var jumpsToTarget in region.Jumps.GroupBy(jump => jump.Next))
            {
                var guards = jumpsToTarget
                    .Where(jump => jump.Earlier.All(earlier => earlier.End is not null))
                    .Select(jump => jump.Earlier.Select(earlier => earlier.End).Prepend(jump.Source).ToList())
                    .ToList();

                if (guards.Count == 0)
                {
                    continue;
                }

                var branch = Connect(finallyEnd, jumpsToTarget.Key);
                branch?.Guards.AddRange(guards);
            }
        }

        // Innermost first, covering the finally regions entered at or after finallyDepth.
        private List<FinallyRegion> ActiveFinallyRegions(int finallyDepth)
        {
            var regions = finallyRegions.Skip(finallyDepth).ToList();
            regions.Reverse();
            return regions;
        }

        // target is null for an exception, which only travels through the finally regions
        private void RouteThroughFinally(BasicBlock source, IReadOnlyList<FinallyRegion> regions, BasicBlock target)
        {
            if (regions.Count == 0)
            {
                if (target is not null)
                {
                    Connect(source, target);
                }

                return;
            }

            Connect(source, regions[0].Entry);
            for (var i = 0; i < regions.Count; i++)
            {
                var next = i + 1 < regions.Count ? regions[i + 1].Entry : target;
                if (next is not null)
                {
                    regions[i].Jumps.Add(new FinallyJump(source, next, regions.Take(i).ToList()));
                }
            }
        }

        // runs the finally regions entered at or after finallyDepth before reaching target
        private void Jump(BasicBlock target, int finallyDepth)
        {
            RouteThroughFinally(current, ActiveFinallyRegions(finallyDepth), target);
            FlushCurrentBlock();
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
            begin.OriginatingStatement = boundConditionalStatement;
            blocks.Add(begin);

            // Constant folding runs before control flow analysis, so a constant condition is visible as a BoundConstant. `unless` is already desugared into swapped Consequence/Alternative at bind time, so no extra negation handling is needed here.
            var constantCondition = EvaluateConstantCondition(boundConditionalStatement.Condition);

            var consequenceEnd = VisitBranch(boundConditionalStatement.Consequence, begin, constantCondition != false);
            var alternativeEnd = VisitBranch(boundConditionalStatement.Alternative, begin, constantCondition != true);

            // Merge block - only connect branches with live, non-terminal flow. The merge
            // is kept even if it stays empty here: FlushCurrentBlock/Build preserve any block
            // that already has an incoming edge instead of silently dropping it.
            current = new BasicBlock();
            ConnectToMerge(consequenceEnd, current);
            ConnectToMerge(alternativeEnd, current);

            return boundConditionalStatement;
        }

        private BasicBlock VisitBranch(BoundStatement branch, BasicBlock from, bool connect)
        {
            current = new BasicBlock();
            if (connect)
            {
                Connect(from, current);
            }
            Visit(branch);

            var end = current;

            if (ShouldPreserve(end) && (connect || HasRealStatement(end)))
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
            preHeader.OriginatingStatement = boundLoopStatement;
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
            loopBlocks[boundLoopStatement.BoundLoopContext] = (header, exit, finallyRegions.Count);

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

            var (_, exit, finallyDepth) = loopBlocks[boundBreakStatement.BoundLoopContext];
            Jump(exit, finallyDepth);

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

            var (header, _, finallyDepth) = loopBlocks[boundContinueStatement.BoundLoopContext];

            // continue re-checks the condition, so it targets the header directly - not the loop's exit.
            Jump(header, finallyDepth);

            return boundContinueStatement;
        }

        public override BoundNode VisitBoundExpressionStatement(BoundExpressionStatement boundExpressionStatement)
        {
            current.Statements.Add(boundExpressionStatement);
            return boundExpressionStatement;
        }

        /// <summary>
        /// Returns a boolean condition's constant value, or null when it is not known to
        /// be constant at this point in the pipeline.
        /// </summary>
        private static bool? EvaluateConstantCondition(BoundExpression condition)
        {
            if (condition is not BoundConstant { Value: ConstantBooleanValue constantBooleanValue })
            {
                return null;
            }

            return constantBooleanValue.BooleanValue;
        }

        /// <summary>
        /// Returns the loop's condition as a constant boolean, accounting for `until`
        /// negation, or null when it is not known to be constant at this point.
        /// </summary>
        private static bool? EvaluateConstantCondition(BoundLoopStatement boundLoopStatement)
        {
            var constantValue = EvaluateConstantCondition(boundLoopStatement.Condition);
            if (constantValue is null)
            {
                return null;
            }

            return boundLoopStatement.ConditionNegated ? !constantValue : constantValue;
        }

        // A block must be kept once it is wired into the graph (has an incoming edge) or
        // holds real statements. Otherwise it is a scratch object nothing ever reaches -
        // e.g. the fresh block created right after a return/break/continue when no further
        // statements follow - and can be safely discarded instead of becoming an orphan
        // that Branches references but Blocks does not contain.
        private static bool ShouldPreserve(BasicBlock block)
            => block.Statements.Any() || block.Incoming.Any();

        private static bool HasRealStatement(BasicBlock block)
            => block.Statements.Any(statement => statement is not BoundNoOpStatement);

        private BasicBlockBranch Connect(BasicBlock from, BasicBlock to, bool isFallOff = false)
        {
            if (from == to)
            {
                return null;
            }

            var branch = new BasicBlockBranch(from, to) { IsFallOff = isFallOff };
            branches.Add(branch);
            from.Outgoing.Add(branch);
            to.Incoming.Add(branch);

            return branch;
        }

        /// <summary>
        /// Flushes `current` into the graph without adding an implicit outgoing edge, for
        /// statements (return, break, continue, throw) that already wired their own outgoing edge.
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
            // Only the block still being filled can run off the end of the function.
            var fallsOff = ShouldPreserve(current) && !current.IsTerminal;

            if (blocks.LastOrDefault() != current && ShouldPreserve(current))
            {
                blocks.Add(current);
            }

            blocks.Insert(0, startBlock);
            blocks.Add(endBlock);

            Connect(startBlock, blocks[1]);

            if (fallsOff)
            {
                Connect(current, endBlock, isFallOff: true);
            }

            ComputeReachability();

            return new()
            {
                Blocks = blocks.ToImmutable(),
                Branches = branches.ToImmutable()
            };
        }

        // Incoming.Any() alone isn't enough: that edge may come from a block nothing reaches.
        private void ComputeReachability()
        {
            var stack = new Stack<BasicBlock>();
            stack.Push(startBlock);

            // a guarded edge can become usable after its source was visited, so repeat until nothing new is reached
            while (stack.Count > 0)
            {
                while (stack.Count > 0)
                {
                    var block = stack.Pop();
                    if (block.Reachable)
                    {
                        continue;
                    }

                    block.Reachable = true;
                    foreach (var branch in block.Outgoing.Where(IsTraversable))
                    {
                        stack.Push(branch.To);
                    }
                }

                foreach (var branch in branches.Where(branch => branch.Guards.Count > 0 && branch.From.Reachable && !branch.To.Reachable && IsTraversable(branch)))
                {
                    stack.Push(branch.To);
                }
            }
        }

        private static bool IsTraversable(BasicBlockBranch branch)
            => branch.Guards.Count == 0 || branch.Guards.Any(guard => guard.All(block => block.Reachable));

        private sealed class FinallyRegion
        {
            public BasicBlock Entry { get; } = new();

            // set when the finally block can complete
            public BasicBlock End { get; set; }

            public List<FinallyJump> Jumps { get; } = new();
        }

        // Earlier: the regions the jump ran through before this one
        private sealed record FinallyJump(BasicBlock Source, BasicBlock Next, IReadOnlyList<FinallyRegion> Earlier);
    }

    [DebuggerDisplay("{GetDebuggerDisplay()}")]
    internal sealed class BasicBlock
    {
        public List<BoundStatement> Statements { get; } = new();
        public List<BasicBlockBranch> Incoming { get; } = new();
        public List<BasicBlockBranch> Outgoing { get; } = new();

        // Points at the if/while/until statement a placeholder block stands in for, used as a diagnostic location fallback.
        public BoundStatement OriginatingStatement { get; set; }

        public bool IsTerminal
        {
            get
            {
                if (!Statements.Any())
                    return false;

                var last = Statements[^1];
                return last is BoundReturnStatement
                    || last is BoundThrowStatement
                    || last is BoundBreakStatement
                    || last is BoundContinueStatement;
            }
        }

        public bool Reachable { get; internal set; }

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
    internal sealed record BasicBlockBranch(BasicBlock From, BasicBlock To)
    {
        // reaches the end block by running off the end of the function
        public bool IsFallOff { get; init; }

        // when present, the edge is taken only if all blocks of at least one entry are reachable
        public List<List<BasicBlock>> Guards { get; } = new();
    }
}
