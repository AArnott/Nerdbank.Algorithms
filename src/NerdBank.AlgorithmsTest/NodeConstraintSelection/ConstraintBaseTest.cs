using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NerdBank.Algorithms.NodeConstraintSelection;

namespace NerdBank.AlgorithmsTest.NodeConstraintSelection {
	[TestClass()]
	public class ConstraintBaseTest : TestBase {
		[TestMethod]
		public void FactorialTest() {
			Assert.AreEqual(1, ConstraintBase.Factorial(1));
			Assert.AreEqual(2 * 1, ConstraintBase.Factorial(2));
			Assert.AreEqual(3 * 2 * 1, ConstraintBase.Factorial(3));
			Assert.AreEqual(4 * 3 * 2 * 1, ConstraintBase.Factorial(4));
		}

		[TestMethod]
		public void ChooseNegativeTest() {
			Assert.AreEqual(0, ConstraintBase.Choose(3, -1));
		}

		[TestMethod]
		public void ChooseTest() {
			Assert.AreEqual(3, ConstraintBase.Choose(3, 2));

			Assert.AreEqual(1, ConstraintBase.Choose(4, 0));
			Assert.AreEqual(4, ConstraintBase.Choose(4, 1));
			Assert.AreEqual(6, ConstraintBase.Choose(4, 2));
			Assert.AreEqual(4, ConstraintBase.Choose(4, 3));
			Assert.AreEqual(1, ConstraintBase.Choose(4, 4));
		}

	}
}
