namespace Todl.Compiler.CodeAnalysis.Symbols
{
    public sealed class VariableSymbol : Symbol
    {
        internal VariableSymbol(
            string name,
            bool readOnly,
            TypeSymbol type)
        {
            this.Name = name;
            this.ReadOnly = readOnly;
            this.Type = type;
        }

        public override string Name { get; }
        public bool ReadOnly { get; }
        public TypeSymbol Type { get; }

        public override bool Equals(Symbol other)
        {
            if (other is VariableSymbol variableSymbol)
            {
                return this.Name == variableSymbol.Name
                    && this.Type.Equals(variableSymbol.Type);
            }

            return false;
        }
    }
}
