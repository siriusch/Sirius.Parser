using System;

using Sirius.Parser.Charset;
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
			var provider = new UnicodeCharSetProvider(UnicodeRanges.FromUnicodeName);
			var result = CharsetParser.Parse(expression).Compute(provider);
			this.output.WriteLine(result.ToString());
		}

		[Fact]
		public void TableTest() {
			Assert.NotNull(CharsetGrammar.LexerStateMachine);
			Assert.NotNull(CharsetGrammar.Table);
		}
	}
}
