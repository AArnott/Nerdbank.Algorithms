// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NerdBank.Algorithms.NodeConstraintSelection;
using Xunit;

public class ConstraintBaseTest
{
	[Fact]
	public void FactorialTest()
	{
		Assert.Equal(1, ConstraintBase.Factorial(1));
		Assert.Equal(2 * 1, ConstraintBase.Factorial(2));
		Assert.Equal(3 * 2 * 1, ConstraintBase.Factorial(3));
		Assert.Equal(4 * 3 * 2 * 1, ConstraintBase.Factorial(4));
	}

	[Fact]
	public void ChooseNegativeTest()
	{
		Assert.Equal(0, ConstraintBase.Choose(3, -1));
	}

	[Fact]
	public void ChooseTest()
	{
		Assert.Equal(3, ConstraintBase.Choose(3, 2));

		Assert.Equal(1, ConstraintBase.Choose(4, 0));
		Assert.Equal(4, ConstraintBase.Choose(4, 1));
		Assert.Equal(6, ConstraintBase.Choose(4, 2));
		Assert.Equal(4, ConstraintBase.Choose(4, 3));
		Assert.Equal(1, ConstraintBase.Choose(4, 4));
	}
}
