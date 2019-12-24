// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NerdBank.Algorithms.NodeConstraintSelection;

/// <summary>
/// A minimal Node class for us with testing constraints.
/// </summary>
internal class DummyNode : NodeBase
{
	/// <summary>
	/// Initializes a new instance of the <see cref="DummyNode"/> class.
	/// </summary>
	public DummyNode()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DummyNode"/> class.
	/// </summary>
	/// <param name="designation"></param>
	public DummyNode(object? designation)
	{
		this.designation = designation;
	}

	private object? designation;

	public override string? ToString()
	{
		if (this.designation is object)
		{
			return this.designation.ToString();
		}
		else
		{
			return base.ToString();
		}
	}
}
