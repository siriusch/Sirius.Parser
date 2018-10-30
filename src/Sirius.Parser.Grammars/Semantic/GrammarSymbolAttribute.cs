using System;

using JetBrains.Annotations;

using Sirius.Parser.Grammar;

namespace Sirius.Parser.Semantic {
	public abstract class GrammarSymbolAttribute: GrammarAttribute {
		private Type[] genericTypeParameters;

		protected GrammarSymbolAttribute([NotNull] Type grammarKey,
				[NotNull] [LocalizationRequired(false)]
				string symbolName): base(grammarKey) {
			this.SymbolName = symbolName;
		}

		[NotNull]
		public string SymbolName {
			get;
		}

		public abstract SymbolKind SymbolKind {
			get;
		}

		public Type GenericTypeParameter {
			get {
				if (this.genericTypeParameters == null) {
					return null;
				}
				switch (this.genericTypeParameters.Length) {
				case 0:
					return null;
				case 1:
					return this.genericTypeParameters[0];
				default:
					throw new InvalidOperationException("Multiple generic type arguments");
				}
			}
			set {
				this.genericTypeParameters = value == null ? null : new[] {value};
			}
		}

		[NotNull]
		public Type[] GenericTypeParameters {
			get => this.genericTypeParameters ?? Type.EmptyTypes;
			set {
				this.genericTypeParameters = value;
			}
		}
	}
}
