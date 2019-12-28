// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NerdBank.Algorithms.NodeConstraintSelection;
using Xunit;

public class SelectionCountConstraintTests
{
	private ImmutableArray<DummyNode> nodes;
	private Scenario<bool> scenario;

	/// <summary>
	/// Initializes a new instance of the <see cref="SelectionCountConstraintTests"/> class.
	/// </summary>
	public SelectionCountConstraintTests()
	{
		this.nodes = ImmutableArray.Create(new DummyNode("a"), new DummyNode("b"), new DummyNode("c"), new DummyNode("d"));
		this.scenario = new Scenario<bool>(this.nodes);
	}

	[Fact]
	public void Ctor_NullNodes()
	{
		Assert.Throws<ArgumentNullException>("nodes", () => new SelectionCountConstraint(default!, 0, 2));
	}

	[Fact]
	public void Ctor_EmptyNodes()
	{
		Assert.Throws<ArgumentException>("nodes", () => new SelectionCountConstraint(ImmutableArray.Create<object>(), 0, 2));
	}

	[Fact]
	public void ToStringTest()
	{
		var nodesString = string.Join(", ", this.nodes.Select(n => n.ToString()).ToArray());
		Assert.Equal(
			"SelectionCountConstraint(1-2 from {" + nodesString + "})",
			new SelectionCountConstraint(this.nodes, 1, 2).ToString());
	}

