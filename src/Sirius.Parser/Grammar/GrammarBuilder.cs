using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Sirius.Parser.Grammar {
	public class GrammarBuilder: IGrammarData, IEnumerable<Production> {
		private readonly Dictionary<SymbolId, Production> productions = new Dictionary<SymbolId, Production>();

		public GrammarBuilder(SymbolId unknown, SymbolId init, SymbolId start, IReadOnlyDictionary<SymbolId, SymbolKind> allSymbols = null) {
			this.Unknown = unknown;
			this.Init = init;
			this.Start = start;
			this.AllSymbols = allSymbols;
		}

		public ICollection<Production> Productions => this.productions.Values;

		private IReadOnlyDictionary<SymbolId, SymbolKind> AllSymbols {
			get;
		}

		IEnumerator<Production> IEnumerable<Production>.GetEnumerator() {
			return this.productions.Values.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return this.productions.Values.GetEnumerator();
		}

		public SymbolId Unknown {
			get;
			set;
		}

		public SymbolId Init {
			get;
			set;
		}

		public SymbolId Start {
			get;
			set;
		}

		public IEnumerable<KeyValuePair<SymbolId, SymbolKind>> Symbols => this.AllSymbols ?? ComputeSymbols();
		IEnumerable<KeyValuePair<SymbolId, SymbolIdSequence>> IGrammarData.Productions => this.productions.Values.SelectMany(p => p.Rules.Select(r => new KeyValuePair<SymbolId, SymbolIdSequence>(p.ProductionSymbolId, r)));

		public void Add(SymbolId productionSymbolId, params SymbolId[] ruleSymbolIds) {
			DefineProduction(productionSymbolId).Rules.Add(new SymbolIdSequence(ruleSymbolIds));
		}

		private IEnumerable<KeyValuePair<SymbolId, SymbolKind>> ComputeSymbols() {
			var allSymbols = new HashSet<SymbolId> {
					SymbolId.Eof,
					this.Unknown,
					this.Init,
					this.Start
			};
			var productionSymbols = new HashSet<SymbolId> {
					this.Init,
					this.Start
			};
			foreach (var production in this.Productions) {
				allSymbols.Add(production.ProductionSymbolId);
				allSymbols.UnionWith(production.Rules.SelectMany(r => r));
				productionSymbols.Add(production.ProductionSymbolId);
			}
			return allSymbols.Select(s => new KeyValuePair<SymbolId, SymbolKind>(s, productionSymbols.Contains(s) ? SymbolKind.Nonterminal : SymbolKind.Terminal));
		}

		public Production DefineProduction(SymbolId productionSymbolId) {
			Production production;
			if (!this.productions.TryGetValue(productionSymbolId, out production)) {
				production = new Production(productionSymbolId);
				this.productions.Add(productionSymbolId, production);
			}
			return production;
		}
	}
}
