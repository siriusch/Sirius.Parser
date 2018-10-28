using System;

namespace Sirius.Parser.Charset {
	public class CharsetUnion: CharsetOperation {
		public CharsetUnion(CharsetNode leftNode, CharsetNode rightNode): base(leftNode, rightNode) { }

		public override TResult Visit<TContext, TResult>(ICharsetVisitor<TContext, TResult> visitor, TContext context) {
			return visitor.Union(this, context);
		}
	}
}
