using System;

using Sirius.RegularExpressions.Parser;

namespace Sirius.Parser.Charset {
	public class CharsetHandle: CharsetNode {
		public CharsetHandle(RangeSetHandle handle) {
			this.Handle = handle;
		}

		public RangeSetHandle Handle {
			get;
		}

		public override TResult Visit<TContext, TResult>(ICharsetVisitor<TContext, TResult> visitor, TContext context) {
			return visitor.Handle(this, context);
		}
	}
}
