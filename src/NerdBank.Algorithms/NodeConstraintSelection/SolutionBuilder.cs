// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NerdBank.Algorithms.NodeConstraintSelection
{
	using System;
	using System.Collections.Generic;
	using System.Threading;

	/// <summary>
	/// Tracks the state of a problem and its solution as constraints are added.
	/// </summary>
	public partial class SolutionBuilder
	{
		/// <summary>
		/// The pool that recycles scenarios for simulations.
		/// </summary>
		private readonly ScenarioPool scenarioPool;

		/// <summary>
		/// The constraints that describe the solution.
		/// </summary>
		private readonly List<IConstraint> constraints = new List<IConstraint>();

		/// <summary>
		/// The latest stable state of the (partial) solution.
		/// </summary>
		private Scenario currentScenario;

		/// <summary>
		/// Initializes a new instance of the <see cref="SolutionBuilder"/> class.
		/// </summary>
		/// <param name="nodes">The nodes in the problem/solution.</param>
		public SolutionBuilder(IReadOnlyList<object> nodes)
		{
			if (nodes is null)
			{
				throw new ArgumentNullException(nameof(nodes));
			}

			if (nodes.Count == 0)
			{
				throw new ArgumentException(Strings.NonEmptyArrayRequired, nameof(nodes));
			}

			// Build up an index for fast node index lookups.
			this.scenarioPool = new ScenarioPool(nodes);
			this.currentScenario = this.scenarioPool.Take();
		}

		/// <summary>
		/// Occurs when one or more nodes' selection state has changed.
		/// </summary>
		public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

		/// <summary>
		/// Gets the selection state for a node with a given index.
		/// </summary>
		/// <param name="index">The index of the node.</param>
		/// <returns>The selection state of the node. Null if the selection state isn't yet determined.</returns>
		/// <exception cref="IndexOutOfRangeException">Thrown if the <paramref name="index"/> is negative or exceeds the number of nodes in the solution.</exception>
		public bool? this[int index] => this.currentScenario[index];

		/// <summary>
		/// Gets the selection state for the given node.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <returns>The selection state of the node. Null if the selection state isn't yet determined.</returns>
		/// <exception cref="IndexOutOfRangeException">Thrown if the <paramref name="node"/> is not among the nodes in the solution.</exception>
		public bool? this[object node] => this.currentScenario[node];

		/// <summary>
		/// Adds a constraint that describes the solution.
		/// </summary>
		/// <param name="constraint">The constraint to be added.</param>
		public void AddConstraint(IConstraint constraint)
		{
			if (constraint is null)
			{
				throw new ArgumentNullException(nameof(constraint));
			}

			this.constraints.Add(constraint);
		}

		/// <summary>
		/// Adds constraints that describes the solution.
		/// </summary>
		/// <param name="constraints">The constraints to be added.</param>
		public void AddConstraints(IEnumerable<IConstraint> constraints)
		{
			if (constraints is null)
			{
				throw new ArgumentNullException(nameof(constraints));
			}

			foreach (IConstraint constraint in constraints)
			{
				if (constraint is null)
				{
					throw new ArgumentException(Strings.NullMemberOfCollection, nameof(constraints));
				}

				this.constraints.Add(constraint);
			}
		}

		/// <summary>
		/// Applies immediately constraint resolutions to the solution where possible.
		/// </summary>
		/// <param name="cancellationToken">A cancellation token.</param>
		public void ResolvePartially(CancellationToken cancellationToken = default)
		{
			using (var experiment = new Experiment(this))
			{
				// Keep looping through constraints asking each one to resolve nodes until no changes are applied.
				bool anyResolved;
				do
				{
					anyResolved = false;
					foreach (IConstraint constraint in this.constraints)
					{
						cancellationToken.ThrowIfCancellationRequested();
						bool resolved;
						int scenarioVersion = experiment.Candidate.Version;
						try
						{
							resolved = constraint.Resolve(experiment.Candidate);
						}
						catch (Exception ex)
						{
							throw new BadConstraintException(constraint, Strings.ConstraintThrewUnexpectedException, ex);
						}

						if (resolved && experiment.Candidate.Version == scenarioVersion)
						{
							throw new BadConstraintException(constraint, Strings.ConstraintResolveReturnedTrueWithNoChanges);
						}

						anyResolved |= resolved;
					}
				}
				while (anyResolved);

				experiment.Commit();
			}
		}

		/// <summary>
		/// Checks whether at least one solution exists that can satisfy all existing constraints
		/// and returns diagnostic data about the conflicting constraints if no solution exists.
		/// </summary>
		/// <param name="timeoutToken">A cancellation token.</param>
		/// <returns><c>null</c> if a solution can be found; or diagnostic data about which sets of constraints conflict.</returns>
		public ConflictedConstraints? CheckForConflictingConstraints(CancellationToken timeoutToken)
		{
			return null;
		}

		/// <summary>
		/// Raises the <see cref="SelectionChanged"/> event.
		/// </summary>
		/// <param name="args">The args to pass to the event handlers.</param>
		protected virtual void OnSelectionChanged(SelectionChangedEventArgs args) => this.SelectionChanged?.Invoke(this, args);

		/// <summary>
		/// Manages the lifetime of a trial scenario.
		/// </summary>
		private struct Experiment : IDisposable
		{
			/// <summary>
			/// The owner.
			/// </summary>
			private readonly SolutionBuilder builder;

			/// <summary>
			/// A value indicating whether <see cref="Commit"/> has been called.
			/// </summary>
			private bool committed;

			/// <summary>
			/// Initializes a new instance of the <see cref="Experiment"/> struct.
			/// </summary>
			/// <param name="builder">The owner of this instance.</param>
			/// <param name="basis">The scenario to use as a template. If unspecified, the <see cref="SolutionBuilder.currentScenario"/> is used.</param>
			internal Experiment(SolutionBuilder builder, Scenario? basis = default)
			{
				this.builder = builder;
				this.Candidate = builder.scenarioPool.Take();
				this.Candidate.CopyFrom(basis ?? builder.currentScenario);
				this.committed = false;
			}

			/// <summary>
			/// Gets the experimental scenario.
			/// </summary>
			public Scenario Candidate { get; }

			/// <summary>
			/// Commits the <see cref="Candidate"/> as the new current scenario in the <see cref="SolutionBuilder"/>
			/// and concludes the experiment.
			/// </summary>
			public void Commit()
			{
				Scenario oldScenario = this.builder.currentScenario;
				this.builder.currentScenario = this.Candidate;
				this.builder.scenarioPool.Return(oldScenario);
				this.committed = true;
			}

			/// <summary>
			/// If <see cref="Commit"/> has not already been called, recycles the <see cref="Candidate"/>
			/// and concludes the experiment.
			/// </summary>
			public void Dispose()
			{
				if (!this.committed)
				{
					this.builder.scenarioPool.Return(this.Candidate);
				}
			}
		}
	}
}
