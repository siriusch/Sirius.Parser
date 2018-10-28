using System;
using System.Collections.Generic;
using System.Linq;

using Sirius.Collections;

namespace Sirius.Parser {
	public abstract class ParserContextBase<TAstNode, TInput, TPosition> {
		internal ParserState<TAstNode> currentState = new ParserState<TAstNode>(default(TAstNode), default(int), null);

		protected internal abstract void Accept(TAstNode result);

		protected virtual string ResolveSymbol(SymbolId symbol) {
			return null;
		}

		protected IEnumerable<TAstNode> StateStack() {
			for (var current = this.currentState; current.Parent != null; current = current.Parent) {
				if (typeof(TAstNode).IsValueType || (current.Node != null)) {
					yield return current.Node;
				}
			}
		}

		protected internal virtual SymbolId? SyntaxError(IEnumerable<SymbolId> expectedSymbols, SymbolId tokenSymbolId, Capture<TInput> tokenValue, TPosition position) {
			throw new InvalidOperationException($"Syntax error at position {position}: found {tokenSymbolId.ToString(this.ResolveSymbol)} but expected one of {string.Join(", ", expectedSymbols.Select(t => t.ToString(this.ResolveSymbol)))}");
		}

		public Action<SymbolId, Capture<TInput>> Bind(Action<ParserContextBase<TAstNode, TInput, TPosition>, SymbolId, Capture<TInput>> parser) {
			return (symbolId, capture) => parser(this, symbolId, capture);
		}
	}
}
