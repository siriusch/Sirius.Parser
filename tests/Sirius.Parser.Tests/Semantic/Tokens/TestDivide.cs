using System;

namespace Sirius.Parser.Semantic.Tokens {
	public class TestDivide: TestOperation {
		[Terminal(typeof(TestToken), "/")]
		public TestDivide() { }

		public override double Compute(double left, double right) {
			return left / right;
		}
	}
}
