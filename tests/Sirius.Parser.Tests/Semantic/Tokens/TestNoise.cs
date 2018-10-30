using System;

namespace Sirius.Parser.Semantic.Tokens {
	public class TestNoise: TestToken {
		[Terminal(typeof(TestToken), "(")]
		[Terminal(typeof(TestToken), ")")]
		public TestNoise(string text) {
			this.Text = text;
		}

		public string Text {
			get;
		}
	}
}
