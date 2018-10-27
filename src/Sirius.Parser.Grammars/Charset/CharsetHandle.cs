using System;

using Sirius.Collections;
using Sirius.RegularExpressions.Parser;

namespace Sirius.Parser.Grammars.Charset {
	public class CharsetHandle<TChar>: CharsetNode<TChar>
			where TChar: IComparable<TChar> {
		public CharsetHandle(RangeSetHandle handle) {
			this.Handle = handle;
		}

		public RangeSetHandle Handle {
			get;
		}

		public override RangeSet<TChar> Compute(IRangeSetProvider<TChar> provider) {
			var ranges = this.Handle.GetCharSet(provider);
			return this.Handle.Negate ? provider.Negate(ranges) : ranges;
		}
	}
}
