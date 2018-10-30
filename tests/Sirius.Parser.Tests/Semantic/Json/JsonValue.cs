using System;
using System.Globalization;
using System.IO;

namespace Sirius.Parser.Semantic.Json {
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
