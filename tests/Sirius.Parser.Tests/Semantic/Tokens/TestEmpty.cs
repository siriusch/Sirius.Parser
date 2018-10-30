using System;

namespace Sirius.Parser.Semantic.Tokens {
	public class TestEmpty: TestToken {
		[Rule(typeof(TestToken), "<Empty>", "NULL")]
		public static TestEmpty Trim(TestEmpty empty) {
			return empty;
		}

		[Terminal(typeof(TestToken), "NULL")]
		[Rule(typeof(TestToken), "<Empty>")]
		public TestEmpty() { }
	}
}
