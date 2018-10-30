using System.IO;

namespace Sirius.Parser.Semantic.Json {
	public sealed class JsonNull: JsonValue {
		public JsonNull() { }

		public override void WriteTo(TextWriter writer) {
			writer.Write("null");
		}
	}
}
