using System;

using Sirius.Parser.Semantic;
using Sirius.Parser.Semantic.Tokens;

[assembly:StartSymbol(typeof(TestToken), "<Expression>")]
[assembly:Rule(typeof(TestToken), "<Value>", "(", "<Expression>", ")", TrimSymbolName = "<Expression>")]
[assembly:Rule(typeof(TestToken), "<Value>", "Integer")]
[assembly:Rule(typeof(TestToken), "<Value>", "Float")]
[assembly:Rule(typeof(TestToken), "<Negate Exp>", "<Value>")]
[assembly:Rule(typeof(TestToken), "<Mult Exp>", "<Negate Exp>")]
[assembly:Rule(typeof(TestToken), "<Expression>", "<Mult Exp>")]
[assembly:Terminal(typeof(TestToken), "(Whitespace)", @"\s+", Flags = TerminalFlags.Noise)]
[assembly:Terminal(typeof(TestToken), "(LineComment)", @"--[^\r\n]*", Flags = TerminalFlags.Noise)]
[assembly:Terminal(typeof(TestToken), "(BlockComment)", @"\/\*([^\*]|\*[^\/])*\*\/", Flags = TerminalFlags.Noise)]

namespace Sirius.Parser.Semantic.Tokens {
	public class TestToken {}
}
