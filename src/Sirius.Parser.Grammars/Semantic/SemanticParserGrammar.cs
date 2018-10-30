using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Sirius.Collections;
using Sirius.RegularExpressions;
using Sirius.RegularExpressions.Automata;
using Sirius.RegularExpressions.Parser;

namespace Sirius.Parser.Semantic {
	public class SemanticParserGrammar<TAstNode, TInput, TPosition>
			where TInput: struct, IEquatable<TInput>, IComparable<TInput> {
		public interface IGrammarData {
			Expression<Action<SemanticParser<TAstNode, TInput, TPosition>, ParserContextBase<TAstNode, TInput, TPosition>, SymbolId, Capture<TInput>, TPosition>> ParserStateMachine {
				get;
			}

			Expression<DfaStateMachine<LetterId, TInput>> LexerStateMachine {
				get;
			}

			IReadOnlyDictionary<SymbolId, string> Symbols {
				get;
			}

			IReadOnlyDictionary<string, SymbolId> SymbolsByName {
				get;
			}

			IReadOnlyDictionary<SymbolId, TerminalFlags> TerminalFlags {
				get;
			}
		}

		internal static IEnumerable<KeyValuePair<GrammarAttribute, MethodBase>> FindGrammarParts() {
			return FindGrammarParts(typeof(TAstNode).Assembly);
		}

		private static IEnumerable<KeyValuePair<GrammarAttribute, MethodBase>> FindGrammarParts(Assembly assembly) {
			bool GrammarKeyMatches(GrammarAttribute g) {
				return g.GrammarKey == typeof(TAstNode);
			}

			foreach (var attribute in assembly.GetCustomAttributes<GrammarAttribute>().Where(GrammarKeyMatches)) {
				yield return new KeyValuePair<GrammarAttribute, MethodBase>(attribute, null);
			}
			foreach (var member in assembly.ExportedTypes.SelectMany(a => a.GetMembers()).OfType<MethodBase>()) {
				foreach (var attribute in member.GetCustomAttributes<GrammarAttribute>(true).Where(GrammarKeyMatches)) {
					yield return new KeyValuePair<GrammarAttribute, MethodBase>(attribute, member);
				}
			}
		}

		public readonly DfaStateMachine<LetterId, TInput> LexerStateMachine;
		public readonly Action<SemanticParser<TAstNode, TInput, TPosition>, ParserContextBase<TAstNode, TInput, TPosition>, SymbolId, Capture<TInput>, TPosition> ParserStateMachine;
		public readonly Func<SymbolId, string> ResolveSymbol;
		public readonly IReadOnlyDictionary<string, SymbolId> SymbolsByName;
		private readonly IReadOnlyDictionary<SymbolId, TerminalFlags> terminalFlags;

		public SemanticParserGrammar(IUnicodeMapper<TInput> mapper, TInput? eof = null): this(new SemanticParserGrammarBuilder<TAstNode, TInput, TPosition>(mapper, eof)) { }

		protected SemanticParserGrammar(IGrammarData grammarData) {
			this.ParserStateMachine = grammarData.ParserStateMachine.Compile();
			this.LexerStateMachine = grammarData.LexerStateMachine.Compile();
			this.ResolveSymbol = grammarData.Symbols.CreateGetter();
			this.SymbolsByName = grammarData.SymbolsByName;
			this.terminalFlags = grammarData.TerminalFlags;
		}

		public bool IsFlagSet(SymbolId symbol, TerminalFlags flag) {
			return this.terminalFlags.TryGetValue(symbol, out var flags) && ((flags & flag) != TerminalFlags.None);
		}
	}
}
