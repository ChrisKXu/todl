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
        #region Built-in types

        public static readonly TypeSymbol ClrObject = ClrTypeSymbol.MapClrType<object>(); // any
        public static readonly TypeSymbol ClrBoolean = ClrTypeSymbol.MapClrType<bool>(); // bool
        public static readonly TypeSymbol ClrByte = ClrTypeSymbol.MapClrType<byte>(); // byte
        public static readonly TypeSymbol ClrInt32 = ClrTypeSymbol.MapClrType<int>(); // int
        public static readonly TypeSymbol ClrInt64 = ClrTypeSymbol.MapClrType<long>(); // long
        public static readonly TypeSymbol ClrDouble = ClrTypeSymbol.MapClrType<double>(); // double
        public static readonly TypeSymbol ClrChar = ClrTypeSymbol.MapClrType<char>(); // char
        public static readonly TypeSymbol ClrString = ClrTypeSymbol.MapClrType<string>(); // string
        public static readonly TypeSymbol ClrVoid = ClrTypeSymbol.MapClrType(typeof(void)); // void

        #endregion

        /// <summary>
        /// Gets if a type is a native CLR type
        /// </summary>
        public virtual bool IsNative => false;
    }
}
