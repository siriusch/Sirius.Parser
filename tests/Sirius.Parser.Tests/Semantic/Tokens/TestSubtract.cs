using System;

namespace Sirius.Parser.Semantic.Tokens {
	public class TestSubtract: TestOperation {
		[Terminal(typeof(TestToken), "-")]
		public TestSubtract() { }

		public override double Compute(double left, double right) {
			return left - right;
		}
	}
}
