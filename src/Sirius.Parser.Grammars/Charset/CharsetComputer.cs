using System;
using System.Collections.Generic;

using Sirius.Collections;
using Sirius.RegularExpressions.Parser;

namespace Sirius.Parser.Charset {
	public sealed class CharsetComputer<TChar>: ICharsetVisitor<IRangeSetProvider<TChar>, RangeSet<TChar>>
			where TChar: IComparable<TChar> {
		RangeSet<TChar> ICharsetVisitor<IRangeSetProvider<TChar>, RangeSet<TChar>>.Difference(CharsetDifference node, IRangeSetProvider<TChar> context) {
			return RangeSet<TChar>.Difference(node.LeftNode.Visit(this, context), node.RightNode.Visit(this, context));
		}

		RangeSet<TChar> ICharsetVisitor<IRangeSetProvider<TChar>, RangeSet<TChar>>.Handle(CharsetHandle node, IRangeSetProvider<TChar> context) {
			var ranges = node.Handle.GetCharSet(context);
			return node.Handle.Negate ? RangeSet<TChar>.Negate(ranges) : ranges;
		}

		RangeSet<TChar> ICharsetVisitor<IRangeSetProvider<TChar>, RangeSet<TChar>>.Intersection(CharsetIntersection node, IRangeSetProvider<TChar> context) {
			return RangeSet<TChar>.Intersection(node.LeftNode.Visit(this, context), node.RightNode.Visit(this, context));
		}

		RangeSet<TChar> ICharsetVisitor<IRangeSetProvider<TChar>, RangeSet<TChar>>.Negate(CharsetNegate node, IRangeSetProvider<TChar> context) {
			return RangeSet<TChar>.Negate(node.Node.Visit(this, context));
		}

		RangeSet<TChar> ICharsetVisitor<IRangeSetProvider<TChar>, RangeSet<TChar>>.Subtract(CharsetSubtract node, IRangeSetProvider<TChar> context) {
			return RangeSet<TChar>.Subtract(node.LeftNode.Visit(this, context), node.RightNode.Visit(this, context));
		}

		RangeSet<TChar> ICharsetVisitor<IRangeSetProvider<TChar>, RangeSet<TChar>>.Union(CharsetUnion node, IRangeSetProvider<TChar> context) {
			return RangeSet<TChar>.Union(node.LeftNode.Visit(this, context), node.RightNode.Visit(this, context));
		}
	}
}
