using System;
using System.Diagnostics;

namespace Sirius.Parser.Charset {
	public class CharsetNegate: CharsetNode {
		public CharsetNegate(CharsetNode node) {
			Debug.Assert(node != null, nameof(node) + " != null");
			this.Node = node;
		}

		public CharsetNode Node {
			get;
		}

		public override TResult Visit<TContext, TResult>(ICharsetVisitor<TContext, TResult> visitor, TContext context) {
			return visitor.Negate(this, context);		}
	}
}
