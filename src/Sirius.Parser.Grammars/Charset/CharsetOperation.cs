using System;

namespace Sirius.Parser.Grammars.Charset {
	public abstract class CharsetOperation<TChar>: CharsetNode<TChar>
			where TChar: IComparable<TChar> {
		protected CharsetOperation(CharsetNode<TChar> leftNode, CharsetNode<TChar> rightNode) {
			this.LeftNode = leftNode;
			this.RightNode = rightNode;
		}

		public CharsetNode<TChar> LeftNode {
			get;
		}

		public CharsetNode<TChar> RightNode {
			get;
		}
	}
}