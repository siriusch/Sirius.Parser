using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using Sirius.Collections;
using Sirius.Parser.Grammar;

namespace Sirius.Parser.Lalr {
	internal class SymbolMetaDictionary: ReadOnlyDictionary<SymbolId, SymbolMeta> {
		public SymbolMetaDictionary(IEnumerable<KeyValuePair<SymbolId, SymbolKind>> symbols): base(symbols.ToDictionary(p => p.Key, p => new SymbolMeta(p.Key, p.Value))) { }

		public bool IsNullable(SymbolId symbol) {
			return this[symbol].Nullable;
		}

		public bool SetNullable(SymbolId symbol) {
			var symbolMeta = this[symbol];
			if (symbolMeta.Nullable) {
				return false;
			}
			symbolMeta.Nullable = true;
			return true;
		}

		/// <summary>
		/// Takes a string of symbols, and computes a new FIRST set (all terminals that could appear first in a derivation of the given string of symbols)
		/// </summary>
		public IEnumerable<SymbolId> FirstOfAll(IEnumerable<SymbolId> symbolIds) {
			using (var enumerator = symbolIds.GetEnumerator()) {
				if (!enumerator.MoveNext()) {
					return Enumerable.Empty<SymbolId>();
				}
				var result = new HashSet<SymbolId>();
				do {
					result.UnionWith(this[enumerator.Current].FirstSet);
				} while (IsNullable(enumerator.Current) && enumerator.MoveNext());
				return result;
			}
		}

		public void ComputeFirstFollowsAndNullable(SymbolId start, SymbolId eof, IEnumerable<ProductionRule> rules) {
			// Initialize FIRST and FOLLOW sets, and Nullable which is used for computation
			// Set the FIRST sets of terminals
			foreach (var p in this.Where(p => p.Value.Kind == SymbolKind.Terminal)) {
				p.Value.FirstSet.Add(p.Key);
			}
			// Add Eof to FOLLOWS[Start]
			this[start].FollowSet.Add(eof);
			// Compute FIRST and FOLLOW sets iteratively, repeating until they are stable
			bool changed;
			do {
				changed = false;
				foreach (var p in rules) {
					var productionSymbol = p.ProductionSymbolId;
					var ruleSymbols = p.RuleSymbolIds;
					// If all rule symbols are nullable (or rule is empty) => Nullable[productionSymbol] = true
					if (ruleSymbols.All(IsNullable)) {
						if (SetNullable(productionSymbol)) {
							changed = true;
						}
					}
					for (var curr = 0; curr < ruleSymbols.Count; curr++) {
						var next = curr + 1;
						if (ruleSymbols.Take(curr).All(IsNullable)) {
							if (this[productionSymbol].FirstSet.AddRange(this[ruleSymbols[curr]].FirstSet)) {
								changed = true;
							}
						}
						if (ruleSymbols.Skip(next).All(IsNullable)) {
							if (this[ruleSymbols[curr]].FollowSet.AddRange(this[productionSymbol].FollowSet)) {
								changed = true;
							}
						}
						// If everything inbetween next and i is nullable then set Follow[ruleSymbol[curr]] += First[ruleSymbol[i]]
						for (var i = next; i < ruleSymbols.Count; i++) {
							if (!ruleSymbols.Skip(next).Take(i - next).All(IsNullable)) {
								continue;
							}
							if (this[ruleSymbols[curr]].FollowSet.AddRange(this[ruleSymbols[i]].FirstSet)) {
								changed = true;
							}
						}
					}
				}
			} while (changed);
		}
	}
}