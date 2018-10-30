using System;
using System.Globalization;
using System.IO;

namespace Sirius.Parser.Semantic.Json {
	public abstract class JsonValue: JsonToken {
		public abstract void WriteTo(TextWriter writer);

		public override string ToString() {
			var result = new StringWriter();
			this.WriteTo(result);
			return result.ToString();
		}
	}

	public sealed class JsonValue<T>: JsonValue
			where T: IConvertible {
		public T Value {
			get;
		}

		public JsonValue(T value) {
			this.Value = value;
		}

		public override void WriteTo(TextWriter writer) {
			writer.Write(this.Value.ToString(CultureInfo.InvariantCulture));
		}
	}
}
