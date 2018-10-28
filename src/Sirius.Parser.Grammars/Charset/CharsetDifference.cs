using System;

namespace Sirius.Parser.Charset {
	public class CharsetDifference: CharsetOperation {
		public CharsetDifference(CharsetNode leftNode, CharsetNode rightNode): base(leftNode, rightNode) { }

		public override TResult Visit<TContext, TResult>(ICharsetVisitor<TContext, TResult> visitor, TContext context) {
			return visitor.Difference(this, context);
		}
	}
}
