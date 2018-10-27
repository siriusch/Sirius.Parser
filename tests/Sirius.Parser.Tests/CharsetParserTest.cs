using System;

using Sirius.Parser.Grammars.Charset;
using Sirius.RegularExpressions.Parser;
using Sirius.Unicode;

using Xunit;
using Xunit.Abstractions;

namespace Sirius.Parser {
	public class CharsetParserTest {
		private readonly ITestOutputHelper output;

		public CharsetParserTest(ITestOutputHelper output) {
			this.output = output;
		}

		[Theory]
		[InlineData("[a-z]")]
		[InlineData("[a-z] - [b] ^ [a-m] & [a-x]")]
		[InlineData("[^a-z] & {Letter}")]
		[InlineData("[0-9] + {Letter}")]
		public void ParseTest(string expression) {
			var result = default(CharsetNode<Codepoint>);
			var context = new ParserContext<CharsetNode<Codepoint>, char, long>(node => result = node);
			var parser = new CharsetParser<Codepoint>(context);
			var lexer = new CharsetLexer(parser.ProcessToken);
			lexer.Push(expression);
			lexer.Push(Utf16Chars.EOF);
			Assert.NotNull(result);
			var provider = new UnicodeCharSetProvider(UnicodeRanges.FromUnicodeName);
			this.output.WriteLine(result.Compute(provider).ToString());
		}

		[Fact]
		public void TableTest() {
			Assert.NotNull(CharsetGrammar.StateMachine);
			Assert.NotNull(CharsetGrammar.Table);
		}
	}
}
