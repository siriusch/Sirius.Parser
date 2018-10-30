using System;

namespace Sirius.Parser.Semantic.Tokens {
	public abstract class TestValue: TestToken {
		public abstract double Compute();
	}
}
