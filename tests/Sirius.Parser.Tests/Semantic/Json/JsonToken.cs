using System;
using System.IO;
using System.Xml;

using Sirius.Parser.Semantic;
using Sirius.Parser.Semantic.Json;

[assembly: Charset(typeof(JsonValue), "unescaped", @"({Letter} | {Mark} | {Number} | {Punctuation} | {Other_Punctuation} | {Symbol} | {Space_Separator}) - [""\\]")]
[assembly: Charset(typeof(JsonValue), "hex", @"[0-9A-Fa-f]")]
[assembly: Terminal(typeof(JsonValue), "whitespace", @"\s+", Flags = TerminalFlags.Noise)]
[assembly: Terminal(typeof(JsonValue), "{")]
[assembly: Terminal(typeof(JsonValue), "}")]
[assembly: Terminal(typeof(JsonValue), "[")]
[assembly: Terminal(typeof(JsonValue), "]")]
[assembly: Terminal(typeof(JsonValue), ",")]
[assembly: Terminal(typeof(JsonValue), ":")]
[assembly: Rule(typeof(JsonValue), "value", "string")]
[assembly: Rule(typeof(JsonValue), "value", "number")]
[assembly: Rule(typeof(JsonValue), "value", "object")]
[assembly: Rule(typeof(JsonValue), "value", "array")]
[assembly: Rule(typeof(JsonValue), "value", "true")]
[assembly: Rule(typeof(JsonValue), "value", "false")]
[assembly: Rule(typeof(JsonValue), "value", "null")]
[assembly: StartSymbol(typeof(JsonValue), "value")]

namespace Sirius.Parser.Semantic.Json {
	public abstract class JsonValue {
		public abstract void WriteTo(TextWriter writer);

		public override string ToString() {
			var result = new StringWriter();
			this.WriteTo(result);
			return result.ToString();
		}

		public static readonly JsonNull Null = new JsonNull();
		public static readonly JsonValue<bool> True = new JsonValue<bool>(true);
		public static readonly JsonValue<bool> False = new JsonValue<bool>(false);

		[Terminal(typeof(JsonValue), "string", @"""({unescaped}|\\([""\\\/bfnrt]|u{hex}{hex}{hex}{hex}))*""")]
		public static JsonValue<string> ParseString(string value) {
			return new JsonValue<string>(value);
		}

		[Terminal(typeof(JsonValue), "true")]
		public static JsonValue<bool> ParseTrue() {
			return True;
		}

		[Terminal(typeof(JsonValue), "false")]
		public static JsonValue<bool> ParseFalse() {
			return False;
		}

		[Terminal(typeof(JsonValue), "number", @"-?(0|[1-9][0-9]*)(\.[0-9]+)?(e[+-]?[0-9]+)?", CaseInsensitive = true)]
		public static JsonValue<double> ParseNumber(string value) {
			return new JsonValue<double>(XmlConvert.ToDouble(value));
		}

		[Terminal(typeof(JsonValue), "null")]
		public static JsonNull ParseNull() {
			return Null;
		}
	}
}
