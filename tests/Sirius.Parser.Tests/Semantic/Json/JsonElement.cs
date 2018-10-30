namespace Sirius.Parser.Semantic.Json {
	public sealed class JsonElement: JsonToken {
		public JsonValue Value {
			get;
		}

		public JsonElement Next {
			get;
		}

		[Rule(typeof(JsonToken), "element", "value")]
		public JsonElement(JsonValue value): this(value, null) { }

		[Rule(typeof(JsonToken), "element", "value", ",", "element")]
		public JsonElement(
				[RuleSymbol("value")]JsonValue value,
				[RuleSymbol("element")]JsonElement next) {
			this.Value = value;
			this.Next = next;
		}
	}
}