	[Fact]
	public void NegativeMinTest()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => new SelectionCountConstraint(this.nodes, -1, 2));
	}

	[Fact]
	public void NegativeMaxTest()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => new SelectionCountConstraint(this.nodes, 0, -2));
	}

	[Fact]
	public void MaxLessThanMinTest()
	{
		Assert.Throws<ArgumentException>(() => new SelectionCountConstraint(this.nodes, 2, 1));
	}

	[Fact]
	public void MaxMoreThanNodesTest()
	{
		var constraint = new SelectionCountConstraint(this.nodes, 0, int.MaxValue);
		Assert.Equal(this.nodes.Length, constraint.Maximum);
	}

	[Fact]
	public void MinSelectedSatisfactionTests()
	{
		var target = SelectionCountConstraint.MinSelected(this.nodes, 2);
		Assert.True(target.GetState(this.scenario).HasFlag(ConstraintStates.Satisfiable));
		Assert.False(target.GetState(this.scenario).HasFlag(ConstraintStates.Satisfied));
		this.scenario[0] = true;
		Assert.True(target.GetState(this.scenario).HasFlag(ConstraintStates.Satisfiable));
		Assert.False(target.GetState(this.scenario).HasFlag(ConstraintStates.Satisfied));
		this.scenario[1] = true;
		Assert.True(target.GetState(this.scenario).HasFlag(ConstraintStates.Satisfiable));
		Assert.True(target.GetState(this.scenario).HasFlag(ConstraintStates.Satisfied));
		this.scenario[2] = true;
		Assert.True(target.GetState(this.scenario).HasFlag(ConstraintStates.Satisfiable));
		Assert.True(target.GetState(this.scenario).HasFlag(ConstraintStates.Satisfied));
	}

	[Fact]
	public void MaxSelectedSatisfactionTest()
	{
		var target = SelectionCountConstraint.MaxSelected(this.nodes, 2);
		Assert.True(target.GetState(this.scenario).HasFlag(ConstraintStates.Satisfiable));
		Assert.True(target.GetState(this.scenario).HasFlag(ConstraintStates.Satisfied));
		this.scenario[0] = true;
		Assert.True(target.GetState(this.scenario).HasFlag(ConstraintStates.Satisfiable));
		Assert.True(target.GetState(this.scenario).HasFlag(ConstraintStates.Satisfied));
		this.scenario[1] = true;
		Assert.True(target.GetState(this.scenario).HasFlag(ConstraintStates.Satisfiable));
		Assert.True(target.GetState(this.scenario).HasFlag(ConstraintStates.Satisfied));
		this.scenario[2] = false;
		Assert.True(target.GetState(this.scenario).HasFlag(ConstraintStates.Satisfiable));
		Assert.True(target.GetState(this.scenario).HasFlag(ConstraintStates.Satisfied));
		this.scenario[3] = true;
		Assert.False(target.GetState(this.scenario).HasFlag(ConstraintStates.Satisfiable));
		Assert.False(target.GetState(this.scenario).HasFlag(ConstraintStates.Satisfied));
	}

	[Fact]
	public void RangeSelectionSatisfactionTest()
	{
		var target = SelectionCountConstraint.RangeSelected(this.nodes, 2, 3);
		Assert.True(target.GetState(this.scenario).HasFlag(ConstraintStates.Satisfiable));
		Assert.False(target.GetState(this.scenario).HasFlag(ConstraintStates.Satisfied));
		this.scenario[0] = true;
		Assert.True(target.GetState(this.scenario).HasFlag(ConstraintStates.Satisfiable));
		Assert.False(target.GetState(this.scenario).HasFlag(ConstraintStates.Satisfied));
		this.scenario[1] = true;
		Assert.True(target.GetState(this.scenario).HasFlag(ConstraintStates.Satisfiable));
		Assert.True(target.GetState(this.scenario).HasFlag(ConstraintStates.Satisfied));
		this.scenario[2] = true;
		Assert.True(target.GetState(this.scenario).HasFlag(ConstraintStates.Satisfiable));
		Assert.True(target.GetState(this.scenario).HasFlag(ConstraintStates.Satisfied));
		this.scenario[3] = true;
		Assert.False(target.GetState(this.scenario).HasFlag(ConstraintStates.Satisfiable));
		Assert.False(target.GetState(this.scenario).HasFlag(ConstraintStates.Satisfied));
	}

	[Fact]
	public void MinSelectedDissatisfactionTest()
	{
		var target = SelectionCountConstraint.RangeSelected(this.nodes, 2, 3);

		// We won't mark any node as selected, but rather we'll eat away
		// at the indeterminate nodes until less than two are available for selecting,
		// which should make the constraint report that it is no longer satisfiable.
		this.scenario[0] = false;
		Assert.True(target.GetState(this.scenario).HasFlag(ConstraintStates.Satisfiable));
		this.scenario[1] = false;
		Assert.True(target.GetState(this.scenario).HasFlag(ConstraintStates.Satisfiable));
		this.scenario[2] = false;
		Assert.False(target.GetState(this.scenario).HasFlag(ConstraintStates.Satisfiable));
	}

	[Fact]
	public void IsBreakableTestNoBreak()
	{
		var target = SelectionCountConstraint.MaxSelected(this.nodes, 3);
		Assert.True(target.GetState(this.scenario).HasFlag(ConstraintStates.Breakable));
		Assert.True(target.GetState(this.scenario).HasFlag(ConstraintStates.Satisfiable));
		this.scenario[0] = false;
		Assert.False(target.GetState(this.scenario).HasFlag(ConstraintStates.Breakable));
		Assert.True(target.GetState(this.scenario).HasFlag(ConstraintStates.Satisfiable));
	}

	[Fact]
	public void IsBreakableTestBreak()
	{
		var target = SelectionCountConstraint.MinSelected(this.nodes, 3);
		Assert.True(target.GetState(this.scenario).HasFlag(ConstraintStates.Breakable));
		Assert.True(target.GetState(this.scenario).HasFlag(ConstraintStates.Satisfiable));
		this.scenario[0] = false;
		Assert.True(target.GetState(this.scenario).HasFlag(ConstraintStates.Breakable));
		Assert.True(target.GetState(this.scenario).HasFlag(ConstraintStates.Satisfiable));
		this.scenario[1] = false;
		Assert.True(target.GetState(this.scenario).HasFlag(ConstraintStates.Breakable));
		Assert.False(target.GetState(this.scenario).HasFlag(ConstraintStates.Satisfiable));
	}

	[Fact]
	public void GetState_NullScenario()
	{
		var target = SelectionCountConstraint.MinSelected(this.nodes, 1);
		Assert.Throws<ArgumentNullException>("scenario", () => target.GetState(null!));
	}

	[Fact]
	public void Resolve_NullScenario()
	{
		var target = SelectionCountConstraint.MinSelected(this.nodes, 1);
		Assert.Throws<ArgumentNullException>("scenario", () => target.Resolve(null!));
	}

	[Fact]
	public void ResolveMinTest()
	{
		// If at least one node should be selected,
		// no amount of selected nodes should cause resolving.
		var target = SelectionCountConstraint.MinSelected(this.nodes, 1);
		Assert.False(target.GetState(this.scenario).HasFlag(ConstraintStates.Resolvable));
		Assert.False(target.Resolve(this.scenario));
		for (var i = 0; i < this.nodes.Length; i++)
		{
			this.scenario[i] = true;
			Assert.False(target.GetState(this.scenario).HasFlag(ConstraintStates.Resolvable));
			Assert.False(target.Resolve(this.scenario));
		}
	}

	[Fact]
	public void ResolveMinComplementTest()
	{
		// If at least one node should be selected,
		// unselecting all but one node should resolve last one to selected.
		var target = SelectionCountConstraint.MinSelected(this.nodes, 1);
		for (var i = 0; i < this.nodes.Length - 1; i++)
		{
			Assert.False(target.GetState(this.scenario).HasFlag(ConstraintStates.Resolvable));
			Assert.False(target.Resolve(this.scenario));
			this.scenario[i] = false;
		}

		Assert.True(target.GetState(this.scenario).HasFlag(ConstraintStates.Resolvable));
		Assert.True(target.Resolve(this.scenario));
		Assert.True(this.scenario[this.nodes.Length - 1]);
	}

	[Fact]
	public void ResolveMaxTest()
	{
		// If up to three nodes can be selected,
		// no amount of unselected nodes should cause resolving.
		var target = SelectionCountConstraint.MaxSelected(this.nodes, 3);
		Assert.False(target.GetState(this.scenario).HasFlag(ConstraintStates.Resolvable));
		Assert.False(target.Resolve(this.scenario));
		for (var i = 0; i < this.nodes.Length; i++)
		{
			this.scenario[i] = false;
			Assert.False(target.GetState(this.scenario).HasFlag(ConstraintStates.Resolvable));
			Assert.False(target.Resolve(this.scenario));
		}
	}

	[Fact]
	public void ResolveMaxComplementTest()
	{
		// If up to three can be selected, then after 3 are selected
		// the 4th should resolve as unselected.
		var target = SelectionCountConstraint.MaxSelected(this.nodes, this.nodes.Length - 1);
		for (var i = 0; i < this.nodes.Length - 1; i++)
		{
			Assert.False(target.GetState(this.scenario).HasFlag(ConstraintStates.Resolvable));
			Assert.False(target.Resolve(this.scenario));
			this.scenario[i] = true;
		}

		Assert.True(target.GetState(this.scenario).HasFlag(ConstraintStates.Resolvable));
		Assert.True(target.Resolve(this.scenario));
		Assert.False(this.scenario[this.nodes.Length - 1]);
	}

	[Fact]
	public void ResolveExactTest()
	{
		// If exactly one node should be selected,
		// once that node is selected the rest should be unselected.
		var target = SelectionCountConstraint.ExactSelected(this.nodes, 1);
		Assert.False(target.GetState(this.scenario).HasFlag(ConstraintStates.Resolvable));
		Assert.False(target.Resolve(this.scenario));
		this.scenario[0] = false;
		Assert.False(target.GetState(this.scenario).HasFlag(ConstraintStates.Resolvable));
		Assert.False(target.Resolve(this.scenario));
		this.scenario[1] = true;
		Assert.True(target.GetState(this.scenario).HasFlag(ConstraintStates.Resolvable));
		Assert.True(target.Resolve(this.scenario));
		Assert.False(this.scenario[2]);
		Assert.False(this.scenario[3]);
	}

	[Fact]
	public void ResolveExactComplementTest()
	{
		// If exactly one node should be selected,
		// once all other nodes are unselected the one should be selected.
		var target = SelectionCountConstraint.ExactSelected(this.nodes, 1);
		Assert.False(target.GetState(this.scenario).HasFlag(ConstraintStates.Resolvable));
		Assert.False(target.Resolve(this.scenario));
		this.scenario[0] = false;
		Assert.False(target.GetState(this.scenario).HasFlag(ConstraintStates.Resolvable));
		Assert.False(target.Resolve(this.scenario));
		this.scenario[1] = false;
		Assert.False(target.GetState(this.scenario).HasFlag(ConstraintStates.Resolvable));
		Assert.False(target.Resolve(this.scenario));
		this.scenario[2] = false;
		Assert.True(target.GetState(this.scenario).HasFlag(ConstraintStates.Resolvable));
		Assert.True(target.Resolve(this.scenario));
		Assert.True(this.scenario[3]);
	}

	[Fact]
	public void ResolveExactlyOneTest()
	{
		// If exactly one node should be selected and one node is in the list,
		// then it should be selected by resolving.
		DummyNode[] shortList = this.nodes.Take(1).ToArray();
		var target = SelectionCountConstraint.ExactSelected(shortList, 1);
		Assert.True(target.GetState(this.scenario).HasFlag(ConstraintStates.Resolvable));
		Assert.True(target.Resolve(this.scenario));
		Assert.True(this.scenario[0]);
		Assert.False(target.Resolve(this.scenario));
	}

	[Fact]
	public void IsEmpty()
	{
		var target = SelectionCountConstraint.RangeSelected(this.nodes, 0, int.MaxValue);
		Assert.True(target.IsEmpty);

		target = SelectionCountConstraint.RangeSelected(this.nodes, 0, 1);
		Assert.False(target.IsEmpty);
	}
}
