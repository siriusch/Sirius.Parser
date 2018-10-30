namespace Sirius.Parser.Semantic.Json {
	public sealed class JsonProperty: JsonToken {
		public JsonValue<string> Name {
			get;
		}

		public JsonValue Value {
			get;
		}

		public JsonProperty Next {
			get;
		}

		[Rule(typeof(JsonToken), "property", "string", ":", "value")]
		public JsonProperty(
				[RuleSymbol("string")]JsonValue<string> name,
				[RuleSymbol("value")]JsonValue value
		): this(name, value, null) {}

		[Rule(typeof(JsonToken), "property", "string", ":", "value", ",", "property")]
		public JsonProperty(
				[RuleSymbol("string")]JsonValue<string> name,
				[RuleSymbol("value")]JsonValue value,
				[RuleSymbol("property")]JsonProperty next) {
			this.Next = next;
			this.Name = name;
			this.Value = value;
		}
	}
}
