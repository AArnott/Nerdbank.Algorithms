// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NerdBank.Algorithms.NodeConstraintSelection
{
	/// <summary>
	/// Extension methods for the <see cref="IConstraint"/> interface.
	/// </summary>
	internal static class ConstraintExtensions
	{
		/// <summary>
		/// Gets a value indicating whether every node in this constraint has a determined selection state.
		/// </summary>
		/// <param name="constraint">The constraint.</param>
		/// <param name="scenario">The scenario to consider.</param>
		/// <returns>A boolean value.</returns>
		/// <remarks>
		/// This check does not test whether the constraint it actually satisfied.
		/// </remarks>
		internal static bool IsResolved(this IConstraint constraint, Scenario scenario)
		{
			foreach (object nodeIndex in constraint.Nodes)
			{
				if (!scenario[nodeIndex].HasValue)
				{
					return false;
				}
			}

			return true;
		}
	}
}
