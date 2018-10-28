using System.Collections.Generic;
using System.Diagnostics;

namespace Sirius.Parser {
	public class ParserState<TAstNode> {
		public ParserState(TAstNode node, int state, ParserState<TAstNode> parent) {
			Debug.Assert((parent != null) || (EqualityComparer<TAstNode>.Default.Equals(node, default(TAstNode)) && (state == default(int))));
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

		public ParserState<TAstNode> Parent {
			get;
		}
	}
}