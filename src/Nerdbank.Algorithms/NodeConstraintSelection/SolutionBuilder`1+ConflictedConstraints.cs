// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Nerdbank.Algorithms.NodeConstraintSelection;

/// <content>
/// Contains the <see cref="ConflictedConstraints"/> nested type.
/// </content>
public partial class SolutionBuilder<TNodeState>
	where TNodeState : unmanaged
{
	/// <summary>
	/// Describes a state where no solution exists.
	/// </summary>
	public class ConflictedConstraints
	{
		private readonly Configuration<TNodeState> configuration;

		/// <summary>
		/// The conflicted scenario.
		/// </summary>
		private readonly Scenario<TNodeState> conflictedScenario;

		/// <summary>
		/// Initializes a new instance of the <see cref="ConflictedConstraints"/> class.
		/// </summary>
		/// <param name="configuration">The problem space configuration.</param>
		/// <param name="conflictedScenario">The conflicted scenario.</param>
		internal ConflictedConstraints(Configuration<TNodeState> configuration, Scenario<TNodeState> conflictedScenario)
		{
			this.configuration = configuration;
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
			List<IConstraint<TNodeState>> conflictingConstraints = new();
			foreach (IConstraint<TNodeState> constraint in this.conflictedScenario.Constraints)
			{
				cancellationToken.ThrowIfCancellationRequested();

				using Experiment experiment = new(this.conflictedScenario);
				experiment.Candidate.RemoveConstraint(constraint);
				if (SolutionBuilder<TNodeState>.CheckForConflictingConstraints(this.configuration, experiment.Candidate, cancellationToken) is null)
				{
					// Removing this constraint removed the conflict. So add it to the list to return.
					conflictingConstraints.Add(constraint);
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
