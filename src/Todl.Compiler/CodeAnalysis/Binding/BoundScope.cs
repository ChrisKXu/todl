using System.Collections.Generic;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Symbols;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    public sealed class BoundScope
    {
        private readonly ICollection<Symbol> symbols = new HashSet<Symbol>();

        public BoundScope Parent { get; }
        public BoundScopeKind BoundScopeKind { get; }

        private BoundScope(BoundScope parent, BoundScopeKind boundScopeKind)
        {
            this.Parent = parent;
            this.BoundScopeKind = boundScopeKind;
        }

        public VariableSymbol LookupVariable(string name)
            => this.LookupSymbol<VariableSymbol>(name);

        private VariableSymbol LookupLocalVariable(string name)
            => this.LookupLocalSymbol<VariableSymbol>(name);

        public VariableSymbol DeclareVariable(VariableSymbol variable)
        {
            var existingVariable = this.LookupLocalVariable(variable.Name);
            if (existingVariable != null)
            {
                return existingVariable;
            }

            this.symbols.Add(variable);

            return variable;
        }

        public TSymbol LookupSymbol<TSymbol>(string name) where TSymbol : Symbol
        {
            var symbol = LookupLocalSymbol<TSymbol>(name);
            return symbol ?? Parent?.LookupSymbol<TSymbol>(name);
        }

        private TSymbol LookupLocalSymbol<TSymbol>(string name) where TSymbol : Symbol
        {
            return this.symbols.Where(s => s.Name == name).OfType<TSymbol>().FirstOrDefault();
        }

        public BoundScope CreateChildScope(BoundScopeKind boundScopeKind) => new(this, boundScopeKind);

        public static readonly BoundScope GlobalScope = new(null, BoundScopeKind.Global);
    }

    public enum BoundScopeKind
    {
        Global,
        Function,
        BlockStatement
    }
}
