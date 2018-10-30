using System;

using JetBrains.Annotations;

namespace Sirius.Parser.Semantic {
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
	public sealed class CharsetAttribute: GrammarAttribute {
		public CharsetAttribute([NotNull] Type grammarKey,
				[NotNull] [LocalizationRequired(false)]
				string charsetName,
				[NotNull] [LocalizationRequired(false)]
				string charsetExpression): base(grammarKey) {
			this.CharsetExpression = charsetExpression;
			this.CharsetName = charsetName;
		}

		[NotNull]
		public string CharsetExpression {
			get;
		}

		[NotNull]
		public string CharsetName {
			get;
		}

		public override string ToString() {
			return $"{{{this.CharsetName}}} = {this.CharsetExpression}";
		}
	}
}
