using System;
using System.Collections.Generic;

namespace Sirius.Parser.Lalr {
	public class LrItemSetCollection: List<LrItemSet> {
		public LrItemSet StartState {
			get;
			set;
		}

		public void RemoveNonkernels() {
			for (var i = 0; i < this.Count; i++) {
				this[i].RemoveNonkernels();
			}
		}
	}
}
