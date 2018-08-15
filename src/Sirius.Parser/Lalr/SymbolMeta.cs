using System.Collections.Generic;

using Sirius.Parser.Grammar;

namespace Sirius.Parser.Lalr {
	internal class SymbolMeta {
		public SymbolMeta(SymbolId symbol, SymbolKind kind) {
			this.Symbol = symbol;
			this.Kind = kind;
		}

		public SymbolId Symbol {
			get;
		}

		public HashSet<SymbolId> FirstSet {
			get;
		} = new HashSet<SymbolId>();

		public HashSet<SymbolId> FollowSet {
			get;
		} = new HashSet<SymbolId>();

		public bool Nullable {
			get;
			set;
		}

		public SymbolKind Kind {
			get;
		}
	}
}
