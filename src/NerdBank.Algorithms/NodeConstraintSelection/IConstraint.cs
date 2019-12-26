// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NerdBank.Algorithms.NodeConstraintSelection
{
	using System.Collections.Generic;

	/// <summary>
	/// Describes some known constraint on the final solution
	/// and tests whether a partial solution satisfies the constraint.
	/// </summary>
	/// <remarks>
	/// Implementations should be immutable.
	/// </remarks>
	public interface IConstraint
	{
		/// <summary>
		/// Gets the set of indexes to nodes that are involved in the constraint.
		/// </summary>
		IReadOnlyCollection<object> Nodes { get; }

		/// <summary>
		/// Gets a value indicating whether this constraint can be discarded without any information loss.
		/// </summary>
		bool IsEmpty { get; }

		/// <summary>
		/// Gets the state of the constraint with respect to a given scenario.
		/// </summary>
		/// <param name="scenario">The scenario to consider.</param>
		/// <returns>A collection of flags that represent the state.</returns>
		ConstraintStates GetState(Scenario scenario);

		/// <summary>
		/// Sets any indeterminate nodes to selected or unselected based on this constraint, if possible.
		/// </summary>
		/// <param name="scenario">The scenario to modify.</param>
		/// <returns>
		/// A value indicating whether any indeterminate nodes were changed.
		/// </returns>
		bool Resolve(Scenario scenario);
	}
}
