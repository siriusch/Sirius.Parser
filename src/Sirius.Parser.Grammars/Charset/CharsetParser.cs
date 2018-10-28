using System;
using System.Collections.Generic;
using System.Diagnostics;

using Sirius.Collections;
using Sirius.Parser.Lalr;
using Sirius.RegularExpressions.Parser;
using Sirius.Unicode;

namespace Sirius.Parser.Charset {
	public class CharsetParser: ParserBase<char, CharsetNode, long> {
		public static CharsetNode Parse(IEnumerable<char> expression) {
			var result = default(CharsetNode);
			var context = new ParserContext<CharsetNode, char, long>(node => result = node, CharsetGrammar.ResolveSymbol);
			var parser = new CharsetParser(context);
			var lexer = new CharsetLexer(parser.ProcessToken);
			lexer.Push(expression.Append(Utf16Chars.EOF));
			return result;
		}

		public CharsetParser(ParserContextBase<CharsetNode, char, long> context): base(CharsetGrammar.Table, context) { }

		protected override bool CheckAndPreprocessTerminal(ref SymbolId symbolId, Capture<char> letters, out long position) {
			position = letters.Index;
			return symbolId != CharsetGrammar.SymWhitespace;
		}

		protected override CharsetNode CreateNonterminal(ProductionRule rule, IReadOnlyList<CharsetNode> nodes) {
			if (nodes.Count == 1) {
				return nodes[0];
			}
			switch (rule.ProductionSymbolId.ToInt32()) {
			case CharsetGrammar.SymNegateExpression:
				Debug.Assert(nodes.Count == 2);
				return new CharsetNegate(nodes[1]);
			case CharsetGrammar.SymValueExpression:
				Debug.Assert(nodes.Count == 3);
				return nodes[1];
			case CharsetGrammar.SymUnionExpression:
				Debug.Assert(nodes.Count == 3);
				return new CharsetUnion(nodes[0], nodes[2]);
			case CharsetGrammar.SymDifferenceExpression:
				Debug.Assert(nodes.Count == 3);
				return new CharsetDifference(nodes[0], nodes[2]);
			case CharsetGrammar.SymIntersectExpression:
				Debug.Assert(nodes.Count == 3);
				return new CharsetIntersection(nodes[0], nodes[2]);
			case CharsetGrammar.SymSubtractExpression:
				Debug.Assert(nodes.Count == 3);
				return new CharsetSubtract(nodes[0], nodes[2]);
			}
			throw new InvalidOperationException("Unexpected non-terminal rule " + rule);
		}

		protected override CharsetNode CreateTerminal(SymbolId symbolId, Capture<char> letters, long offset) {
			switch (symbolId.ToInt32()) {
			case CharsetGrammar.SymCharset:
				return new CharsetHandle(RegexMatchSet.FromNamedCharset(letters.AsString()).Handle);
			case CharsetGrammar.SymRegexCharset:
				return new CharsetHandle(RegexMatchSet.FromSet(letters.AsString()).Handle);
			}
			return null;
		}
	}
}
