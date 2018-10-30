using System;

namespace Sirius.Parser.Semantic.Tokens {
	public class TestMultiply: TestOperation {
		[Terminal(typeof(TestToken), "*")]
		public TestMultiply() { }

		public override double Compute(double left, double right) {
			return left * right;
		}
	}
}
