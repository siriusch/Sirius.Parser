using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Sirius.Collections;

namespace Sirius.Parser.Lalr {
	public sealed class ReduceMultiAction: ReduceAction {
		private readonly HashSet<ProductionRule> productionRules;

		public ReduceMultiAction(params ProductionRule[] productionRules) {
			this.productionRules = new HashSet<ProductionRule>(productionRules);
			Debug.Assert(this.productionRules.Count >= 2);
		}

		public override ActionType Type => ActionType.ReduceMulti;

		public override bool IsAmbiguous => true;

		public override ReduceAction AddProductionRule(ProductionRule productionRule) {
			this.productionRules.Add(productionRule);
			return this;
		}

		protected override bool EqualsInternal(LalrAction other) {
			return SetEqualityComparer<ProductionRule>.Default.Equals(this.productionRules, ((ReduceMultiAction)other).productionRules);
		}

		protected override int HashCodeInternal() {
			return SetEqualityComparer<ProductionRule>.Default.GetHashCode(this.productionRules);
		}

		public override string ToString() {
			return $"r({string.Join(",", this.productionRules.Select(r => r.Index))})";
		}
	}
}
