// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Nerdbank.Algorithms.NodeConstraintSelection;

/// <summary>
/// Tracks the state of a problem and its solution as constraints are added.
/// </summary>
/// <typeparam name="TNodeState">The type of value that a node may be set to.</typeparam>
/// <remarks>
/// Thread safety: Instance members on this class are not thread safe.
/// </remarks>
public partial class SolutionBuilder<TNodeState>
	where TNodeState : unmanaged
{
	/// <summary>
	/// The pool that recycles scenarios for simulations.
	/// </summary>
	private readonly ScenarioPool<TNodeState> scenarioPool;

	/// <summary>
	/// A map of nodes to their index as they appear in an ordered list.
	/// </summary>
	private readonly IReadOnlyDictionary<object, int> nodeIndex;

	/// <summary>
	/// An array of allowed values for each node.
	/// </summary>
	private readonly ImmutableArray<TNodeState> resolvedNodeStates;

	/// <summary>
	/// Tracks when the next <see cref="ResolvePartially(CancellationToken)"/>
	/// should clear the <see cref="Scenario{TNodeState}"/> before re-applying all constraints.
	/// For example when removing constraints, its side-effects must be removed.
	/// </summary>
	private bool fullRefreshNeeded;

	/// <summary>
	/// Initializes a new instance of the <see cref="SolutionBuilder{TNodeState}"/> class.
	/// </summary>
	/// <param name="nodes">The nodes in the problem/solution.</param>
	/// <param name="resolvedNodeStates">An array of allowed values for each node.</param>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="nodes"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentException">Thrown if <paramref name="nodes"/> is empty or contains duplicates.</exception>
	public SolutionBuilder(ImmutableArray<object> nodes, ImmutableArray<TNodeState> resolvedNodeStates)
	{
		if (nodes.IsDefault)
		{
			throw new ArgumentNullException(nameof(nodes));
		}

		if (nodes.IsEmpty)
		{
			throw new ArgumentException(Strings.NonEmptyArrayRequired, nameof(nodes));
		}

		if (resolvedNodeStates.IsDefaultOrEmpty)
		{
			throw new ArgumentException(Strings.NonEmptyArrayRequired, nameof(resolvedNodeStates));
		}

		if (resolvedNodeStates.Length < 2)
		{
			throw new ArgumentException(Strings.AtLeastTwoNodeStatesRequired, nameof(resolvedNodeStates));
		}

		this.resolvedNodeStates = resolvedNodeStates;
		this.nodeIndex = Scenario<TNodeState>.CreateNodeIndex(nodes);
		this.scenarioPool = new ScenarioPool<TNodeState>(nodes, this.nodeIndex);
		this.CurrentScenario = this.scenarioPool.Take();
	}

	/// <summary>
	/// Gets the applied constraints.
	/// </summary>
	public IReadOnlyCollection<IConstraint<TNodeState>> Constraints => this.CurrentScenario.Constraints;

	/// <summary>
	/// Gets the current scenario.
	/// </summary>
	internal Scenario<TNodeState> CurrentScenario { get; }

	/// <summary>
	/// Gets the number of nodes in the problem/solution.
	/// </summary>
	private int NodeCount => this.CurrentScenario.NodeCount;

	/// <summary>
	/// Gets the state for a node with a given index.
	/// </summary>
	/// <param name="index">The index of the node.</param>
	/// <returns>The state of the node. Null if the state isn't yet determined.</returns>
	/// <exception cref="IndexOutOfRangeException">Thrown if the <paramref name="index"/> is negative or exceeds the number of nodes in the solution.</exception>
	public TNodeState? this[int index] => this.CurrentScenario[index];

	/// <summary>
	/// Gets the state for the given node.
	/// </summary>
	/// <param name="node">The node.</param>
	/// <returns>The state of the node. Null if the state isn't yet determined.</returns>
	/// <exception cref="IndexOutOfRangeException">Thrown if the <paramref name="node"/> is not among the nodes in the solution.</exception>
	public TNodeState? this[object node] => this.CurrentScenario[node];

	/// <summary>
	/// Adds a constraint that describes the solution.
	/// </summary>
	/// <param name="constraint">The constraint to be added.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="constraint"/> is <see langword="null"/>.</exception>
	/// <exception cref="BadConstraintException{TNodeState}">Thrown when the <paramref name="constraint"/> has an empty set of <see cref="IConstraint{TNodeState}.Nodes"/>.</exception>
	/// <exception cref="KeyNotFoundException">Thrown when the <paramref name="constraint"/> refers to nodes that do not belong to this problem/solution.</exception>
	public void AddConstraint(IConstraint<TNodeState> constraint)
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
	public void AddConstraints(IEnumerable<IConstraint<TNodeState>> constraints)
	{
		if (constraints is null)
		{
			throw new ArgumentNullException(nameof(constraints));
		}

		foreach (IConstraint<TNodeState> constraint in constraints)
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
	public void RemoveConstraint(IConstraint<TNodeState> constraint)
	{
		if (constraint is null)
		{
			throw new ArgumentNullException(nameof(constraint));
		}

		this.CurrentScenario.RemoveConstraint(constraint);
		this.fullRefreshNeeded = true;
	}

	/// <summary>
	/// Removes constraints from the solution.
	/// </summary>
	/// <param name="constraints">The constraints to remove.</param>
	public void RemoveConstraints(IEnumerable<IConstraint<TNodeState>> constraints)
	{
		if (constraints is null)
		{
			throw new ArgumentNullException(nameof(constraints));
		}

		using var experiment = new Experiment(this);
		experiment.Candidate.RemoveConstraints(constraints);
		experiment.Commit();
		this.fullRefreshNeeded = true;
	}

	/// <summary>
	/// Checks whether viable solutions remain after applying the given constraint,
	/// without actually adding the constraint to the solution.
	/// </summary>
	/// <param name="constraint">The constraint to test.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns><see langword="true"/> if the constraint leaves viable solutions discoverable; <see langword="false"/> otherwise.</returns>
	/// <remarks>
	/// If no viable solutions exist before calling this method, this method will return <see langword="false"/>.
	/// </remarks>
	public bool CheckConstraint(IConstraint<TNodeState> constraint, CancellationToken cancellationToken)
	{
		if (constraint is null)
		{
			throw new ArgumentNullException(nameof(constraint));
		}

		using var experiment = new Experiment(this);
		experiment.Candidate.AddConstraint(constraint);
		return this.CheckForConflictingConstraints(experiment.Candidate, cancellationToken) is null;
	}

	/// <summary>
	/// Applies immediately constraint resolutions to the solution where possible.
	/// </summary>
	/// <param name="cancellationToken">A cancellation token.</param>
	public void ResolvePartially(CancellationToken cancellationToken = default)
	{
		using var experiment = new Experiment(this);
		if (this.fullRefreshNeeded)
		{
			for (int i = 0; i < this.NodeCount; i++)
			{
				experiment.Candidate.ResetNode(i, null);
			}
		}

		ResolvePartially(experiment.Candidate, cancellationToken);

		experiment.Commit();
		this.fullRefreshNeeded = false;
	}

	/// <summary>
	/// Checks whether at least one solution exists that can satisfy all existing constraints
	/// and returns diagnostic data about the conflicting constraints if no solution exists.
	/// </summary>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns><see langword="null"/> if a solution can be found; or diagnostic data about which sets of constraints conflict.</returns>
	public ConflictedConstraints? CheckForConflictingConstraints(CancellationToken cancellationToken)
	{
		using var experiment = new Experiment(this);
		return this.CheckForConflictingConstraints(experiment.Candidate, cancellationToken);
	}

	/// <summary>
	/// Exhaustively scan the solution space and collect statistics on the aggregate set.
	/// </summary>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The results of the analysis.</returns>
	public SolutionsAnalysis AnalyzeSolutions(CancellationToken cancellationToken)
	{
		using var experiment = new Experiment(this);
		ResolvePartially(experiment.Candidate, cancellationToken);
		var stats = default(SolutionStats);
		try
		{
			this.EnumerateSolutions(experiment.Candidate, 0, ref stats, cancellationToken);
			return new SolutionsAnalysis(this, stats.SolutionsFound, stats.NodesResolvedStateInSolutions, this.CreateConflictedConstraints(stats));
		}
		catch (OperationCanceledException ex)
		{
			throw new OperationCanceledException("Canceled after considering " + stats.ConsideredScenarios + " scenarios.", ex);
		}
	}

	/// <summary>
	/// Suggests the most probable solution.
	/// </summary>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>A probable scenario. Some nodes may still have indeterminate states when we have no constraints that impact them.</returns>
	/// <remarks>
	/// This isn't simply a matter of choosing the most probable state for each node, since selecting the state for some node
	/// may impact the probabilities of other nodes.
	/// This method instead sets the most probable state for the node with the most information,
	/// then re-analyzes the remaining problem space before repeating the process until all nodes have known states.
	/// As a result, this method can be significantly slower than <see cref="AnalyzeSolutions(CancellationToken)"/>.
	/// </remarks>
	public Scenario<TNodeState> GetProbableSolution(CancellationToken cancellationToken)
	{
		// We are taking a scenario from our pool, but we will *not* return it to the pool since we're returning it to our caller.
		Scenario<TNodeState>? scenario = this.scenarioPool.Take();
		scenario.CopyFrom(this.CurrentScenario);
		while (true)
		{
			// Calculate fresh probabilities for the nodes that remain.
			var stats = default(SolutionStats);
			this.EnumerateSolutions(scenario, 0, ref stats, cancellationToken);
			if (stats.NodesResolvedStateInSolutions is null)
			{
				throw new ApplicationException();
			}

			// Find the node and state with the highest probability of being correct.
			int mostLikelyNodeIndex = 0;
			long mostMatches = 0;
			TNodeState? mostLikelyState = null;
			for (int nodeIndex = 0; nodeIndex < this.NodeCount; nodeIndex++)
			{
				if (scenario[nodeIndex].HasValue)
				{
					// Skip this node as its state is already known.
					continue;
				}

				if (stats.NodesResolvedStateInSolutions[nodeIndex] is { } stateProbabilities)
				{
					foreach (TNodeState state in this.resolvedNodeStates)
					{
						cancellationToken.ThrowIfCancellationRequested();

						if (stateProbabilities.TryGetValue(state, out long matches) && matches > mostMatches)
						{
							mostLikelyNodeIndex = nodeIndex;
							mostMatches = matches;
							mostLikelyState = state;
						}
					}
				}
			}

			if (mostLikelyState is null)
			{
				// No more indeterminate nodes exist for which we have *any* data.
				break;
			}

			scenario[mostLikelyNodeIndex] = mostLikelyState;
			ResolvePartially(scenario, cancellationToken);
		}

		return scenario;
	}

	private static void ResolvePartially(Scenario<TNodeState> scenario, CancellationToken cancellationToken)
	{
		// Keep looping through constraints asking each one to resolve nodes until no changes are applied.
		bool anyResolved;
		do
		{
			anyResolved = false;
			for (int i = 0; i < scenario.Constraints.Length; i++)
			{
				IConstraint<TNodeState> constraint = scenario.Constraints[i];
				cancellationToken.ThrowIfCancellationRequested();
				bool resolved;
				int scenarioVersion = scenario.Version;
				try
				{
					resolved = constraint.Resolve(scenario);
				}
				catch (Exception ex)
				{
					throw new BadConstraintException<TNodeState>(constraint, Strings.ConstraintThrewUnexpectedException, ex);
				}

				if (resolved && scenario.Version == scenarioVersion)
				{
					throw new BadConstraintException<TNodeState>(constraint, Strings.ConstraintResolveReturnedTrueWithNoChanges);
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
	private static void ResolveByCascadingConstraints(Scenario<TNodeState> scenario, ImmutableArray<IConstraint<TNodeState>> applicableConstraints, CancellationToken cancellationToken)
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

	private ConflictedConstraints? CheckForConflictingConstraints(Scenario<TNodeState> scenario, CancellationToken cancellationToken)
	{
		ResolvePartially(scenario, cancellationToken);
		var stats = new SolutionStats { StopAfterFirstSolutionFound = true };
		this.EnumerateSolutions(scenario, 0, ref stats, cancellationToken);
		return this.CreateConflictedConstraints(stats);
	}

	private ConflictedConstraints? CreateConflictedConstraints(SolutionStats stats) => stats.SolutionsFound == 0 ? new ConflictedConstraints(this, this.CurrentScenario) : null;

	private void EnumerateSolutions(Scenario<TNodeState> basis, int firstNode, ref SolutionStats stats, CancellationToken cancellationToken)
	{
		stats.ConsideredScenarios++;
		cancellationToken.ThrowIfCancellationRequested();
		bool canAnyConstraintsBeBroken = false;
		for (int j = 0; j < basis.Constraints.Length; j++)
		{
			IConstraint<TNodeState> constraint = basis.Constraints[j];
			cancellationToken.ThrowIfCancellationRequested();
			ConstraintStates state = constraint.GetState(basis);
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
			stats.RecordSolutionFound(basis);
			return;
		}

		int i;
		for (i = firstNode; i < this.NodeCount; i++)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (basis[i].HasValue)
			{
				// Skip any node that already has a set value.
				continue;
			}

			// We don't need to enumerate possibilities for a node for which no constraints exist.
			ImmutableArray<IConstraint<TNodeState>> applicableConstraints = basis.GetConstraintsThatApplyTo(i);
			if (applicableConstraints.IsEmpty)
			{
				// Skip any node that can be any value without impact to constraints.
				continue;
			}

			// Try selecting the node. In doing so, resolve whatever nodes we can immediately.
			for (int k = 0; k < this.resolvedNodeStates.Length; k++)
			{
				TNodeState value = this.resolvedNodeStates[k];

				using var experiment = new Experiment(this, basis);
				experiment.Candidate[i] = value;
				ResolveByCascadingConstraints(experiment.Candidate, applicableConstraints, cancellationToken);
				this.EnumerateSolutions(experiment.Candidate, i + 1, ref stats, cancellationToken);

				if (stats.StopAfterFirstSolutionFound && stats.SolutionsFound > 0)
				{
					return;
				}
			}

			// Once we drill into one node, we don't want to drill into any more nodes since
			// we did that via our recursive call.
			break;
		}

		if (i >= this.NodeCount)
		{
			stats.RecordSolutionFound(basis);
		}
	}

	private ref struct SolutionStats
	{
		internal bool StopAfterFirstSolutionFound { get; set; }

		internal long SolutionsFound { get; private set; }

		internal Dictionary<TNodeState, long>?[]? NodesResolvedStateInSolutions { get; private set; }

		internal long ConsideredScenarios { get; set; }

		internal void RecordSolutionFound(Scenario<TNodeState> scenario)
		{
			checked
			{
				this.SolutionsFound++;

				if (!this.StopAfterFirstSolutionFound)
				{
					this.NodesResolvedStateInSolutions ??= new Dictionary<TNodeState, long>?[scenario.NodeCount];

					for (int i = 0; i < scenario.NodeCount; i++)
					{
						if (scenario[i] is TNodeState resolvedState)
						{
							Dictionary<TNodeState, long>? statesAndCounts = this.NodesResolvedStateInSolutions[i];
							if (statesAndCounts is null)
							{
								this.NodesResolvedStateInSolutions[i] = statesAndCounts = new Dictionary<TNodeState, long>();
							}

							statesAndCounts.TryGetValue(resolvedState, out long counts);
							statesAndCounts[resolvedState] = counts + 1;
						}
						else
						{
							// This node is not constrained by anything. So it is a free radical and shouldn't be counted as selected or unselected
							// since solutions are not enumerated based on flipping this.
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
		private readonly SolutionBuilder<TNodeState> builder;

		/// <summary>
		/// The scenario from which this experiment started.
		/// </summary>
		private readonly Scenario<TNodeState> basis;

		/// <summary>
		/// Initializes a new instance of the <see cref="Experiment"/> struct.
		/// </summary>
		/// <param name="builder">The owner of this instance.</param>
		/// <param name="basis">The scenario to use as a template. If unspecified, the <see cref="SolutionBuilder{TNodeState}.CurrentScenario"/> is used.</param>
		internal Experiment(SolutionBuilder<TNodeState> builder, Scenario<TNodeState>? basis = default)
		{
			this.basis = basis ?? builder.CurrentScenario;
			this.builder = builder;
			this.Candidate = builder.scenarioPool.Take();
			this.Candidate.CopyFrom(this.basis);
		}

		/// <summary>
		/// Gets the experimental scenario.
		/// </summary>
		public Scenario<TNodeState> Candidate { get; }

		/// <summary>
		/// Commits the <see cref="Candidate"/> to the current scenario in the <see cref="SolutionBuilder{TNodeState}"/>.
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
