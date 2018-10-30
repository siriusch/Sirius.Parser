using System;
using System.Diagnostics;

using Sirius.RegularExpressions.Parser;
using Sirius.Unicode;

using Xunit;
using Xunit.Abstractions;

namespace Sirius.Parser.Semantic.Json {
	public class JsonTest {
		private static readonly Lazy<JsonGrammar<char>> grammar = new Lazy<JsonGrammar<char>>(() => new JsonGrammar<char>(new UnicodeUtf16Mapper(false, false), Utf16Chars.EOF));

		private readonly ITestOutputHelper output;

		public JsonTest(ITestOutputHelper output) {
			this.output = output;
		}

		[Fact]
		public void Grammar() {
			this.output.WriteLine(string.Join(", ", grammar.Value.SymbolsByName.Keys));
		}

		[Theory]
		[InlineData("{}")]
		[InlineData(@"[{
  ""Herausgeber"": ""Xema"",
  ""Nummer"": ""1234-5678-9012-3456"",
  ""Deckung"": 2e+6,
  ""Waehrung"": ""EURO"",
  ""Inhaber"":
  {
    ""Name"": ""Mustermann"",
    ""Vorname"": ""Max"",
    ""maennlich"": true,
    ""Hobbys"": [""Reiten"", ""Golfen"", ""Lesen""],
    ""Alter"": 42,
    ""Kinder"": [],
    ""Partner"": null
  }
}]")]
		[InlineData(@"{
  ""Herausgeber"": ""Xema"",
  ""Nummer"": ""1234-5678-9012-3456"",
  ""Deckung"": 2e+6,
  ""Waehrung"": ""EURO"",
  ""Inhaber"":
  {
    ""Name"": ""Mustermann"",
    ""Vorname"": ""Max"",
    ""maennlich"": true,
    ""Hobbys"": [""Reiten"", ""Golfen"", ""Lesen""],
    ""Alter"": 42,
    ""Kinder"": [],
    ""Partner"": null
  }
}")]
		public void ParseJson(string json) {
			var done = false;
			var sw = new Stopwatch();
			var parser = new JsonParser<char>(grammar.Value, new ParserContext<JsonToken, char, long>(node => {
				sw.Stop();
				done = true;
				this.output.WriteLine($"Parse ticks: {sw.ElapsedTicks} ({sw.ElapsedMilliseconds}ms)");
				this.output.WriteLine(node.ToString());
			}));
			sw.Start();
			parser.Push(json);
			parser.Push(Utf16Chars.EOF);
		}
	}
}
