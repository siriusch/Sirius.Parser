namespace Sirius.Parser.Lalr {
	public abstract class ReduceAction: LalrAction {
		public abstract bool IsAmbiguous {
			get;
		}

		public abstract ReduceAction AddProductionRule(ProductionRule productionRule);
	}
}