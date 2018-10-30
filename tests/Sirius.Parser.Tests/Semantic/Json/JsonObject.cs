using System.Collections;
using System.Collections.Generic;
using System.IO;

using Sirius.Parser.Semantic;
using Sirius.Parser.Semantic.Json;

[assembly: Rule(typeof(JsonValue), "object", "{", "property", "}", TrimSymbolName = "property")]

namespace Sirius.Parser.Semantic.Json {
	public sealed class JsonObject: JsonValue, IEnumerable<KeyValuePair<string, JsonValue>> {
		[Rule(typeof(JsonValue), "property", "string", ":", "value")]
		public static JsonObject FromProperty(
				[RuleSymbol("string")] JsonValue<string> name,
				[RuleSymbol("value")] JsonValue value
		) {
			var obj = new JsonObject();
			obj.properties.Add(name.Value, value);
			return obj;
		}

		[Rule(typeof(JsonValue), "property", "property", ",", "string", ":", "value")]
		public static JsonObject AddProperty(
				[RuleSymbol("string")] JsonValue<string> name,
				[RuleSymbol("value")] JsonValue value,
				[RuleSymbol("property")] JsonObject obj
		) {
			obj.properties.Add(name.Value, value);
			return obj;
		}

		private readonly Dictionary<string, JsonValue> properties = new Dictionary<string, JsonValue>();

		[Rule(typeof(JsonValue), "object", "{", "}")]
		public JsonObject() { }

		public IEnumerator<KeyValuePair<string, JsonValue>> GetEnumerator() {
			return this.properties.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return this.GetEnumerator();
		}

		public override void WriteTo(TextWriter writer) {
			writer.Write('{');
			var first = true;
			foreach (var property in this.properties) {
				if (first) {
					first = false;
				} else {
					writer.Write(',');
				}
				writer.Write(property.Key);
				writer.Write(':');
				property.Value.WriteTo(writer);
			}
			writer.Write('}');
		}
	}
}
