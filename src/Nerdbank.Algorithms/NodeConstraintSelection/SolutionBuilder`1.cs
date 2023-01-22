// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Collections.ObjectModel;

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
	/// Immutable configuration for this builder.
	/// </summary>
	private readonly Configuration<TNodeState> configuration;

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
		: this(new Configuration<TNodeState>(nodes, resolvedNodeStates))
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SolutionBuilder{TNodeState}"/> class.
	/// </summary>
	/// <param name="configuration">The problem space configuration.</param>
	public SolutionBuilder(Configuration<TNodeState> configuration)
	{
		this.configuration = configuration;
		this.CurrentScenario = this.configuration.ScenarioPool.Take();
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

		using Experiment experiment = this.NewExperiment();
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

		using Experiment experiment = this.NewExperiment();
		experiment.Candidate.AddConstraint(constraint);
		return CheckForConflictingConstraints(this.configuration, experiment.Candidate, cancellationToken) is null;
	}

	/// <summary>
	/// Applies immediately constraint resolutions to the solution where possible.
	/// </summary>
	/// <param name="cancellationToken">A cancellation token.</param>
	public void ResolvePartially(CancellationToken cancellationToken = default)
	{
		using Experiment experiment = this.NewExperiment();
		if (this.fullRefreshNeeded)
		{
			for (int i = 0; i < this.configuration.Nodes.Length; i++)
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
		Experiment experiment = this.NewExperiment();
		try
		{
			ConflictedConstraints? result = CheckForConflictingConstraints(this.configuration, experiment.Candidate, cancellationToken);
			if (result is null)
			{
				experiment.Dispose();
			}

			return result;
		}
		catch
		{
			experiment.Dispose();
			throw;
		}
	}

	/// <summary>
	/// Exhaustively scan the solution space and collect statistics on the aggregate set.
	/// </summary>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The results of the analysis.</returns>
	/// <remarks>
	/// <para>
	/// This method may take a <em>very</em> long time to complete.
	/// Callers should provide a way for users to cancel the operation that is linked to the <paramref name="cancellationToken"/>,
	/// and/or configure that token to self-cancel after some timeout.
	/// </para>
	/// <para>
	/// This method enqueues async work that is safe to run concurrently with other methods on this class.
	/// The synchronous preamble in this method should not run concurrently with any method on this class, however.
	/// The recommended pattern then is to invoke this method while on the application's exclusive (i.e. main/UI) thread,
	/// just like all other methods on this class.
	/// Then to continue using this class from that thread as usual even while this method finishes its asynchronous execution.
	/// </para>
	/// </remarks>
	public Task<SolutionsAnalysis> AnalyzeSolutionsAsync(CancellationToken cancellationToken)
	{
		// Any mutable data that this analysis must read from this instance must be copied now, before yielding back to the caller.
		Experiment experiment = this.NewExperiment();
		ResolvePartially(experiment.Candidate, cancellationToken);

		return Task.Run(() => AnalyzeSolutions(this.configuration, experiment, cancellationToken));
	}

	/// <summary>
	/// Exhaustively scan the solution space and collect statistics on the aggregate set.
	/// </summary>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The results of the analysis.</returns>
	/// <remarks>
	/// <para>
	/// This method may take a <em>very</em> long time to complete.
	/// Callers should provide a way for users to cancel the operation that is linked to the <paramref name="cancellationToken"/>,
	/// and/or configure that token to self-cancel after some timeout.
	/// </para>
	/// </remarks>
	public SolutionsAnalysis AnalyzeSolutions(CancellationToken cancellationToken)
	{
		using Experiment experiment = this.NewExperiment();
		ResolvePartially(experiment.Candidate, cancellationToken);
		return AnalyzeSolutions(this.configuration, experiment, cancellationToken);
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
		Scenario<TNodeState>? scenario = this.configuration.ScenarioPool.Take();
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
			for (int nodeIndex = 0; nodeIndex < this.configuration.Nodes.Length; nodeIndex++)
			{
				if (scenario[nodeIndex].HasValue)
				{
					// Skip this node as its state is already known.
					continue;
				}

				if (stats.NodesResolvedStateInSolutions[nodeIndex] is { } stateProbabilities)
				{
					foreach (TNodeState state in this.configuration.ResolvedNodeStates)
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

	/// <summary>
	/// Select or unselect any indeterminate nodes which are unconditionally in just one state in all viable solutions.
	/// </summary>
	/// <param name="analysis">The analysis to apply to this solution.</param>
	/// <remarks>
	/// Constraints can interact as they narrow the field of viable solutions such that
	/// some nodes may be effectively constrained to a certain value even though that value
	/// isn't explicitly required by any individual constraint.
	/// When these interactions are understood, applying them back to the <see cref="SolutionBuilder{T}"/>
	/// can speed up future analyses and lead each node's value to reflect the remaining possibilities.
	/// </remarks>
	public void CommitAnalysis(SolutionsAnalysis analysis)
	{
		if (analysis.ViableSolutionsFound == 0 || analysis.NodeValueCount is null)
		{
			throw new InvalidOperationException(Strings.ViableSolutionStatsNotAvailable);
		}

		if (analysis.ViableSolutionsFound > 0)
		{
			for (int i = 0; i < analysis.NodeValueCount.Length; i++)
			{
				if (this.CurrentScenario[i] is null && analysis.NodeValueCount[i] is { } valuesAndCounts)
				{
					foreach (TNodeState value in this.configuration.ResolvedNodeStates)
					{
						if (valuesAndCounts.TryGetValue(value, out long counts) && counts == analysis.ViableSolutionsFound)
						{
							this.CurrentScenario[i] = value;
							break;
						}
					}
				}
			}
		}
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

	private static ConflictedConstraints? CheckForConflictingConstraints(Configuration<TNodeState> configuration, Scenario<TNodeState> scenario, CancellationToken cancellationToken)
	{
		ResolvePartially(scenario, cancellationToken);
		var stats = new SolutionStats { StopAfterFirstSolutionFound = true };
		EnumerateSolutions(configuration, scenario, 0, ref stats, cancellationToken);
		return CreateConflictedConstraints(configuration, stats, scenario);
	}

	private static ConflictedConstraints? CreateConflictedConstraints(Configuration<TNodeState> configuration, SolutionStats stats, Scenario<TNodeState> conflictedScenario) => stats.SolutionsFound == 0 ? new ConflictedConstraints(configuration, conflictedScenario) : null;

	private static void EnumerateSolutions(Configuration<TNodeState> configuration, Scenario<TNodeState> basis, int firstNode, ref SolutionStats stats, CancellationToken cancellationToken)
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
		for (i = firstNode; i < configuration.Nodes.Length; i++)
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
			for (int k = 0; k < configuration.ResolvedNodeStates.Length; k++)
			{
				TNodeState value = configuration.ResolvedNodeStates[k];

				using Experiment experiment = new(basis);
				experiment.Candidate[i] = value;
				ResolveByCascadingConstraints(experiment.Candidate, applicableConstraints, cancellationToken);
				EnumerateSolutions(configuration, experiment.Candidate, i + 1, ref stats, cancellationToken);

				if (stats.StopAfterFirstSolutionFound && stats.SolutionsFound > 0)
				{
					return;
				}
			}

			// Once we drill into one node, we don't want to drill into any more nodes since
			// we did that via our recursive call.
			break;
		}

		if (i >= configuration.Nodes.Length)
		{
			stats.RecordSolutionFound(basis);
		}
	}

	private static SolutionsAnalysis AnalyzeSolutions(Configuration<TNodeState> configuration, Experiment experiment, CancellationToken cancellationToken)
	{
		SolutionBuilder<TNodeState>.SolutionStats stats = default;
		try
		{
			EnumerateSolutions(configuration, experiment.Candidate, 0, ref stats, cancellationToken);
			return new SolutionsAnalysis(configuration, stats.SolutionsFound, stats.NodesResolvedStateInSolutions, CreateConflictedConstraints(configuration, stats, experiment.Candidate));
		}
		catch (OperationCanceledException ex)
		{
			throw new OperationCanceledException("Canceled after considering " + stats.ConsideredScenarios + " scenarios.", ex);
		}
	}

	private Experiment NewExperiment() => new(this.CurrentScenario);

	private void EnumerateSolutions(Scenario<TNodeState> basis, int firstNode, ref SolutionStats stats, CancellationToken cancellationToken) => EnumerateSolutions(this.configuration, basis, firstNode, ref stats, cancellationToken);

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
		private Scenario<TNodeState>? candidate;

		/// <summary>
		/// Initializes a new instance of the <see cref="Experiment"/> struct.
		/// </summary>
		/// <param name="basis">The scenario to use as a template. If unspecified, the <see cref="SolutionBuilder{TNodeState}.CurrentScenario"/> is used.</param>
		internal Experiment(Scenario<TNodeState> basis)
		{
			this.Basis = basis;
			this.candidate = basis.Configuration.ScenarioPool.Take();
			this.candidate.CopyFrom(basis);
		}

		/// <summary>
		/// Gets the scenario that was originally provided to the constructor.
		/// </summary>
		public Scenario<TNodeState> Basis { get; }

		/// <summary>
		/// Gets the experimental scenario.
		/// </summary>
		public Scenario<TNodeState> Candidate => this.candidate ?? throw new ObjectDisposedException(nameof(Experiment));

		/// <summary>
		/// Commits the <see cref="Candidate"/> to the <see cref="Basis"/> scenario.
		/// </summary>
		public void Commit()
		{
			this.Basis.CopyFrom(this.Candidate);
		}

		/// <summary>
		/// Recycles the <see cref="Candidate"/> and concludes the experiment.
		/// </summary>
		public void Dispose()
		{
			if (this.candidate is { } candidate)
			{
				candidate.Configuration.ScenarioPool.Return(candidate);
				this.candidate = null;
			}
		}
	}
}
