using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Sirius.Collections;
using Sirius.Parser.Charset;
using Sirius.Parser.Grammar;
using Sirius.Parser.Lalr;
using Sirius.RegularExpressions;
using Sirius.RegularExpressions.Alphabet;
using Sirius.RegularExpressions.Automata;
using Sirius.RegularExpressions.Invariant;
using Sirius.RegularExpressions.Parser;
using Sirius.Unicode;

namespace Sirius.Parser.Semantic {
	internal class SemanticParserGrammarBuilder<TAstNode, TInput, TPosition>: IParserFragmentEmitter<SemanticParser<TAstNode, TInput, TPosition>, TAstNode, TInput, TPosition>, SemanticParserGrammar<TAstNode, TInput, TPosition>.IGrammarData
			where TInput: struct, IEquatable<TInput>, IComparable<TInput> {
		private static readonly MethodInfo meth_SemanticParser_SyntaxError = typeof(SemanticParser<TAstNode, TInput, TPosition>).GetMethod(nameof(SemanticParser<TAstNode, TInput, TPosition>.SyntaxError), BindingFlags.NonPublic | BindingFlags.Instance);
		private static readonly MethodInfo meth_SemanticParser_GetParameterValue = typeof(SemanticParser<TAstNode, TInput, TPosition>).GetMethod(nameof(SemanticParser<TAstNode, TInput, TPosition>.GetParameterValue), BindingFlags.NonPublic | BindingFlags.Instance);
		private static readonly MethodInfo meth_UnicodeExtensions_AsString = typeof(UnicodeExtensions).GetMethods(BindingFlags.Static|BindingFlags.Public).SingleOrDefault(m => m.ReturnType == typeof(string) && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType.IsAssignableFrom(typeof(Capture<TInput>)));

		private static Expression ExpressionInvoke(MethodBase methodBase, Expression[] paramsCall) {
			if (methodBase is ConstructorInfo ctor) {
				return Expression.New(ctor, paramsCall);
			}
			if (methodBase is MethodInfo method) {
				return Expression.Call(method, paramsCall);
			}
			throw new InvalidOperationException($"Internal error: unexpected {methodBase.GetType().Name}");
		}

		private readonly Dictionary<ProductionKey, KeyValuePair<RuleAttribute, MethodBase>> nonterminals = new Dictionary<ProductionKey, KeyValuePair<RuleAttribute, MethodBase>>();
		private readonly Dictionary<string, SymbolId> symbolsByName = new Dictionary<string, SymbolId>(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<SymbolId, TerminalFlags> terminalFlags = new Dictionary<SymbolId, TerminalFlags>();
		private readonly Dictionary<SymbolId, MethodBase> terminals = new Dictionary<SymbolId, MethodBase>();

		public IReadOnlyDictionary<string, SymbolId> SymbolsByName => this.symbolsByName;

		public IReadOnlyDictionary<SymbolId, TerminalFlags> TerminalFlags => this.terminalFlags;

		public SemanticParserGrammarBuilder(IUnicodeMapper<TInput> mapper, TInput? eof) {
			string GetGrammarKeyForDisplay() {
				return $"typeof({typeof(TAstNode).FullName})";
			}

			string MemberInfoForDisplay(MethodBase member) {
				return member == null ? "(assembly)" : $"{member.DeclaringType.FullName}.{member.Name}";
			}

			var errors = new List<Exception>();
			try {
				var parts = SemanticParserGrammar<TAstNode, TInput, TPosition>.FindGrammarParts()
						.OrderByDescending(p => p.Key.GetType().Name)
						.ThenBy(p => (p.Key as GrammarSymbolAttribute)?.SymbolName ?? (p.Key as CharsetAttribute)?.CharsetName ?? "")
						.ToList();

				// Compute charsets
				var charsetQueue = new Queue<KeyValuePair<string, CharsetNode>>(parts
						.Select(p => p.Key)
						.OfType<CharsetAttribute>()
						.Select(a => new KeyValuePair<string, CharsetNode>(a.CharsetName, CharsetParser.Parse(a.CharsetExpression))));
				var charsets = charsetQueue
						.SelectMany(p => p.Value.GetCharsetNames())
						.Except(charsetQueue.Select(p => p.Key), StringComparer.OrdinalIgnoreCase)
						.ToDictionary(n => n, UnicodeRanges.FromUnicodeName, StringComparer.OrdinalIgnoreCase);
				var provider = new UnicodeCharSetProvider(charsets);
				var skipCount = 0;
				while (charsetQueue.Count > 0) {
					var current = charsetQueue.Dequeue();
					if (current.Value.GetCharsetNames().All(charsets.ContainsKey)) {
						charsets.Add(current.Key, current.Value.Compute(provider));
						skipCount = 0;
					} else {
						charsetQueue.Enqueue(current);
						if (skipCount++ > charsetQueue.Count) {
							errors.Add(new InvalidOperationException($"The charsets cannot be computed because {String.Join(", ", charsetQueue.Select(p => p.Key))} contain circular references"));
							break;
						}
					}
				}

				// Gather symbol information
				var startsymbol = parts.Select(p => p.Key).OfType<StartSymbolAttribute>().SingleOrDefault();
				if (startsymbol == null) {
					errors.Add(new InvalidOperationException($"Start symbol has not been defined: [assembly: StartSymbol({GetGrammarKeyForDisplay()}, ...)]"));
				}
				foreach (var symbol in parts
						.Select(p => p.Key)
						.OfType<GrammarSymbolAttribute>()
						.GroupBy(a => a.SymbolName, a => a.SymbolKind, StringComparer.OrdinalIgnoreCase)) {
					if (symbol.Distinct().Skip(1).Any()) {
						errors.Add(new InvalidOperationException($"The symbol {symbol.Key} must not be defined as both terminal and nonterminal"));
					} else if (StringComparer.OrdinalIgnoreCase.Equals(symbol.Key, startsymbol?.SymbolName) && (symbol.First() != SymbolKind.Nonterminal)) {
						errors.Add(new InvalidOperationException($"The start symbol {symbol.Key} must be a nonterminal"));
					}
					this.symbolsByName.Add(symbol.Key, this.symbolsByName.Count + 1);
				}

				SymbolId GetSymbol(string symbolName) {
					if (this.symbolsByName.TryGetValue(symbolName, out var id)) {
						return id;
					}
					errors.Add(new InvalidOperationException($"The symbol {symbolName} has not been defined. If the symbol name is correct, define it as virtual: [assembly: VirtualSymbol({GetGrammarKeyForDisplay()}, ...)]"));
					return SymbolId.Eof;
				}

				MethodBase PopulateGenericArguments(MethodBase methodBase, GrammarSymbolAttribute attribute) {
					var genericTypeParameters = attribute.GenericTypeParameters;
					if (methodBase?.DeclaringType.IsGenericTypeDefinition == true) {
						var typeGenericArguments = methodBase.DeclaringType.GetGenericArguments();
						if (genericTypeParameters.Length < typeGenericArguments.Length) {
							errors.Add(new InvalidOperationException($"Missing type generic arguments for {attribute} on {MemberInfoForDisplay(methodBase)}"));
							return methodBase;
						}
						var genericType = methodBase.DeclaringType.MakeGenericType(genericTypeParameters.Take(typeGenericArguments.Length).ToArray());
						genericTypeParameters = genericTypeParameters.Skip(typeGenericArguments.Length).ToArray();
						IReadOnlyDictionary<Type, Type> genericArgumentMap = genericType.GetGenericArguments().Select((t, ix) => new KeyValuePair<Type, Type>(typeGenericArguments[ix], t)).ToDictionary(p => p.Key, p => p.Value);
						var mappedParameters = methodBase.GetParameters().Select(p => genericArgumentMap.GetValueOrDefault(p.ParameterType, p.ParameterType)).ToArray();
						if (methodBase is ConstructorInfo) {
							methodBase = genericType.GetConstructor(mappedParameters);
						} else {
							methodBase = genericType.GetMethod(methodBase.Name, BindingFlags.Static | BindingFlags.Public, null, mappedParameters, null);
						}
					}
					if (methodBase is MethodInfo method && method.IsGenericMethodDefinition) {
						if (method.GetGenericArguments().Length != genericTypeParameters.Length) {
							errors.Add(new InvalidOperationException($"Invalid number of method generic arguments for {attribute} on {MemberInfoForDisplay(methodBase)}"));
						}
						methodBase = method.MakeGenericMethod(genericTypeParameters);
					} else if (genericTypeParameters.Length > 0) {
						errors.Add(new InvalidOperationException($"Excess generic arguments for {attribute} on {MemberInfoForDisplay(methodBase)}"));
					}
					return methodBase;
				}

				// Populate builders with symbols
				var lexerBuilder = new LexerBuilder<TInput>(mapper, eof, provider);
				var grammarBuilder = new GrammarBuilder(-1, 0, GetSymbol(startsymbol?.SymbolName));
				foreach (var part in parts) {
					if (part.Value is MethodInfo method && !method.IsStatic) {
						errors.Add(new InvalidOperationException($"Grammar attribute cannot be used on instance method ({part.Key} on {MemberInfoForDisplay(part.Value)})"));
					}
					switch (part.Key) {
					case TerminalAttribute terminalAttribute:
						var terminalSymbolId = GetSymbol(terminalAttribute.SymbolName);
						lexerBuilder.Add(terminalSymbolId, terminalAttribute.RegularExpression, terminalAttribute.CaseInsensitive);
						try {
							this.terminals.Add(terminalSymbolId, PopulateGenericArguments(part.Value, terminalAttribute));
							if (terminalAttribute.Flags != Semantic.TerminalFlags.None) {
								this.terminalFlags.Add(terminalSymbolId, terminalAttribute.Flags);
							}
						} catch (ArgumentException) {
							errors.Add(new InvalidOperationException($"Duplicate definition of terminal {terminalAttribute.SymbolName} ({MemberInfoForDisplay(this.terminals[terminalSymbolId])}, {MemberInfoForDisplay(part.Value)})"));
						}
						break;
					case RuleAttribute ruleAttribute:
						var production = grammarBuilder.DefineProduction(GetSymbol(ruleAttribute.SymbolName)).Add(ruleAttribute.RuleSymbolNames.Select(GetSymbol));
						try {
							this.nonterminals.Add(production, new KeyValuePair<RuleAttribute, MethodBase>(ruleAttribute, PopulateGenericArguments(part.Value, ruleAttribute)));
						} catch (ArgumentException) {
							errors.Add(new InvalidOperationException($"Duplicate definition of production rule {ruleAttribute} ({MemberInfoForDisplay(this.nonterminals[production].Value)}, {MemberInfoForDisplay(part.Value)})"));
						}
						if (part.Value != null) {
							if (!String.IsNullOrEmpty(ruleAttribute.TrimSymbolName)) {
								errors.Add(new InvalidOperationException($"Trimming is only allowed at the assembly level (rule {ruleAttribute} on {part.Value.DeclaringType})"));
							}
						} else {
							if (!String.IsNullOrEmpty(ruleAttribute.TrimSymbolName)) {
								if (!ruleAttribute.RuleSymbolNames.Any(s => StringComparer.OrdinalIgnoreCase.Equals(s, ruleAttribute.TrimSymbolName))) {
									errors.Add(new InvalidOperationException($"Trimming requires to be done with one of the production symbols (symbol {ruleAttribute.TrimSymbolName} not found in rule {ruleAttribute})"));
								}
							} else {
								if (ruleAttribute.RuleSymbolNames.Length != 1) {
									errors.Add(new InvalidOperationException($"Implicit trimming requires exactly one production symbols (rule {ruleAttribute})"));
								}
							}
						}
						break;
					case CharsetAttribute _:
					case StartSymbolAttribute _:
					case VirtualSymbolAttribute _:
						// These have already been handled
						break;
					default:
						errors.Add(new InvalidOperationException($"The attribute {part.Key.GetType().FullName} is not supported"));
						break;
					}
				}
				this.symbolsByName.Add("(EOF)", SymbolId.Eof);

				if (errors.Count == 0) {
					this.Symbols = this.symbolsByName.ToDictionary(p => p.Value, p => p.Key);
					// Compute automata and table
					AlphabetBuilder<TInput> alpha;
					var dfa = lexerBuilder.ComputeDfa(out alpha);
					if (dfa.StartState.Id != default(Id<DfaState<LetterId>>)) {
						errors.Add(new InvalidOperationException("Internal error: DFA start state is not default (0)"));
					}
					this.LexerStateMachine = DfaStateMachineEmitter.CreateExpression(dfa, AlphabetMapperEmitter<TInput>.CreateExpression(alpha));

					var table = new LalrTableGenerator(grammarBuilder).ComputeTable();
					this.ParserStateMachine = ParserEmitter.CreateExpression(table, this, this.Symbols.CreateGetter());
				}
			} catch (Exception ex) {
				errors.Add(ex);
			}
			if (errors.Count > 0) {
				throw new AggregateException(errors);
			}
		}

		public Expression CreateNonterminal(ProductionRule rule, ParameterExpression varParser, ParameterExpression[] varsProductionNodes) {
			var info = this.nonterminals[rule.Key];
			var methodBase = info.Value;
			if (methodBase == null) {
				// Assembly-Level Trim
				var trimIndex = info.Key.RuleSymbolNames
						.Select((s, ix) => new KeyValuePair<string, int>(s, ix))
						.FirstOrDefault(p => String.Equals(p.Key, info.Key.TrimSymbolName, StringComparison.OrdinalIgnoreCase)).Value;
				return varsProductionNodes[trimIndex];
			}
			Debug.Assert(methodBase.DeclaringType != null, $"{nameof(methodBase)}.{nameof(MethodBase.DeclaringType)} != null");
			// Generate parameter list
			var parameters = methodBase.GetParameters();
			var paramsCall = new Expression[parameters.Length];
			if (parameters.Any(p => p.IsDefined(typeof(RuleSymbolAttribute)))) {
				for (var i = 0; i < paramsCall.Length; i++) {
					var parameterMapping = parameters[i].GetCustomAttributes<RuleSymbolAttribute>().SingleOrDefault(a => Equals(a.RuleKey, info.Key.RuleKey));
					if (parameterMapping != null) {
						var mappingSymbolId = this.symbolsByName[parameterMapping.SymbolName];
						var varProductionNode = rule.RuleSymbolIds.Select((id, ix) => new KeyValuePair<SymbolId, ParameterExpression>(id, varsProductionNodes[ix])).Where(p => p.Key == mappingSymbolId).Skip(parameterMapping.Occurrence - 1).FirstOrDefault().Value;
						if (varProductionNode == null) {
							throw new InvalidOperationException($"There is no {parameterMapping.SymbolName} [{parameterMapping.Occurrence}] in rule {info.Key} (parameter {parameters[i].Name} of {methodBase.DeclaringType.FullName}.{methodBase.Name})");
						}
						paramsCall[i] = Expression.Convert(varProductionNode, parameters[i].ParameterType);
					}
				}
			} else {
				for (var i = 0; (i < paramsCall.Length) && (i < varsProductionNodes.Length); i++) {
					paramsCall[i] = Expression.Convert(varsProductionNodes[i], parameters[i].ParameterType);
				}
			}
			for (var i = 0; i < paramsCall.Length; i++) {
				if (paramsCall[i] == null) {
					paramsCall[i] = Expression.Call(
							varParser,
							meth_SemanticParser_GetParameterValue.MakeGenericMethod(parameters[i].ParameterType),
							Expression.Constant(parameters[i]));
				}
			}
			return ExpressionInvoke(methodBase, paramsCall);
		}

		public Expression CreateTerminal(SymbolId symbolId, ParameterExpression varParser, ParameterExpression varTokenValue, ParameterExpression varPosition) {
			var methodBase = this.terminals[symbolId];
			var parameters = methodBase.GetParameters();
			var paramsCall = new Expression[parameters.Length];
			for (var i = 0; i < parameters.Length; i++) {
				var parameterType = parameters[i].ParameterType;
				if (parameterType == typeof(SymbolId)) {
					paramsCall[i] = Expression.New(
							ParserEmitter.ctor_SymbolId_Int32,
							Expression.Constant(symbolId.ToInt32()));
				} else if (parameterType == typeof(TPosition)) {
					paramsCall[i] = varPosition;
				} else if ((parameterType != typeof(object)) && parameterType.IsAssignableFrom(typeof(SemanticParser<TAstNode, TInput, TPosition>))) {
					paramsCall[i] = Expression.Convert(varParser, parameterType);
				} else if ((parameterType != typeof(object)) && parameterType.IsAssignableFrom(typeof(Capture<TInput>))) {
					paramsCall[i] = Expression.Convert(varTokenValue, parameterType);
				} else if (parameterType == typeof(string) && meth_UnicodeExtensions_AsString != null) {
					paramsCall[i] = Expression.Call(
							meth_UnicodeExtensions_AsString,
							Expression.Convert(varTokenValue, meth_UnicodeExtensions_AsString.GetParameters()[0].ParameterType));
				} else {
					paramsCall[i] = Expression.Call(
							varParser,
							meth_SemanticParser_GetParameterValue.MakeGenericMethod(parameters[i].ParameterType),
							Expression.Constant(parameters[i]));
				}
			}
			return ExpressionInvoke(methodBase, paramsCall);
		}

		public Expression SyntaxError(ParameterExpression varParser, ParameterExpression varTokenSymbolId, ParameterExpression varTokenValue, ParameterExpression varPosition, Expression exprExpectedSymbols) {
			var varExpectedSymbols = Expression.Parameter(typeof(IEnumerable<SymbolId>), "expectedSymbols");
			return
					Expression.Block(new[] {varExpectedSymbols},
							Expression.Assign(varExpectedSymbols, exprExpectedSymbols),
							Expression.Call(
									varParser,
									meth_SemanticParser_SyntaxError,
									varTokenSymbolId,
									varTokenValue,
									varPosition,
									varExpectedSymbols));
		}

		public Expression<Action<SemanticParser<TAstNode, TInput, TPosition>, ParserContextBase<TAstNode, TInput, TPosition>, SymbolId, Capture<TInput>, TPosition>> ParserStateMachine {
			get;
		}

		public Expression<DfaStateMachine<LetterId, TInput>> LexerStateMachine {
			get;
		}

		public IReadOnlyDictionary<SymbolId, string> Symbols {
			get;
		}
	}
}
