using System;
using System.Collections.Generic;

using Sirius.Parser.Lalr;

namespace Sirius.Parser {
	public class Parser<TLetter, TAstNode>: ParserBase<TLetter, TAstNode> {
		private readonly Action<TAstNode> accept;
		private readonly Func<ProductionRule, IReadOnlyList<TAstNode>, TAstNode> createNonterminal;
		private readonly Func<SymbolId, IEnumerable<TLetter>, long, TAstNode> createTerminal;
		private readonly Action<IEnumerable<TAstNode>, IEnumerable<SymbolId>> syntaxError;

		public Parser(LalrTable table, Func<SymbolId, IEnumerable<TLetter>, long, TAstNode> createTerminal, Func<ProductionRule, IReadOnlyList<TAstNode>, TAstNode> createNonterminal, Action<TAstNode> accept, Action<IEnumerable<TAstNode>, IEnumerable<SymbolId>> syntaxError): base(table) {
			this.createTerminal = createTerminal;
			this.createNonterminal = createNonterminal;
			this.accept = accept;
			this.syntaxError = syntaxError;
		}

		protected override void Accept(TAstNode node) {
			this.accept(node);
		}

		protected override TAstNode CreateNonterminal(ProductionRule rule, IReadOnlyList<TAstNode> nodes) {
			return this.createNonterminal(rule, nodes);
		}

		protected override TAstNode CreateTerminal(SymbolId symbolId, IEnumerable<TLetter> letters, long offset) {
			return this.createTerminal(symbolId, letters, offset);
		}

		protected override void SyntaxError(IEnumerable<TAstNode> stack, IEnumerable<SymbolId> expectedSymbols) {
			this.syntaxError(stack, expectedSymbols);
		}
	}
}
