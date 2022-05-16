using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Todl.Compiler.CodeAnalysis.Symbols
{
    /// <summary>
    /// Todl supports both types defined in CLR and types defined by the user program.
    /// </summary>
    public abstract class TypeSymbol : Symbol
    {
        /// <summary>
        /// Gets if a type is a native CLR type
        /// </summary>
        public virtual bool IsNative => false;

        public virtual bool IsArray => false;
    }
}
