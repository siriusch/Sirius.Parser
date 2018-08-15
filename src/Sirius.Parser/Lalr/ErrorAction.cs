namespace Sirius.Parser.Lalr {
	public sealed class ErrorAction: LalrAction {
		public override ActionType Type => ActionType.Error;

		public override string ToString() {
			return "(error)";
		}
	}
}
