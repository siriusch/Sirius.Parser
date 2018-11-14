using System;
using System.Collections.Generic;
using System.Linq;

using Sirius.Collections;
using Sirius.Parser.Grammar;

namespace Sirius.Parser.Lalr {
	public class LalrTableGenerator {
		private static IEnumerable<SymbolId> FirstSymbols(IEnumerable<ProductionRule> productionRules) {
			return productionRules.Where(r => r.RuleSymbolIds.Count > 0).Select(r => r.RuleSymbolIds[0]).Distinct();
		}

		private static IEnumerable<SymbolId> MarkedSymbols(IEnumerable<LrItem> items) {
			return items.Where(i => i.Marker < i.Rule.RuleSymbolIds.Count).Select(i => i.Rule.RuleSymbolIds[i.Marker]);
		}

		private readonly IReadOnlyDictionary<SymbolId, SymbolKind> symbols;

		/// <summary>
		/// The constructor transfers all normalized data from the <see cref="IGrammarData" /> to the table generator, but does not yet compute any table-related data
		/// </summary>
		/// <param name="data">The data describing the grammar</param>
		public LalrTableGenerator(IGrammarData data): base() {
			this.symbols = data.Symbols.ToDictionary();
			this.Unknown = this.AssertTerminal(data.Unknown, nameof(this.Unknown));
			this.Eof = this.AssertTerminal(SymbolId.Eof, nameof(this.Eof));
			this.Init = this.AssertNonterminal(data.Init, nameof(this.Init));
			this.Start = this.AssertNonterminal(data.Start, nameof(this.Start));
			var productionKeys = new HashSet<ProductionKey>();
			var productionRules = new List<ProductionRule>();
			var productionRulesBySymbol = new Dictionary<SymbolId, List<ProductionRule>>();
			var addProduction = new Action<SymbolId, SymbolIdSequence>((productionSymbolId, ruleSymbolIds) => {
				if (productionKeys.Add(new ProductionKey(productionSymbolId, ruleSymbolIds))) {
					var productionRule = new ProductionRule(productionRules.Count, productionSymbolId, ruleSymbolIds);
					productionRules.Add(productionRule);
					if (!productionRulesBySymbol.TryGetValue(productionSymbolId, out var productions)) {
						productions = new List<ProductionRule>();
						productionRulesBySymbol.Add(productionSymbolId, productions);
					}
					productions.Add(productionRule);
				}
			});
			addProduction(this.Init, new SymbolIdSequence(this.Start.Yield()));
			foreach (var production in data.Productions.Where(p => p.Value != null)) {
				addProduction(production.Key, production.Value);
			}
			this.ProductionRules = productionRules;
			this.ProductionRulesBySymbol = productionRulesBySymbol.ToDictionary(p => p.Key, p => (IReadOnlyCollection<ProductionRule>)p.Value);
		}

		public IReadOnlyList<ProductionRule> ProductionRules {
			get;
		}

		public IReadOnlyDictionary<SymbolId, IReadOnlyCollection<ProductionRule>> ProductionRulesBySymbol {
			get;
		}

		public SymbolId Unknown {
			get;
		}

		public SymbolId Eof {
			get;
		}

		public SymbolId Init {
			get;
		}

		public SymbolId Start {
			get;
		}

		private SymbolId AssertNonterminal(SymbolId symbolId, string name) {
			if (this.symbols[symbolId] != SymbolKind.Nonterminal) {
				throw new InvalidOperationException("The '"+name+"' symbol must be a non-terminal");
			}
			return symbolId;
		}

		private SymbolId AssertTerminal(SymbolId symbolId, string name) {
			if (this.symbols[symbolId] != SymbolKind.Terminal) {
				throw new InvalidOperationException("The '"+name+"' symbol must be a terminal");
			}
			return symbolId;
		}

