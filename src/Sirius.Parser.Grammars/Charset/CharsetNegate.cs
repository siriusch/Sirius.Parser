using System;
using System.Diagnostics;

using Sirius.Collections;
using Sirius.RegularExpressions.Parser;

namespace Sirius.Parser.Grammars.Charset {
	public class CharsetNegate<TChar>: CharsetNode<TChar>
			where TChar: IComparable<TChar> {
		public CharsetNegate(CharsetNode<TChar> node) {
			Debug.Assert(node != null, nameof(node) + " != null");
			this.Node = node;
		}

		public CharsetNode<TChar> Node {
			get;
		}

		public override RangeSet<TChar> Compute(IRangeSetProvider<TChar> provider) {
			return RangeSet<TChar>.Negate(this.Node.Compute(provider));
		}
	}
}