using System;

namespace Sirius.Parser.Semantic.Tokens {
	public class TestAdd: TestOperation {
		[Terminal(typeof(TestToken), "+")]
		public TestAdd() { }

		public override double Compute(double left, double right) {
			return left+right;
		}
	}
}
