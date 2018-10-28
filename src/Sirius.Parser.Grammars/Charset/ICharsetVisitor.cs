using System;

namespace Sirius.Parser.Charset {
	public interface ICharsetVisitor<in TContext, out TResult> { 
		TResult Handle(CharsetHandle node, TContext context);
		TResult Negate(CharsetNegate node, TContext context);
		TResult Union(CharsetUnion node, TContext context);
		TResult Intersection(CharsetIntersection node, TContext context);
		TResult Subtract(CharsetSubtract node, TContext context);
		TResult Difference(CharsetDifference node, TContext context);
	}
}
