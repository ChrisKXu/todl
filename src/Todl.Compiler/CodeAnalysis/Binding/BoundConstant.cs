using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Todl.Compiler.CodeAnalysis.Symbols;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    public sealed class BoundConstant : BoundExpression
    {
        public override TypeSymbol ResultType { get; }

        public object Value { get; }

        public BoundConstant(TypeSymbol resultType, object value)
        {
            this.ResultType = resultType;
            this.Value = value;
        }
    }
}
