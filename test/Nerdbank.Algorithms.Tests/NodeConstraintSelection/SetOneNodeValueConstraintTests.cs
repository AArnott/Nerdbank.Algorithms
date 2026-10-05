// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Nerdbank.Algorithms.NodeConstraintSelection;
using TUnit;
using Xunit;

public class SetOneNodeValueConstraintTests : TestBase
{
	public SetOneNodeValueConstraintTests()
    {
    }

	[Test]
	public void Ctor_ThrowsOnNull()
	{
		Assert.Throws<ArgumentNullException>("node", () => new SetOneNodeValueConstraint<bool>(null!, true));
	}

	[Test]
	public void Resolve_PreviouslyUnset()
	{
		var nodes = ImmutableArray.Create(new object());
		var scenario = new Scenario<bool>(new Configuration<bool>(nodes, ImmutableArray.Create(true, false)));
		Assert.True(new SetOneNodeValueConstraint<bool>(nodes[0], true).Resolve(scenario));
		Assert.True(scenario[0]);
	}

	[Test]
	[Arguments(true)]
	[Arguments(false)]
	public void Resolve_AlreadySet(bool matchingValue)
	{
		var nodes = ImmutableArray.Create(new object());
		var scenario = new Scenario<bool>(new Configuration<bool>(nodes, ImmutableArray.Create(true, false)));
		scenario[0] = matchingValue ? true : false;
		Assert.False(new SetOneNodeValueConstraint<bool>(nodes[0], true).Resolve(scenario));
	}

	[Test]
	public void Resolve_NullScenario()
	{
		Assert.Throws<ArgumentNullException>("scenario", () => new SetOneNodeValueConstraint<bool>(new object(), true).Resolve(null!));
	}

	[Test]
	public void GetState_NullScenario()
	{
		Assert.Throws<ArgumentNullException>("scenario", () => new SetOneNodeValueConstraint<bool>(new object(), true).GetState(null!));
	}

	[Test]
	public void GetState_Resolvable()
	{
		var nodes = ImmutableArray.Create("my node");
		var scenario = new Scenario<bool>(new Configuration<bool>(nodes.As<object>(), ImmutableArray.Create(true, false)));
		var constraint = new SetOneNodeValueConstraint<bool>(nodes[0], true);
		Assert.Equal(ConstraintStates.Resolvable | ConstraintStates.Satisfiable | ConstraintStates.Breakable, constraint.GetState(scenario));
	}

	[Test]
	public void GetState_Resolved()
	{
		var nodes = ImmutableArray.Create("my node");
		var scenario = new Scenario<bool>(new Configuration<bool>(nodes.As<object>(), ImmutableArray.Create(true, false)));
		var constraint = new SetOneNodeValueConstraint<bool>(nodes[0], true);
		scenario[0] = true;
		Assert.Equal(ConstraintStates.Resolved | ConstraintStates.Satisfied, constraint.GetState(scenario));
	}

	[Test]
	public void GetState_Broken()
	{
		var nodes = ImmutableArray.Create("my node");
		var scenario = new Scenario<bool>(new Configuration<bool>(nodes.As<object>(), ImmutableArray.Create(true, false)));
		var constraint = new SetOneNodeValueConstraint<bool>(nodes[0], true);
		scenario[0] = false;
		Assert.Equal(ConstraintStates.Resolved, constraint.GetState(scenario));
	}

	[Test]
	public void ToString_LogOutput()
	{
		var constraint = new SetOneNodeValueConstraint<bool>("My node", true);
		this.Logger.LogInformation(constraint.ToString());
	}
}
