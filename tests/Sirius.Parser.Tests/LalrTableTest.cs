using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;

using Sirius.Collections;
using Sirius.Parser.Grammar;
using Sirius.Parser.Lalr;
using Sirius.RegularExpressions;
using Sirius.RegularExpressions.Automata;
using Sirius.RegularExpressions.Parser;
using Sirius.Unicode;

using Xunit;
using Xunit.Abstractions;

namespace Sirius.Parser {
	public class LalrTableTest {
		public class ExpressionFragmentEmitter: IParserFragmentEmitter<Token, char, long> {
			Expression IParserFragmentEmitter<Token, char, long>.CheckAndPreprocessTerminal(ParameterExpression tokenSymbolId, ParameterExpression tokenValue, ParameterExpression position) {
				return Expression.Block(
						Expression.Assign(
								position,
								Expression.Property(
										tokenValue,
										Reflect<Capture<char>>.GetProperty(c => c.Index))),
						Expression.NotEqual(
								Expression.Call(
										tokenSymbolId,
										Reflect<SymbolId>.GetMethod(id => id.ToInt32())),
								Expression.Constant(whitespace)));
			}

			Expression IParserFragmentEmitter<Token, char, long>.CreateNonterminal(ProductionRule rule, ParameterExpression[] productionNodes) {
				if (productionNodes.Length == 1) {
					return productionNodes[0];
				}
				return Expression.New(
						Reflect.GetConstructor(() => new Token(default(SymbolId), default(IEnumerable<Token>))),
						Expression.New(
								Reflect.GetConstructor(() => new SymbolId(default(int))),
								Expression.Constant(rule.ProductionSymbolId.ToInt32())),
						Expression.NewArrayInit(typeof(Token), productionNodes));
			}

			Expression IParserFragmentEmitter<Token, char, long>.CreateTerminal(SymbolId symbolId, ParameterExpression tokenValue, ParameterExpression position) {
				return Expression.New(
						Reflect.GetConstructor(() => new Token(default(SymbolId), default(string))),
						Expression.New(
								Reflect.GetConstructor(() => new SymbolId(default(int))),
								Expression.Constant(symbolId.ToInt32())),
						Expression.Call(
								Reflect.GetStaticMethod(() => default(IEnumerable<char>).AsString()),
								Expression.Convert(
										tokenValue,
										typeof(IEnumerable<char>))));
			}
		}

		public class ExpressionParser: ParserBase<char, Token, long> {
			private readonly ITestOutputHelper output;

			public ExpressionParser(ITestOutputHelper output, ParserContextBase<Token, char, long> context): base(ExpressionLalrTable.Value, context) {
				this.output = output;
			}

			protected override bool CheckAndPreprocessTerminal(ref SymbolId symbolId, Capture<char> letters, out long position) {
				position = letters.Index;
				return symbolId != whitespace;
			}

			protected override Token CreateNonterminal(ProductionRule rule, IReadOnlyList<Token> tokens) {
				return tokens.Count == 1 ? tokens[0] : new Token(rule.ProductionSymbolId, tokens);
			}

			protected override Token CreateTerminal(SymbolId symbol, Capture<char> data, long offset) {
				var value = data.AsString();
				this.output.WriteLine("Token: {0} {1}", Resolve(symbol), value);
				return new Token(symbol, value);
			}
		}

		public class Token {
			public Token(SymbolId symbol, string value) {
				this.Symbol = symbol;
				this.Value = value;
				this.Children = new Token[0];
			}

			public Token(SymbolId symbol, IEnumerable<Token> children) {
				this.Symbol = symbol;
				this.Children = children.ToArray();
			}

			public SymbolId Symbol {
				get;
			}

			public string Value {
				get;
			}

			public Token[] Children {
				get;
			}

			private void AppendTo(StringBuilder builder, Stack<bool> indent, Func<SymbolId, string> resolver) {
				var index = builder.Length;
				foreach (var ind in indent.Skip(1)) {
					builder.Insert(index, ind ? " |  " : "    ");
				}
				builder.Append(" +- ");
				builder.Append(resolver(this.Symbol));
				if (!string.IsNullOrEmpty(this.Value)) {
					builder.Append(": ");
					builder.Append(this.Value);
				}
				builder.AppendLine();
				if (this.Children != null) {
					for (var i = 0; i < this.Children.Length; i++) {
						indent.Push(i < (this.Children.Length - 1));
						this.Children[i].AppendTo(builder, indent, resolver);
						indent.Pop();
					}
				}
			}

