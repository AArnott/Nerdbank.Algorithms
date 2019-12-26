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
		/// Gets a value indicating whether the group of remaining indeterminate nodes (if any) has exactly one
		/// determinate state left that would satisfy this constraint.
		/// </summary>
		/// <param name="scenario">The scenario to consider.</param>
		/// <returns>A boolean value.</returns>
		bool CanResolve(Scenario scenario);

		/// <summary>
		/// Forces all related Nodes into a determinate state consistent with
		/// this constraint, if there is only one state left that the indeterminate
		/// ones can be in while keeping this constraint satisfied.
		/// </summary>
		/// <param name="scenario">The scenario to modify.</param>
		/// <returns>
		/// A value indicating whether the operation was successful, and all related nodes are now resolved.
		/// </returns>
		bool Resolve(Scenario scenario);

		/// <summary>
		/// Gets a value indicating whether a given scenario already fully satisifies this constraint.
		/// </summary>
		/// <param name="scenario">The scenario to consider.</param>
		/// <returns>A boolean value.</returns>
		bool IsSatisfied(Scenario scenario);

		/// <summary>
		/// Gets a value indicating whether this constraint may still be satisfied in the future.
		/// </summary>
		/// <param name="scenario">The scenario to consider.</param>
		/// <returns>A boolean value.</returns>
		bool IsSatisfiable(Scenario scenario);

		/// <summary>
		/// Gets a value indicating whether this constraint may still be broken in the future.
		/// </summary>
		/// <param name="scenario">The scenario to consider.</param>
		/// <returns>A boolean value.</returns>
		bool IsBreakable(Scenario scenario);
	}
}
