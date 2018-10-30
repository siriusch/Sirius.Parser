using System;

using Sirius.Collections;

namespace Sirius.Parser.Semantic.Json {
	public class JsonParser<TInput>: SemanticParser<JsonToken, TInput, long>
			where TInput: struct, IEquatable<TInput>, IComparable<TInput> {
		public JsonParser(SemanticParserGrammar<JsonToken, TInput, long> grammar, ParserContextBase<JsonToken, TInput, long> context): base(grammar, context) { }

		protected override void TokenAction(SymbolId symbolId, Capture<TInput> value) {
			if (!this.Grammar.IsFlagSet(symbolId, TerminalFlags.Noise)) {
				this.PushTokenToParser(symbolId, value, value.Index);
			}
		}
	}
}
