using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Sirius.Parser.Grammar {
	public class Production: IEnumerable<SymbolIdSequence> {
		internal Production(SymbolId productionSymbolId) {
			this.ProductionSymbolId = productionSymbolId;
		}

		public SymbolId ProductionSymbolId {
			get;
		}

		public ICollection<SymbolIdSequence> Rules {
			get;
		} = new HashSet<SymbolIdSequence>();

		IEnumerator<SymbolIdSequence> IEnumerable<SymbolIdSequence>.GetEnumerator() {
			return this.Rules.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return this.Rules.GetEnumerator();
		}

		public SymbolIdSequence Add(IEnumerable<SymbolId> ruleSymbolIds) {
			var symbolIdSequence = new SymbolIdSequence(ruleSymbolIds);
			this.Rules.Add(symbolIdSequence);
			return symbolIdSequence;
		}

		public SymbolIdSequence Add(params SymbolId[] ruleSymbolIds) {
			return Add((IEnumerable<SymbolId>)ruleSymbolIds);
		}

		public override string ToString() {
			return ToString(id => id.ToString());
		}

		public string ToString(Func<SymbolId, string> resolver) {
			return this.Rules.Count == 0
					? $"{resolver(this.ProductionSymbolId)} (no rules)"
					: $"{resolver(this.ProductionSymbolId)} ::= {string.Join(" | ", this.Rules.Select(r => r.ToString(resolver)))}";
		}
	}
}
