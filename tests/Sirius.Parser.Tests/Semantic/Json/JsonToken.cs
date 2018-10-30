using System;
using System.Xml;

using Sirius.Parser.Semantic;
using Sirius.Parser.Semantic.Json;

[assembly: Charset(typeof(JsonToken), "unescaped", @"({Letter} | {Mark} | {Number} | {Punctuation} | {Other_Punctuation} | {Symbol} | {Space_Separator}) - [""\\]")]
[assembly: Charset(typeof(JsonToken), "hex", @"[0-9A-Fa-f]")]
[assembly: Terminal(typeof(JsonToken), "whitespace", @"\s+", Flags = TerminalFlags.Noise)]
[assembly: Terminal(typeof(JsonToken), "{")]
[assembly: Terminal(typeof(JsonToken), "}")]
[assembly: Terminal(typeof(JsonToken), "[")]
[assembly: Terminal(typeof(JsonToken), "]")]
[assembly: Terminal(typeof(JsonToken), ",")]
[assembly: Terminal(typeof(JsonToken), ":")]
[assembly: Rule(typeof(JsonToken), "value", "string")]
[assembly: Rule(typeof(JsonToken), "value", "number")]
[assembly: Rule(typeof(JsonToken), "value", "object")]
[assembly: Rule(typeof(JsonToken), "value", "array")]
[assembly: Rule(typeof(JsonToken), "value", "true")]
[assembly: Rule(typeof(JsonToken), "value", "false")]
[assembly: Rule(typeof(JsonToken), "value", "null")]
[assembly: StartSymbol(typeof(JsonToken), "value")]

namespace Sirius.Parser.Semantic.Json {
	public abstract class JsonToken {
		public static readonly JsonNull Null = new JsonNull();
		public static readonly JsonValue<bool> True = new JsonValue<bool>(true);
		public static readonly JsonValue<bool> False = new JsonValue<bool>(false);

		[Terminal(typeof(JsonToken), "string", @"""({unescaped}|\\([""\\\/bfnrt]|u{hex}{hex}{hex}{hex}))*""")]
		public static JsonValue<string> ParseString(string value) {
			return new JsonValue<string>(value);
		}

		[Terminal(typeof(JsonToken), "true")]
		public static JsonValue<bool> ParseTrue() {
			return True;
		}

		[Terminal(typeof(JsonToken), "false")]
		public static JsonValue<bool> ParseFalse() {
			return False;
		}

		[Terminal(typeof(JsonToken), "number", @"-?(0|[1-9][0-9]*)(\.[0-9]+)?(e[+-]?[0-9]+)?", CaseInsensitive = true)]
		public static JsonValue<double> ParseNumber(string value) {
			return new JsonValue<double>(XmlConvert.ToDouble(value));
		}

		[Terminal(typeof(JsonToken), "null")]
		public static JsonNull ParseNull() {
			return Null;
		}
	}
}
