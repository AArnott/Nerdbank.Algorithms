// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.Algorithms.NodeConstraintSelection
{
	using System;
	using System.Collections.Generic;
	using System.Threading;

	/// <content>
	/// Contains the <see cref="ConflictedConstraints"/> nested type.
	/// </content>
	public partial class SolutionBuilder<TNodeState>
	{
		/// <summary>
		/// Describes a state where no solution exists.
		/// </summary>
#pragma warning disable CA1034 // Nested types should not be visible
		public class ConflictedConstraints
#pragma warning restore CA1034 // Nested types should not be visible
		{
			/// <summary>
			/// The solution builder that created this.
			/// </summary>
			private readonly SolutionBuilder<TNodeState> owner;

			/// <summary>
			/// The conflicted scenario.
			/// </summary>
			private readonly Scenario<TNodeState> conflictedScenario;

			/// <summary>
			/// Initializes a new instance of the <see cref="ConflictedConstraints"/> class.
			/// </summary>
			/// <param name="owner">The solution builder that created this.</param>
			/// <param name="conflictedScenario">The conflicted scenario.</param>
			internal ConflictedConstraints(SolutionBuilder<TNodeState> owner, Scenario<TNodeState> conflictedScenario)
			{
				this.owner = owner;
				this.conflictedScenario = conflictedScenario;
			}

			/// <summary>
			/// Gets a collection of constraints which individually conflict with some other set of constraints within the solution.
			/// If any one of these constraints are removed, the conflict will be resolved.
			/// </summary>
			/// <param name="cancellationToken">A cancellation token.</param>
			/// <returns>A set of constraints.</returns>
			/// <exception cref="ComplexConflictException">Thrown when there is no single constraint whose removal would remove the conflict.</exception>
			public IReadOnlyCollection<IConstraint<TNodeState>> GetConflictingConstraints(CancellationToken cancellationToken)
			{
				var conflictingConstraints = new List<IConstraint<TNodeState>>();
				foreach (IConstraint<TNodeState> constraint in this.conflictedScenario.Constraints)
				{
					cancellationToken.ThrowIfCancellationRequested();

					using (var experiment = new Experiment(this.owner, this.conflictedScenario))
					{
						experiment.Candidate.RemoveConstraint(constraint);
						if (this.owner.CheckForConflictingConstraints(experiment.Candidate, cancellationToken) is null)
						{
							// Removing this constraint removed the conflict. So add it to the list to return.
							conflictingConstraints.Add(constraint);
						}
					}
				}

				if (conflictingConstraints.Count == 0)
				{
					throw new ComplexConflictException();
				}

				return conflictingConstraints;
			}
		}
	}
}
