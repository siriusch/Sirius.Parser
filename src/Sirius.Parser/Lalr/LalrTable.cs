using System;
using System.Collections.Generic;

namespace Sirius.Parser.Lalr {
	public class LalrTable {
		public LalrTable(int startState, IReadOnlyDictionary<StateKey<SymbolId>, LalrAction> action, IReadOnlyList<ProductionRule> productions) {
			this.StartState = startState;
			this.Action = action;
			this.Productions = productions;
		}

		public int StartState {
			get;
		}

		public IReadOnlyDictionary<StateKey<SymbolId>, LalrAction> Action {
			get;
		}

		public IReadOnlyList<ProductionRule> Productions {
			get;
		}
	}
}
