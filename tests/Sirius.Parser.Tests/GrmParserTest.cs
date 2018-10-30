using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Web;

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
	public class GrmParserTest {
		private class GrmGrammar {
			public GrmGrammar() {
				this.Resolve = ((IReadOnlyDictionary<SymbolId, string>)new Dictionary<SymbolId, string>() {
								{SymbolId.Eof, "(EOF)"},
								{SymUnknown, "(Unknown)"},
								{SymWhitespace, "(Whitespace)"},
								{SymNewline, "(Newline)"},
								{SymLineComment, "(LineComment)"},
								{SymBlockComment, "(BlockComment)"},
								{SymParameterName, "ParameterName"},
								{SymNonterminal, "Nonterminal"},
								{SymTerminal, "Terminal"},
								{SymAssign, "="},
								{SymDefine, "::="},
								{SymQuestion, "?"},
								{SymStar, "*"},
								{SymParensOpen, "("},
								{SymParensClose, ")"},
								{SymPlus, "+"},
								{SymMinus, "-"},
								{SymOr, "|"},
								{SymSetLiteral, "SetLiteral"},
								{SymSetName, "SetName"},
								{SymInit, "<Init>"},
								{SymGrammar, "<Grammar>"},
								{SymContent, "<Content>"},
								{SymDefinition, "<Definition>"},
								{SymNlOpt, "<NlOpt>"},
								{SymNl, "<Nl>"},
								{SymParameter, "<Parameter>"},
								{SymParameterBody, "<ParameterBody>"},
								{SymParameterItems, "<ParameterItems>"},
								{SymParameterItem, "<ParameterItem>"},
								{SymSetDecl, "<SetDecl>"},
								{SymSetExp, "<SetExp>"},
								{SymSetItem, "<SetItem>"},
								{SymTerminalDecl, "<TerminalDecl>"},
								{SymTerminalName, "<TerminalName>"},
								{SymRegExp, "<RegExp>"},
								{SymRegExpSeq, "<RegExpSeq>"},
								{SymRegExpItem, "<RegExpItem>"},
								{SymRegExp2, "<RegExp2>"},
								{SymKleeneOpt, "<KleeneOpt>"},
								{SymRuleDecl, "<RuleDecl>"},
								{SymHandles, "<Handles>"},
								{SymHandle, "<Handle>"},
								{SymSymbol, "<Symbol>"},
						})
						.CreateGetter();
				var mapper = new UnicodeUtf16Mapper(false, false);
				var charsetPrintable = Codepoints.ValidBmp - UnicodeRanges.FromUnicodeCategory(UnicodeCategory.Control) - UnicodeRanges.InCombiningDiacriticalMarks;
				var charsetAlphanumeric = UnicodeRanges.Letter | UnicodeRanges.Number;
				var charset = new UnicodeCharSetProvider(new Dictionary<string, RangeSet<Codepoint>>() {
						{"Parameter Ch", charsetPrintable - '\'' - '"'},
						{"Nonterminal Ch", charsetAlphanumeric | '_' | '-' | '.' | ' '},
						{"Terminal Ch", charsetAlphanumeric | '_' | '-' | '.'},
						{"Literal Ch", charsetPrintable - '\''},
						{"Set Literal Ch", charsetPrintable - '[' - ']' - '\''},
						{"Set Name Ch", charsetPrintable - '{' - '}'},
						{"Whitespace Ch", UnicodeRanges.SpaceSeparator | '\t' | '\v'}
				});
				this.DfaStateMachine = new LexerBuilder<char>(mapper, Utf16Chars.EOF, charset) {
								{SymParameterName, @"""{Parameter Ch}+"""},
								{SymNonterminal, @"<{Nonterminal Ch}+>"},
								{SymTerminal, @"{Terminal Ch}+|'{Literal Ch}*'"},
								{SymSetLiteral, @"\[({Set Literal Ch}+|'{Literal Ch}*')+\]"},
								{SymSetName, @"\{{Set Name Ch}+\}"},
								{SymWhitespace, @"{Whitespace Ch}+"},
								{SymNewline, @"\r\n?|\n\r?"},
								{SymLineComment, @"![^\r\n]*"},
								{SymBlockComment, @"!\*([^\*]|\*[^!])*\*!"},
								{SymAssign, @"="},
								{SymDefine, @"::="},
								{SymPlus, @"\+"},
								{SymMinus, @"\-"},
								{SymOr, @"\|"},
								{SymQuestion, @"\?"},
								{SymStar, @"\*"},
								{SymParensOpen, @"\("},
								{SymParensClose, @"\)"}
						}
						.CreateStateMachine(out var dfaStartState)
						.Compile();
				this.DfaStartState = dfaStartState;
				this.LalrTable = new LalrTableGenerator(new GrammarBuilder(SymUnknown, SymInit, SymGrammar) {
								{SymGrammar, SymNlOpt, SymContent},
								{SymContent, SymContent, SymDefinition},
								{SymContent, SymDefinition},
								{SymDefinition, SymParameter},
								{SymDefinition, SymSetDecl},
								{SymDefinition, SymTerminalDecl},
								{SymDefinition, SymRuleDecl},
								{SymNlOpt, SymNewline, SymNlOpt},
								{SymNlOpt},
								{SymNl, SymNewline, SymNl},
								{SymNl, SymNewline},
								{SymParameter, SymParameterName, SymNlOpt, SymAssign, SymParameterBody, SymNl},
								{SymParameterBody, SymParameterBody, SymNlOpt, SymOr, SymParameterItems},
								{SymParameterBody, SymParameterItems},
								{SymParameterItems, SymParameterItems, SymParameterItem},
								{SymParameterItems, SymParameterItem},
								{SymParameterItem, SymParameterName},
								{SymParameterItem, SymTerminal},
								{SymParameterItem, SymSetLiteral},
								{SymParameterItem, SymSetName},
								{SymParameterItem, SymNonterminal},
								{SymSetDecl, SymSetName, SymNlOpt, SymAssign, SymSetExp, SymNl},
								{SymSetExp, SymSetExp, SymNlOpt, SymPlus, SymSetItem},
								{SymSetExp, SymSetExp, SymNlOpt, SymMinus, SymSetItem},
								{SymSetExp, SymSetItem},
								{SymSetItem, SymSetLiteral},
								{SymSetItem, SymSetName},
								{SymTerminalDecl, SymTerminalName, SymNlOpt, SymAssign, SymRegExp, SymNl},
								{SymTerminalName, SymTerminalName, SymTerminal},
								{SymTerminalName, SymTerminal},
								{SymRegExp, SymRegExp, SymNlOpt, SymOr, SymRegExpSeq},
								{SymRegExp, SymRegExpSeq},
								{SymRegExpSeq, SymRegExpSeq, SymRegExpItem},
								{SymRegExpSeq, SymRegExpItem},
								{SymRegExpItem, SymSetLiteral, SymKleeneOpt},
								{SymRegExpItem, SymSetName, SymKleeneOpt},
								{SymRegExpItem, SymTerminal, SymKleeneOpt},
								{SymRegExpItem, SymParensOpen, SymRegExp2, SymParensClose, SymKleeneOpt},
								{SymRegExp2, SymRegExp2, SymOr, SymRegExpSeq},
								{SymRegExp2, SymRegExpSeq},
								{SymKleeneOpt, SymPlus},
								{SymKleeneOpt, SymQuestion},
								{SymKleeneOpt, SymStar},
								{SymKleeneOpt},
								{SymRuleDecl, SymNonterminal, SymNlOpt, SymDefine, SymHandles, SymNl},
								{SymHandles, SymHandles, SymNlOpt, SymOr, SymHandle},
								{SymHandles, SymHandle},
								{SymHandle, SymHandle, SymSymbol},
								{SymHandle},
								{SymSymbol, SymTerminal},
								{SymSymbol, SymNonterminal}
						})
						.ComputeTable();
			}

			public Func<SymbolId, string> Resolve {
				get;
			}

			public Id<DfaState<LetterId>> DfaStartState {
				get;
			}

			public DfaStateMachine<LetterId, char> DfaStateMachine {
				get;
			}

			public LalrTable LalrTable {
				get;
			}
		}

		private class GrmNode { }

		private class GrmParser: ParserBase<char, GrmNode, LineInfo> {
			private int column = 1;
			private int line = 1;

			public GrmParser(ParserContextBase<GrmNode, char, LineInfo> context): base(grammar.Value.LalrTable, context) {}

			protected override bool CheckAndPreprocessTerminal(ref SymbolId symbolId, ref Capture<char> letters, out LineInfo position) {
				position = new LineInfo(this.line, this.column);
				switch (symbolId.ToInt32()) {
				case SymWhitespace:
				case SymLineComment:
					return false;
				case SymBlockComment:
					using (var iter = letters.GetEnumerator()) {
						var hasChar = iter.MoveNext();
						while (hasChar) {
							switch (iter.Current) {
							case '\r':
								this.ConsumeNewline(iter, ref hasChar, '\n');
								continue;
							case '\n':
								this.ConsumeNewline(iter, ref hasChar, '\r');
								continue;
							}
							this.column++;
							hasChar = iter.MoveNext();
						}
					}
					return false;
				case SymNewline:
					this.line++;
					this.column = 1;
					break;
				default:
					this.column += letters.Count;
					break;
				}
				return true;
			}

			private void ConsumeNewline(IEnumerator<char> iter, ref bool hasChar, char optionalFollowupChar) {
				Debug.Assert(hasChar);
				this.line++;
				this.column = 1;
				hasChar = iter.MoveNext();
				if (hasChar && (iter.Current == optionalFollowupChar)) {
					hasChar = iter.MoveNext();
				}
			}

			protected override GrmNode CreateNonterminal(ProductionRule rule, IReadOnlyList<GrmNode> nodes) {
				return null;
			}

			protected override GrmNode CreateTerminal(SymbolId symbolId, Capture<char> letters, LineInfo offset) {
				return null;
			}
		}

		private struct LineInfo {
			public LineInfo(int line, int column) {
				this.Line = line;
				this.Column = column;
			}

			public int Line {
				get;
			}

			public int Column {
				get;
			}

			public override string ToString() {
				return $"{this.Line}:{this.Column}";
			}
		}

		private const int SymUnknown = -1;
		private const int SymWhitespace = 0;
		private const int SymNewline = 100;
		private const int SymLineComment = 101;
		private const int SymBlockComment = 102;
		private const int SymParameterName = 110;
		private const int SymNonterminal = 111;
		private const int SymTerminal = 112;
		private const int SymAssign = 113;
		private const int SymDefine = 114;
		private const int SymQuestion = 115;
		private const int SymStar = 116;
		private const int SymParensOpen = 117;
		private const int SymParensClose = 118;
		private const int SymPlus = 119;
		private const int SymMinus = 120;
		private const int SymOr = 121;
		private const int SymSetLiteral = 122;
		private const int SymSetName = 123;
		private const int SymInit = 200;
		private const int SymGrammar = 201;
		private const int SymContent = 202;
		private const int SymDefinition = 203;
		private const int SymNlOpt = 204;
		private const int SymNl = 205;
		private const int SymParameter = 206;
		private const int SymParameterBody = 207;
		private const int SymParameterItems = 208;
		private const int SymParameterItem = 209;
		private const int SymSetDecl = 210;
		private const int SymSetExp = 211;
		private const int SymSetItem = 212;

		private const int SymTerminalDecl = 213;

		private const int SymTerminalName = 214;
		private const int SymRegExp = 215;
		private const int SymRegExpSeq = 216;
		private const int SymRegExpItem = 217;
		private const int SymRegExp2 = 218;
		private const int SymKleeneOpt = 219;
		private const int SymRuleDecl = 220;
		private const int SymHandles = 221;
		private const int SymHandle = 222;
		private const int SymSymbol = 223;

		private static readonly Lazy<GrmGrammar> grammar = new Lazy<GrmGrammar>(LazyThreadSafetyMode.PublicationOnly);
		private readonly ITestOutputHelper output;

		public GrmParserTest(ITestOutputHelper output) {
			this.output = output;
		}

		[Fact]
		public void GrammarDfaTest() {
			var dfa = grammar.Value;
			this.output.WriteLine(dfa.ToString());
		}

		[Theory]
		[InlineData(@"!-----------------------------------------------------------------------------------
! GOLD Meta-Language
!
! This is the very simple grammar used to define grammars using the GOLD Parser.
! The grammar was revised for version 2.0.5 of the GOLD Parser Builder. The changes
! were designed to:
!
!   1. Make it easier to use line comments to disable individual rules. 
!   2. Allow the developer to use optional newlines for readability.
! 
! www.devincook.com/goldparser 
! -----------------------------------------------------------------------------------
 
 
""Name""         = 'GOLD Meta-Language'
""Version""      = '2.6.0'
""Author""       = 'Devin Cook'
""About""        = 'This grammar defines the GOLD Meta-Language.'

""Start Symbol"" = <Grammar>


! The token definitions are very complex. Many definitions allow an 
! ""Override Sequence"" such as the backslash in C. In this case, it is 
! single quotes. Not all the tokens have overrides. I only added them where
! their use could be justified.


! ====================================================================
! Special Terminals
! ====================================================================

{Parameter Ch}   = {Printable}    - [""] - ['']
{Nonterminal Ch} = {Alphanumeric} + [_-.] + {Space} 
{Terminal Ch}    = {Alphanumeric} + [_-.] 
{Literal Ch}     = {Printable}    - ['']       !Basically anything, DO NOT CHANGE!
{Set Literal Ch} = {Printable}    - ['['']'] - ['']
{Set Name Ch}    = {Printable}    - [{}]

ParameterName  = '""' {Parameter Ch}+ '""' 
Nonterminal    = '<' {Nonterminal Ch}+ '>'
Terminal       = {Terminal Ch}+  | '' {Literal Ch}* ''  
SetLiteral     = '[' ({Set Literal Ch} | '' {Literal Ch}* '' )+ ']'
SetName        = '{' {Set Name Ch}+ '}'


! ====================================================================
! Line-Based Grammar Declarations
! ====================================================================

{Whitespace Ch} = {Whitespace} - {CR} - {LF}

Whitespace = {Whitespace Ch}+
Newline    = {CR}{LF} | {CR} | {LF}  

! ====================================================================
! Comments
! ====================================================================

Comment Line  = '!'
Comment Start = '!*'
Comment End   = '*!'


! ====================================================================
! Rules
! ====================================================================

<Grammar>  ::= <nl opt> <Content>     ! The <nl opt> here removes all newlines before the first definition

<Content> ::= <Content> <Definition> 
			| <Definition>

<Definition> ::= <Parameter>
			   | <Set Decl>
			   | <Terminal Decl>
			   | <Rule Decl>
				

! Optional series of New Line - use below is restricted
<nl opt> ::= NewLine <nl opt>
		   |

! One or more New Lines
<nl> ::= NewLine  <nl>
	   | NewLine 

! ====================================================================
! Parameter Definition
! ====================================================================

<Parameter> ::= ParameterName <nl opt> '=' <Parameter Body> <nl>

<Parameter Body>  ::= <Parameter Body> <nl opt> '|' <Parameter Items>  
					| <Parameter Items> 

<Parameter Items> ::= <Parameter Items> <Parameter Item> 
					| <Parameter Item>

<Parameter Item>  ::= ParameterName 
					| Terminal    
					| SetLiteral    
					| SetName       
					| Nonterminal

! ====================================================================
! Set Definition
! ====================================================================

<Set Decl>  ::= SetName <nl opt> '=' <Set Exp> <nl>

<Set Exp>   ::= <Set Exp> <nl opt> '+' <Set Item>
			  | <Set Exp> <nl opt> '-' <Set Item>
			  | <Set Item>
			
<Set Item>  ::= SetLiteral         ! [ ... ]
			  | SetName            ! { ... }

! ====================================================================
! Terminal Definition
! ====================================================================
			 
<Terminal Decl> ::= <Terminal Name> <nl opt> '=' <Reg Exp> <nl>
 
<Terminal Name> ::= <Terminal Name> Terminal
				  | Terminal 


<Reg Exp>       ::= <Reg Exp> <nl opt> '|' <Reg Exp Seq>
				  | <Reg Exp Seq>

<Reg Exp Seq>   ::= <Reg Exp Seq> <Reg Exp Item>
				  | <Reg Exp Item> 

<Reg Exp Item>  ::= SetLiteral          <Kleene Opt>
				  | SetName             <Kleene Opt>
				  | Terminal            <Kleene Opt>
				  | '(' <Reg Exp 2> ')' <Kleene Opt>

!No newlines allowed

<Reg Exp 2>     ::= <Reg Exp 2> '|' <Reg Exp Seq>
				  | <Reg Exp Seq>

<Kleene Opt> ::= '+'
			   | '?' 
			   | '*' 
			   | 

! ====================================================================
! Rule Definition
! ====================================================================

<Rule Decl>  ::= Nonterminal <nl opt> '::=' <Handles> <nl>  

<Handles>    ::= <Handles> <nl opt> '|' <Handle>
			   | <Handle>
			 
<Handle>     ::= <Handle> <Symbol>   !Zero or more               
			   |                     !Leave the entry blank - makes a ""null""

<Symbol>     ::= Terminal
			   | Nonterminal")]
		public void GrammarParseTest(string grm) {
			var dfa = grammar.Value.DfaStateMachine;
			var done = false;
			var context = new ParserContext<GrmNode, char, LineInfo>(node => done = true, grammar.Value.Resolve);
			var parser = new GrmParser(context);
			var lexer = new Lexer<char, LetterId>(dfa, grammar.Value.DfaStartState, true, (symbolId, capture) => {
				this.output.WriteLine($"{grammar.Value.Resolve(symbolId)}@{capture.Index}: {HttpUtility.JavaScriptStringEncode(capture.AsString(), false)}");
				parser.ProcessToken(symbolId, capture);
			});
			lexer.Push(grm);
			lexer.Push(Environment.NewLine);
			lexer.Push(Utf16Chars.EOF);
			lexer.Terminate();
			Assert.True(done);
		}
	}
}
