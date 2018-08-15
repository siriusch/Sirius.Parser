namespace Sirius.Parser.Lalr {
	public sealed class ReduceSingleAction: ReduceAction {
		public ReduceSingleAction(ProductionRule productionRule) {
			this.ProductionRule = productionRule;
		}

		public ProductionRule ProductionRule {
			get;
		}

		public override ActionType Type => ActionType.Reduce;

		public override bool IsAmbiguous => false;

		public override ReduceAction AddProductionRule(ProductionRule productionRule) {
			if (productionRule.Equals(this.ProductionRule)) {
				return this;
			}
			return new ReduceMultiAction(this.ProductionRule, productionRule);
		}

		protected override bool EqualsInternal(LalrAction other) {
			return this.ProductionRule == ((ReduceSingleAction)other).ProductionRule;
		}

		protected override int HashCodeInternal() {
			return this.ProductionRule.GetHashCode();
		}

		public override string ToString() {
			return $"r{this.ProductionRule}";
		}
	}
}
