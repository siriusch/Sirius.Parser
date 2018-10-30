using System;

using Sirius.Collections;

namespace Sirius.Parser.Semantic.Json {
	public class JsonParser<TInput>: SemanticParser<JsonValue, TInput, long>
			where TInput: struct, IEquatable<TInput>, IComparable<TInput> {
		public JsonParser(SemanticParserGrammar<JsonValue, TInput, long> grammar, ParserContextBase<JsonValue, TInput, long> context): base(grammar, context) { }

		protected override void TokenAction(SymbolId symbolId, Capture<TInput> value) {
			if (!this.Grammar.IsFlagSet(symbolId, TerminalFlags.Noise)) {
				this.PushTokenToParser(symbolId, value, value.Index);
			}
		}
	}
}