		private Dictionary<StateKey<SymbolId>, int> ComputeGotoLookup(SymbolMetaDictionary symbolMeta, LrItemSetCollection itemSets) {
			var gotos = new Dictionary<StateKey<SymbolId>, int>();
			var itemSetByKernels = itemSets.ToDictionary(s => s.Kernels.ToArray(), s => s, SetEqualityComparer<LrItem>.Default);
			foreach (var itemSet in itemSets) {
				foreach (var sym in symbolMeta.Keys) {
					// Compute dynamic GOTO
					var gotoClosure = this.Lr1Closure(symbolMeta, new LrItemSet(itemSet
						.Where(item => (item.Marker < item.Length) && item.Rule.RuleSymbolIds[item.Marker].Equals(sym))
						.Select(item => new LrItem(item.Rule, item.Marker+1, true))));
					if (!gotoClosure.Any()) {
						continue;
					}
					var key = new StateKey<SymbolId>(itemSet.Index, sym);
					if (gotos.ContainsKey(key)) {
						continue;
					}
					// Match the dynamic GOTO to an actual state
					if (itemSetByKernels.TryGetValue(gotoClosure.Kernels, out var existingGoto)) {
						gotos.Add(key, existingGoto.Index);
					}
				}
			}
			return gotos;
		}

		private LrItemSetCollection ComputeItemSets(SymbolMetaDictionary symbolMeta, out int acceptIndex) {
			this.ComputeLr0ItemSetKernelsAndGotoLookup(out var itemSets, out var gotoSymbol, out acceptIndex);
			var dummyLookahead = new[] { this.Unknown };
			itemSets.StartState.Single().LookaheadIds.Add(this.Eof);
			var rulePropagations = new Dictionary<StateKey<LrItem>, HashSet<StateKey<LrItem>>>();
			foreach (var itemSet in itemSets) {
				var gotosForState = gotoSymbol[itemSet.Index];
				foreach (var kernelItem in itemSet) {
					var itemKey = new StateKey<LrItem>(itemSet.Index, kernelItem);
					// Create an item set with a dummy lookahead, based on the current item set
					var dummyItem = new LrItem(kernelItem.Rule, kernelItem.Marker, kernelItem.Marker > 0, dummyLookahead);
					var dummyItemSet = new LrItemSet(dummyItem.Yield());
					var j = this.Lr1Closure(symbolMeta, dummyItemSet);
					foreach (var gotoForState in gotosForState) {
						var gotoItemSet = itemSets[gotoForState.Value];
						var gotoItemLookup = gotoItemSet.ToDictionary(g => g, g => g);
						// Find the items in the dummy set with the marker before this symbol
						foreach (var b in j.Where(bb => (bb.Marker < bb.Length) && bb.Rule.RuleSymbolIds[bb.Marker].Equals(gotoForState.Key))) {
							// Get the item corresponding to the goto state with the marker past the current symbol
							var gotoItem = gotoItemLookup[new LrItem(b.Rule, b.Marker+1, true)];
							if (b.LookaheadIds.Any(l => l == this.Unknown)) {
								if (!rulePropagations.ContainsKey(itemKey)) {
									rulePropagations[itemKey] = new HashSet<StateKey<LrItem>>();
								}
								rulePropagations[itemKey].Add(new StateKey<LrItem>(gotoItemSet.Index, gotoItem));
							}
							gotoItem.LookaheadIds.UnionWith(b.LookaheadIds.Where(l => l != this.Unknown));
						}
					}
				}
			}
			bool changed;
			do {
				changed = false;
				foreach (var itemSet in itemSets) {
					foreach (var item in itemSet) {
						var itemKey = new StateKey<LrItem>(itemSet.Index, item);
						if (!rulePropagations.TryGetValue(itemKey, out var propagated)) {
							continue;
						}
						foreach (var key in propagated) {
							if (key.Value.LookaheadIds.AddRange(item.LookaheadIds)) {
								changed = true;
							}
						}
					}
				}
			} while (changed);
			// Close all kernels
			for (var i = 0; i < itemSets.Count; i++) {
				itemSets[i] = this.Lr1Closure(symbolMeta, itemSets[i]);
				itemSets[i].Index = i;
			}
			return itemSets;
		}

