using System;

namespace Sirius.Parser.Charset {
	public abstract class CharsetNode {
		public abstract TResult Visit<TContext, TResult>(ICharsetVisitor<TContext, TResult> visitor, TContext context);
	}
}
