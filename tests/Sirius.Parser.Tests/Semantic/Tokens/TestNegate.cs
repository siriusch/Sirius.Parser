using System;

namespace Sirius.Parser.Semantic.Tokens {
	public class TestNegate: TestValue {
		private readonly TestValue value;

		[Rule(typeof(TestToken), "<Negate Exp>", "-", "<Value>")]
		public TestNegate([RuleSymbol("<Value>")]TestValue value) {
			this.value = value;
		}

		public override double Compute() {
			return -this.value.Compute();
		}
	}
}
