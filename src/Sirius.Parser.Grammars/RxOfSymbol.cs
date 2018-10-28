using System;

using Sirius.RegularExpressions.Invariant;

namespace Sirius.Parser {
	internal sealed class RxOfSymbol<TLetter>: IRegexVisitor<TLetter, SymbolId, RxNode<TLetter>>
			where TLetter: IEquatable<TLetter>, IComparable<TLetter> {
		public static RxNode<TLetter> Extract(RxNode<TLetter> rx, SymbolId symbolId) {
			return rx.Visit(new RxOfSymbol<TLetter>(), symbolId);
		}

		RxNode<TLetter> IRegexVisitor<TLetter, SymbolId, RxNode<TLetter>>.Accept(RxAccept<TLetter> node, SymbolId context) {
			if (node.Symbol == context) {
				return node.Inner;
			}
			return node.Inner.Visit(this, context);
		}

		RxNode<TLetter> IRegexVisitor<TLetter, SymbolId, RxNode<TLetter>>.Alternation(RxAlternation<TLetter> node, SymbolId context) {
			return node.Left.Visit(this, context) ?? node.Right.Visit(this, context);
		}

		RxNode<TLetter> IRegexVisitor<TLetter, SymbolId, RxNode<TLetter>>.Concatenation(RxConcatenation<TLetter> node, SymbolId context) {
			return node.Left.Visit(this, context) ?? node.Right.Visit(this, context);
		}

		RxNode<TLetter> IRegexVisitor<TLetter, SymbolId, RxNode<TLetter>>.Empty(RxEmpty<TLetter> node, SymbolId context) {
			return null;
		}

		RxNode<TLetter> IRegexVisitor<TLetter, SymbolId, RxNode<TLetter>>.Match(RxMatch<TLetter> node, SymbolId context) {
			return null;
		}

		RxNode<TLetter> IRegexVisitor<TLetter, SymbolId, RxNode<TLetter>>.Quantified(RxQuantified<TLetter> node, SymbolId context) {
			return node.Inner.Visit(this, context);
		}
	}
}
