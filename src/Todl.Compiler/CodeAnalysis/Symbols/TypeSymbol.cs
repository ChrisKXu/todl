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
    public sealed class TypeSymbol : Symbol
    {
        #region Built-in types

        public static readonly TypeSymbol ClrObject = TypeSymbol.MapClrType<Object>(); // any
        public static readonly TypeSymbol ClrBoolean = TypeSymbol.MapClrType<Boolean>(); // bool
        public static readonly TypeSymbol ClrByte = TypeSymbol.MapClrType<Byte>(); // byte
        public static readonly TypeSymbol ClrInt32 = TypeSymbol.MapClrType<Int32>(); // int
        public static readonly TypeSymbol ClrInt64 = TypeSymbol.MapClrType<Int64>(); // long
        public static readonly TypeSymbol ClrDouble = TypeSymbol.MapClrType<Double>(); // double
        public static readonly TypeSymbol ClrChar = TypeSymbol.MapClrType<Char>(); // char
        public static readonly TypeSymbol ClrString = TypeSymbol.MapClrType<String>(); // string

        #endregion

        /// <summary>
        /// Gets if a type is a native CLR type
        /// </summary>
        public bool IsNative { get; private init; }

        /// <summary>
        /// Gets the native clr type, or null if it's not a type defined by CLR
        /// </summary>
        public Type ClrType { get; private init; }

        public override string Name
        {
            get
            {
                return $"[{ClrType.FullName}]";
            }
        }

        public override bool Equals(Symbol other)
        {
            if (other is TypeSymbol typeSymbol)
            {
                return object.ReferenceEquals(this, typeSymbol);
            }

            return false;
        }

        public static TypeSymbol MapClrType<T>()
        {
            return new TypeSymbol()
            {
                ClrType = typeof(T),
                IsNative = true
            };
        }
    }
}
