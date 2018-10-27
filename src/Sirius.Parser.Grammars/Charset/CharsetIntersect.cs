using System;

using Sirius.Collections;
using Sirius.RegularExpressions.Parser;

namespace Sirius.Parser.Grammars.Charset {
	public class CharsetIntersect<TChar>: CharsetOperation<TChar>
			where TChar: IComparable<TChar> {
		public CharsetIntersect(CharsetNode<TChar> leftNode, CharsetNode<TChar> rightNode): base(leftNode, rightNode) { }

		public override RangeSet<TChar> Compute(IRangeSetProvider<TChar> provider) {
			return RangeSet<TChar>.Intersection(this.LeftNode.Compute(provider), this.RightNode.Compute(provider));
		}
	}
}