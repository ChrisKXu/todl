using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Primitives;
using Todl.Compiler.Utilities;

namespace Todl.Compiler.Tests
{
    public static class TestUtilities
    {
        public static TreeWalkableAssertions<TNode> Should<TNode>(
            this ITreeWalkable<TNode> treeWalkable)
            where TNode : ITreeWalkable<TNode>
            => new(treeWalkable);

        public class TreeWalkableAssertions<TNode> : ObjectAssertions
            where TNode : ITreeWalkable<TNode>
        {
            public TreeWalkableAssertions(ITreeWalkable<TNode> treeWalkable) : base(treeWalkable) { }

            public void HaveChildren(params Action<TNode>[] asserts)
            {
                var nodes = Subject.As<ITreeWalkable<TNode>>().GetChildren().ToList();

                nodes.Count.Should().BeGreaterOrEqualTo(asserts.Length);

                for (var i = 0; i < asserts.Length; ++i)
                {
                    asserts[i].Invoke(nodes[i]);
                }
            }
        }
    }
}
