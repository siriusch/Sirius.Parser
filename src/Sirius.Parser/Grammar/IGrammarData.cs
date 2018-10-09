using System.Collections.Generic;

namespace Sirius.Parser.Grammar {
	public interface IGrammarData {
		SymbolId Unknown {
			get;
		}

		SymbolId Init {
			get;
		}

		SymbolId Start {
			get;
		}

		IEnumerable<KeyValuePair<SymbolId, SymbolIdSequence>> Productions {
			get;
		}

		IEnumerable<KeyValuePair<SymbolId, SymbolKind>> Symbols {
			get;
		}
	}
}
