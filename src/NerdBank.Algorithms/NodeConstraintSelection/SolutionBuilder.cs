// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NerdBank.Algorithms.NodeConstraintSelection
{
	using System;
	using System.Collections.Generic;
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
		/// The constraints that describe the solution.
		/// </summary>
		private readonly List<IConstraint> constraints = new List<IConstraint>();

		/// <summary>
		/// All constraints, indexed by each node that impact them.
		/// </summary>
		private readonly List<IConstraint>[] constraintsPerNode;

		/// <summary>
		/// A map of nodes to their index as they appear in an ordered list.
		/// </summary>
		private readonly IReadOnlyDictionary<object, int> nodeIndex;

		/// <summary>
		/// The latest stable state of the (often partial) solution.
		/// </summary>
		private readonly Scenario currentScenario;

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
			this.currentScenario = this.scenarioPool.Take();

			this.constraintsPerNode = nodes.Select(n => new List<IConstraint>()).ToArray();
		}

		/// <summary>
		/// Occurs when one or more nodes' selection state has changed.
		/// </summary>
		public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

		/// <summary>
		/// Gets the current scenario.
		/// </summary>
		internal Scenario CurrentScenario => this.currentScenario;

		/// <summary>
		/// Gets the number of nodes in the problem/solution.
		/// </summary>
		private int NodeCount => this.currentScenario.NodeCount;

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

			if (constraint.Nodes.Count == 0)
			{
				throw new BadConstraintException(constraint, Strings.ConstraintForEmptySetOfNodes);
			}

			foreach (var node in constraint.Nodes)
			{
				List<IConstraint> applicableConstraints = this.constraintsPerNode[this.nodeIndex[node]];
				applicableConstraints.Add(constraint);
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

				this.AddConstraint(constraint);
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
				this.ResolvePartially(experiment.Candidate, cancellationToken);

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
			var stats = new SolutionStats { StopAfterFirstSolutionFound = true };
			this.EnumerateSolutions(this.currentScenario, 0, ref stats, cancellationToken);
			return CreateConflictedConstraints(stats);
		}

		/// <summary>
		/// Exhaustively scan the solution space and collect statistics on the aggregate set.
		/// </summary>
		/// <param name="cancellationToken">A cancellation token.</param>
		/// <returns>The results of the analysis.</returns>
		public SolutionsAnalysis AnalyzeSolutions(CancellationToken cancellationToken)
		{
			var stats = default(SolutionStats);
			this.EnumerateSolutions(this.currentScenario, 0, ref stats, cancellationToken);
			return new SolutionsAnalysis(this, stats.SolutionsFound, stats.NodesSelectedInSolutions, CreateConflictedConstraints(stats));
		}

		/// <summary>
		/// Raises the <see cref="SelectionChanged"/> event.
		/// </summary>
		/// <param name="args">The args to pass to the event handlers.</param>
		protected virtual void OnSelectionChanged(SelectionChangedEventArgs args) => this.SelectionChanged?.Invoke(this, args);

		private static ConflictedConstraints? CreateConflictedConstraints(SolutionStats stats) => stats.SolutionsFound == 0 ? new ConflictedConstraints() : null;

		private void ResolvePartially(Scenario scenario, CancellationToken cancellationToken)
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

		private void EnumerateSolutions(Scenario basis, int firstNode, ref SolutionStats stats, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			using (var experiment = new Experiment(this, basis))
			{
				this.ResolvePartially(experiment.Candidate, cancellationToken);

				bool canAnyConstraintsBeBroken = false;
				foreach (IConstraint constraint in this.constraints)
				{
					cancellationToken.ThrowIfCancellationRequested();
					ConstraintStates state = constraint.GetState(experiment.Candidate);
					if (!state.HasFlag(ConstraintStates.Satisfiable))
					{
						return;
					}

					canAnyConstraintsBeBroken |= state.HasFlag(ConstraintStates.Breakable);
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

					if (stats.StopAfterFirstSolutionFound)
					{
						// When we're only interested in whether there's a solution,
						// we don't need to enumerate possibilities for a node for which no constraints exist.
						List<IConstraint> applicableConstraints = this.constraintsPerNode[i];
						if (applicableConstraints.Count == 0)
						{
							// Skip any node that can be any value without impact to constraints.
							continue;
						}
					}

					experiment.Candidate[i] = true;
					this.EnumerateSolutions(experiment.Candidate, i + 1, ref stats, cancellationToken);

					experiment.Candidate.ResetNode(i, false);
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

			internal void RecordSolutionFound(Scenario scenario)
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
						if (scenario[i] is bool selected && selected)
						{
							this.NodesSelectedInSolutions[i]++;
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
			/// <param name="basis">The scenario to use as a template. If unspecified, the <see cref="SolutionBuilder.currentScenario"/> is used.</param>
			internal Experiment(SolutionBuilder builder, Scenario? basis = default)
			{
				this.builder = builder;
				this.Candidate = builder.scenarioPool.Take();
				this.Candidate.CopyFrom(basis ?? builder.currentScenario);
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
				this.builder.currentScenario.CopyFrom(this.Candidate);
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