		private void ComputeLr0ItemSetKernelsAndGotoLookup(out LrItemSetCollection itemSets, out Dictionary<int, Dictionary<SymbolId, int>> gotos, out int acceptIndex) {
			itemSets = new LrItemSetCollection();
			gotos = new Dictionary<int, Dictionary<SymbolId, int>>();
			var itemSetByKernels = new Dictionary<IEnumerable<LrItem>, LrItemSet>(SetEqualityComparer<LrItem>.Default);
			var queue = new Queue<LrItemSet>();
			var startItem = new LrItemSet(new LrItem(this.ProductionRulesBySymbol[this.Init].Single(), 0, true));
			this.Lr0ComputeClosureNonterminals(startItem);
			itemSets.StartState = startItem;
			acceptIndex = this.ProductionRulesBySymbol[this.Init].Single().Index;
			startItem.Index = itemSets.Count;
			itemSets.Add(startItem);
			itemSetByKernels.Add(startItem.Kernels.ToArray(), startItem);
			queue.Enqueue(startItem);
			while (queue.Count > 0) {
				var itemSet = queue.Dequeue();
				var gotoLookup = this.Lr0GotoKernels(itemSet);
				var gotosForState = new Dictionary<SymbolId, int>();
				gotos.Add(itemSet.Index, gotosForState);
				foreach (var symbol in gotoLookup.Keys) {
					if (!itemSetByKernels.TryGetValue(gotoLookup[symbol].Kernels, out var gotoState)) {
						gotoState = gotoLookup[symbol];
						this.Lr0ComputeClosureNonterminals(gotoState);
						gotoState.Index = itemSets.Count;
						itemSets.Add(gotoState);
						itemSetByKernels.Add(gotoState.Kernels.ToArray(), gotoState);
						queue.Enqueue(gotoState);
					}
					gotosForState.Add(symbol, gotoState.Index);
				}
			}
		}

		public LalrTable ComputeTable() {
			var symbolMeta = new SymbolMetaDictionary(this.symbols);
			symbolMeta.ComputeFirstFollowsAndNullable(this.Start, this.Eof, this.ProductionRules);
			var states = this.ComputeItemSets(symbolMeta, out var acceptIndex);
			var gotoLookup = this.ComputeGotoLookup(symbolMeta, states);
			var actionTable = new Dictionary<StateKey<SymbolId>, LalrAction>();
			foreach (var state in states) {
				foreach (var sym in this.symbols) {
					var key = new StateKey<SymbolId>(state.Index, sym.Key);
					if (sym.Value == SymbolKind.Terminal) {
						foreach (var item in state) {
							if ((item.Marker < item.Length) && (item.Rule.RuleSymbolIds[item.Marker] == sym.Key)) {
								if (gotoLookup.TryGetValue(key, out var gotoState)) {
									actionTable[key] = new ShiftAction(gotoState);
								}
							} else if (item.Length == item.Marker) {
								if (item.Rule.Index == acceptIndex) {
									if (sym.Key == this.Eof) {
										actionTable[key] = new AcceptAction();
									}
								} else if (item.LookaheadIds.Contains(sym.Key) && (item.Rule.ProductionSymbolId != this.Init)) {
									if (actionTable.TryGetValue(key, out var action)) {
										// Reduce-reduce conflict - plain LALR parser will fail, GLR parsers will try both rules in parallel
										actionTable[key] = ((ReduceAction)action).AddProductionRule(item.Rule);
									} else {
										actionTable[key] = new ReduceSingleAction(item.Rule);
									}
								}
							}
						}
					} else {
						if (gotoLookup.TryGetValue(key, out var gotoState)) {
							actionTable[key] = new GotoAction(gotoState);
						}
					}
				}
			}
			return new LalrTable(states.StartState.Index, actionTable, this.ProductionRules);
		}

		private bool IsNonterminal(SymbolId id) {
			return this.symbols[id] == SymbolKind.Nonterminal;
		}

