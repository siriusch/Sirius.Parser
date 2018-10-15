using System;
using System.Collections.Generic;

using Sirius.Collections;
using Sirius.Parser.Lalr;

namespace Sirius.Parser {
	public class Parser<TInput, TAstNode>: ParserBase<TInput, TAstNode, long> {
		private readonly Action<TAstNode> accept;
		private readonly Func<ProductionRule, IReadOnlyList<TAstNode>, TAstNode> createNonterminal;
		private readonly Func<SymbolId, IEnumerable<TInput>, long, TAstNode> createTerminal;
		private readonly Action<IEnumerable<TAstNode>, IEnumerable<SymbolId>> syntaxError;

		public Parser(LalrTable table, Func<SymbolId, IEnumerable<TInput>, long, TAstNode> createTerminal, Func<ProductionRule, IReadOnlyList<TAstNode>, TAstNode> createNonterminal, Action<TAstNode> accept, Action<IEnumerable<TAstNode>, IEnumerable<SymbolId>> syntaxError): base(table) {
			this.createTerminal = createTerminal;
			this.createNonterminal = createNonterminal;
			this.accept = accept;
			this.syntaxError = syntaxError;
		}

		protected override void Accept(TAstNode node) {
			this.accept(node);
		}

		protected override bool CheckAndPreprocessTerminal(ref SymbolId symbolId, Capture<TInput> letters, out long position) {
			position = letters.Index;
			return true;
		}

		protected override TAstNode CreateNonterminal(ProductionRule rule, IReadOnlyList<TAstNode> nodes) {
			return this.createNonterminal(rule, nodes);
		}

		protected override TAstNode CreateTerminal(SymbolId symbolId, Capture<TInput> letters, long offset) {
			return this.createTerminal(symbolId, letters, offset);
		}

		protected override void SyntaxError(IEnumerable<TAstNode> stack, IEnumerable<SymbolId> expectedSymbols, long offset) {
			this.syntaxError(stack, expectedSymbols);
		}
	}
}
