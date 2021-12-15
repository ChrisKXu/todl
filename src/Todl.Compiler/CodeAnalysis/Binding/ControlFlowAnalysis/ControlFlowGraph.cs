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
                    Connect(current, end);
                    StartNewBlock();
                    break;
                case BoundBlockStatement boundBlockStatement:
                    foreach (var innerStatement in boundBlockStatement.Statements)
                    {
                        AddStatement(innerStatement);
                    }
                    break;
                default:
                    current.Statements.Add(boundStatement);
                    break;
            }
        }

        private void Connect(BasicBlock from, BasicBlock to)
        {
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
            current = new();
        }

        public ControlFlowGraph Build()
        {
            if (blocks.LastOrDefault() != current && current.Statements.Any())
            {
                blocks.Add(current);
                Connect(current, end);
            }

            blocks.Insert(0, start);
            blocks.Add(end);

            Connect(start, blocks[1]);

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
    }

    internal sealed class BasicBlockBranch
    {
        public BasicBlock From { get; internal init; }
        public BasicBlock To { get; internal init; }
    }
}
