using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Sirius.Collections;
using Sirius.Parser.Lalr;

namespace Sirius.Parser {
	public interface IParserFragmentEmitter<TAstNode, TInput, TPosition> {
		/// <summary>
		///     Create an expression to preprocess the token.
		/// </summary>
		/// <remarks>This is executed for every data supplied by the tokenizer, use it to compute the position based on input.</remarks>
		/// <param name="tokenSymbolId"><see cref="SymbolId" />: Read/Write</param>
		/// <param name="tokenValue"><see cref="Capture{T}" /> (T = TInput): Read/Write</param>
		/// <param name="position">TPosition: Write</param>
		/// <returns>Expression returning a <see cref="Boolean" />: <c>true</c> to process the token, <c>false</c> to skip it</returns>
		Expression CheckAndPreprocessTerminal(ParameterExpression tokenSymbolId, ParameterExpression tokenValue, ParameterExpression position);

		/// <summary>
		///     Create a TAstNode descendant for a nonterminal rule.
		/// </summary>
		/// <param name="rule">Production rule</param>
		/// <param name="productionNodes">Variables containing the TAstNodes for the rule production</param>
		/// <returns>Expression returning a TAstNode descendant (may also be <c>null</c>)</returns>
		Expression CreateNonterminal(ProductionRule rule, ParameterExpression[] productionNodes);

		/// <summary>
		///     Create a TAstNode descendant for a terminal symbol.
		/// </summary>
		/// <param name="symbolId">Symbol of the terminal to create</param>
		/// <param name="tokenValue"><see cref="Capture{T}" /> (T = TInput): Read</param>
		/// <param name="position">TPosition: Read</param>
		/// <returns>Expression returning a TAstNode descendant (may also be <c>null</c>)</returns>
		Expression CreateTerminal(SymbolId symbolId, ParameterExpression tokenValue, ParameterExpression position);

		/// <summary>
		///     Extension point to implement syntax error retry handling (with a different symbol).
		/// </summary>
		/// <param name="expectedSymbols"><see cref="IEnumerable{T}" /> (T = <see cref="SymbolId" />): Read</param>
		/// <param name="tokenSymbolId"><see cref="SymbolId" />: Read/Write</param>
		/// <param name="tokenValue"><see cref="Capture{T}" /> (T = TInput): Read/Write</param>
		/// <param name="position">TPosition: Read</param>
		/// <returns>
		///     An expression with a boolean return type (<c>true</c> retries the parsing), or <c>null</c> to invoke
		///     <c>context.SyntaxError</c> and break.
		/// </returns>
		Expression SyntaxError(Expression expectedSymbols, Expression tokenSymbolId, Expression tokenValue, Expression position);
	}

	public static class ParserEmitter {
		private static readonly MethodInfo meth_SymbolId_ToInt32 = Reflect<SymbolId>.GetMethod(id => id.ToInt32());
		private static readonly ConstructorInfo ctor_InvalidOperationException = Reflect.GetConstructor(() => new InvalidOperationException());
		private static readonly ConstructorInfo ctor_ListOfSymbolId = Reflect.GetConstructor(() => new List<SymbolId>(default(int)));
		private static readonly ConstructorInfo ctor_SymbolId = Reflect.GetConstructor(() => new SymbolId(default(int)));
		private static readonly MethodInfo meth_ListOfSymbolId_Add = Reflect<List<SymbolId>>.GetMethod(l => l.Add(default(SymbolId)));

