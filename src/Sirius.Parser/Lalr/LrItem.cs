using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Sirius.Parser.Grammar;

namespace Sirius.Parser.Lalr {
	public sealed class LrItem: IEquatable<LrItem> {
		private readonly int hash;

		public LrItem(ProductionRule rule, int marker, bool isKernel, IEnumerable<SymbolId> lookaheadIds = null) {
			this.Rule = rule;
			this.Marker = marker;
			this.IsKernel = isKernel;
			this.LookaheadIds = new HashSet<SymbolId>(lookaheadIds ?? Enumerable.Empty<SymbolId>());
			this.hash = rule.GetHashCode()^marker.GetHashCode();
		}

		public ProductionRule Rule {
			get;
		}

		public int Marker {
			get;
		}

		public bool IsKernel {
			get;
		}

		public int Length => this.Rule.RuleSymbolIds.Count;

		public HashSet<SymbolId> LookaheadIds {
			get;
		}

		public bool Equals(LrItem other) {
			if (ReferenceEquals(other, null)) {
				return false;
			}
			if (ReferenceEquals(other, this)) {
				return true;
			}
			return ReferenceEquals(this.Rule, other.Rule) && (this.Marker == other.Marker);
		}

		public override bool Equals(object obj) {
			return this.Equals(obj as LrItem);
		}

		public override int GetHashCode() {
			return this.hash;
		}

		public override string ToString() {
			return this.ToString(id => id.ToString());
		}

		public string ToString(Func<SymbolId, string> resolver) {
			var sb = new StringBuilder();
			sb.Append(resolver(this.Rule.ProductionSymbolId));
			sb.Append(" ->");
			for (var i = 0; i < this.Length; i++) {
				if (i == this.Marker) {
					sb.Append(" *");
				}
				sb.Append(' ');
				sb.Append(resolver(this.Rule.RuleSymbolIds[i]));
			}
			if (this.Marker >= this.Length) {
				sb.Append(" *");
			}
			if (this.LookaheadIds.Count > 0) {
				sb.Append(" (");
				sb.Append(string.Join(", ", this.LookaheadIds));
				sb.Append(')');
			}
			return sb.ToString();
		}
	}
}
