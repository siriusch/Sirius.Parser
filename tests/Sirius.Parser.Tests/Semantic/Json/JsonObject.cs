using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Sirius.Parser.Semantic.Json {
	public sealed class JsonObject: JsonValue, IEnumerable<JsonProperty> {
		[Rule(typeof(JsonToken), "object", "{", "}")]
		public JsonObject(): this(null) { }

		[Rule(typeof(JsonToken), "object", "{", "property", "}")]
		public JsonObject(
				[RuleSymbol("property")] JsonProperty property
		) {
			this.Property = property;
		}

		public JsonProperty Property {
			get;
		}

		public IEnumerator<JsonProperty> GetEnumerator() {
			for (var current = this.Property; current != null; current = current.Next) {
				yield return current;
			}
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return this.GetEnumerator();
		}

		public override void WriteTo(TextWriter writer) {
			writer.Write('{');
			for (var current = this.Property; current != null; current = current.Next) {
				current.Name.WriteTo(writer);
				writer.Write(':');
				current.Value.WriteTo(writer);
				if (current.Next != null) {
					writer.Write(',');
				}
			}
			writer.Write('}');
		}
	}
}
