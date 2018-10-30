using System;

using JetBrains.Annotations;

namespace Sirius.Parser.Semantic {
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
	public sealed class StartSymbolAttribute: GrammarAttribute {
		public StartSymbolAttribute([NotNull] Type grammarKey,
				[NotNull] [LocalizationRequired(false)]
				string symbolName): base(grammarKey) {
			this.SymbolName = symbolName;
		}

		public string SymbolName {
			get;
		}

		public override string ToString() {
			return $"Start Symbol: {this.SymbolName}";
		}
	}
}
