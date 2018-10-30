using System;

using JetBrains.Annotations;

using Sirius.Parser.Grammar;

namespace Sirius.Parser.Semantic {
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Constructor | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	public sealed class RuleAttribute: GrammarSymbolAttribute {
		public RuleAttribute([NotNull] Type grammarKey,
				[NotNull] [LocalizationRequired(false)]
				string symbolName,
				[NotNull] [LocalizationRequired(false)]
				params string[] ruleSymbolNames): base(grammarKey, symbolName) {
			this.RuleSymbolNames = ruleSymbolNames;
		}

		[NotNull]
		public string[] RuleSymbolNames {
			get;
		}

		public string TrimSymbolName {
			get;
			set;
		}

		public object RuleKey {
			get;
			set;
		}

		public override string ToString() {
			return $"{this.SymbolName} ::= {string.Join(" ", this.RuleSymbolNames)}";
		}

		public override SymbolKind SymbolKind => SymbolKind.Nonterminal;
	}
}
