using System;

using Sirius.Collections;
using Sirius.RegularExpressions.Parser;

namespace Sirius.Parser.Grammars.Charset {
	public abstract class CharsetNode<TChar>
			where TChar: IComparable<TChar> {
		public abstract RangeSet<TChar> Compute(IRangeSetProvider<TChar> provider);
	}
}
