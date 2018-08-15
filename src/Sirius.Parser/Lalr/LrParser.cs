using System;
using System.Collections.Generic;
using System.Linq;

namespace Sirius.Parser.Lalr {
	public class LrParser<TToken>: ParserBase<TToken, LalrGrammar>
			where TToken: TokenBase {
		public LrParser(LalrGrammar grammar, IPeekable<TToken> tokenStream)
				: base(grammar, tokenStream) { }

		private Stack<ValueTuple<ParseTreeNode<TToken>, int>> Stack {
			get;
		} = new Stack<ValueTuple<ParseTreeNode<TToken>, int>>();

		public override ParseTreeNode<TToken> Parse() {
			var table = Grammar.ComputeParseTable();
			var currentState = table.StartState;

			// Push the start state onto the stack
			this.Stack.Push((new ParseTreeNode<TToken>(Grammar.Start, default(TToken)), currentState));
			foreach (var token in TokenStream) {
				var tokenSymbolId = token.Symbol;
				// Reduce any number of times for a given token. Always advance to the next token for no reduction.
				bool reduced;
				do {
					reduced = false;
					table.Action.TryGetValue((currentState, tokenSymbolId), out var action);

					// Get the action type. If action is null, default to the 'Error' action
					switch (action) {
					case ShiftAction shiftAction:
						// Shift N
						currentState = shiftAction.Number;
						this.Stack.Push((new ParseTreeNode<TToken>(tokenSymbolId, token), currentState));
						break;
					case ReduceSingleAction reduceAction:
						// Reduce by rule N
						var rule = Grammar.IndexedProductions[reduceAction.Number];
						var reduceLhs = rule.ProductionSymbolId;

						// Now create an array for the symbols:
						var symbols = new ParseTreeNode<TToken>[rule.RuleSymbolIds.Count];

						// Pop the thing off the stack
						for (var i = rule.RuleSymbolIds.Count-1; i >= 0; i--) {
							symbols[i] = this.Stack.Pop().Item1;
						}

						// Create a new Ast node
						var reducedNode = new ParseTreeNode<TToken>(reduceLhs, symbols);

						// Get the state at the top of the stack
						var topState = this.Stack.Peek().Item2;

						// Get the next transition key based on the item we're reducing by
						// It should exist in the goto table, we should never try to reduce when it doesn't make sense.
						var newState = table.Goto[(topState, reduceLhs)];

						// Push that onto the stack
						this.Stack.Push((reducedNode, newState));

						// Transition to the top state
						currentState = newState;

						// Keep reducing before moving to the next token
						reduced = true;
						break;
					case AcceptAction _:
						return this.Stack.Pop().Item1;
					default:
						var tokenStr = token.Lexeme ?? tokenSymbolId.ToString();
						Errors.Add($"Unexpected symbol: {tokenStr}");
						if (tokenSymbolId == Grammar.Eof) {
							// Just return whatever is on the stack
							return new ParseTreeNode<TToken>(Grammar.Unknown, this.Stack.Skip(1).Select(s => s.Item1).ToArray());
						}
						break;
					}
				} while (reduced);
			}
			return null;
		}
	}
}
