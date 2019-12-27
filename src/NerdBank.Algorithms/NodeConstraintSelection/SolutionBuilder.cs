// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NerdBank.Algorithms.NodeConstraintSelection
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Diagnostics;
	using System.Linq;
	using System.Threading;

	/// <summary>
	/// Tracks the state of a problem and its solution as constraints are added.
	/// </summary>
	/// <remarks>
	/// Thread safety: Instance members on this class are not thread safe.
	/// </remarks>
	public partial class SolutionBuilder
	{
		/// <summary>
		/// The pool that recycles scenarios for simulations.
		/// </summary>
		private readonly ScenarioPool scenarioPool;

		/// <summary>
		/// A map of nodes to their index as they appear in an ordered list.
		/// </summary>
		private readonly IReadOnlyDictionary<object, int> nodeIndex;

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

			this.nodeIndex = Scenario.CreateNodeIndex(nodes);
			this.scenarioPool = new ScenarioPool(nodes, this.nodeIndex);
			this.CurrentScenario = this.scenarioPool.Take();
		}

		/// <summary>
		/// Occurs when one or more nodes' selection state has changed.
		/// </summary>
		public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

		/// <summary>
		/// Gets the current scenario.
		/// </summary>
		internal Scenario CurrentScenario { get; }

		/// <summary>
		/// Gets the number of nodes in the problem/solution.
		/// </summary>
		private int NodeCount => this.CurrentScenario.NodeCount;

		/// <summary>
		/// Gets the selection state for a node with a given index.
		/// </summary>
		/// <param name="index">The index of the node.</param>
		/// <returns>The selection state of the node. Null if the selection state isn't yet determined.</returns>
		/// <exception cref="IndexOutOfRangeException">Thrown if the <paramref name="index"/> is negative or exceeds the number of nodes in the solution.</exception>
		public bool? this[int index] => this.CurrentScenario[index];

		/// <summary>
		/// Gets the selection state for the given node.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <returns>The selection state of the node. Null if the selection state isn't yet determined.</returns>
		/// <exception cref="IndexOutOfRangeException">Thrown if the <paramref name="node"/> is not among the nodes in the solution.</exception>
		public bool? this[object node] => this.CurrentScenario[node];

		/// <summary>
		/// Adds a constraint that describes the solution.
		/// </summary>
		/// <param name="constraint">The constraint to be added.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="constraint"/> is <c>null</c>.</exception>
		/// <exception cref="BadConstraintException">Thrown when the <paramref name="constraint"/> has an empty set of <see cref="IConstraint.Nodes"/>.</exception>
		/// <exception cref="KeyNotFoundException">Thrown when the <paramref name="constraint"/> refers to nodes that do not belong to this problem/solution.</exception>
		public void AddConstraint(IConstraint constraint)
		{
			if (constraint is null)
			{
				throw new ArgumentNullException(nameof(constraint));
			}

			this.CurrentScenario.AddConstraint(constraint);
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

				this.AddConstraint(constraint);
			}
		}

		/// <summary>
		/// Removes a constraint from the solution.
		/// </summary>
		/// <param name="constraint">The constraint to remove.</param>
		/// <remarks>
		/// This is typically used to remove a constraint previously identified as the cause for a conflict
		/// by <see cref="ConflictedConstraints.GetConflictingConstraints(CancellationToken)"/>.
		/// </remarks>
		public void RemoveConstraint(IConstraint constraint)
		{
			if (constraint is null)
			{
				throw new ArgumentNullException(nameof(constraint));
			}

			this.CurrentScenario.RemoveConstraint(constraint);
		}

		/// <summary>
		/// Applies immediately constraint resolutions to the solution where possible.
		/// </summary>
		/// <param name="cancellationToken">A cancellation token.</param>
		public void ResolvePartially(CancellationToken cancellationToken = default)
		{
			using (var experiment = new Experiment(this))
			{
				ResolvePartially(experiment.Candidate, cancellationToken);

				experiment.Commit();
			}
		}

		/// <summary>
		/// Checks whether at least one solution exists that can satisfy all existing constraints
		/// and returns diagnostic data about the conflicting constraints if no solution exists.
		/// </summary>
		/// <param name="cancellationToken">A cancellation token.</param>
		/// <returns><c>null</c> if a solution can be found; or diagnostic data about which sets of constraints conflict.</returns>
		public ConflictedConstraints? CheckForConflictingConstraints(CancellationToken cancellationToken)
		{
			using (var experiment = new Experiment(this))
			{
				return this.CheckForConflictingConstraints(experiment.Candidate, cancellationToken);
			}
		}

		/// <summary>
		/// Exhaustively scan the solution space and collect statistics on the aggregate set.
		/// </summary>
		/// <param name="cancellationToken">A cancellation token.</param>
		/// <returns>The results of the analysis.</returns>
		public SolutionsAnalysis AnalyzeSolutions(CancellationToken cancellationToken)
		{
			using (var experiment = new Experiment(this))
			{
				ResolvePartially(experiment.Candidate, cancellationToken);
				var stats = default(SolutionStats);
				try
				{
					this.EnumerateSolutions(experiment.Candidate, 0, ref stats, cancellationToken);
					return new SolutionsAnalysis(this, stats.SolutionsFound, stats.NodesSelectedInSolutions, this.CreateConflictedConstraints(stats));
				}
				catch (OperationCanceledException ex)
				{
					throw new OperationCanceledException("Canceled after considering " + stats.ConsideredScenarios + " scenarios.", ex);
				}
			}
		}

		/// <summary>
		/// Raises the <see cref="SelectionChanged"/> event.
		/// </summary>
		/// <param name="args">The args to pass to the event handlers.</param>
		protected virtual void OnSelectionChanged(SelectionChangedEventArgs args) => this.SelectionChanged?.Invoke(this, args);

		private static void ResolvePartially(Scenario scenario, CancellationToken cancellationToken)
		{
			// Keep looping through constraints asking each one to resolve nodes until no changes are applied.
			bool anyResolved;
			do
			{
				anyResolved = false;
				for (int i = 0; i < scenario.Constraints.Length; i++)
				{
					IConstraint constraint = scenario.Constraints[i];
					cancellationToken.ThrowIfCancellationRequested();
					bool resolved;
					int scenarioVersion = scenario.Version;
					try
					{
						resolved = constraint.Resolve(scenario);
					}
					catch (Exception ex)
					{
						throw new BadConstraintException(constraint, Strings.ConstraintThrewUnexpectedException, ex);
					}

					if (resolved && scenario.Version == scenarioVersion)
					{
						throw new BadConstraintException(constraint, Strings.ConstraintResolveReturnedTrueWithNoChanges);
					}

					anyResolved |= resolved;
				}
			}
			while (anyResolved);
		}

		/// <summary>
		/// Resolves a scenario using only a limited set of constraints,
		/// and engage the rest of them only if changes are made.
		/// </summary>
		/// <param name="scenario">The scenario to resolve.</param>
		/// <param name="applicableConstraints">The constraints to use to resolve.</param>
		/// <param name="cancellationToken">A cancellation token.</param>
		private static void ResolveByCascadingConstraints(Scenario scenario, ImmutableArray<IConstraint> applicableConstraints, CancellationToken cancellationToken)
		{
			bool anyResolved = false;
			for (int i = 0; i < applicableConstraints.Length; i++)
			{
				anyResolved |= applicableConstraints[i].Resolve(scenario);
			}

			// If any nodes changed, engage a regular resolve operation
			if (anyResolved)
			{
				ResolvePartially(scenario, cancellationToken);
			}
		}

		private ConflictedConstraints? CheckForConflictingConstraints(Scenario scenario, CancellationToken cancellationToken)
		{
			ResolvePartially(scenario, cancellationToken);
			var stats = new SolutionStats { StopAfterFirstSolutionFound = true };
			this.EnumerateSolutions(scenario, 0, ref stats, cancellationToken);
			return this.CreateConflictedConstraints(stats);
		}

		private ConflictedConstraints? CreateConflictedConstraints(SolutionStats stats) => stats.SolutionsFound == 0 ? new ConflictedConstraints(this, this.CurrentScenario) : null;

		private void EnumerateSolutions(Scenario basis, int firstNode, ref SolutionStats stats, CancellationToken cancellationToken)
		{
			stats.ConsideredScenarios++;
			cancellationToken.ThrowIfCancellationRequested();
			using (var experiment = new Experiment(this, basis))
			{
				bool canAnyConstraintsBeBroken = false;
				for (int j = 0; j < experiment.Candidate.Constraints.Length; j++)
				{
					IConstraint constraint = experiment.Candidate.Constraints[j];
					cancellationToken.ThrowIfCancellationRequested();
					ConstraintStates state = constraint.GetState(experiment.Candidate);
					if ((state & ConstraintStates.Satisfiable) != ConstraintStates.Satisfiable)
					{
						return;
					}

					canAnyConstraintsBeBroken |= (state & ConstraintStates.Breakable) == ConstraintStates.Breakable;
				}

				if (stats.StopAfterFirstSolutionFound && !canAnyConstraintsBeBroken)
				{
					// There's nothing we can simulate that would break constraints, so everything we might try constitutes a valid solution.
					// Don't waste time enumerating them.
					stats.RecordSolutionFound(experiment.Candidate);
					return;
				}

				int i;
				for (i = firstNode; i < this.NodeCount; i++)
				{
					cancellationToken.ThrowIfCancellationRequested();

					if (experiment.Candidate[i].HasValue)
					{
						// Skip any node that already has a set value.
						continue;
					}

					// When we're only interested in whether there's a solution,
					// we don't need to enumerate possibilities for a node for which no constraints exist.
					ImmutableArray<IConstraint> applicableConstraints = experiment.Candidate.GetConstraintsThatApplyTo(i);
					if (applicableConstraints.IsEmpty)
					{
						// Skip any node that can be any value without impact to constraints.
						continue;
					}

					// Try selecting the node. In doing so, resolve whatever nodes we can immediately.
					experiment.Candidate[i] = true;
					int version1 = experiment.Candidate.Version;
					ResolveByCascadingConstraints(experiment.Candidate, applicableConstraints, cancellationToken);
					this.EnumerateSolutions(experiment.Candidate, i + 1, ref stats, cancellationToken);

					// If our resolving actually changed arbitrary nodes, we need to rollback
					// before we can try our second test of UNselecting the node.
					if (experiment.Candidate.Version != version1)
					{
						experiment.Candidate.CopyFrom(basis);
					}

					experiment.Candidate.ResetNode(i, false);
					ResolveByCascadingConstraints(experiment.Candidate, applicableConstraints, cancellationToken);
					this.EnumerateSolutions(experiment.Candidate, i + 1, ref stats, cancellationToken);

					// Once we drill into one node, we don't want to drill into any more nodes since
					// we did that via our recursive call.
					break;
				}

				if (i >= this.NodeCount)
				{
					stats.RecordSolutionFound(experiment.Candidate);
				}
			}
		}

		private ref struct SolutionStats
		{
			internal bool StopAfterFirstSolutionFound { get; set; }

			internal long SolutionsFound { get; private set; }

			internal long[]? NodesSelectedInSolutions { get; private set; }

			internal long ConsideredScenarios { get; set; }

			internal void RecordSolutionFound(Scenario scenario)
			{
				checked
				{
					this.SolutionsFound++;

					if (!this.StopAfterFirstSolutionFound)
					{
						if (this.NodesSelectedInSolutions is null)
						{
							this.NodesSelectedInSolutions = new long[scenario.NodeCount];
						}

						for (int i = 0; i < scenario.NodeCount; i++)
						{
							if (scenario[i] is bool selected)
							{
								if (selected)
								{
									this.NodesSelectedInSolutions[i]++;
								}
							}
							else
							{
								// This node is not constrained by anything. So it is a free radical and shouldn't be counted as selected or unselected
								// since solutions are not enumerated based on flipping this.
								this.NodesSelectedInSolutions[i] = -1;
							}
						}
					}
				}
			}
		}

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
			/// Initializes a new instance of the <see cref="Experiment"/> struct.
			/// </summary>
			/// <param name="builder">The owner of this instance.</param>
			/// <param name="basis">The scenario to use as a template. If unspecified, the <see cref="SolutionBuilder.CurrentScenario"/> is used.</param>
			internal Experiment(SolutionBuilder builder, Scenario? basis = default)
			{
				this.builder = builder;
				this.Candidate = builder.scenarioPool.Take();
				this.Candidate.CopyFrom(basis ?? builder.CurrentScenario);
			}

			/// <summary>
			/// Gets the experimental scenario.
			/// </summary>
			public Scenario Candidate { get; }

			/// <summary>
			/// Commits the <see cref="Candidate"/> to the current scenario in the <see cref="SolutionBuilder"/>.
			/// </summary>
			public void Commit()
			{
				this.builder.CurrentScenario.CopyFrom(this.Candidate);
			}

			/// <summary>
			/// Recycles the <see cref="Candidate"/> and concludes the experiment.
			/// </summary>
			public void Dispose()
			{
				this.builder.scenarioPool.Return(this.Candidate);
			}
		}
	}
}
