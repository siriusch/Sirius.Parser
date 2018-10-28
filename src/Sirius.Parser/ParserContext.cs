using System;
using System.Collections.Generic;

using Sirius.Collections;

namespace Sirius.Parser {
	public class ParserContext<TAstNode, TInput, TPosition>: ParserContextBase<TAstNode, TInput, TPosition> {
		private readonly Action<TAstNode> accept;
		private readonly Func<SymbolId, string> resolveSymbol;
		private readonly Func<IEnumerable<SymbolId>, IEnumerable<TAstNode>, SymbolId, Capture<TInput>, TPosition, SymbolId?> syntaxError;

		public ParserContext(Action<TAstNode> accept): this(accept, null, null) { }

		public ParserContext(Action<TAstNode> accept, Func<SymbolId, string> resolveSymbol): this(accept, null, resolveSymbol) { }

		public ParserContext(Action<TAstNode> accept, Func<IEnumerable<SymbolId>, IEnumerable<TAstNode>, SymbolId, Capture<TInput>, TPosition, SymbolId?> syntaxError): this(accept, syntaxError, null) { }

		private ParserContext(Action<TAstNode> accept, Func<IEnumerable<SymbolId>, IEnumerable<TAstNode>, SymbolId, Capture<TInput>, TPosition, SymbolId?> syntaxError, Func<SymbolId, string> resolveSymbol) {
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

		protected internal override SymbolId? SyntaxError(IEnumerable<SymbolId> expectedSymbols, SymbolId tokenSymbolId, Capture<TInput> tokenValue, TPosition position) {
			return this.syntaxError != null ? this.syntaxError(expectedSymbols, this.StateStack(), tokenSymbolId, tokenValue, position) : base.SyntaxError(expectedSymbols, tokenSymbolId, tokenValue, position);
		}
	}
}