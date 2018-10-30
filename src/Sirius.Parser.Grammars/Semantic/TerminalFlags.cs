using System;

namespace Sirius.Parser.Semantic {
	[Flags]
	public enum TerminalFlags: int {
		None = 0,
		Noise = 1
	}
}
