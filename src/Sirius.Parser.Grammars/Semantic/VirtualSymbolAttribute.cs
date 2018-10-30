using System;

using JetBrains.Annotations;

using Sirius.Parser.Grammar;

namespace Sirius.Parser.Semantic {
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
	public sealed class VirtualSymbolAttribute: GrammarSymbolAttribute {
		public VirtualSymbolAttribute([NotNull] Type grammarKey,
				[NotNull] [LocalizationRequired(false)]
				string symbolName): base(grammarKey, symbolName) { }

		public override SymbolKind SymbolKind => SymbolKind.Terminal;
	}
}
