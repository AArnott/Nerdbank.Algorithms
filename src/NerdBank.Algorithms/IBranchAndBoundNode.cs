// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NerdBank.Algorithms
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents one solution or partial solution to the problem.
	/// </summary>
	/// <remarks>
	/// Each node must be able to enumerate any child nodes.
	/// Each node must be able to compare itself with other nodes
	/// so that the priority queue may do its job.
	/// The <see cref="IComparable{T}.CompareTo(T)"/> method must consider cost/value to be the
	/// first criteria, and <see cref="IsSolution"/> to be the second,
	/// giving preference among equal cost nodes to those nodes that
	/// are solutions.
	/// </remarks>
	public interface IBranchAndBoundNode : IComparable<IBranchAndBoundNode>
	{
		/// <summary>
		/// Gets the child nodes of this node.
		/// </summary>
		IEnumerable<IBranchAndBoundNode> ChildNodes { get; }

		/// <summary>
		/// Gets a value indicating whether this node represents a solution.
		/// </summary>
		bool IsSolution { get; }
	}
}
