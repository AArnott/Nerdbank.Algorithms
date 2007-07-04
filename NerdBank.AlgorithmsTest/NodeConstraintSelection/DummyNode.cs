using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NerdBank.Algorithms.NodeConstraintSelection;

namespace NerdBank.AlgorithmsTest.NodeConstraintSelection {
	/// <summary>
	/// A minimal Node class for us with testing constraints.
	/// </summary>
	class DummyNode : NodeBase {
		public DummyNode() { }
		public DummyNode(object designation) {
			this.designation = designation;
		}
		object designation;

		public override string ToString() {
			if (designation != null)
				return designation.ToString();
			else
				return base.ToString();
		}
	}
}
