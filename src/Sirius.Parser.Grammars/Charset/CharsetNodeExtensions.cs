using System;
using System.Collections.Generic;
using System.Linq;

using Sirius.Collections;
using Sirius.RegularExpressions.Parser;

namespace Sirius.Parser.Charset {
	public static class CharsetNodeExtensions {
		public static IEnumerable<string> GetCharsetNames(this CharsetNode that) {
			return that.Visit(new CharsetHandleVisitor<string>(), handle => {
				if (handle is RangeSetHandle.Named named) {
					return named.Name.Yield();
				}
				return Enumerable.Empty<string>();
			});
		}

		public static RangeSet<TChar> Compute<TChar>(this CharsetNode that, IRangeSetProvider<TChar> provider)
				where TChar: IComparable<TChar> {
			return that.Visit(new CharsetComputer<TChar>(), provider);
		}
	}
}
