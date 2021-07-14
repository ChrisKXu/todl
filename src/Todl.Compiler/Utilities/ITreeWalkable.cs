using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Todl.Compiler.Utilities
{
    public interface ITreeWalkable<TNode> where TNode : ITreeWalkable<TNode>
    {
        IEnumerable<TNode> GetChildren();
    }
}
