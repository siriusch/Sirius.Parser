using System;

using Sirius.Collections;
using Sirius.RegularExpressions;
using Sirius.RegularExpressions.Automata;

namespace Sirius.Parser.Charset {
	public class CharsetLexer: Lexer<char, LetterId> {
		public CharsetLexer(Action<SymbolId, Capture<char>> tokenAction): base(CharsetGrammar.StateMachine, default(Id<DfaState<LetterId>>), true, tokenAction) { }
	}
}
