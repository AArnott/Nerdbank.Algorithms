// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Nerdbank.Algorithms.NodeConstraintSelection;

/// <summary>
/// A scenario where nodes are considered to be selected or not.
/// </summary>
/// <typeparam name="TNodeState">The type of value that a node may be set to.</typeparam>
/// <remarks>
/// Thread safety: Instance members on this class are not thread safe.
/// All state on an instance is either immutable or exclusive to this instance.
/// </remarks>
[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
public sealed class Scenario<TNodeState>
	where TNodeState : unmanaged
{
	/// <summary>
	/// The selection state for each node.
	/// </summary>
	private readonly TNodeState?[] selectionState;

	private readonly Configuration<TNodeState> configuration;

	/// <summary>
	/// Tracks when the next <see cref="SolutionBuilder{TNodeState}.ResolvePartially(Scenario{TNodeState}, CancellationToken)"/>
	/// should clear the <see cref="Scenario{TNodeState}"/> before re-applying all constraints.
	/// For example when removing constraints, its side-effects must be removed.
	/// </summary>
	private bool fullRefreshNeeded;

	/// <summary>
	/// The constraints that describe the solution.
	/// </summary>
	private ImmutableArray<IConstraint<TNodeState>> constraints = ImmutableArray.Create<IConstraint<TNodeState>>();

	/// <summary>
	/// All constraints, indexed by each node that impact them.
	/// </summary>
	private ImmutableArray<ImmutableArray<IConstraint<TNodeState>>> constraintsPerNode;

	/// <summary>
	/// When true, node mutations are recorded into <see cref="dirtyNodes"/>.
	/// </summary>
	private bool trackDirtyNodes;

	/// <summary>
	/// Buffer of node indexes mutated while <see cref="trackDirtyNodes"/> is true.
	/// </summary>
	private int[]? dirtyNodes;

	/// <summary>
	/// Number of valid entries in <see cref="dirtyNodes"/>.
	/// </summary>
	private int dirtyNodeCount;

	/// <summary>
	/// Reused work queue for partial resolution.
	/// </summary>
	private Queue<IConstraint<TNodeState>>? resolveQueue;

	/// <summary>
	/// Reused set of constraints already present in <see cref="resolveQueue"/>.
	/// </summary>
	private HashSet<IConstraint<TNodeState>>? resolveEnqueued;

	/// <summary>
	/// Initializes a new instance of the <see cref="Scenario{TNodeState}"/> class.
	/// </summary>
	/// <param name="configuration">The problem space configuration.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration"/> is <see langword="null"/>.</exception>
	public Scenario(Configuration<TNodeState> configuration)
	{
		if (configuration is null)
		{
			throw new ArgumentNullException(nameof(configuration));
		}

		this.selectionState = new TNodeState?[configuration.Nodes.Length];
		this.configuration = configuration;

		ImmutableArray<IConstraint<TNodeState>> emptyConstraints = ImmutableArray<IConstraint<TNodeState>>.Empty;
		ImmutableArray<ImmutableArray<IConstraint<TNodeState>>>.Builder constraintsPerNodeBuilder =
			ImmutableArray.CreateBuilder<ImmutableArray<IConstraint<TNodeState>>>(configuration.Nodes.Length);
		for (int i = 0; i < configuration.Nodes.Length; i++)
		{
			constraintsPerNodeBuilder.Add(emptyConstraints);
		}

		this.constraintsPerNode = constraintsPerNodeBuilder.MoveToImmutable();
	}

	/// <summary>
	/// Gets the number of nodes in the problem/solution.
	/// </summary>
	public int NodeCount => this.configuration.Nodes.Length;

	/// <summary>
	/// Gets a list of the states of every node.
	/// </summary>
	public IReadOnlyList<TNodeState?> NodeStates => this.selectionState;

	/// <summary>
	/// Gets the configuration that this scenario belongs to.
	/// </summary>
	internal Configuration<TNodeState> Configuration => this.configuration;

	/// <summary>
	/// Gets the constraints that are applied in this scenario.
	/// </summary>
	internal ImmutableArray<IConstraint<TNodeState>> Constraints => this.constraints;

	/// <summary>
	/// Gets a value that changes each time a node selection or constraint is changed.
	/// </summary>
	internal int Version { get; private set; }

	private string DebuggerDisplay => this.Configuration.ToString(this);

	/// <summary>
	/// Gets or sets the selection state for a node with a given index.
	/// </summary>
	/// <param name="index">The index of the node.</param>
	/// <returns>The selection state of the node. Null if the selection state isn't yet determined.</returns>
	/// <exception cref="InvalidOperationException">Thrown when setting a node that already has a known state.</exception>
	/// <exception cref="IndexOutOfRangeException">Thrown if the <paramref name="index"/> is negative or exceeds the number of nodes in the solution.</exception>
	public TNodeState? this[int index]
	{
		get => this.selectionState[index];
		set
		{
			if (value is null)
			{
				throw new ArgumentNullException(nameof(value));
			}

			if (this.selectionState[index].HasValue)
			{
				throw new InvalidOperationException(Strings.NodeAlreadySet);
			}

			this.selectionState[index] = value;
			this.Version++;
			this.RecordDirtyNode(index);
		}
	}

	/// <summary>
	/// Gets or sets the selection state for the given node.
	/// </summary>
	/// <param name="node">The node.</param>
	/// <returns>The selection state of the node. Null if the selection state isn't yet determined.</returns>
	/// <exception cref="InvalidOperationException">Thrown when setting a node that already has a known state.</exception>
	/// <exception cref="KeyNotFoundException">Thrown if the <paramref name="node"/> is not among the nodes in the solution.</exception>
	/// <remarks>
	/// As this call incurs a dictionary lookup penalty to translate the <paramref name="node"/> into an array index,
	/// frequent callers should use <see cref="GetNodeIndex(object)"/> to perform this lookup and store the result
	/// so that node indexes can be used instead of node objects in perf-critical code.
	/// </remarks>
	public TNodeState? this[object node]
	{
		get => this[this.configuration.Index[node]];
		set => this[this.configuration.Index[node]] = value;
	}

	/// <summary>
	/// Gets the index for the given node.
	/// </summary>
	/// <param name="node">The node.</param>
	/// <returns>The index of the given node.</returns>
	/// <exception cref="KeyNotFoundException">Thrown if the <paramref name="node"/> is not among the nodes in the solution.</exception>
	/// <remarks>
	/// This method can be used by <see cref="IConstraint{TNodeState}"/> implementations to translate and cache nodes into indexes
	/// for improved performance in <see cref="IConstraint{TNodeState}.GetState(Scenario{TNodeState})"/>.
	/// </remarks>
	public int GetNodeIndex(object node) => this.configuration.Index[node];

	/// <inheritdoc/>
	public override string ToString() => this.Configuration.ToString(this);

	/// <summary>
	/// Sets the selection state of a given node, even if it is already set.
	/// </summary>
	/// <param name="index">The index of the node to change.</param>
	/// <param name="selected">The new state.</param>
	internal void ResetNode(int index, TNodeState? selected)
	{
		this.selectionState[index] = selected;
		this.Version++;
		this.RecordDirtyNode(index);
	}

	/// <summary>
	/// Begins recording node mutations into a reusable dirty-node buffer.
	/// </summary>
	internal void BeginDirtyTracking()
	{
		this.dirtyNodes ??= new int[this.selectionState.Length];
		this.dirtyNodeCount = 0;
		this.trackDirtyNodes = true;
	}

	/// <summary>
	/// Stops dirty-node tracking and returns the nodes mutated since <see cref="BeginDirtyTracking"/>.
	/// </summary>
	/// <returns>The dirty node indexes.</returns>
	internal ReadOnlySpan<int> EndDirtyTrackingAndGetDirtyNodes()
	{
		this.trackDirtyNodes = false;
		return this.dirtyNodes.AsSpan(0, this.dirtyNodeCount);
	}

	/// <summary>
	/// Gets reusable collections used by partial resolution.
	/// </summary>
	/// <param name="queue">The work queue of constraints to process.</param>
	/// <param name="enqueued">The set of constraints already present in <paramref name="queue"/>.</param>
	internal void GetResolveWorkBuffers(out Queue<IConstraint<TNodeState>> queue, out HashSet<IConstraint<TNodeState>> enqueued)
	{
		queue = this.resolveQueue ??= new Queue<IConstraint<TNodeState>>();
		enqueued = this.resolveEnqueued ??= new HashSet<IConstraint<TNodeState>>();
		queue.Clear();
		enqueued.Clear();
	}

	/// <summary>
	/// Resets all nodes to their default state if constraints have been removed recently.
	/// </summary>
	internal void ResetIfNeeded()
	{
		if (this.fullRefreshNeeded)
		{
			for (int i = 0; i < this.configuration.Nodes.Length; i++)
			{
				this.ResetNode(i, null);
			}

			this.fullRefreshNeeded = false;
		}
	}

	/// <summary>
	/// Gets the constraints that apply to a node with the given index.
	/// </summary>
	/// <param name="nodeIndex">The index of the node.</param>
	/// <returns>The constraints that apply to that node.</returns>
	internal ImmutableArray<IConstraint<TNodeState>> GetConstraintsThatApplyTo(int nodeIndex) => this.constraintsPerNode[nodeIndex];

	/// <summary>
	/// Adds a constraint to this scenario.
	/// </summary>
	/// <param name="constraint">The constraint to be added.</param>
	/// <exception cref="BadConstraintException{TNodeState}">Thrown when the <paramref name="constraint"/> has an empty set of <see cref="IConstraint{TNodeState}.Nodes"/>.</exception>
	internal void AddConstraint(IConstraint<TNodeState> constraint)
	{
		if (constraint.Nodes.IsEmpty)
		{
			throw new BadConstraintException<TNodeState>(constraint, Strings.ConstraintForEmptySetOfNodes);
		}

		this.constraints = this.constraints.Add(constraint);

		var constraintsPerNode = this.constraintsPerNode.ToBuilder();
		foreach (var node in constraint.Nodes)
		{
			int nodeIndex = this.configuration.Index[node];
			constraintsPerNode[nodeIndex] = constraintsPerNode[nodeIndex].Add(constraint);
		}

		this.constraintsPerNode = constraintsPerNode.ToImmutable();

		this.Version++;
	}

	/// <summary>
	/// Removes a constraint from this scenario.
	/// </summary>
	/// <param name="constraint">The constraint to remove.</param>
	internal void RemoveConstraint(IConstraint<TNodeState> constraint)
	{
		this.constraints = this.constraints.Remove(constraint);

		var constraintsPerNode = this.constraintsPerNode.ToBuilder();
		foreach (var node in constraint.Nodes)
		{
			int nodeIndex = this.configuration.Index[node];
			constraintsPerNode[nodeIndex] = constraintsPerNode[nodeIndex].Remove(constraint);
		}

		this.constraintsPerNode = constraintsPerNode.ToImmutable();

		this.Version++;
		this.fullRefreshNeeded = true;
	}

	/// <summary>
	/// Removes constraints from this scenario.
	/// </summary>
	/// <param name="constraints">The constraints to remove.</param>
	internal void RemoveConstraints(IEnumerable<IConstraint<TNodeState>> constraints)
	{
		this.constraints = this.constraints.RemoveRange(constraints);

		var constraintsPerNode = this.constraintsPerNode.ToBuilder();
		foreach (IConstraint<TNodeState> constraint in constraints)
		{
			if (constraint is null)
			{
				throw new ArgumentException(Strings.NullMemberOfCollection, nameof(constraints));
			}

			foreach (var node in constraint.Nodes)
			{
				int nodeIndex = this.configuration.Index[node];
				constraintsPerNode[nodeIndex] = constraintsPerNode[nodeIndex].Remove(constraint);
			}
		}

		this.constraintsPerNode = constraintsPerNode.ToImmutable();

		this.Version++;
		this.fullRefreshNeeded = true;
	}

	/// <summary>
	/// Applies the selection state of another scenario to this one.
	/// </summary>
	/// <param name="copyFrom">The template scenario.</param>
	/// <exception cref="ArgumentException">Thrown if the <paramref name="copyFrom"/> scenario does not have the same number of nodes as this one.</exception>
	internal unsafe void CopyFrom(Scenario<TNodeState> copyFrom)
	{
		if (copyFrom.selectionState.Length != this.selectionState.Length)
		{
			throw new ArgumentException(Strings.NodeCountMismatch);
		}

		// Copy using memmove because it's much faster than a loop that iterates over the array copying one element at a time.
		CopySelectionState(copyFrom.selectionState, this.selectionState);

		this.constraints = copyFrom.Constraints;
		this.constraintsPerNode = copyFrom.constraintsPerNode;
		this.fullRefreshNeeded = copyFrom.fullRefreshNeeded;

		this.Version++;
	}

	/// <summary>
	/// Creates a checkpoint of the current selection state that can be restored by disposing the returned value.
	/// </summary>
	/// <returns>A disposable checkpoint.</returns>
	internal SelectionCheckpoint Checkpoint() => new(this);

	private static unsafe void CopySelectionState(TNodeState?[] src, TNodeState?[] dest)
	{
		fixed (void* pSrc = &src[0])
		{
			fixed (void* pDest = &dest[0])
			{
				int bytesToCopy = sizeof(TNodeState?) * src.Length;
				Buffer.MemoryCopy(pSrc, pDest, bytesToCopy, bytesToCopy);
			}
		}
	}

	private void RecordDirtyNode(int index)
	{
		if (this.trackDirtyNodes)
		{
			this.dirtyNodes![this.dirtyNodeCount++] = index;
		}
	}

	private void RestoreFromSnapshot(TNodeState?[] snapshot, int version, bool fullRefreshNeeded)
	{
		CopySelectionState(snapshot, this.selectionState);
		this.Version = version;
		this.fullRefreshNeeded = fullRefreshNeeded;
		this.configuration.ScenarioPool.ReturnSelectionBuffer(snapshot);
	}

	/// <summary>
	/// A disposable snapshot of selection state used for in-place backtracking.
	/// </summary>
	internal ref struct SelectionCheckpoint
	{
		private Scenario<TNodeState>? owner;
		private TNodeState?[]? snapshot;
		private int version;
		private bool fullRefreshNeeded;

		/// <summary>
		/// Initializes a new instance of the <see cref="SelectionCheckpoint"/> struct.
		/// </summary>
		/// <param name="owner">The scenario to snapshot.</param>
		internal SelectionCheckpoint(Scenario<TNodeState> owner)
		{
			this.owner = owner;
			this.version = owner.Version;
			this.fullRefreshNeeded = owner.fullRefreshNeeded;
			this.snapshot = owner.configuration.ScenarioPool.TakeSelectionBuffer();
			CopySelectionState(owner.selectionState, this.snapshot);
		}

		/// <summary>
		/// Restores the scenario selection state captured at construction.
		/// </summary>
		public void Dispose()
		{
			if (this.owner is { } owner && this.snapshot is { } snapshot)
			{
				owner.RestoreFromSnapshot(snapshot, this.version, this.fullRefreshNeeded);
				this.owner = null;
				this.snapshot = null;
			}
		}
	}
}
