using System;
using System.Collections.Generic;
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
        private readonly BasicBlock start = new();
        private readonly BasicBlock end = new();
        private BasicBlock current = new();

        public void AddStatement(BoundStatement boundStatement)
        {
            switch (boundStatement)
            {
                case BoundReturnStatement:
                    current.Statements.Add(boundStatement);
                    StartNewBlock();
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
                    var begin = current;

                    if (boundConditionalStatement.Consequence is not null)
                    {
                        StartNewBlock();
                        Connect(begin, current);
                        AddStatement(boundConditionalStatement.Consequence);
                    }

                    if (boundConditionalStatement.Alternative is not null)
                    {
                        StartNewBlock();
                        Connect(begin, current);
                        AddStatement(boundConditionalStatement.Alternative);
                    }

                    StartNewBlock();
                    Connect(begin, current);
                    break;
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

            var branch = new BasicBlockBranch() { From = from, To = to };
            branches.Add(branch);
            from.Outgoing.Add(branch);
            to.Incoming.Add(branch);
        }

        private void StartNewBlock()
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
                StartNewBlock();
            }

            blocks.Insert(0, start);
            blocks.Add(end);

            Connect(start, blocks[1]);

            if (blocks.Count > 2)
            {
                Connect(blocks[^2], end);
            }

            return new()
            {
                Blocks = blocks,
                Branches = branches
            };
        }
    }

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

                return Statements[^1] is BoundReturnStatement;
            }
        }

        public bool Reachable => Incoming.Any();
    }

    internal sealed class BasicBlockBranch
    {
        public BasicBlock From { get; internal init; }
        public BasicBlock To { get; internal init; }
    }
}
