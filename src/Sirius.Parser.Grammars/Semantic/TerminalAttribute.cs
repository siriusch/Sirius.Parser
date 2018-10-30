using System;
using System.Text.RegularExpressions;

using JetBrains.Annotations;

using Sirius.Parser.Grammar;

namespace Sirius.Parser.Semantic {
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Constructor | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	public sealed class TerminalAttribute: GrammarSymbolAttribute {
		public TerminalAttribute([NotNull]Type grammarKey,
				[NotNull] [LocalizationRequired(false)]
				string symbol): this(grammarKey, symbol, Regex.Escape(symbol).Replace("/", @"\/")) { }

		public TerminalAttribute([NotNull]Type grammarKey,
				[NotNull] [LocalizationRequired(false)]
				string symbolName, [NotNull] [RegexPattern] string regularExpression): base(grammarKey, symbolName) {
			this.RegularExpression = regularExpression;
		}

		[NotNull]
		public string RegularExpression {
			get;
		}

		public bool CaseInsensitive {
			get;
			set;
		}

		public TerminalFlags Flags {
			get;
			set;
		}

		public override string ToString() {
			return $"{this.SymbolName} = /{this.RegularExpression}/{(this.CaseInsensitive ? "i" : "")}";
		}

		public override SymbolKind SymbolKind => SymbolKind.Terminal;
	}
}
