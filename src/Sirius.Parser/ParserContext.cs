using System;
using System.Collections.Generic;

using Sirius.Collections;

namespace Sirius.Parser {
	public class ParserContext<TAstNode, TInput, TPosition>: ParserContextBase<TAstNode, TInput, TPosition> {
		private readonly Action<TAstNode> accept;
		private readonly Func<SymbolId, string> resolveSymbol;
		private readonly Action<SymbolId, Capture<TInput>, TPosition, IEnumerable<SymbolId>, IEnumerable<TAstNode>> syntaxError;

		public ParserContext(Action<TAstNode> accept): this(accept, null, null) { }

		public ParserContext(Action<TAstNode> accept, Func<SymbolId, string> resolveSymbol): this(accept, null, resolveSymbol) { }

		public ParserContext(Action<TAstNode> accept, Action<SymbolId, Capture<TInput>, TPosition, IEnumerable<SymbolId>, IEnumerable<TAstNode>> syntaxError): this(accept, syntaxError, null) { }

		private ParserContext(Action<TAstNode> accept, Action<SymbolId, Capture<TInput>, TPosition, IEnumerable<SymbolId>, IEnumerable<TAstNode>> syntaxError, Func<SymbolId, string> resolveSymbol) {
			this.accept = accept;
			this.syntaxError = syntaxError;
			this.resolveSymbol = resolveSymbol;
		}

		protected internal override void Accept(TAstNode result) {
			this.accept(result);
		}

		protected override string ResolveSymbol(SymbolId symbol) {
			return this.resolveSymbol != null ? this.resolveSymbol(symbol) : base.ResolveSymbol(symbol);
		}

		protected internal override void SyntaxError(SymbolId tokenSymbolId, Capture<TInput> tokenValue, TPosition position, IEnumerable<SymbolId> expectedSymbols) {
			if (this.syntaxError != null) {
				this.syntaxError(tokenSymbolId, tokenValue, position, expectedSymbols, this.StateStack());
			} else {
				base.SyntaxError(tokenSymbolId, tokenValue, position, expectedSymbols);
			}
		}
	}
}
