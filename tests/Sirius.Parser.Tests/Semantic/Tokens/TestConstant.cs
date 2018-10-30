using System;
using System.Globalization;

namespace Sirius.Parser.Semantic.Tokens {
	public class TestConstant<T>: TestValue
			where T: struct, IConvertible {
		private readonly T constant;

		[Terminal(typeof(TestToken), "Integer", @"[0-9]+", GenericTypeParameter = typeof(int))]
		[Terminal(typeof(TestToken), "Float", @"[0-9]*\.[0-9]+(e[+-]?[0-9]+)?", CaseInsensitive = true, GenericTypeParameter = typeof(double))]
		public TestConstant(string constant) {
			this.constant = (T)Convert.ChangeType(constant, typeof(T), NumberFormatInfo.InvariantInfo);
		}

		public override double Compute() {
			return this.constant.ToDouble(NumberFormatInfo.InvariantInfo);
		}
	}
}
