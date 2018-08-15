namespace Sirius.Parser.Lalr {
	public sealed class AcceptAction: LalrAction {
		public override ActionType Type => ActionType.Accept;

		public override string ToString() {
			return "(accept)";
		}
	}
}