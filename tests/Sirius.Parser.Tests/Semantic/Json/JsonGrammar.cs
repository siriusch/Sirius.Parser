using System;

using Sirius.RegularExpressions.Parser;

namespace Sirius.Parser.Semantic.Json {
	public class JsonGrammar<TInput>: SemanticParserGrammar<JsonToken, TInput, long>
			where TInput: struct, IEquatable<TInput>, IComparable<TInput> {
		public JsonGrammar(IUnicodeMapper<TInput> mapper, TInput? eof = null): base(mapper, eof) { }
	}
}