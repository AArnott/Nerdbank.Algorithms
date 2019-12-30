// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

internal class DummyNode
{
	private readonly object? designation;

	/// <summary>
	/// Initializes a new instance of the <see cref="DummyNode"/> class.
	/// </summary>
	/// <param name="designation">The value to return from <see cref="ToString"/>.</param>
	public DummyNode(object? designation)
	{
		this.designation = designation;
	}

	/// <inheritdoc/>
	public override string? ToString() => this.designation is object ? this.designation.ToString() : base.ToString();
}
