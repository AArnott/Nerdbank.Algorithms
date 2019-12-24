// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using NerdBank.Algorithms.NodeConstraintSelection;
using Xunit;

public class SelectionCountConstraintTest
{
	private INode[] nodes;

	/// <summary>
	/// Initializes a new instance of the <see cref="SelectionCountConstraintTest"/> class.
	/// </summary>
	public SelectionCountConstraintTest()
	{
		this.nodes = new INode[] { new DummyNode(), new DummyNode(), new DummyNode(), new DummyNode() };
	}

	[Fact]
	public void ConstructorNullNodesTest()
	{
		Assert.Throws<ArgumentNullException>(() => new SelectionCountConstraint(0, 2, true, null!));
	}

	[Fact]
	public void ConstructorNoNodesTest()
	{
		Assert.Throws<ArgumentException>(() => new SelectionCountConstraint(0, 2, true, Array.Empty<DummyNode>()));
	}

	[Fact]
	public void ToStringTest()
	{
		var nodesString = string.Join(", ", this.nodes.Select(n => n.ToString()).ToArray());
		Assert.Equal(
			"SelectionCountConstraint(1, 2, True, [" + nodesString + "])",
			SelectionCountConstraint.RangeSelected(1, 2, this.nodes).ToString());
	}

	[Fact]
	public void NegativeMinTest()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => new SelectionCountConstraint(-1, 2, true, this.nodes));
	}

	[Fact]
	public void NegativeMaxTest()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => new SelectionCountConstraint(0, -2, true, this.nodes));
	}

	[Fact]
	public void MaxLessThanMinTest()
	{
		Assert.Throws<ArgumentException>(() => new SelectionCountConstraint(2, 1, true, this.nodes));
	}

	[Fact]
	public void MaxMoreThanNodesTest()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => new SelectionCountConstraint(0, this.nodes.Length + 1, true, this.nodes));
	}

	[Fact]
	public void MinSelectedSatisfactionTests()
	{
		var target = SelectionCountConstraint.MinSelected(2, this.nodes);
		Assert.True(target.IsSatisfiable);
		Assert.False(target.IsSatisfied);
		this.nodes[0].IsSelected = true;
		Assert.True(target.IsSatisfiable);
		Assert.False(target.IsSatisfied);
		this.nodes[1].IsSelected = true;
		Assert.True(target.IsSatisfiable);
		Assert.True(target.IsSatisfied);
		this.nodes[2].IsSelected = true;
		Assert.True(target.IsSatisfiable);
		Assert.True(target.IsSatisfied);
	}

	[Fact]
	public void MaxSelectedSatisfactionTest()
	{
		var target = SelectionCountConstraint.MaxSelected(2, this.nodes);
		Assert.True(target.IsSatisfiable);
		Assert.True(target.IsSatisfied);
		this.nodes[0].IsSelected = true;
		Assert.True(target.IsSatisfiable);
		Assert.True(target.IsSatisfied);
		this.nodes[1].IsSelected = true;
		Assert.True(target.IsSatisfiable);
		Assert.True(target.IsSatisfied);
		this.nodes[2].IsSelected = false;
		Assert.True(target.IsSatisfiable);
		Assert.True(target.IsSatisfied);
		this.nodes[3].IsSelected = true;
		Assert.False(target.IsSatisfiable);
		Assert.False(target.IsSatisfied);
	}

	[Fact]
	public void RangeSelectionSatisfactionTest()
	{
		var target = SelectionCountConstraint.RangeSelected(2, 3, this.nodes);
		Assert.True(target.IsSatisfiable);
		Assert.False(target.IsSatisfied);
		this.nodes[0].IsSelected = true;
		Assert.True(target.IsSatisfiable);
		Assert.False(target.IsSatisfied);
		this.nodes[1].IsSelected = true;
		Assert.True(target.IsSatisfiable);
		Assert.True(target.IsSatisfied);
		this.nodes[2].IsSelected = true;
		Assert.True(target.IsSatisfiable);
		Assert.True(target.IsSatisfied);
		this.nodes[3].IsSelected = true;
		Assert.False(target.IsSatisfiable);
		Assert.False(target.IsSatisfied);
	}

	[Fact]
	public void NotSelectedSatisfactionTest()
	{
		var target = new SelectionCountConstraint(1, 2, false, this.nodes);
		Assert.False(target.IsSatisfied);
		Assert.True(target.IsSatisfiable);
		this.nodes[0].IsSelected = false;
		Assert.True(target.IsSatisfied);
		Assert.True(target.IsSatisfiable);
		this.nodes[1].IsSelected = false;
		Assert.True(target.IsSatisfied);
		Assert.True(target.IsSatisfiable);
		this.nodes[2].IsSelected = true;
		Assert.True(target.IsSatisfied);
		Assert.True(target.IsSatisfiable);
		this.nodes[3].IsSelected = false;
		Assert.False(target.IsSatisfied);
		Assert.False(target.IsSatisfiable);
	}

	[Fact]
	public void MinSelectedDissatisfactionTest()
	{
		var target = SelectionCountConstraint.RangeSelected(2, 3, this.nodes);

		// We won't mark any node as selected, but rather we'll eat away
		// at the indeterminate nodes until less than two are available for selecting,
		// which should make the constraint report that it is no longer satisfiable.
		this.nodes[0].IsSelected = false;
		Assert.True(target.IsSatisfiable);
		this.nodes[1].IsSelected = false;
		Assert.True(target.IsSatisfiable);
		this.nodes[2].IsSelected = false;
		Assert.False(target.IsSatisfiable);
	}

	[Fact]
	public void IsBreakableTestNoBreak()
	{
		var target = SelectionCountConstraint.MaxSelected(3, this.nodes);
		Assert.True(target.IsBreakable);
		Assert.False(target.IsBroken);
		this.nodes[0].IsSelected = false;
		Assert.False(target.IsBreakable);
		Assert.False(target.IsBroken);
	}

	[Fact]
	public void IsBreakableTestBreak()
	{
		var target = SelectionCountConstraint.MinSelected(3, this.nodes);
		Assert.True(target.IsBreakable);
		Assert.False(target.IsBroken);
		this.nodes[0].IsSelected = false;
		Assert.True(target.IsBreakable);
		Assert.False(target.IsBroken);
		this.nodes[1].IsSelected = false;
		Assert.True(target.IsBreakable);
		Assert.True(target.IsBroken);
	}

	[Fact]
	public void ResolveMinTest()
	{
		// If at least one node should be selected,
		// no amount of selected nodes should cause resolving.
		var target = SelectionCountConstraint.MinSelected(1, this.nodes);
		Assert.False(target.CanResolve);
		Assert.False(target.Resolve());
		for (var i = 0; i < this.nodes.Length; i++)
		{
			this.nodes[i].IsSelected = true;
			Assert.False(target.CanResolve);
			Assert.False(target.Resolve());
		}
	}

	[Fact]
	public void ResolveMinComplementTest()
	{
		// If at least one node should be selected,
		// unselecting all but one node should resolve last one to selected.
		var target = SelectionCountConstraint.MinSelected(1, this.nodes);
		for (var i = 0; i < this.nodes.Length - 1; i++)
		{
			Assert.False(target.CanResolve);
			Assert.False(target.Resolve());
			this.nodes[i].IsSelected = false;
		}

		Assert.True(target.CanResolve);
		Assert.True(target.Resolve());
		Assert.True(this.nodes[this.nodes.Length - 1].IsSelected!.Value);
	}

	[Fact]
	public void ResolveMaxTest()
	{
		// If up to three nodes can be selected,
		// no amount of unselected nodes should cause resolving.
		var target = SelectionCountConstraint.MaxSelected(3, this.nodes);
		Assert.False(target.CanResolve);
		Assert.False(target.Resolve());
		for (var i = 0; i < this.nodes.Length; i++)
		{
			this.nodes[i].IsSelected = false;
			Assert.False(target.CanResolve);
			Assert.False(target.Resolve());
		}
	}

	[Fact]
	public void ResolveMaxComplementTest()
	{
		// If up to three can be selected, then after 3 are selected
		// the 4th should resolve as unselected.
		var target = SelectionCountConstraint.MaxSelected(this.nodes.Length - 1, this.nodes);
		for (var i = 0; i < this.nodes.Length - 1; i++)
		{
			Assert.False(target.CanResolve);
			Assert.False(target.Resolve());
			this.nodes[i].IsSelected = true;
		}

		Assert.True(target.CanResolve);
		Assert.True(target.Resolve());
		Assert.False(this.nodes[this.nodes.Length - 1].IsSelected!.Value);
	}

	[Fact]
	public void ResolveExactTest()
	{
		// If exactly one node should be selected,
		// once that node is selected the rest should be unselected.
		var target = SelectionCountConstraint.ExactSelected(1, this.nodes);
		Assert.False(target.CanResolve);
		Assert.False(target.Resolve());
		this.nodes[0].IsSelected = false;
		Assert.False(target.CanResolve);
		Assert.False(target.Resolve());
		this.nodes[1].IsSelected = true;
		Assert.True(target.CanResolve);
		Assert.True(target.Resolve());
		Assert.False(this.nodes[2].IsSelected!.Value);
		Assert.False(this.nodes[3].IsSelected!.Value);
	}

	[Fact]
	public void ResolveExactComplementTest()
	{
		// If exactly one node should be selected,
		// once all other nodes are unselected the one should be selected.
		var target = SelectionCountConstraint.ExactSelected(1, this.nodes);
		Assert.False(target.CanResolve);
		Assert.False(target.Resolve());
		this.nodes[0].IsSelected = false;
		Assert.False(target.CanResolve);
		Assert.False(target.Resolve());
		this.nodes[1].IsSelected = false;
		Assert.False(target.CanResolve);
		Assert.False(target.Resolve());
		this.nodes[2].IsSelected = false;
		Assert.True(target.CanResolve);
		Assert.True(target.Resolve());
		Assert.True(this.nodes[3].IsSelected!.Value);
	}

	[Fact]
	public void ResolveExactlyOneTest()
	{
		// If exactly one node should be selected and one node is in the list,
		// then it should be selected by resolving.
		IEnumerable<INode> shortList = this.nodes.Where((n, i) => i == 0).OfType<INode>();
		var target = SelectionCountConstraint.ExactSelected(1, shortList);
		Assert.True(target.CanResolve);
		Assert.True(target.Resolve());
		Assert.True(this.nodes.First().IsSelected!.Value);
	}

	[SkippableFact]
	public void PossibleSolutionsCountTest()
	{
		Skip.If(this.nodes.Length != 4, "Test depends on a list of 4 nodes.");

		// For any help confirming these numbers, refer to Pascal's triangle

		// The first several sets test the possibility calculations when no selections have been made.
		Assert.Equal(1, SelectionCountConstraint.ExactSelected(0, this.nodes).PossibleSolutionsCount);
		Assert.Equal(4, SelectionCountConstraint.ExactSelected(1, this.nodes).PossibleSolutionsCount);
		Assert.Equal(6, SelectionCountConstraint.ExactSelected(2, this.nodes).PossibleSolutionsCount);
		Assert.Equal(4, SelectionCountConstraint.ExactSelected(3, this.nodes).PossibleSolutionsCount);
		Assert.Equal(1, SelectionCountConstraint.ExactSelected(4, this.nodes).PossibleSolutionsCount);

		Assert.Equal(1 + 4 + 6 + 4 + 1, SelectionCountConstraint.MinSelected(0, this.nodes).PossibleSolutionsCount);
		Assert.Equal(1 + 4 + 6 + 4, SelectionCountConstraint.MinSelected(1, this.nodes).PossibleSolutionsCount);
		Assert.Equal(1 + 4 + 6, SelectionCountConstraint.MinSelected(2, this.nodes).PossibleSolutionsCount);
		Assert.Equal(1 + 4, SelectionCountConstraint.MinSelected(3, this.nodes).PossibleSolutionsCount);
		Assert.Equal(1, SelectionCountConstraint.MinSelected(4, this.nodes).PossibleSolutionsCount);

		Assert.Equal(1, SelectionCountConstraint.MaxSelected(0, this.nodes).PossibleSolutionsCount);
		Assert.Equal(1 + 4, SelectionCountConstraint.MaxSelected(1, this.nodes).PossibleSolutionsCount);
		Assert.Equal(1 + 4 + 6, SelectionCountConstraint.MaxSelected(2, this.nodes).PossibleSolutionsCount);
		Assert.Equal(1 + 4 + 6 + 4, SelectionCountConstraint.MaxSelected(3, this.nodes).PossibleSolutionsCount);
		Assert.Equal(1 + 4 + 6 + 4 + 1, SelectionCountConstraint.MaxSelected(4, this.nodes).PossibleSolutionsCount);

		Assert.Equal(4 + 6, SelectionCountConstraint.RangeSelected(1, 2, this.nodes).PossibleSolutionsCount);
		Assert.Equal(6 + 4, SelectionCountConstraint.RangeSelected(2, 3, this.nodes).PossibleSolutionsCount);
		Assert.Equal(4 + 1, SelectionCountConstraint.RangeSelected(3, 4, this.nodes).PossibleSolutionsCount);
		Assert.Equal(4 + 6 + 4, SelectionCountConstraint.RangeSelected(1, 3, this.nodes).PossibleSolutionsCount);
		Assert.Equal(4 + 6 + 4 + 1, SelectionCountConstraint.RangeSelected(1, 4, this.nodes).PossibleSolutionsCount);

		// These next sets of tests preset some of the nodes to limit the future possibilities.
		this.nodes[2].IsSelected = true;

		Assert.Equal(0, SelectionCountConstraint.ExactSelected(0, this.nodes).PossibleSolutionsCount);
		Assert.Equal(1, SelectionCountConstraint.ExactSelected(1, this.nodes).PossibleSolutionsCount);
		Assert.Equal(3, SelectionCountConstraint.ExactSelected(2, this.nodes).PossibleSolutionsCount);
		Assert.Equal(3, SelectionCountConstraint.ExactSelected(3, this.nodes).PossibleSolutionsCount);
		Assert.Equal(1, SelectionCountConstraint.ExactSelected(4, this.nodes).PossibleSolutionsCount);

		Assert.Equal(1 + 3 + 3 + 1, SelectionCountConstraint.MinSelected(0, this.nodes).PossibleSolutionsCount);
		Assert.Equal(1 + 3 + 3 + 1, SelectionCountConstraint.MinSelected(1, this.nodes).PossibleSolutionsCount);
		Assert.Equal(1 + 3 + 3, SelectionCountConstraint.MinSelected(2, this.nodes).PossibleSolutionsCount);
		Assert.Equal(1 + 3, SelectionCountConstraint.MinSelected(3, this.nodes).PossibleSolutionsCount);
		Assert.Equal(1, SelectionCountConstraint.MinSelected(4, this.nodes).PossibleSolutionsCount);

		Assert.Equal(0, SelectionCountConstraint.MaxSelected(0, this.nodes).PossibleSolutionsCount);
		Assert.Equal(1, SelectionCountConstraint.MaxSelected(1, this.nodes).PossibleSolutionsCount);
		Assert.Equal(1 + 3, SelectionCountConstraint.MaxSelected(2, this.nodes).PossibleSolutionsCount);
		Assert.Equal(1 + 3 + 3, SelectionCountConstraint.MaxSelected(3, this.nodes).PossibleSolutionsCount);
		Assert.Equal(1 + 3 + 3 + 1, SelectionCountConstraint.MaxSelected(4, this.nodes).PossibleSolutionsCount);

		Assert.Equal(1 + 3, SelectionCountConstraint.RangeSelected(0, 2, this.nodes).PossibleSolutionsCount);
		Assert.Equal(1 + 3, SelectionCountConstraint.RangeSelected(1, 2, this.nodes).PossibleSolutionsCount);
		Assert.Equal(3 + 3, SelectionCountConstraint.RangeSelected(2, 3, this.nodes).PossibleSolutionsCount);
		Assert.Equal(3 + 1, SelectionCountConstraint.RangeSelected(3, 4, this.nodes).PossibleSolutionsCount);
		Assert.Equal(1 + 3 + 3, SelectionCountConstraint.RangeSelected(1, 3, this.nodes).PossibleSolutionsCount);
		Assert.Equal(1 + 3 + 3 + 1, SelectionCountConstraint.RangeSelected(1, 4, this.nodes).PossibleSolutionsCount);
	}

	[Fact]
	public void PossibleSolutionsTest()
	{
		this.nodes = this.nodes.Take(3).ToArray();

		var expected = new IList<INode>[]
		{
			new List<INode>(new INode[] { this.nodes[0] }),
			new List<INode>(new INode[] { this.nodes[1] }),
			new List<INode>(new INode[] { this.nodes[2] }),
		};
		IList<INode>[] actual = SelectionCountConstraint.ExactSelected(1, this.nodes).PossibleSolutions.ToArray();
		Assert.Equal(expected.Length, actual.Length);
		for (var i = 0; i < expected.Length; i++)
		{
			Assert.Equal<INode>(expected[i], actual[i]);
		}

		expected = new IList<INode>[]
		{
			new List<INode>(new INode[] { this.nodes[0], this.nodes[1] }),
			new List<INode>(new INode[] { this.nodes[0], this.nodes[2] }),
			new List<INode>(new INode[] { this.nodes[1], this.nodes[2] }),
		};
		actual = SelectionCountConstraint.ExactSelected(2, this.nodes).PossibleSolutions.ToArray();
		Assert.Equal(expected.Length, actual.Length);
		for (var i = 0; i < expected.Length; i++)
		{
			Assert.Equal<INode>(expected[i], actual[i]);
		}

		expected = new IList<INode>[]
		{
			new List<INode>(Array.Empty<INode>()),
			new List<INode>(new INode[] { this.nodes[0] }),
			new List<INode>(new INode[] { this.nodes[1] }),
			new List<INode>(new INode[] { this.nodes[2] }),
			new List<INode>(new INode[] { this.nodes[0], this.nodes[1] }),
			new List<INode>(new INode[] { this.nodes[0], this.nodes[2] }),
			new List<INode>(new INode[] { this.nodes[1], this.nodes[2] }),
		};
		actual = SelectionCountConstraint.RangeSelected(0, 2, this.nodes).PossibleSolutions.ToArray();
		Assert.Equal(expected.Length, actual.Length);
		for (var i = 0; i < expected.Length; i++)
		{
			Assert.Equal<INode>(expected[i], actual[i]);
		}
	}
}