			public override string ToString() {
				return this.ToString(s => s.ToString());
			}

			public string ToString(Func<SymbolId, string> resolver) {
				var builder = new StringBuilder();
				var indent = new Stack<bool>();
				indent.Push(false);
				this.AppendTo(builder, indent, resolver);
				return builder.ToString();
			}
		}

		private const int unknown = -2;
		private const int init = 0;
		private const int start = 1;
		private const int n1 = 2;
		private const int n2 = 3;
		private const int t1 = 4;
		private const int t2 = 5;
		private const int exprDash = 10;
		private const int exprDot = 11;
		private const int exprUnary = 12;
		private const int exprValue = 13;
		private const int opPlus = 14;
		private const int opMinus = 15;
		private const int opMultiply = 16;
		private const int opDivide = 17;
		private const int braceOpen = 18;
		private const int braceClose = 19;
		private const int number = 20;
		private const int whitespace = 21;

		private static readonly Func<SymbolId, string> Resolve = ((IReadOnlyDictionary<SymbolId, string>)new Dictionary<SymbolId, string>() {
						{SymbolId.Eof, "(EOF)"},
						{exprDash, "<ExprDash>"},
						{exprDot, "<ExprDot>"},
						{exprUnary, "<ExprUnary>"},
						{exprValue, "<ExprValue>"},
						{opPlus, "OpPlus"},
						{opMinus, "OpMinus"},
						{opMultiply, "OpMultiply"},
						{opDivide, "OpDivide"},
						{braceOpen, "BraceOpen"},
						{braceClose, "BraceClose"},
						{number, "Number"},
						{whitespace, "Whitespace"},
				})
				.CreateGetter();

		private static readonly Lazy<LalrTable> ExpressionLalrTable = new Lazy<LalrTable>(() =>
				new LalrTableGenerator(new GrammarBuilder(unknown, init, exprDash) {
								{exprDash, exprDash, opPlus, exprDot},
								{exprDash, exprDash, opMinus, exprDot},
								{exprDash, exprDot},
								{exprDot, exprDot, opMultiply, exprUnary},
								{exprDot, exprDot, opDivide, exprUnary},
								{exprDot, exprUnary},
								{exprUnary, opMinus, exprValue},
								{exprUnary, exprValue},
								{exprValue, braceOpen, exprDash, braceClose},
								{exprValue, number},
						})
						.ComputeTable(), LazyThreadSafetyMode.PublicationOnly);

		private static readonly Lazy<Dfa<char>> ExpressionDfa = new Lazy<Dfa<char>>(() =>
				new LexerBuilder<char>(new UnicodeUtf16Mapper(false, false), Utf16Chars.EOF) {
								{whitespace, @"\s+"},
								{braceOpen, @"\("},
								{braceClose, @"\)"},
								{number, @"[0-9]+"},
								{opPlus, @"\+"},
								{opMinus, @"-"},
								{opMultiply, @"\*"},
								{opDivide, @"\/"}
						}
						.ComputeDfa(), LazyThreadSafetyMode.PublicationOnly);

		private static readonly Lazy<Action<ParserContextBase<Token, char, long>, SymbolId, Capture<char>>> parserFunc = new Lazy<Action<ParserContextBase<Token, char, long>, SymbolId, Capture<char>>>(() => {
			var parserExpr = ParserEmitter.EmitParser(ExpressionLalrTable.Value, new ExpressionFragmentEmitter(), Resolve);
			return parserExpr.Compile();
		}, LazyThreadSafetyMode.PublicationOnly);

		private readonly ITestOutputHelper output;

		public LalrTableTest(ITestOutputHelper output) {
			this.output = output;
		}

		[Theory]
		[InlineData("10", true)]
		[InlineData("(10)", true)]
		[InlineData("1 + 2 * 3 + 4", true)]
		[InlineData("(1 + 2) * (3 + 4 * 1)", true)]
		[InlineData("(1", false)]
		[InlineData("(1+", false)]
		[InlineData("(1 + 2) * (3 + 4", false)]
		public void ExpressionGrammar(string expression, bool success) {
			var done = false;
			var parser = new ExpressionParser(this.output, new ParserContext<Token, char, long>(
					token => {
						this.output.WriteLine("");
						this.output.WriteLine("Parse tree:");
						this.output.WriteLine(token.ToString(Resolve));
						Assert.True(success);
						Assert.False(done);
						done = true;
					},
					(symbols, stack, symbol, value, position) => {
						this.output.WriteLine("");
						this.output.WriteLine("Syntax error, expected tokens:");
						this.output.WriteLine(string.Join(", ", symbols.Select(Resolve)));
						this.output.WriteLine("Current stack:");
						foreach (var token in stack) {
							this.output.WriteLine(token.ToString(Resolve));
						}
						Assert.False(success);
						Assert.False(done);
						done = true;
						return null;
					}));
			var lexer = new Lexer<char>(ExpressionDfa.Value, parser.ProcessToken, whitespace);
			lexer.Push(expression.Append(Utf16Chars.EOF).ToArray());
			lexer.Terminate();
			Assert.True(done);
		}

