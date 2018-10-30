using System;

namespace Sirius.Parser.Semantic.Tokens {
	public class TestExpression<T>: TestValue where T: TestOperation {
		private readonly TestValue left;
		private readonly T operation;
		private readonly TestValue right;

		[Rule(typeof(TestToken), "<Expression>", "<Expression>", "+", "<Mult Exp>", GenericTypeParameter = typeof(TestAdd))]
		[Rule(typeof(TestToken), "<Expression>", "<Expression>", "-", "<Mult Exp>", GenericTypeParameter = typeof(TestSubtract))]
		[Rule(typeof(TestToken), "<Mult Exp>", "<Mult Exp>", "*", "<Negate Exp>", GenericTypeParameter = typeof(TestMultiply))]
		[Rule(typeof(TestToken), "<Mult Exp>", "<Mult Exp>", "/", "<Negate Exp>", GenericTypeParameter = typeof(TestDivide))]
		public TestExpression(TestValue left, T operation, TestValue right) {
			this.left = left;
			this.operation = operation;
			this.right = right;
		}

		public override double Compute() {
			return this.operation.Compute(this.left.Compute(), this.right.Compute());
		}
	}
}
