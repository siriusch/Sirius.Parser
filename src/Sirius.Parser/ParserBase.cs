using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Sirius.Collections;
using Sirius.Parser.Lalr;

namespace Sirius.Parser {
	public abstract class ParserBase<TInput, TAstNode, TPosition> {
		private readonly LalrTable table;
		private readonly ParserContextBase<TAstNode, TInput, TPosition> context;

		protected ParserBase(LalrTable table, ParserContextBase<TAstNode, TInput, TPosition> context) {
			this.table = table;
			this.context = context;
		}

		private bool CanShift(SymbolId tokenSymbolId) {
			var simulatedState = this.context.currentState;
			for (;;) {
				this.table.Action.TryGetValue(new StateKey<SymbolId>(simulatedState.State, tokenSymbolId), out var action);
				switch (action) {
				case ShiftAction _:
				case AcceptAction _:
					return true;
				case ReduceSingleAction reduceAction:
					foreach (var _ in reduceAction.ProductionRule.RuleSymbolIds) {
						simulatedState = simulatedState.Parent;
					}
					var newState = ((GotoAction)this.table.Action[new StateKey<SymbolId>(simulatedState.State, reduceAction.ProductionRule.ProductionSymbolId)]).NewState;
					simulatedState = new ParserState<TAstNode>(default(TAstNode), newState, simulatedState);
					continue;
				default:
					return false;
				}
			}
		}

		protected abstract TAstNode CreateNonterminal(ProductionRule rule, IReadOnlyList<TAstNode> nodes);

		protected abstract TAstNode CreateTerminal(SymbolId symbolId, Capture<TInput> letters, TPosition offset);

		protected abstract bool CheckAndPreprocessTerminal(ref SymbolId symbolId, ref Capture<TInput> letters, out TPosition position);

		private void DoReduce(ProductionRule rule) {
			var currentState = this.context.currentState;
			// Take all AST nodes required for reduction from stack
			var nodes = new TAstNode[rule.RuleSymbolIds.Count];
			for (var i = nodes.Length-1; i >= 0; i--) {
				nodes[i] = currentState.Node;
				currentState = currentState.Parent;
			}
			var node = this.CreateNonterminal(rule, nodes);
			// Get the next transition key based on the item being reduced (should exist as goto in the table) and push onto the stack
			var topState = currentState.State;
			var newState = ((GotoAction)this.table.Action[new StateKey<SymbolId>(topState, rule.ProductionSymbolId)]).NewState;
			// Transition to the top state
			this.context.currentState = new ParserState<TAstNode>(node, newState, currentState);
		}

		protected virtual void SyntaxError(ref SymbolId tokenSymbolId, ref Capture<TInput> tokenValue, TPosition position, IEnumerable<SymbolId> expectedSymbols) {
			this.context.SyntaxError(tokenSymbolId, tokenValue, position, expectedSymbols);
		}

		public virtual void ProcessToken(SymbolId tokenSymbolId, Capture<TInput> tokenValue) {
			if (this.CheckAndPreprocessTerminal(ref tokenSymbolId, ref tokenValue, out var position)) {
				var initialState = this.context.currentState;
				for (;;) {
					this.table.Action.TryGetValue(new StateKey<SymbolId>(this.context.currentState.State, tokenSymbolId), out var action);
					// Get the action type. If action is null, default to the 'Error' action
					switch (action) {
					case ShiftAction shiftAction:
						this.context.currentState = new ParserState<TAstNode>(this.CreateTerminal(tokenSymbolId, tokenValue, position), shiftAction.NewState, this.context.currentState);
						return;
					case ReduceSingleAction reduceAction:
						this.DoReduce(reduceAction.ProductionRule);
						// Keep reducing before moving to the next token
						continue;
					case AcceptAction _:
						this.context.Accept(this.context.currentState.Node);
						this.context.currentState = this.context.currentState.Parent;
						Debug.Assert(this.context.currentState.Parent == null);
						return;
					}
					this.context.currentState = initialState;
					var expectedSymbols = this.table.Action.Where(p => p.Key.State == this.context.currentState.State && p.Value.Type != ActionType.Goto).Select(p => p.Key.Value).Where(this.CanShift);
					var initialSymbolId = tokenSymbolId;
					this.SyntaxError(ref tokenSymbolId, ref tokenValue, position, expectedSymbols);
					if (initialSymbolId == tokenSymbolId) {
						return;
					}
				}
			}
		}
	}
}
