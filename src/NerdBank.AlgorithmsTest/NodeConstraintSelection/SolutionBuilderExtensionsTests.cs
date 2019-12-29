// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using NerdBank.Algorithms.NodeConstraintSelection;
using Xunit;
using Xunit.Abstractions;

public class SolutionBuilderExtensionsTests : TestBase
{
	private static readonly ImmutableArray<object> Nodes = ImmutableArray.Create<object>("only node");
	private readonly SolutionBuilder<bool> builder = new SolutionBuilder<bool>(Nodes, ImmutableArray.Create(true, false));

	public SolutionBuilderExtensionsTests(ITestOutputHelper logger)
		: base(logger)
	{
	}

	[Fact]
	public void SetNodeState_NullArgs()
	{
		Assert.Throws<ArgumentNullException>("node", () => this.builder.SetNodeState(null!, true));
		Assert.Throws<ArgumentNullException>("builder", () => SolutionBuilderExtensions.SetNodeState(null!, Nodes[0], true));
	}

	[Fact]
	public void SetNodeState()
	{
		this.builder.SetNodeState(Nodes[0], true);
		Assert.Null(this.builder[0]);

		this.builder.ResolvePartially(this.TimeoutToken);
		Assert.True(this.builder[0]);
	}
}
