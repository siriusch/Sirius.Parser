using System;
using System.Linq;
using System.Threading;

using Sirius.Collections;
using Sirius.Parser.Semantic;
using Sirius.Parser.Semantic.Tokens;
using Sirius.RegularExpressions.Parser;
using Sirius.Unicode;

using Xunit;
using Xunit.Abstractions;

namespace Sirius.Parser {
	public class SemanticParserTest {
		private class SemanticParser: SemanticParser<TestToken, char, long> {
			public SemanticParser(ParserContextBase<TestToken, char, long> context): base(grammar.Value, context) { }

			protected override void TokenAction(SymbolId symbolId, Capture<char> value) {
				if (!this.Grammar.IsFlagSet(symbolId, TerminalFlags.Noise)) {
					this.PushTokenToParser(symbolId, value, value.Index);
				}
			}
		}

		private static readonly Lazy<SemanticParserGrammar<TestToken, char, long>> grammar = new Lazy<SemanticParserGrammar<TestToken, char, long>>(() => new SemanticParserGrammar<TestToken, char, long>(new UnicodeUtf16Mapper(false, false), Utf16Chars.EOF), LazyThreadSafetyMode.PublicationOnly);

		private readonly ITestOutputHelper output;

		public SemanticParserTest(ITestOutputHelper output) {
			this.output = output;
		}

		[Fact]
		public void BuildGrammarTest() {
			this.output.WriteLine(grammar.Value.ToString());
		}

		[Fact]
		public void FindGrammarPartsTest() {
			var parts = SemanticParserGrammar<TestToken, char, long>.FindGrammarParts()
					.OrderByDescending(p => p.Key is StartSymbolAttribute)
					.ThenBy(p => (p.Key as GrammarSymbolAttribute)?.SymbolName ?? "")
					.ThenBy(p => p.Key.GetType().Name)
					.ThenByDescending(p => (p.Key as RuleAttribute)?.RuleSymbolNames.Length)
					.ThenBy(p => (p.Key as RuleAttribute)?.ToString())
					.ToList();
			this.output.WriteLine(string.Join(Environment.NewLine, parts.Select(p => p.Key.ToString())));
			Assert.Equal(26, parts.Count);
		}

		[Theory]
		[InlineData("", false)]
		[InlineData("10", true)]
		[InlineData("(10)", true)]
		[InlineData("1 + 2 * 3 + 4", true)]
		[InlineData("(1 + 2) * (3 + 4 * 1)", true)]
		[InlineData("(1 + 2) * (3 + 4 * 1) / (1 + 2) * (3 + 4 * 1) / (1 + 2) * (3 + 4 * 1) / (1 + 2) * (3 + 4 * 1) / (1 + 2) * (3 + 4 * 1) / (1 + 2) * (3 + 4 * 1) / (1 + 2) * (3 + 4 * 1) / (1 + 2) * (3 + 4 * 1)", true)]
		[InlineData("(1", false)]
		[InlineData("(1+", false)]
		[InlineData("(1 + 2) * (3 + 4", false)]
		public void Parse(string text, bool success) {
			var done = false;
			TestToken result = null;
			var parser = new SemanticParser(new ParserContext<TestToken, char, long>(node => {
				result = node;
				done = true;
			}, ((symbolId, capture, position, expectedTokens, stack) => {
				this.output.WriteLine($"Syntax error. Expected tokens: {string.Join(", ", expectedTokens.Select(s => s.ToString(grammar.Value.ResolveSymbol)))}");
				done = true;
			})));
			try {
				parser.Push(text);
				parser.Push(Utf16Chars.EOF);
			} catch (Exception ex) {
				this.output.WriteLine(ex.ToString());
				done = true;
			}
			Assert.True(done);
			Assert.Equal(success, result != null);
		}
	}
}
