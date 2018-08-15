namespace Sirius.Parser.Lalr {
	public enum ActionType {
		Error = 0,
		Accept = 1,
		Shift = 2,
		Reduce = 3,
		ReduceMulti = 4,
		Goto = 5
	}
}
