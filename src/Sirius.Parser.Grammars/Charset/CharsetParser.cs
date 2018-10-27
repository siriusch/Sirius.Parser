using System;
using System.Collections.Generic;
using System.Diagnostics;

using Sirius.Collections;
using Sirius.Parser.Lalr;
using Sirius.RegularExpressions.Parser;
using Sirius.Unicode;

namespace Sirius.Parser.Grammars.Charset {
	public class CharsetParser<TChar>: ParserBase<char, CharsetNode<TChar>, long>
			where TChar: IComparable<TChar> {
		public CharsetParser(ParserContextBase<CharsetNode<TChar>, char, long> context): base(CharsetGrammar.Table, context) { }

		protected override bool CheckAndPreprocessTerminal(ref SymbolId symbolId, Capture<char> letters, out long position) {
			position = letters.Index;
			return symbolId != CharsetGrammar.SymWhitespace;
		}

		protected override CharsetNode<TChar> CreateNonterminal(ProductionRule rule, IReadOnlyList<CharsetNode<TChar>> nodes) {
			if (nodes.Count == 1) {
				return nodes[0];
			}
			switch (rule.ProductionSymbolId.ToInt32()) {
			case CharsetGrammar.SymNegateExpression:
				Debug.Assert(nodes.Count == 2);
				return new CharsetNegate<TChar>(nodes[1]);
			case CharsetGrammar.SymValueExpression:
				Debug.Assert(nodes.Count == 3);
				return nodes[1];
			case CharsetGrammar.SymUnionExpression:
				Debug.Assert(nodes.Count == 3);
				return new CharsetUnion<TChar>(nodes[0], nodes[2]);
			case CharsetGrammar.SymDifferenceExpression:
				Debug.Assert(nodes.Count == 3);
				return new CharsetDifference<TChar>(nodes[0], nodes[2]);
			case CharsetGrammar.SymIntersectExpression:
				Debug.Assert(nodes.Count == 3);
				return new CharsetIntersect<TChar>(nodes[0], nodes[2]);
			case CharsetGrammar.SymSubtractExpression:
				Debug.Assert(nodes.Count == 3);
				return new CharsetSubtract<TChar>(nodes[0], nodes[2]);
			}
			throw new InvalidOperationException("Unexpected non-terminal rule " + rule);
		}

		protected override CharsetNode<TChar> CreateTerminal(SymbolId symbolId, Capture<char> letters, long offset) {
			switch (symbolId.ToInt32()) {
			case CharsetGrammar.SymCharset:
				return new CharsetHandle<TChar>(RegexMatchSet.FromNamedCharset(letters.AsString()).Handle);
			case CharsetGrammar.SymRegexCharset:
				return new CharsetHandle<TChar>(RegexMatchSet.FromSet(letters.AsString()).Handle);
			}
			return null;
		}
	}
}