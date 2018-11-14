using System;

namespace Sirius.Parser.Lalr {
	public abstract class LalrAction: IEquatable<LalrAction> {
		public abstract ActionType Type {
			get;
		}

		public bool Equals(LalrAction other) {
			if (ReferenceEquals(other, null)) {
				return false;
			}
			if (ReferenceEquals(other, this)) {
				return true;
			}
			if (other.GetType() != this.GetType()) {
				return false;
			}
			return this.EqualsInternal(other);
		}

		public sealed override bool Equals(object obj) {
			return base.Equals(obj as LalrAction);
		}

		protected virtual bool EqualsInternal(LalrAction other) {
			return true;
		}

		public sealed override int GetHashCode() {
			return this.GetType().GetHashCode()^ this.HashCodeInternal();
		}

		protected virtual int HashCodeInternal() {
			return 0;
		}

		public override string ToString() {
			return this.Type.ToString();
		}
	}
}
