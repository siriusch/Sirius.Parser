using System;

using JetBrains.Annotations;

namespace Sirius.Parser.Semantic {
	[AttributeUsage(AttributeTargets.Parameter)]
	public sealed class RuleSymbolAttribute: Attribute {
		public RuleSymbolAttribute(
				[NotNull] [LocalizationRequired(false)]
				string symbolName) {
			this.SymbolName = symbolName;
			this.Occurrence = 1;
		}

		public object RuleKey {
			get;
			set;
		}

		[NotNull]
		public string SymbolName {
			get;
		}

		public int Occurrence {
			get;
			set;
		}
	}
}