		private void Lr0ComputeClosureNonterminals(LrItemSet itemSet) {
			if (itemSet.ClosureProductions != null) {
				return;
			}
			// Initialize the set with the next symbol at each marker
			var nonterminalSet = new HashSet<SymbolId>(MarkedSymbols(itemSet).Where(this.IsNonterminal));
			var queue = new Queue<SymbolId>(nonterminalSet);
			while (queue.Count > 0) {
				var current = queue.Dequeue();
				// Get the first nonterminal from rules starting with these
				foreach (var nonterminal in FirstSymbols(this.ProductionRulesBySymbol[current]).Where(this.IsNonterminal).Distinct()) {
					if (nonterminalSet.Add(nonterminal)) {
						// New production, add it to the closure
						queue.Enqueue(nonterminal);
					}
				}
			}
			itemSet.ClosureProductions = nonterminalSet;
		}

		/// <summary>
		/// Time- and space-optimized goto which returns gotos for all relevant symbols, storing kernels only.
		/// For this function, the input state must have its closure production heads computed (with LR0ComputeClosureNonterminals).
		/// The output states do not have closure productions.
		/// </summary>
		private Dictionary<SymbolId, LrItemSet> Lr0GotoKernels(LrItemSet itemSet) {
			if (itemSet.ClosureProductions == null) {
				throw new InvalidOperationException();
			}
			// Create new items by advancing the marker for the input kernels
			var kernelItems = itemSet
				.Kernels
				.Where(k => k.Marker < k.Length)
				.Select(k => new KeyValuePair<SymbolId, LrItem>(k.Rule.RuleSymbolIds[k.Marker], new LrItem(k.Rule, k.Marker+1, true)));
			// Create new items from the nonkernels (using closure production heads)
			// Since goto is to be computed for ALL symbols, advance the marker for all nonkernels
			// The first item in the rule is the goto symbol, and the marker is advanced past the first item
			var nonKernelItems = itemSet
				.ClosureProductions
				.SelectMany(n => this.ProductionRulesBySymbol[n])
				.Where(r => r.RuleSymbolIds.Count > 0)
				.Select(r => new KeyValuePair<SymbolId, LrItem>(r.RuleSymbolIds[0], new LrItem(r, 1, true)));
			// The dictionary will contain item sets for all possible goto symbols X, Y, and Z for the given input state
			return kernelItems
				.Concat(nonKernelItems)
				.GroupBy(t => t.Key, t => t.Value)
				.ToDictionary(g => g.Key, g => new LrItemSet(g));
		}

		/// <summary>
		/// Compute the LR(1) closure including lookaheads in the item sets (page 261 of Compilers 2nd Ed.)
		/// </summary>
		private LrItemSet Lr1Closure(SymbolMetaDictionary symbolMeta, LrItemSet items) {
			// Initialize the return set to the item set
			var newset = new LrItemSet(items) {
				IsClosed = true
			};
			var toAdd = new List<LrItem>();
			var newLookaheads = new HashSet<SymbolId>();
			do {
				toAdd.Clear();
				// For each item in the set with a marker before a nonterminal
				foreach (var item in newset.Where(i => (i.Marker < i.Length) && (this.symbols[i.Rule.RuleSymbolIds[i.Marker]] == SymbolKind.Nonterminal))) {
					var nonterminal = item.Rule.RuleSymbolIds[item.Marker];
					// Get all the possible lookaheads past this symbol
					newLookaheads.Clear();
					foreach (var lookahead in item.LookaheadIds) {
						var followingSymbols = item.Rule.RuleSymbolIds.Skip(item.Marker+1).Append(lookahead);
						newLookaheads.UnionWith(symbolMeta.FirstOfAll(followingSymbols));
					}
					if (newLookaheads.Any()) {
						// For each rule of the production past the marker for this item
						toAdd.AddRange(this.ProductionRulesBySymbol[nonterminal].Select(rule => new LrItem(rule, 0, false, newLookaheads)));
					}
				}
			} while ((toAdd.Count > 0) && newset.MergeWith(toAdd));
			return newset;
		}
	}
}
