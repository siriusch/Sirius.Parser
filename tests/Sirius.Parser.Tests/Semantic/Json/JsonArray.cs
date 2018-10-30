using System.Collections;
using System.Collections.Generic;
using System.IO;

using Sirius.Parser.Semantic;
using Sirius.Parser.Semantic.Json;

[assembly: Rule(typeof(JsonValue), "array", "[", "element", "]", TrimSymbolName = "element")]

namespace Sirius.Parser.Semantic.Json {
	public sealed class JsonArray: JsonValue, IEnumerable<JsonValue> {
		[Rule(typeof(JsonValue), "element", "value")]
		public static JsonArray FromValue(JsonValue value) {
			var arr = new JsonArray();
			arr.items.Add(value);
			return arr;
		}

		[Rule(typeof(JsonValue), "element", "element", ",", "value")]
		public static JsonArray AddValue(
				[RuleSymbol("value")]JsonValue value,
				[RuleSymbol("element")]JsonArray arr) {
			arr.items.Add(value);
			return arr;
		}

		private readonly List<JsonValue> items = new List<JsonValue>();

		[Rule(typeof(JsonValue), "array", "[", "]")]
		public JsonArray() { }

		public IEnumerator<JsonValue> GetEnumerator() {
			return this.items.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return this.GetEnumerator();
		}

		public override void WriteTo(TextWriter writer) {
			writer.Write('[');
			var first = true;
			foreach (var item in this.items) {
				if (first) {
					first = false;
				} else {
					writer.Write(',');
				}
				item.WriteTo(writer);
			}
			writer.Write(']');
		}
	}
}
