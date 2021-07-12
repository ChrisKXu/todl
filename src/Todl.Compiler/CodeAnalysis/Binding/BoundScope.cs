using System.Collections.Generic;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Symbols;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    public sealed class BoundScope
    {
        private readonly BoundScope parent;
        private readonly ICollection<Symbol> symbols = new HashSet<Symbol>();

        public BoundScopeKind BoundScopeKind { get; }

        private BoundScope(BoundScope parent, BoundScopeKind boundScopeKind)
        {
            this.parent = parent;
            this.BoundScopeKind = boundScopeKind;
        }

        public VariableSymbol LookupVariable(string name)
            => this.LookupSymbol<VariableSymbol>(name);

        public VariableSymbol DeclareVariable(VariableSymbol variable)
        {
            var existingVariable = this.LookupVariable(variable.Name);
            if (existingVariable != null)
            {
                return existingVariable;
            }

            this.symbols.Add(variable);

            return variable;
        }

        public TSymbol LookupSymbol<TSymbol>(string name) where TSymbol : Symbol
        {
            var symbol = this.symbols.Where(s => s.Name == name).OfType<TSymbol>().FirstOrDefault();
            return symbol ?? parent?.LookupSymbol<TSymbol>(name);
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
