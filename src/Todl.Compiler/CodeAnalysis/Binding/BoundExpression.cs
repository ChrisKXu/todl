using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Todl.Compiler.CodeAnalysis.Symbols;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    public abstract class BoundExpression
    {
        public abstract TypeSymbol ResultType { get; }
    }
}
