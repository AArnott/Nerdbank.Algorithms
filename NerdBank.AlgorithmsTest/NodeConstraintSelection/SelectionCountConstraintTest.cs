using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NerdBank.Algorithms.NodeConstraintSelection;

namespace NerdBank.AlgorithmsTest.NodeConstraintSelection {
	[TestClass()]
	public class SelectionCountConstraintTest : TestBase {
		INode[] nodes;

		[TestInitialize]
		public void Setup() {
			nodes = new INode[] { new DummyNode(), new DummyNode(), new DummyNode(), new DummyNode() };
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ConstructorNullNodesTest() {
			new SelectionCountConstraint(0, 2, true, null);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void ConstructorNoNodesTest() {
			new SelectionCountConstraint(0, 2, true, new DummyNode[] { });
		}

		[TestMethod]
		public void ToStringTest() {
			string nodesString = string.Join(", ", nodes.Select(n => n.ToString()).ToArray());
			Assert.AreEqual("SelectionCountConstraint(1, 2, True, [" + nodesString + "])",
				SelectionCountConstraint.RangeSelected(1, 2, nodes).ToString());
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void NegativeMinTest() {
			new SelectionCountConstraint(-1, 2, true, nodes);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void NegativeMaxTest() {
			new SelectionCountConstraint(0, -2, true, nodes);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void MaxLessThanMinTest() {
			new SelectionCountConstraint(2, 1, true, nodes);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void MaxMoreThanNodesTest() {
			new SelectionCountConstraint(0, nodes.Count() + 1, true, nodes);
		}

		[TestMethod]
		public void MinSelectedSatisfactionTests() {
			var target = SelectionCountConstraint.MinSelected(2, nodes);
			Assert.IsTrue(target.IsSatisfiable);
			Assert.IsFalse(target.IsSatisfied);
			nodes[0].IsSelected = true;
			Assert.IsTrue(target.IsSatisfiable);
			Assert.IsFalse(target.IsSatisfied);
			nodes[1].IsSelected = true;
			Assert.IsTrue(target.IsSatisfiable);
			Assert.IsTrue(target.IsSatisfied);
			nodes[2].IsSelected = true;
			Assert.IsTrue(target.IsSatisfiable);
			Assert.IsTrue(target.IsSatisfied);
		}

		[TestMethod]
		public void MaxSelectedSatisfactionTest() {
			var target = SelectionCountConstraint.MaxSelected(2, nodes);
			Assert.IsTrue(target.IsSatisfiable);
			Assert.IsTrue(target.IsSatisfied);
			nodes[0].IsSelected = true;
			Assert.IsTrue(target.IsSatisfiable);
			Assert.IsTrue(target.IsSatisfied);
			nodes[1].IsSelected = true;
			Assert.IsTrue(target.IsSatisfiable);
			Assert.IsTrue(target.IsSatisfied);
			nodes[2].IsSelected = false;
			Assert.IsTrue(target.IsSatisfiable);
			Assert.IsTrue(target.IsSatisfied);
			nodes[3].IsSelected = true;
			Assert.IsFalse(target.IsSatisfiable);
			Assert.IsFalse(target.IsSatisfied);
		}

		[TestMethod]
		public void RangeSelectionSatisfactionTest() {
			var target = SelectionCountConstraint.RangeSelected(2, 3, nodes);
			Assert.IsTrue(target.IsSatisfiable);
			Assert.IsFalse(target.IsSatisfied);
			nodes[0].IsSelected = true;
			Assert.IsTrue(target.IsSatisfiable);
			Assert.IsFalse(target.IsSatisfied);
			nodes[1].IsSelected = true;
			Assert.IsTrue(target.IsSatisfiable);
			Assert.IsTrue(target.IsSatisfied);
			nodes[2].IsSelected = true;
			Assert.IsTrue(target.IsSatisfiable);
			Assert.IsTrue(target.IsSatisfied);
			nodes[3].IsSelected = true;
			Assert.IsFalse(target.IsSatisfiable);
			Assert.IsFalse(target.IsSatisfied);
		}

		[TestMethod]
		public void NotSelectedSatisfactionTest() {
			var target = new SelectionCountConstraint(1, 2, false, nodes);
			Assert.IsFalse(target.IsSatisfied);
			Assert.IsTrue(target.IsSatisfiable);
			nodes[0].IsSelected = false;
			Assert.IsTrue(target.IsSatisfied);
			Assert.IsTrue(target.IsSatisfiable);
			nodes[1].IsSelected = false;
			Assert.IsTrue(target.IsSatisfied);
			Assert.IsTrue(target.IsSatisfiable);
			nodes[2].IsSelected = true;
			Assert.IsTrue(target.IsSatisfied);
			Assert.IsTrue(target.IsSatisfiable);
			nodes[3].IsSelected = false;
			Assert.IsFalse(target.IsSatisfied);
			Assert.IsFalse(target.IsSatisfiable);
		}

		[TestMethod]
		public void MinSelectedDissatisfactionTest() {
			var target = SelectionCountConstraint.RangeSelected(2, 3, nodes);
			// We won't mark any node as selected, but rather we'll eat away
			// at the indeterminate nodes until less than two are available for selecting,
			// which should make the constraint report that it is no longer satisfiable.
			nodes[0].IsSelected = false;
			Assert.IsTrue(target.IsSatisfiable);
			nodes[1].IsSelected = false;
			Assert.IsTrue(target.IsSatisfiable);
			nodes[2].IsSelected = false;
			Assert.IsFalse(target.IsSatisfiable);
		}

		[TestMethod]
		public void IsBreakableTestNoBreak() {
			var target = SelectionCountConstraint.MaxSelected(3, nodes);
			Assert.IsTrue(target.IsBreakable);
			Assert.IsFalse(target.IsBroken);
			nodes[0].IsSelected = false;
			Assert.IsFalse(target.IsBreakable);
			Assert.IsFalse(target.IsBroken);
		}

		[TestMethod]
		public void IsBreakableTestBreak() {
			var target = SelectionCountConstraint.MinSelected(3, nodes);
			Assert.IsTrue(target.IsBreakable);
			Assert.IsFalse(target.IsBroken);
			nodes[0].IsSelected = false;
			Assert.IsTrue(target.IsBreakable);
			Assert.IsFalse(target.IsBroken);
			nodes[1].IsSelected = false;
			Assert.IsTrue(target.IsBreakable);
			Assert.IsTrue(target.IsBroken);
		}

		[TestMethod]
		public void ResolveMinTest() {
			// If at least one node should be selected, 
			// no amount of selected nodes should cause resolving.
			var target = SelectionCountConstraint.MinSelected(1, nodes);
			Assert.IsFalse(target.CanResolve);
			Assert.IsFalse(target.Resolve());
			for (int i = 0; i < nodes.Length; i++) {
				nodes[i].IsSelected = true;
				Assert.IsFalse(target.CanResolve);
				Assert.IsFalse(target.Resolve());
			}
		}

		[TestMethod]
		public void ResolveMinComplementTest() {
			// If at least one node should be selected, 
			// unselecting all but one node should resolve last one to selected.
			var target = SelectionCountConstraint.MinSelected(1, nodes);
			for (int i = 0; i < nodes.Length - 1; i++) {
				Assert.IsFalse(target.CanResolve);
				Assert.IsFalse(target.Resolve());
				nodes[i].IsSelected = false;
			}
			Assert.IsTrue(target.CanResolve);
			Assert.IsTrue(target.Resolve());
			Assert.IsTrue(nodes[nodes.Length - 1].IsSelected.Value);
		}

		[TestMethod]
		public void ResolveMaxTest() {
			// If up to three nodes can be selected, 
			// no amount of unselected nodes should cause resolving.
			var target = SelectionCountConstraint.MaxSelected(3, nodes);
			Assert.IsFalse(target.CanResolve);
			Assert.IsFalse(target.Resolve());
			for (int i = 0; i < nodes.Length; i++) {
				nodes[i].IsSelected = false;
				Assert.IsFalse(target.CanResolve);
				Assert.IsFalse(target.Resolve());
			}
		}

		[TestMethod]
		public void ResolveMaxComplementTest() {
			// If up to three can be selected, then after 3 are selected
			// the 4th should resolve as unselected.
			var target = SelectionCountConstraint.MaxSelected(nodes.Length - 1, nodes);
			for (int i = 0; i < nodes.Length - 1; i++) {
				Assert.IsFalse(target.CanResolve);
				Assert.IsFalse(target.Resolve());
				nodes[i].IsSelected = true;
			}
			Assert.IsTrue(target.CanResolve);
			Assert.IsTrue(target.Resolve());
			Assert.IsFalse(nodes[nodes.Length - 1].IsSelected.Value);
		}

		[TestMethod]
		public void ResolveExactTest() {
			// If exactly one node should be selected, 
			// once that node is selected the rest should be unselected.
			var target = SelectionCountConstraint.ExactSelected(1, nodes);
			Assert.IsFalse(target.CanResolve);
			Assert.IsFalse(target.Resolve());
			nodes[0].IsSelected = false;
			Assert.IsFalse(target.CanResolve);
			Assert.IsFalse(target.Resolve());
			nodes[1].IsSelected = true;
			Assert.IsTrue(target.CanResolve);
			Assert.IsTrue(target.Resolve());
			Assert.IsFalse(nodes[2].IsSelected.Value);
			Assert.IsFalse(nodes[3].IsSelected.Value);
		}

		[TestMethod]
		public void ResolveExactComplementTest() {
			// If exactly one node should be selected, 
			// once all other nodes are unselected the one should be selected.
			var target = SelectionCountConstraint.ExactSelected(1, nodes);
			Assert.IsFalse(target.CanResolve);
			Assert.IsFalse(target.Resolve());
			nodes[0].IsSelected = false;
			Assert.IsFalse(target.CanResolve);
			Assert.IsFalse(target.Resolve());
			nodes[1].IsSelected = false;
			Assert.IsFalse(target.CanResolve);
			Assert.IsFalse(target.Resolve());
			nodes[2].IsSelected = false;
			Assert.IsTrue(target.CanResolve);
			Assert.IsTrue(target.Resolve());
			Assert.IsTrue(nodes[3].IsSelected.Value);
		}

		[TestMethod]
		public void ResolveExactlyOneTest() {
			// If exactly one node should be selected and one node is in the list,
			// then it should be selected by resolving.
			var shortList = nodes.Where((n, i) => i == 0).OfType<INode>();
			var target = SelectionCountConstraint.ExactSelected(1, shortList);
			Assert.IsTrue(target.CanResolve);
			Assert.IsTrue(target.Resolve());
			Assert.IsTrue(nodes.First().IsSelected.Value);
		}

		[TestMethod]
		public void PossibleSolutionsCountTest() {
			if (nodes.Length != 4) Assert.Inconclusive("Test depends on a list of 4 nodes.");

			// For any help confirming these numbers, refer to Pascal's triangle

			// The first several sets test the possibility calculations when no selections have been made.
			Assert.AreEqual(1, SelectionCountConstraint.ExactSelected(0, nodes).PossibleSolutionsCount);
			Assert.AreEqual(4, SelectionCountConstraint.ExactSelected(1, nodes).PossibleSolutionsCount);
			Assert.AreEqual(6, SelectionCountConstraint.ExactSelected(2, nodes).PossibleSolutionsCount);
			Assert.AreEqual(4, SelectionCountConstraint.ExactSelected(3, nodes).PossibleSolutionsCount);
			Assert.AreEqual(1, SelectionCountConstraint.ExactSelected(4, nodes).PossibleSolutionsCount);

			Assert.AreEqual(1 + 4 + 6 + 4 + 1, SelectionCountConstraint.MinSelected(0, nodes).PossibleSolutionsCount);
			Assert.AreEqual(1 + 4 + 6 + 4, SelectionCountConstraint.MinSelected(1, nodes).PossibleSolutionsCount);
			Assert.AreEqual(1 + 4 + 6, SelectionCountConstraint.MinSelected(2, nodes).PossibleSolutionsCount);
			Assert.AreEqual(1 + 4, SelectionCountConstraint.MinSelected(3, nodes).PossibleSolutionsCount);
			Assert.AreEqual(1, SelectionCountConstraint.MinSelected(4, nodes).PossibleSolutionsCount);

			Assert.AreEqual(1, SelectionCountConstraint.MaxSelected(0, nodes).PossibleSolutionsCount);
			Assert.AreEqual(1 + 4, SelectionCountConstraint.MaxSelected(1, nodes).PossibleSolutionsCount);
			Assert.AreEqual(1 + 4 + 6, SelectionCountConstraint.MaxSelected(2, nodes).PossibleSolutionsCount);
			Assert.AreEqual(1 + 4 + 6 + 4, SelectionCountConstraint.MaxSelected(3, nodes).PossibleSolutionsCount);
			Assert.AreEqual(1 + 4 + 6 + 4 + 1, SelectionCountConstraint.MaxSelected(4, nodes).PossibleSolutionsCount);

			Assert.AreEqual(4 + 6, SelectionCountConstraint.RangeSelected(1, 2, nodes).PossibleSolutionsCount);
			Assert.AreEqual(6 + 4, SelectionCountConstraint.RangeSelected(2, 3, nodes).PossibleSolutionsCount);
			Assert.AreEqual(4 + 1, SelectionCountConstraint.RangeSelected(3, 4, nodes).PossibleSolutionsCount);
			Assert.AreEqual(4 + 6 + 4, SelectionCountConstraint.RangeSelected(1, 3, nodes).PossibleSolutionsCount);
			Assert.AreEqual(4 + 6 + 4 + 1, SelectionCountConstraint.RangeSelected(1, 4, nodes).PossibleSolutionsCount);

			// These next sets of tests preset some of the nodes to limit the future possibilities.
			nodes[2].IsSelected = true;

			Assert.AreEqual(0, SelectionCountConstraint.ExactSelected(0, nodes).PossibleSolutionsCount);
			Assert.AreEqual(1, SelectionCountConstraint.ExactSelected(1, nodes).PossibleSolutionsCount);
			Assert.AreEqual(3, SelectionCountConstraint.ExactSelected(2, nodes).PossibleSolutionsCount);
			Assert.AreEqual(3, SelectionCountConstraint.ExactSelected(3, nodes).PossibleSolutionsCount);
			Assert.AreEqual(1, SelectionCountConstraint.ExactSelected(4, nodes).PossibleSolutionsCount);

			Assert.AreEqual(1 + 3 + 3 + 1, SelectionCountConstraint.MinSelected(0, nodes).PossibleSolutionsCount);
			Assert.AreEqual(1 + 3 + 3 + 1, SelectionCountConstraint.MinSelected(1, nodes).PossibleSolutionsCount);
			Assert.AreEqual(1 + 3 + 3, SelectionCountConstraint.MinSelected(2, nodes).PossibleSolutionsCount);
			Assert.AreEqual(1 + 3, SelectionCountConstraint.MinSelected(3, nodes).PossibleSolutionsCount);
			Assert.AreEqual(1, SelectionCountConstraint.MinSelected(4, nodes).PossibleSolutionsCount);

			Assert.AreEqual(0, SelectionCountConstraint.MaxSelected(0, nodes).PossibleSolutionsCount);
			Assert.AreEqual(1, SelectionCountConstraint.MaxSelected(1, nodes).PossibleSolutionsCount);
			Assert.AreEqual(1 + 3, SelectionCountConstraint.MaxSelected(2, nodes).PossibleSolutionsCount);
			Assert.AreEqual(1 + 3 + 3, SelectionCountConstraint.MaxSelected(3, nodes).PossibleSolutionsCount);
			Assert.AreEqual(1 + 3 + 3 + 1, SelectionCountConstraint.MaxSelected(4, nodes).PossibleSolutionsCount);

			Assert.AreEqual(1 + 3, SelectionCountConstraint.RangeSelected(0, 2, nodes).PossibleSolutionsCount);
			Assert.AreEqual(1 + 3, SelectionCountConstraint.RangeSelected(1, 2, nodes).PossibleSolutionsCount);
			Assert.AreEqual(3 + 3, SelectionCountConstraint.RangeSelected(2, 3, nodes).PossibleSolutionsCount);
			Assert.AreEqual(3 + 1, SelectionCountConstraint.RangeSelected(3, 4, nodes).PossibleSolutionsCount);
			Assert.AreEqual(1 + 3 + 3, SelectionCountConstraint.RangeSelected(1, 3, nodes).PossibleSolutionsCount);
			Assert.AreEqual(1 + 3 + 3 + 1, SelectionCountConstraint.RangeSelected(1, 4, nodes).PossibleSolutionsCount);
		}

		[TestMethod]
		public void PossibleSolutionsTest() {
			nodes = nodes.Take(3).ToArray();

			IList<INode>[] expected = new IList<INode>[] { 
											   new List<INode>(new INode[] {nodes[0]}), 
											   new List<INode>(new INode[] {nodes[1]}), 
											   new List<INode>(new INode[] {nodes[2]}),
										   };
			IList<INode>[] actual = SelectionCountConstraint.ExactSelected(1, nodes).PossibleSolutions.ToArray();
			Assert.AreEqual(expected.Length, actual.Length);
			for (int i = 0; i < expected.Length; i++)
				CollectionAssert.AreEquivalent(expected[i].ToArray(), actual[i].ToArray());

			expected = new IList<INode>[] { 
											   new List<INode>(new INode[] {nodes[0], nodes[1]}), 
											   new List<INode>(new INode[] {nodes[0], nodes[2]}), 
											   new List<INode>(new INode[] {nodes[1], nodes[2]}), 
										   };
			actual = SelectionCountConstraint.ExactSelected(2, nodes).PossibleSolutions.ToArray();
			Assert.AreEqual(expected.Length, actual.Length);
			for (int i = 0; i < expected.Length; i++)
				CollectionAssert.AreEquivalent(expected[i].ToArray(), actual[i].ToArray());

			expected = new IList<INode>[] { 
											   new List<INode>(new INode[] {}), 
											   new List<INode>(new INode[] {nodes[0]}), 
											   new List<INode>(new INode[] {nodes[1]}), 
											   new List<INode>(new INode[] {nodes[2]}),
											   new List<INode>(new INode[] {nodes[0], nodes[1]}), 
											   new List<INode>(new INode[] {nodes[0], nodes[2]}), 
											   new List<INode>(new INode[] {nodes[1], nodes[2]}), 
										   };
			actual = SelectionCountConstraint.RangeSelected(0, 2, nodes).PossibleSolutions.ToArray();
			Assert.AreEqual(expected.Length, actual.Length);
			for (int i = 0; i < expected.Length; i++)
				CollectionAssert.AreEquivalent(expected[i].ToArray(), actual[i].ToArray());
		}
	}
}
