using System;
using System.Collections.Generic;
using System.Reflection;

using Sirius.Collections;
using Sirius.RegularExpressions;
using Sirius.RegularExpressions.Automata;

namespace Sirius.Parser.Semantic {
	public abstract class SemanticParser<TAstNode, TInput, TPosition>: LexerBase<TInput, LetterId>
			where TInput: struct, IEquatable<TInput>, IComparable<TInput> {
		protected SemanticParser(SemanticParserGrammar<TAstNode, TInput, TPosition> grammar, ParserContextBase<TAstNode, TInput, TPosition> context): base(true, SymbolId.Eof, default(Id<DfaState<LetterId>>)) {
			this.Grammar = grammar;
			this.Context = context;
		}

		protected override SymbolId? ProcessStateMachine(ref Id<DfaState<LetterId>> state, TInput input) {
			return Grammar.LexerStateMachine(ref state, input);
		}

		public ParserContextBase<TAstNode, TInput, TPosition> Context {
			get;
		}

		public SemanticParserGrammar<TAstNode, TInput, TPosition> Grammar {
			get;
		}

		protected internal virtual T GetParameterValue<T>(ParameterInfo param) {
			return default(T);
		}

		protected void PushTokenToParser(SymbolId symbol, Capture<TInput> letters, TPosition position) {
			this.Grammar.ParserStateMachine(this, this.Context, symbol, letters, position);
		}

		protected internal virtual bool SyntaxError(ref SymbolId tokenSymbolId, ref Capture<TInput> tokenValue, TPosition position, IEnumerable<SymbolId> expectedSymbols) {
			this.Context.SyntaxError(tokenSymbolId, tokenValue, position, expectedSymbols);
			return false;
		}
	}
}
