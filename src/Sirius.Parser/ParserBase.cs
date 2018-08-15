using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Sirius.Parser.Lalr;

namespace Sirius.Parser {
	public abstract class ParserBase<TLetter, TAstNode> {
		private class ParserState {
			public ParserState(TAstNode node, int state, ParserState parent) {
				this.Parent = parent;
				this.Node = node;
				this.State = state;
			}

			public TAstNode Node {
				get;
			}

			public int State {
				get;
			}

			public ParserState Parent {
				get;
			}
		}

		private readonly LalrTable table;
		private ParserState currentState;

		public ParserBase(LalrTable table) {
			this.table = table;
			this.currentState = new ParserState(default(TAstNode), table.StartState, null);
		}

		protected abstract void Accept(TAstNode node);

		private bool CanShift(SymbolId tokenSymbolId) {
			var simulatedState = this.currentState;
			for (;;) {
				this.table.Action.TryGetValue(new StateKey<SymbolId>(simulatedState.State, tokenSymbolId), out var action);
				switch (action) {
				case ShiftAction _:
				case AcceptAction _:
					return true;
				case ReduceSingleAction reduceAction:
					DoReduce(ref simulatedState, reduceAction, true);
					continue;
				default:
					return false;
				}
			}
		}

		protected abstract TAstNode CreateNonterminal(ProductionRule rule, IReadOnlyList<TAstNode> nodes);

		protected abstract TAstNode CreateTerminal(SymbolId symbolId, IEnumerable<TLetter> letters, long offset);

		private void DoReduce(ref ParserState currentState, ReduceSingleAction reduceSingleAction, bool simulate) {
			var rule = reduceSingleAction.ProductionRule;
			// Take all AST nodes required for reduction from stack
			var node = default(TAstNode);
			if (simulate) {
				for (var i = rule.RuleSymbolIds.Count-1; i >= 0; i--) {
					currentState = currentState.Parent;
				}
			} else {
				var nodes = new TAstNode[rule.RuleSymbolIds.Count];
				for (var i = nodes.Length-1; i >= 0; i--) {
					nodes[i] = currentState.Node;
					currentState = currentState.Parent;
				}
				node = CreateNonterminal(rule, nodes);
			}
			// Get the next transition key based on the item being reduced (should exist as goto in the table) and push onto the stack
			var topState = currentState.State;
			var newState = ((GotoAction)this.table.Action[new StateKey<SymbolId>(topState, rule.ProductionSymbolId)]).NewState;
			// Transition to the top state
			currentState = new ParserState(node, newState, currentState);
		}

		public void ProcessToken(SymbolId tokenSymbolId, IEnumerable<TLetter> tokenValue, long offset) {
			var initialState = this.currentState;
			for (;;) {
				this.table.Action.TryGetValue(new StateKey<SymbolId>(this.currentState.State, tokenSymbolId), out var action);
				// Get the action type. If action is null, default to the 'Error' action
				switch (action) {
				case ShiftAction shiftAction:
					this.currentState = new ParserState(CreateTerminal(tokenSymbolId, tokenValue, offset), shiftAction.NewState, this.currentState);
					return;
				case ReduceSingleAction reduceAction:
					DoReduce(ref this.currentState, reduceAction, false);
					// Keep reducing before moving to the next token
					continue;
				case AcceptAction _:
					Accept(this.currentState.Node);
					this.currentState = this.currentState.Parent;
					Debug.Assert(this.currentState.Parent == null);
					return;
				default:
					this.currentState = initialState;
					SyntaxError(StateStack(), this.table.Action.Where(p => p.Key.State == this.currentState.State && p.Value.Type != ActionType.Goto).Select(p => p.Key.Value).Where(CanShift));
					return;
				}
			}
		}

		private IEnumerable<TAstNode> StateStack() {
			for (var current = this.currentState; current.Parent != null; current = current.Parent) {
				yield return current.Node;
			}
		}

		protected abstract void SyntaxError(IEnumerable<TAstNode> stack, IEnumerable<SymbolId> expectedSymbols);
	}
}