		[Theory]
		[InlineData("10", true)]
		[InlineData("(10)", true)]
		[InlineData("1 + 2 * 3 + 4", true)]
		[InlineData("(1 + 2) * (3 + 4 * 1)", true)]
		//		[InlineData("(1", false)]
		//		[InlineData("(1+", false)]
		//		[InlineData("(1 + 2) * (3 + 4", false)]
		public void ExpressionGrammarCompiled(string expression, bool success) {
			var done = false;
			var context = new ParserContext<Token, char, long>(
					token => {
						this.output.WriteLine("");
						this.output.WriteLine("Parse tree:");
						this.output.WriteLine(token.ToString(Resolve));
						Assert.True(success);
						Assert.False(done);
						done = true;
					},
					(symbols, stack, symbol, value, position) => {
						this.output.WriteLine("");
						this.output.WriteLine("Syntax error, expected tokens:");
						this.output.WriteLine(string.Join(", ", symbols.Select(Resolve)));
						this.output.WriteLine("Current stack:");
						foreach (var token in stack) {
							this.output.WriteLine(token.ToString(Resolve));
						}
						Assert.False(success);
						Assert.False(done);
						done = true;
						return null;
					});
			var lexer = new Lexer<char>(ExpressionDfa.Value, context.Bind(parserFunc.Value), whitespace);
			lexer.Push(expression.Append(Utf16Chars.EOF).ToArray());
			lexer.Terminate();
			Assert.True(done);
		}

		[Fact]
		public void SimpleGrammarTable() {
			// $accept ::= start $eof
			// start ::= n1 n1
			// n1 ::= t1 n1
			// n1 ::= t2
			var builder = new GrammarBuilder(unknown, init, start) {
					{start, n1, n1},
					{n1, t1, n1},
					{n1, t2}
			};
			var generator = new LalrTableGenerator(builder);
			var table = generator.ComputeTable();
			Assert.Equal(18, table.Action.Count);
			Assert.StartsWith("s3", table.Action[new StateKey<SymbolId>(0, t1)].ToString());
			Assert.StartsWith("s4", table.Action[new StateKey<SymbolId>(0, t2)].ToString());
			Assert.StartsWith("(accept)", table.Action[new StateKey<SymbolId>(1, SymbolId.Eof)].ToString());
			Assert.StartsWith("s3", table.Action[new StateKey<SymbolId>(2, t1)].ToString());
			Assert.StartsWith("s4", table.Action[new StateKey<SymbolId>(2, t2)].ToString());
			Assert.StartsWith("s3", table.Action[new StateKey<SymbolId>(3, t1)].ToString());
			Assert.StartsWith("s4", table.Action[new StateKey<SymbolId>(3, t2)].ToString());
			Assert.StartsWith("r3", table.Action[new StateKey<SymbolId>(4, SymbolId.Eof)].ToString());
			Assert.StartsWith("r3", table.Action[new StateKey<SymbolId>(4, t1)].ToString());
			Assert.StartsWith("r3", table.Action[new StateKey<SymbolId>(4, t2)].ToString());
			Assert.StartsWith("r1", table.Action[new StateKey<SymbolId>(5, SymbolId.Eof)].ToString());
			Assert.StartsWith("r2", table.Action[new StateKey<SymbolId>(6, SymbolId.Eof)].ToString());
			Assert.StartsWith("r2", table.Action[new StateKey<SymbolId>(6, t1)].ToString());
			Assert.StartsWith("r2", table.Action[new StateKey<SymbolId>(6, t2)].ToString());
			Assert.StartsWith("g2", table.Action[new StateKey<SymbolId>(0, n1)].ToString());
			Assert.StartsWith("g1", table.Action[new StateKey<SymbolId>(0, start)].ToString());
			Assert.StartsWith("g5", table.Action[new StateKey<SymbolId>(2, n1)].ToString());
			Assert.StartsWith("g6", table.Action[new StateKey<SymbolId>(3, n1)].ToString());
		}
	}
}
