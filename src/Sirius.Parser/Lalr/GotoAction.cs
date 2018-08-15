using System;

namespace Sirius.Parser.Lalr {
	public sealed class GotoAction: LalrAction {
		public GotoAction(int newState) {
			this.NewState = newState;
		}

		public override ActionType Type => ActionType.Goto;

		public int NewState {
			get;
		}

		protected override bool EqualsInternal(LalrAction other) {
			return this.NewState == ((GotoAction)other).NewState;
		}

		protected override int HashCodeInternal() {
			return this.NewState.GetHashCode();
		}

		public override string ToString() {
			return $"g{this.NewState}";
		}
	}
}
