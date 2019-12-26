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
		private readonly long[]? nodeSelectedCount;

		/// <summary>
		/// Initializes a new instance of the <see cref="SolutionsAnalysis"/> class.
		/// </summary>
		/// <param name="viableSolutionsFound">The number of viable solutions that exist.</param>
		/// <param name="nodeSelectedCount">The number of times each node was selected in a viable solution.</param>
		/// <param name="conflicts">Information about the conflicting constraints that prevent any viable solution from being found.</param>
		internal SolutionsAnalysis(long viableSolutionsFound, long[]? nodeSelectedCount, ConflictedConstraints? conflicts)
		{
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
		public ConflictedConstraints? Conflicts { get; }

		/// <summary>
		/// Gets the number of solutions in which a given node was selected.
		/// </summary>
		/// <param name="nodeIndex">The index of the node of interest.</param>
		/// <returns>The number of viable solutions where the node at <paramref name="nodeIndex"/> was selected.</returns>
		/// <exception cref="IndexOutOfRangeException">Thrown if <paramref name="nodeIndex"/> is negative or exceeds the number of nodes in the solution.</exception>
		public long GetNodeSelectedCount(int nodeIndex) => this.nodeSelectedCount?[nodeIndex] ?? throw new InvalidOperationException(Strings.ViableSolutionStatsNotAvailable);
	}
}