		public static Expression<Action<ParserContextBase<TAstNode, TInput, TPosition>, SymbolId, Capture<TInput>>> EmitParser<TAstNode, TInput, TPosition>(LalrTable table, IParserFragmentEmitter<TAstNode, TInput, TPosition> fragmentEmitter, Func<SymbolId, string> resolver = null) {
			var paramContext = Expression.Parameter(typeof(ParserContextBase<TAstNode, TInput, TPosition>), "context");
			var paramTokenSymbolId = Expression.Parameter(typeof(SymbolId), "tokenSymbolId");
			var paramTokenValue = Expression.Parameter(typeof(Capture<TInput>), "tokenValue");
			var varPosition = Expression.Variable(typeof(TPosition), "position");
			var varInitialState = Expression.Variable(typeof(ParserState<TAstNode>), "initialState");
			var exprCurrentState = Expression.Field(paramContext, Reflect<ParserContext<TAstNode, TInput, TPosition>>.GetField(c => c.currentState));
			var lblBreak = Expression.Label("Break");
			var lblContinue = Expression.Label("Continue");
			var ctor_ParserState = Reflect.GetConstructor(() => new ParserState<TAstNode>(default(TAstNode), default(int), default(ParserState<TAstNode>)));
			var prop_ParserState_State = Reflect<ParserState<TAstNode>>.GetProperty(s => s.State);
			var prop_ParserState_Node = Reflect<ParserState<TAstNode>>.GetProperty(s => s.Node);
			var prop_ParserState_Parent = Reflect<ParserState<TAstNode>>.GetProperty(s => s.Parent);
			var meth_Accept = Reflect<ParserContext<TAstNode, TInput, TPosition>>.GetMethod(c => c.Accept(default(TAstNode)));
			var meth_SyntaxError = Reflect<ParserContext<TAstNode, TInput, TPosition>>.GetMethod(c => c.SyntaxError(default(IEnumerable<SymbolId>), default(SymbolId), default(Capture<TInput>), default(TPosition)));

			Expression DoShift(SymbolId symbolId, ShiftAction action) {
				return Expression.Block(typeof(void),
						Expression.Assign(
								exprCurrentState,
								Expression.New(
										ctor_ParserState,
										fragmentEmitter.CreateTerminal(symbolId, paramTokenValue, varPosition),
										Expression.Constant(action.NewState),
										exprCurrentState)),
						Expression.Break(lblBreak));
			}

			Expression DoReduce(ReduceSingleAction action) {
				var varCurrentState = Expression.Variable(typeof(ParserState<TAstNode>), "currentState");
				var varsNodes = action.ProductionRule.RuleSymbolIds.Select(s => Expression.Variable(typeof(TAstNode), s.ToString(resolver))).ToArray();
				var body = new List<Expression>(varsNodes.Length * 2 + 3);
				body.Add(Expression.Assign(
						varCurrentState,
						exprCurrentState));
				for (var i = varsNodes.Length - 1; i >= 0; i--) {
					body.Add(Expression.Assign(
							varsNodes[i],
							Expression.Property(
									varCurrentState,
									prop_ParserState_Node)));
					body.Add(Expression.Assign(
							varCurrentState,
							Expression.Property(
									varCurrentState,
									prop_ParserState_Parent)));
				}
				body.Add(Expression.Assign(
						exprCurrentState,
						Expression.New(
								ctor_ParserState,
								fragmentEmitter.CreateNonterminal(action.ProductionRule, varsNodes),
								Expression.Switch(
										Expression.Property(
												varCurrentState,
												prop_ParserState_State),
										Expression.Throw(
												Expression.New(ctor_InvalidOperationException), typeof(int)),
										table.Action
												.Where(a => (a.Value.Type == ActionType.Goto) && (a.Key.Value == action.ProductionRule.ProductionSymbolId))
												.Select(a => Expression.SwitchCase(
														Expression.Constant(((GotoAction)a.Value).NewState),
														Expression.Constant(a.Key.State)
												)).ToArray()),
								varCurrentState)));
				body.Add(Expression.Continue(lblContinue));
				return Expression.Block(typeof(void),
						varsNodes.Append(varCurrentState),
						body);
			}

			Expression DoAccept() {
				return Expression.Block(typeof(void),
						Expression.Call(
								paramContext,
								meth_Accept,
								Expression.Property(
										exprCurrentState,
										prop_ParserState_Node)),
						Expression.Assign(
								exprCurrentState,
								Expression.Property(
										exprCurrentState,
										prop_ParserState_Parent)),
						Expression.Break(lblBreak));
			}

			Expression DoThrow() {
				throw new InvalidOperationException("Internal error: unexpected action type");
			}

			Expression DoSyntaxError() {
				var varSimulatedState = Expression.Variable(typeof(ParserState<TAstNode>), "simulatedState");
				var lblSimulationBreak = Expression.Label(typeof(bool), "simulationBreak");

				Expression DoSimulateReduce(ProductionRule rule) {
					Expression exprNewSimulatedState = Expression.Assign(
							varSimulatedState,
							Expression.New(
									ctor_ParserState,
									Expression.Default(typeof(TAstNode)),
									Expression.Switch(
											Expression.Property(
													varSimulatedState,
													prop_ParserState_State),
											Expression.Break(
													lblSimulationBreak,
													Expression.Constant(false),
													typeof(int)),
											table.Action
													.Where(a => (a.Key.Value == rule.ProductionSymbolId) && (a.Value.Type == ActionType.Goto))
													.GroupBy(a => ((GotoAction)a.Value).NewState)
													.Select(g => Expression.SwitchCase(
															Expression.Constant(g.Key),
															g.Select(a => Expression.Constant(a.Key.State)))).ToArray()),
									varSimulatedState));
					return rule.RuleSymbolIds.Count == 0
							? exprNewSimulatedState
							: Expression.Block(
									Expression.Assign(varSimulatedState,
											rule.RuleSymbolIds.Aggregate<SymbolId, Expression>(varSimulatedState, (expression, id) => Expression.Property(expression, prop_ParserState_Parent))),
									exprNewSimulatedState);
				}

				var varExpectedTokens = Expression.Variable(typeof(List<SymbolId>), "expectedTokens");
				var terminalSymbolIds = table.Action
						.Where(a => (a.Value.Type == ActionType.Accept) || (a.Value.Type == ActionType.Shift))
						.Select(a => a.Key.Value)
						.Distinct()
						.OrderBy(id => id.ToInt32())
						.ToArray();
				var body = new List<Expression>(terminalSymbolIds.Length + 2);
				body.Add(Expression.Assign(
								varExpectedTokens,
								Expression.New(
										ctor_ListOfSymbolId,
										Expression.Constant(terminalSymbolIds.Length))));
				foreach (var symbolId in terminalSymbolIds) {
					body.Add(Expression.IfThen(
									Expression.Block(typeof(bool), new[] {varSimulatedState},
											Expression.Assign(
													varSimulatedState,
													exprCurrentState),
											Expression.Loop(
													Expression.Switch(typeof(void),
															Expression.Property(
																	varSimulatedState,
																	prop_ParserState_State),
															Expression.Break(lblSimulationBreak,
																	Expression.Constant(false)),
															null,
															table.Action
																	.Where(a => (a.Key.Value == symbolId) && (a.Value.Type == ActionType.Reduce))
																	.GroupBy(a => ((ReduceSingleAction)a.Value).ProductionRule)
																	.Select(g => Expression.SwitchCase(
																			DoSimulateReduce(g.Key),
																			g.Select(a => Expression.Constant(a.Key.State))))
																	.Append(Expression.SwitchCase(
																			Expression.Break(lblSimulationBreak,
																					Expression.Constant(true)),
																			table.Action
																					.Where(a => (a.Key.Value == symbolId) && ((a.Value.Type == ActionType.Accept) || (a.Value.Type == ActionType.Shift)))
																					.Select(a => Expression.Constant(a.Key.State))
																	)).ToArray()), lblSimulationBreak)),
									Expression.Call(
											varExpectedTokens,
											meth_ListOfSymbolId_Add,
											Expression.New(
													ctor_SymbolId,
													Expression.Constant(symbolId.ToInt32())))));
				}
				body.Add(Expression.Convert(
								varExpectedTokens,
								typeof(IEnumerable<SymbolId>)));
				var exprExpectedSymbolIds = Expression.Block(typeof(IEnumerable<SymbolId>), new[] {varExpectedTokens}, body);
				var exprCustomSyntaxError = fragmentEmitter.SyntaxError(exprExpectedSymbolIds, paramTokenSymbolId, paramTokenValue, varPosition);
				if (exprCustomSyntaxError?.Type == typeof(bool)) {
					exprCustomSyntaxError = Expression.IfThen(
							exprCustomSyntaxError,
							Expression.Continue(lblContinue));
				}
				return Expression.Block(typeof(void),
						Expression.Assign(
								exprCurrentState,
								varInitialState),
						exprCustomSyntaxError ?? Expression.Call(
								paramContext,
								meth_SyntaxError,
								exprExpectedSymbolIds,
								paramTokenSymbolId,
								paramTokenValue,
								varPosition),
						Expression.Break(lblBreak));
			}

			return Expression.Lambda<Action<ParserContextBase<TAstNode, TInput, TPosition>, SymbolId, Capture<TInput>>>(
					Expression.Block(new[] {varPosition},
							Expression.IfThen(
									fragmentEmitter.CheckAndPreprocessTerminal(paramTokenSymbolId, paramTokenValue, varPosition),
									Expression.Block(typeof(void), new[] {varInitialState},
											Expression.Assign(
													varInitialState,
													exprCurrentState),
											Expression.Loop(
													Expression.Block(typeof(void),
															Expression.Switch(
																	Expression.Property(
																			exprCurrentState,
																			prop_ParserState_State),
																	table.Action
																			.Where(a => (a.Value.Type == ActionType.Shift) || (a.Value.Type == ActionType.Reduce) || (a.Value.Type == ActionType.Accept))
																			.GroupBy(a => a.Key.State)
																			.Select(g => Expression.SwitchCase(
																					Expression.Switch(
																							Expression.Call(
																									paramTokenSymbolId,
																									meth_SymbolId_ToInt32),
																							g.Select(a => Expression.SwitchCase(
																									a.Value.Type == ActionType.Shift
																											? DoShift(a.Key.Value, (ShiftAction)a.Value)
																											: a.Value.Type == ActionType.Reduce
																													? DoReduce((ReduceSingleAction)a.Value)
																													: a.Value.Type == ActionType.Accept
																															? DoAccept()
																															: DoThrow(),
																									Expression.Constant(a.Key.Value.ToInt32()))).ToArray()),
																					Expression.Constant(g.Key))).ToArray()),
															DoSyntaxError()
													), lblBreak, lblContinue)))),
					paramContext,
					paramTokenSymbolId,
					paramTokenValue);
		}
	}
}
