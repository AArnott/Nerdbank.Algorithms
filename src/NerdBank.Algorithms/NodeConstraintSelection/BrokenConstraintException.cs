using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NerdBank.Algorithms.NodeConstraintSelection {
	[Serializable]
	public class BrokenConstraintException : InvalidOperationException {
		public BrokenConstraintException() : this(Strings.BrokenConstraint) { }
		public BrokenConstraintException(string message) : base(message) { }
		public BrokenConstraintException(string message, Exception inner) : base(message, inner) { }
#if !PROFILE328 && !NETSTANDARD1_0 && !PROFILE259
		protected BrokenConstraintException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context)
			: base(info, context) { }
#endif
	}
}
