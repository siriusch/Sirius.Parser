namespace Sirius.Parser.Lalr {
	public sealed class ShiftAction: LalrAction {
		public ShiftAction(int newState) {
			this.NewState = newState;
		}

		public override ActionType Type => ActionType.Shift;

		public int NewState {
			get;
		}

		protected override bool EqualsInternal(LalrAction other) {
			return this.NewState == ((ShiftAction)other).NewState;
		}

		protected override int HashCodeInternal() {
			return this.NewState.GetHashCode();
		}

		public override string ToString() {
			return $"s{this.NewState}";
		}
	}
}
