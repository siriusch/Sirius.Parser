using System;

using JetBrains.Annotations;

namespace Sirius.Parser.Semantic {
	public abstract class GrammarAttribute: Attribute {
		protected GrammarAttribute([NotNull] Type grammarKey) {
			this.GrammarKey = grammarKey;
		}

		[NotNull]
		public Type GrammarKey {
			get;
		}
	}
}
