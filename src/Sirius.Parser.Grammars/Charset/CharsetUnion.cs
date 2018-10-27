using System;

using Sirius.Collections;
using Sirius.RegularExpressions.Parser;

namespace Sirius.Parser.Grammars.Charset {
	public class CharsetUnion<TChar>: CharsetOperation<TChar>
			where TChar: IComparable<TChar> {
		public CharsetUnion(CharsetNode<TChar> leftNode, CharsetNode<TChar> rightNode): base(leftNode, rightNode) { }

		public override RangeSet<TChar> Compute(IRangeSetProvider<TChar> provider) {
			return RangeSet<TChar>.Union(this.LeftNode.Compute(provider), this.RightNode.Compute(provider));
		}
	}
}