using System;

namespace Sirius.Parser.Charset {
	public class CharsetIntersection: CharsetOperation {
		public CharsetIntersection(CharsetNode leftNode, CharsetNode rightNode): base(leftNode, rightNode) { }

		public override TResult Visit<TContext, TResult>(ICharsetVisitor<TContext, TResult> visitor, TContext context) {
			return visitor.Intersection(this, context);
		}
	}
}
