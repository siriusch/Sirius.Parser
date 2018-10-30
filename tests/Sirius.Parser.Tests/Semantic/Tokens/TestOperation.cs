using System;

namespace Sirius.Parser.Semantic.Tokens {
	public abstract class TestOperation: TestToken {
		public abstract double Compute(double left, double right);
	}
}
