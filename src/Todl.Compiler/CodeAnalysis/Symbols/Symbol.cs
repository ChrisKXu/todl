using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Todl.Compiler.CodeAnalysis.Symbols
{
    public abstract class Symbol : IEquatable<Symbol>
    {
        public abstract bool Equals(Symbol other);

        public virtual string Name { get; }
    }
}
