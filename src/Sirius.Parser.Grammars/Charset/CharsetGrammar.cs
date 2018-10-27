using System;
using System.Threading;

using Sirius.Parser.Grammar;
using Sirius.Parser.Lalr;
using Sirius.RegularExpressions;
using Sirius.RegularExpressions.Alphabet;
using Sirius.RegularExpressions.Automata;
using Sirius.RegularExpressions.Invariant;
using Sirius.RegularExpressions.Parser;
using Sirius.Unicode;

namespace Sirius.Parser.Grammars.Charset {
	public sealed class CharsetGrammar {
		private class RegexVisitor<TLetter>: IRegexVisitor<TLetter, SymbolId, RxNode<TLetter>>
				where TLetter: IEquatable<TLetter>, IComparable<TLetter> {
			public static readonly RegexVisitor<TLetter> Default = new RegexVisitor<TLetter>();

			public RxNode<TLetter> Accept(RxAccept<TLetter> node, SymbolId context) {
				if (node.Symbol == context) {
					return node.Inner;
				}
				return node.Inner.Visit(this, context);
			}

			public RxNode<TLetter> Alternation(RxAlternation<TLetter> node, SymbolId context) {
				return node.Left.Visit(this, context) ?? node.Right.Visit(this, context);
			}

			public RxNode<TLetter> Concatenation(RxConcatenation<TLetter> node, SymbolId context) {
				return node.Left.Visit(this, context) ?? node.Right.Visit(this, context);
			}

			public RxNode<TLetter> Empty(RxEmpty<TLetter> node, SymbolId context) {
				return null;
			}

			public RxNode<TLetter> Match(RxMatch<TLetter> node, SymbolId context) {
				return null;
			}

			public RxNode<TLetter> Quantified(RxQuantified<TLetter> node, SymbolId context) {
				return node.Inner.Visit(this, context);
			}
		}

		public const int SymWhitespace = 0;
		public const int SymCharset = 1;
		public const int SymRegexCharset = 2;
		public const int SymUnion = 3;
		public const int SymSubtract = 4;
		public const int SymIntersect = 5;
		public const int SymDifference = 6;
		public const int SymNegate = 7;
		public const int SymParensOpen = 8;
		public const int SymParensClose = 9;
		public const int SymExpression = 10;
		public const int SymNegateExpression = 11;
		public const int SymValueExpression = 12;
		public const int SymUnionExpression = 13;
		public const int SymSubtractExpression = 14;
		public const int SymIntersectExpression = 15;
		public const int SymDifferenceExpression = 16;

		private static readonly Lazy<CharsetGrammar> @default = new Lazy<CharsetGrammar>(() => new CharsetGrammar(), LazyThreadSafetyMode.PublicationOnly);

		public static DfaStateMachine<LetterId, char> StateMachine => @default.Value.stateMachine;
		public static LalrTable Table => @default.Value.table;

		private readonly DfaStateMachine<LetterId, char> stateMachine;
		private readonly LalrTable table;

		private CharsetGrammar() {
			var provider = new UnicodeCharSetProvider();
			var mapper = new UnicodeUtf16Mapper(false, false);
			var rx = RegexLexer.CreateRx(mapper);
			var rxWhitespace = new RxAccept<char>(rx.Visit(RegexVisitor<char>.Default, new SymbolId(RegexLexer.SymWhitespace)), SymWhitespace, 0);
			var rxCharset = new RxAccept<char>(rx.Visit(RegexVisitor<char>.Default, new SymbolId(RegexLexer.SymCharset)), SymCharset, 0);
			var rxRegexCharset = new RxAccept<char>(rx.Visit(RegexVisitor<char>.Default, new SymbolId(RegexLexer.SymRegexCharset)), SymRegexCharset, 0);
			var rxUnion = new RxAccept<char>(RegexMatchSet.FromChars('|', '+').ToInvariant(mapper, provider, true), SymUnion, 0);
			var rxSubtract = new RxAccept<char>(RegexMatchSet.FromChars('-').ToInvariant(mapper, provider, true), SymSubtract, 0);
			var rxIntersect = new RxAccept<char>(RegexMatchSet.FromChars('&').ToInvariant(mapper, provider, true), SymIntersect, 0);
			var rxDifference = new RxAccept<char>(RegexMatchSet.FromChars('^').ToInvariant(mapper, provider, true), SymDifference, 0);
			var rxNegate = new RxAccept<char>(RegexMatchSet.FromChars('~').ToInvariant(mapper, provider, true), SymNegate, 0);
			var rxParensOpen = new RxAccept<char>(RegexMatchSet.FromChars('(').ToInvariant(mapper, provider, true), SymParensOpen, 0);
			var rxParensClose = new RxAccept<char>(RegexMatchSet.FromChars(')').ToInvariant(mapper, provider, true), SymParensClose, 0);
			var alpha = new AlphabetBuilder<char>(
					new RxAlternation<char>(rxWhitespace,
							new RxAlternation<char>(rxCharset,
									new RxAlternation<char>(rxRegexCharset,
											new RxAlternation<char>(rxUnion,
													new RxAlternation<char>(rxSubtract,
															new RxAlternation<char>(rxIntersect,
																	new RxAlternation<char>(rxDifference,
																			new RxAlternation<char>(rxNegate,
																					new RxAlternation<char>(rxParensOpen, rxParensClose))))))))),
					Utf16Chars.EOF,
					Utf16Chars.ValidBmp);
			var nfa = NfaBuilder<LetterId>.Build(alpha.Expression);
			var dfa = DfaBuilder<LetterId>.Build(nfa, LetterId.Eof);
			if (dfa.StartState.Id != default(Id<DfaState<LetterId>>)) {
				throw new InvalidOperationException($"Internal error: Unexpected DFA start state {dfa.StartState.Id}");
			}
			this.stateMachine = DfaStateMachineEmitter.CreateExpression(dfa, AlphabetMapperEmitter<char>.CreateExpression(alpha)).Compile();
			this.table = new LalrTableGenerator(new GrammarBuilder(-2, -1, SymExpression) {
							{SymUnionExpression, SymExpression, SymUnion, SymNegateExpression},
							{SymExpression, SymUnionExpression},
							{SymSubtractExpression, SymExpression, SymSubtract, SymNegateExpression},
							{SymExpression, SymSubtractExpression},
							{SymIntersectExpression, SymExpression, SymIntersect, SymNegateExpression},
							{SymExpression, SymIntersectExpression},
							{SymDifferenceExpression, SymExpression, SymDifference, SymNegateExpression},
							{SymExpression, SymDifferenceExpression},
							{SymExpression, SymNegateExpression},
							{SymNegateExpression, SymNegate, SymValueExpression},
							{SymNegateExpression, SymValueExpression},
							{SymValueExpression, SymParensOpen, SymExpression, SymParensClose},
							{SymValueExpression, SymCharset},
							{SymValueExpression, SymRegexCharset}
					})
					.ComputeTable();
		}
	}
}
