using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NerdBank.Algorithms.NodeConstraintSelection;

namespace NerdBank.AlgorithmsTest.NodeConstraintSelection {
	[TestClass]
	public class ConstraintGeneratorTest {
		[TestMethod]
		[Ignore]
		public void TwoExactlyOneConstraintsOverlappingByOneTest() {
			// Nodes: 1 2 3 4 5
			// Constraint A: exactly one of these nodes is selected: 1 2 3
			// Constraint B: exactly one of these nodes is selected: 3 4 5
			// Node 2 is selected.
			// Deduction: Node 3 cannot possibly be the selected node for constraint B.
			// But this is automatically determined because once Node 2 is selected and Constraint A resolves,
			// Node 3 is marked as unselected, and constraint B notes that only 4 and 5 are possibilities now.
			// Then this scenario is already tested in the set of constraints tests.
		}

		[TestMethod]
		public void StrictSubsetConstraintIncludesEntireRequirementTest() {
			// Nodes: 1 2 3 4 5
			// Constraint A: exactly 1 is selected: 1 2 3 4 5
			// Constraint B: exactly 1 is selected: 2 3 4
			// Deduction: Generate Constraint C: exactly 0 are selected: 1 5
			// Even before any nodes are in a known state, if 2 3 4 have exactly one selected, 
			// then 1 5 cannot possibly be selected or else constraints A, B would conflict with each other
			// as A and B could not both be satisified at once.
			INode[] nodes = new INode[] { new DummyNode(), new DummyNode(), new DummyNode(), new DummyNode(), new DummyNode() };
			ConstraintBase a = SelectionCountConstraint.ExactSelected(1, nodes);
			ConstraintBase b = SelectionCountConstraint.ExactSelected(1, nodes.Where((n, i) => i > 0 && i < 4));
			IConstraint[] deduced = ConstraintGenerator.GenerateDeducedConstraints(new ConstraintBase[] { a, b }, true, false).ToArray();
			Assert.AreEqual(1, deduced.Length);
			SelectionCountConstraint c = deduced[0] as SelectionCountConstraint;
			Assert.IsNotNull(c);
			Assert.AreEqual(0, c.Max);
			Assert.IsTrue(c.SelectionState);
			INode[] expectedNodes = new INode[] { nodes[0], nodes[4] };
			INode[] actualNodes = c.Nodes.ToArray();
			CollectionAssert.AreEquivalent(expectedNodes, actualNodes);
		}

		[TestMethod]
		public void MinimumConstraintsFillExactConstraintTest() {
			// Nodes: 0 1 2 3 4
			// Constraint A: minimum 1 is selected: 0 1
			// Constraint B: minimum 1 is selected: 3 4
			// Constraint C: exactly 2 are selected: 0 1 2 3 4
			// Deduce: Constraints A B exactly fill the requirement for C such that 2 cannot be in the solution.
			//  * Constraint E: exactly 0 are selected: 2
			INode[] nodes = new INode[] { new DummyNode(), new DummyNode(), new DummyNode(), new DummyNode(), new DummyNode() };
			ConstraintBase a = SelectionCountConstraint.MinSelected(1, new INode[] { nodes[0], nodes[1] });
			ConstraintBase b = SelectionCountConstraint.MinSelected(1, new INode[] { nodes[3], nodes[4] });
			ConstraintBase c = SelectionCountConstraint.ExactSelected(2, nodes);
			IConstraint[] deduced = ConstraintGenerator.GenerateDeducedConstraints(new IConstraint[] { a, b, c }, true, false).ToArray();
			Assert.AreEqual(1, deduced.Length);
			SelectionCountConstraint e = deduced[0] as SelectionCountConstraint;
			Assert.IsNotNull(e);
			Assert.AreEqual(0, e.Max);
			Assert.IsTrue(e.SelectionState);
			INode[] expectedNodes = new INode[] { nodes[2] };
			INode[] actualNodes = e.Nodes.ToArray();
			CollectionAssert.AreEquivalent(expectedNodes, actualNodes);
		}

		[TestMethod]
		public void CombinationEliminationTest() {
			// Nodes: 1 2 3 4 5
			// Constraint A: exactly 1 is selected: 1 3 5
			// Constraint B: exactly 1 is selected: 2 4
			// Constraint C: at least 1 is selected: 1 2
			// Constraint D: at least 1 is selected: 3 4
			// Deduce: 
			//  * The following selected nodes would satisfy A, B: (1,2) (1,4) (3,2) (3,4) (5,2) (5,4)
			//  * Considering C and D, these combinations are left: (1,4) (3,2)
			//  * Given that no valid combination includes 5, we can generate:
			//      * Constraint E: exactly 0 is selected: 5
			//  * As soon as any of 1 2 3 4 are selected, we immediately know the state of the other 3,
			//    but once the existing constraints cascade resolve, this information will automatically
			//    propagate through the other nodes.
			INode[] nodes = new INode[] { new DummyNode(), new DummyNode(), new DummyNode(), new DummyNode(), new DummyNode() };
			ConstraintBase a = SelectionCountConstraint.ExactSelected(1, new INode[] { nodes[0], nodes[2], nodes[4] });
			ConstraintBase b = SelectionCountConstraint.ExactSelected(1, new INode[] { nodes[1], nodes[3] });
			ConstraintBase c = SelectionCountConstraint.MinSelected(1, new INode[] { nodes[0], nodes[1] });
			ConstraintBase d = SelectionCountConstraint.MinSelected(1, new INode[] { nodes[2], nodes[3] });
			IConstraint[] deduced = ConstraintGenerator.GenerateDeducedConstraints(new IConstraint[] { a, b, c, d }, true, false).ToArray();
			Assert.AreEqual(1, deduced.Length);
			SelectionCountConstraint e = deduced[0] as SelectionCountConstraint;
			Assert.IsNotNull(e);
			Assert.AreEqual(0, e.Max);
			Assert.IsTrue(e.SelectionState);
			INode[] expectedNodes = new INode[] { nodes[4] };
			INode[] actualNodes = e.Nodes.ToArray();
			CollectionAssert.AreEquivalent(expectedNodes, actualNodes);
		}
	}
}
