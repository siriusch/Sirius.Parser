using System;
using System.Collections.Generic;
using System.Linq;

using Sirius.Collections;
using Sirius.Parser.Grammar;

namespace Sirius.Parser.Lalr {
	public sealed class LrItemSet: HashSet<LrItem> {
		public LrItemSet(IEnumerable<LrItem> items): base(items) { }

		public LrItemSet(params LrItem[] items): base(items) { }

		public int Index {
			get;
			set;
		} = -1;

		public bool IsClosed {
			get;
			set;
		}

		public HashSet<SymbolId> ClosureProductions {
			get;
			set;
		}

		public IEnumerable<LrItem> Kernels => this.Where(i => i.IsKernel);

		/// <summary>
		///     Merge the given item set by adding items that don't exist yet, and update the lookaheads of existing items
		/// </summary>
		public bool MergeWith(IEnumerable<LrItem> set) {
			var changed = false;
			var ownItemLookup = this.ToDictionary(i => i, i => i);
			foreach (var item in set) {
				if (ownItemLookup.TryGetValue(item, out var ownItem)) {
					if (ownItem.LookaheadIds.AddRange(item.LookaheadIds)) {
						changed = true;
					}
				} else {
					ownItem = new LrItem(item.Rule, item.Marker, item.IsKernel, item.LookaheadIds);
					ownItemLookup.Add(ownItem, ownItem);
					Add(ownItem);
					changed = true;
				}
			}
			return changed;
		}

		public void RemoveNonkernels() {
			this.IsClosed = false;
			RemoveWhere(i => !i.IsKernel); // Remove items with the marker on the left (except start symbol)
			TrimExcess();
		}

		public override string ToString() {
			return ToString(i => i.ToString());
		}

		public string ToString(Func<SymbolId, string> resolver) {
			return string.Join(Environment.NewLine, this.Select(i => i.ToString(resolver)));
		}
	}
}
