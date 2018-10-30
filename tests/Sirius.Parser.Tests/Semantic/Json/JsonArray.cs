using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Sirius.Parser.Semantic.Json {
	public sealed class JsonArray: JsonValue, IEnumerable<JsonValue> {
		public JsonElement Element {
			get;
		}

		[Rule(typeof(JsonToken), "array", "[", "]")]
		public JsonArray(): this(null) { }

		[Rule(typeof(JsonToken), "array", "[", "element", "]")]
		public JsonArray(
				[RuleSymbol("element")]JsonElement element
				) {
			this.Element = element;
		}

		public IEnumerator<JsonValue> GetEnumerator() {
			for (var current = this.Element; current != null; current = current.Next) {
				yield return current.Value;
			}
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return this.GetEnumerator();
		}

		public override void WriteTo(TextWriter writer) {
			writer.Write('[');
			for (var current = this.Element; current != null; current = current.Next) {
				current.Value.WriteTo(writer);
				if (current.Next != null) {
					writer.Write(',');
				}
			}
			writer.Write(']');
		}
	}
}
