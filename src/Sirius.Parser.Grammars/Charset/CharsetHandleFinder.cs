using System;
using System.Collections.Generic;
using System.Linq;

using Sirius.Collections;
using Sirius.RegularExpressions.Parser;

namespace Sirius.Parser.Charset {
	public sealed class CharsetHandleVisitor<TResult>: ICharsetVisitor<Func<RangeSetHandle, IEnumerable<TResult>>, IEnumerable<TResult>> {
		IEnumerable<TResult> ICharsetVisitor<Func<RangeSetHandle, IEnumerable<TResult>>, IEnumerable<TResult>>.Handle(CharsetHandle node, Func<RangeSetHandle, IEnumerable<TResult>> context) {
			return context(node.Handle);
		}

		IEnumerable<TResult> ICharsetVisitor<Func<RangeSetHandle, IEnumerable<TResult>>, IEnumerable<TResult>>.Negate(CharsetNegate node, Func<RangeSetHandle, IEnumerable<TResult>> context) {
			return node.Node.Visit(this, context);
		}

		IEnumerable<TResult> ICharsetVisitor<Func<RangeSetHandle, IEnumerable<TResult>>, IEnumerable<TResult>>.Union(CharsetUnion node, Func<RangeSetHandle, IEnumerable<TResult>> context) {
			return node.LeftNode.Visit(this, context).Concat(node.RightNode.Visit(this, context));
		}

		IEnumerable<TResult> ICharsetVisitor<Func<RangeSetHandle, IEnumerable<TResult>>, IEnumerable<TResult>>.Intersection(CharsetIntersection node, Func<RangeSetHandle, IEnumerable<TResult>> context) {
			return node.LeftNode.Visit(this, context).Concat(node.RightNode.Visit(this, context));
		}

		IEnumerable<TResult> ICharsetVisitor<Func<RangeSetHandle, IEnumerable<TResult>>, IEnumerable<TResult>>.Subtract(CharsetSubtract node, Func<RangeSetHandle, IEnumerable<TResult>> context) {
			return node.LeftNode.Visit(this, context).Concat(node.RightNode.Visit(this, context));
		}

		IEnumerable<TResult> ICharsetVisitor<Func<RangeSetHandle, IEnumerable<TResult>>, IEnumerable<TResult>>.Difference(CharsetDifference node, Func<RangeSetHandle, IEnumerable<TResult>> context) {
			return node.LeftNode.Visit(this, context).Concat(node.RightNode.Visit(this, context));
		}
	}
}
