using System;

namespace Sirius.Parser.Charset {
	public abstract class CharsetOperation: CharsetNode {
		protected CharsetOperation(CharsetNode leftNode, CharsetNode rightNode) {
			this.LeftNode = leftNode;
			this.RightNode = rightNode;
		}

		public CharsetNode LeftNode {
			get;
		}

		public CharsetNode RightNode {
			get;
		}
	}
}
