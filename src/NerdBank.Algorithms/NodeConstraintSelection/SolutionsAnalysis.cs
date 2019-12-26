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
		/// <summary>
		/// Initializes a new instance of the <see cref="SolutionsAnalysis"/> class.
		/// </summary>
		/// <param name="viableSolutionsFound">The number of viable solutions that exist.</param>
		/// <param name="conflicts">Information about the conflicting constraints that prevent any viable solution from being found.</param>
		internal SolutionsAnalysis(long viableSolutionsFound, ConflictedConstraints? conflicts)
		{
			this.ViableSolutionsFound = viableSolutionsFound;
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
	}
}
