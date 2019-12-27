// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NerdBank.Algorithms.NodeConstraintSelection
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Describes the set of viable solutions available.
	/// </summary>
	public class SolutionsAnalysis
	{
		private readonly SolutionBuilder owner;

		private readonly long[]? nodeSelectedCount;

		/// <summary>
		/// Initializes a new instance of the <see cref="SolutionsAnalysis"/> class.
		/// </summary>
		/// <param name="owner">The <see cref="SolutionBuilder"/> that created this instance.</param>
		/// <param name="viableSolutionsFound">The number of viable solutions that exist.</param>
		/// <param name="nodeSelectedCount">The number of times each node was selected in a viable solution.</param>
		/// <param name="conflicts">Information about the conflicting constraints that prevent any viable solution from being found.</param>
		internal SolutionsAnalysis(SolutionBuilder owner, long viableSolutionsFound, long[]? nodeSelectedCount, SolutionBuilder.ConflictedConstraints? conflicts)
		{
			this.owner = owner;
			this.ViableSolutionsFound = viableSolutionsFound;
			this.nodeSelectedCount = nodeSelectedCount;
			this.Conflicts = conflicts;
		}

		/// <summary>
		/// Gets the number of viable solutions that exist.
		/// </summary>
		public long ViableSolutionsFound { get; }

		/// <summary>
		/// Gets information about the conflicting constraints that prevent any viable solution from being found.
		/// </summary>
		/// <value>Null if there were no conflicting constraints.</value>
		public SolutionBuilder.ConflictedConstraints? Conflicts { get; }

		/// <summary>
		/// Gets the number of solutions in which a given node was selected.
		/// </summary>
		/// <param name="nodeIndex">The index of the node of interest.</param>
		/// <returns>
		/// The number of viable solutions where the node at <paramref name="nodeIndex"/> was selected.
		/// May be -1 if the node at <paramref name="nodeIndex"/> is not constrained by anything and therefore can be anything in any solution.
		/// </returns>
		/// <exception cref="IndexOutOfRangeException">Thrown if <paramref name="nodeIndex"/> is negative or exceeds the number of nodes in the solution.</exception>
		public long GetNodeSelectedCount(int nodeIndex) => this.nodeSelectedCount?[nodeIndex] ?? throw new InvalidOperationException(Strings.ViableSolutionStatsNotAvailable);

		/// <summary>
		/// Select or unselect any indeterminate nodes which are unconditionally in just one state in all viable solutions.
		/// </summary>
		/// <remarks>
		/// Constraints can interact as they narrow the field of viable solutions such that
		/// some nodes may be effectively constrained to a certain value even though that value
		/// isn't explicitly required by any individual constraint.
		/// When these interactions are understood, applying them back to the <see cref="SolutionBuilder"/>
		/// can speed up future analyses and lead each node's value to reflect the remaining possibilities.
		/// </remarks>
		public void ApplyAnalysisBackToBuilder()
		{
			if (this.ViableSolutionsFound == 0 || this.nodeSelectedCount is null)
			{
				throw new InvalidOperationException(Strings.ViableSolutionStatsNotAvailable);
			}

			if (this.ViableSolutionsFound > 0)
			{
				for (int i = 0; i < this.nodeSelectedCount.Length; i++)
				{
					if (this.owner.CurrentScenario[i] is null)
					{
						if (this.nodeSelectedCount[i] == this.ViableSolutionsFound)
						{
							this.owner.CurrentScenario[i] = true;
						}
						else if (this.nodeSelectedCount[i] == 0)
						{
							this.owner.CurrentScenario[i] = false;
						}
					}
				}
			}
		}
	}
}
