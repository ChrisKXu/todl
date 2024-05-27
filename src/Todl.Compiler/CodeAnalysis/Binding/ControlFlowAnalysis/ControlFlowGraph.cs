using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Todl.Compiler.CodeAnalysis.Binding.ControlFlowAnalysis;

internal sealed class ControlFlowGraph
{
    public BasicBlock StartBlock => Blocks.First();
    public BasicBlock EndBlock => Blocks.Last();
    public IReadOnlyCollection<BasicBlock> Blocks { get; private init; }
    public IReadOnlyCollection<BasicBlockBranch> Branches { get; private init; }

    internal static ControlFlowGraph Create(BoundFunctionMember boundFunctionMember)
    {
        var builder = new Builder();
        foreach (var statement in boundFunctionMember.Body.Statements)
        {
            builder.AddStatement(statement);
        }

        return builder.Build();
    }

    private sealed class Builder
    {
        private readonly List<BasicBlock> blocks = new();
        private readonly List<BasicBlockBranch> branches = new();
        private readonly BasicBlock startBlock = new();
        private readonly BasicBlock endBlock = new();

        // this helps to keep track of begin and end blocks for a given loop
        private readonly Dictionary<BoundLoopContext, (BasicBlock, BasicBlock)> loopBlocks = new();

        private BasicBlock current = new();

        public void AddStatement(BoundStatement boundStatement)
        {
            switch (boundStatement)
            {
                case BoundReturnStatement:
                    current.Statements.Add(boundStatement);
                    StartNewBlock(endBlock);
                    break;
                case BoundBlockStatement boundBlockStatement:
                    if (!boundBlockStatement.Statements.Any())
                    {
                        AddStatement(new BoundNoOpStatement());
                    }
                    else
                    {
                        foreach (var innerStatement in boundBlockStatement.Statements)
                        {
                            AddStatement(innerStatement);
                        }
                    }
                    break;
                case BoundConditionalStatement boundConditionalStatement:
                    {
                        var begin = current;

                        StartNewBlock(endBlock);
                        Connect(begin, current);
                        AddStatement(boundConditionalStatement.Consequence);
                        var consequence = current;

                        StartNewBlock(endBlock);
                        Connect(begin, current);
                        AddStatement(boundConditionalStatement.Alternative);
                        var alternative = current;

                        StartNewBlock(endBlock);
                        Connect(consequence, current);
                        Connect(alternative, current);

                        if (boundConditionalStatement.Consequence is BoundNoOpStatement || boundConditionalStatement.Alternative is BoundNoOpStatement)
                        {
                            AddStatement(new BoundNoOpStatement());
                        }
                        break;
                    }
                case BoundLoopStatement boundLoopStatement:
                    {
                        var begin = current;

                        StartNewBlock(endBlock);
                        Connect(begin, current);

                        var end = new BasicBlock();
                        Connect(begin, end);
                        loopBlocks[boundLoopStatement.BoundLoopContext] = (begin, end);

                        AddStatement(boundLoopStatement.Body);
                        var body = current;

                        StartNewBlock(end);
                        Connect(body, current);
                        Connect(begin, current);

                        current = end;

                        break;
                    }
                case BoundBreakStatement boundBreakStatement:
                    {
                        current.Statements.Add(boundStatement);
                        var (_, end) = loopBlocks[boundBreakStatement.BoundLoopContext];

                        StartNewBlock(end);
                        break;
                    }
                case BoundContinueStatement boundContinueStatement:
                    {
                        current.Statements.Add(boundStatement);
                        var (begin, end) = loopBlocks[boundContinueStatement.BoundLoopContext];

                        Connect(current, begin);
                        StartNewBlock(end);
                        break;
                    }
                default:
                    current.Statements.Add(boundStatement);
                    break;
            }
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

            if (current.IsTeminal)
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

            if (blocks.Count > 2)
            {
                Connect(blocks[^2], endBlock);
            }

            return new()
            {
                Blocks = blocks,
                Branches = branches
            };
        }
    }

    [DebuggerDisplay("{GetDebuggerDisplay()}")]
    internal sealed class BasicBlock
    {
        public List<BoundStatement> Statements { get; } = new();
        public List<BasicBlockBranch> Incoming { get; } = new();
        public List<BasicBlockBranch> Outgoing { get; } = new();

        public bool IsTeminal
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
