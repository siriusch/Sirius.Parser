using System;

using Sirius.Parser.Grammar;

namespace Sirius.Parser.Lalr {
	internal struct ProductionKey: IEquatable<ProductionKey> {
		public ProductionKey(SymbolId productionSymbolId, SymbolIdSequence ruleSymbolIds) {
			this.ProductionSymbolId = productionSymbolId;
			this.RuleSymbolIds = ruleSymbolIds;
		}

		public SymbolId ProductionSymbolId {
			get;
		}

		public SymbolIdSequence RuleSymbolIds {
			get;
		}

		public bool Equals(ProductionKey other) {
			return this.ProductionSymbolId.Equals(other.ProductionSymbolId) && this.RuleSymbolIds.Equals(other.RuleSymbolIds);
		}

		public override bool Equals(object obj) {
			if (ReferenceEquals(null, obj)) {
				return false;
			}
			return obj is ProductionKey && Equals((ProductionKey)obj);
		}

		public override int GetHashCode() {
			unchecked {
				return (this.ProductionSymbolId.GetHashCode() * 397)^this.RuleSymbolIds.GetHashCode();
			}
		}
	}
}