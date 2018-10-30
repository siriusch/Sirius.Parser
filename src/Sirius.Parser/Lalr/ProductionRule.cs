using System;
using System.Linq;

using Sirius.Parser.Grammar;

namespace Sirius.Parser.Lalr {
	public sealed class ProductionRule: IEquatable<ProductionRule> {
		public ProductionRule(int index, SymbolId productionSymbolId, SymbolIdSequence ruleSymbolIds) {
			this.Index = index;
			this.ProductionSymbolId = productionSymbolId;
			this.RuleSymbolIds = ruleSymbolIds;
		}

		public ProductionKey Key => new ProductionKey(ProductionSymbolId, RuleSymbolIds);

		public SymbolId ProductionSymbolId {
			get;
		}

		public SymbolIdSequence RuleSymbolIds {
			get;
		}

		public int Index {
			get;
		}

		public bool Equals(ProductionRule other) {
			if (ReferenceEquals(null, other)) {
				return false;
			}
			if (ReferenceEquals(this, other)) {
				return true;
			}
			return this.Index == other.Index;
		}

		public override bool Equals(object obj) {
			return Equals(obj as ProductionRule);
		}

		public override int GetHashCode() {
			return this.Index;
		}

		public override string ToString() {
			return ToString(s => s.ToString());
		}

		public string ToString(Func<SymbolId, string> resolver) {
			return $"{this.Index}: {resolver(this.ProductionSymbolId)} ::= {string.Join(" ", this.RuleSymbolIds.Select(resolver))}";
		}
	}
}
