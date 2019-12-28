// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NerdBank.Algorithms.NodeConstraintSelection
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Collections.ObjectModel;
	using System.Linq;

	/// <summary>
	/// A scenario where nodes are considered to be selected or not.
	/// </summary>
	/// <typeparam name="TNodeState">The type of value that a node may be set to.</typeparam>
	/// <remarks>
	/// Thread safety: Instance members on this class are not thread safe.
	/// </remarks>
	public sealed class Scenario<TNodeState>
		where TNodeState : struct
	{
		/// <summary>
		/// The selection state for each node.
		/// </summary>
		private readonly TNodeState?[] selectionState;

		/// <summary>
		/// The nodes in the solution.
		/// </summary>
		private readonly IReadOnlyList<object> nodes;

		/// <summary>
		/// A map of nodes to their index into <see cref="nodes"/>.
		/// </summary>
		private readonly IReadOnlyDictionary<object, int> nodeIndex;

		/// <summary>
		/// The constraints that describe the solution.
		/// </summary>
		private ImmutableArray<IConstraint<TNodeState>> constraints = ImmutableArray.Create<IConstraint<TNodeState>>();

		/// <summary>
		/// All constraints, indexed by each node that impact them.
		/// </summary>
		private ImmutableArray<ImmutableArray<IConstraint<TNodeState>>> constraintsPerNode;

		/// <summary>
		/// Initializes a new instance of the <see cref="Scenario{TNodeState}"/> class.
		/// </summary>
		/// <param name="nodes">The list of nodes.</param>
		/// <remarks>
		/// This constructor is designed for unit testing constraints.
		/// </remarks>
		public Scenario(IReadOnlyList<object> nodes)
		{
			if (nodes is null)
			{
				throw new ArgumentNullException(nameof(nodes));
			}

			this.selectionState = new TNodeState?[nodes.Count];
			this.nodes = nodes;
			this.nodeIndex = CreateNodeIndex(nodes);
			this.constraintsPerNode = nodes.Select(n => ImmutableArray.Create<IConstraint<TNodeState>>()).ToImmutableArray();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Scenario{TNodeState}"/> class.
		/// </summary>
		/// <param name="nodes">The nodes in the problem/solution.</param>
		/// <param name="nodeIndex">A map of nodes to their index into <paramref name="nodes"/>.</param>
		internal Scenario(IReadOnlyList<object> nodes, IReadOnlyDictionary<object, int> nodeIndex)
		{
			this.selectionState = new TNodeState?[nodes.Count];
			this.nodes = nodes;
			this.nodeIndex = nodeIndex;
			this.constraintsPerNode = nodes.Select(n => ImmutableArray.Create<IConstraint<TNodeState>>()).ToImmutableArray();
		}

		/// <summary>
		/// Gets the number of nodes in the problem/solution.
		/// </summary>
		public int NodeCount => this.nodes.Count;

		/// <summary>
		/// Gets the constraints that are applied in this scenario.
		/// </summary>
		internal ImmutableArray<IConstraint<TNodeState>> Constraints => this.constraints;

		/// <summary>
		/// Gets a value that changes each time a node selection is changed.
		/// </summary>
		internal int Version { get; private set; }

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
			get => this[this.nodeIndex[node]];
			set => this[this.nodeIndex[node]] = value;
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
		public int GetNodeIndex(object node) => this.nodeIndex[node];

		/// <summary>
		/// Creates a map of nodes to the index in a list.
		/// </summary>
		/// <param name="nodes">The list of nodes.</param>
		/// <returns>The map of nodes to where they are found in the <paramref name="nodes"/> list.</returns>
		internal static IReadOnlyDictionary<object, int> CreateNodeIndex(IReadOnlyList<object> nodes)
		{
			var lookup = new Dictionary<object, int>();
			for (int i = 0; i < nodes.Count; i++)
			{
				lookup.Add(nodes[i], i);
			}

			return new ReadOnlyDictionary<object, int>(lookup);
		}

		/// <summary>
		/// Sets the selection state of a given node, even if it is already set.
		/// </summary>
		/// <param name="index">The index of the node to change.</param>
		/// <param name="selected">The new state.</param>
		internal void ResetNode(int index, TNodeState? selected)
		{
			this.selectionState[index] = selected;
			this.Version++;
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
			if (constraint.Nodes.Count == 0)
			{
				throw new BadConstraintException<TNodeState>(constraint, Strings.ConstraintForEmptySetOfNodes);
			}

			this.constraints = this.constraints.Add(constraint);

			var constraintsPerNode = this.constraintsPerNode.ToBuilder();
			foreach (var node in constraint.Nodes)
			{
				int nodeIndex = this.nodeIndex[node];
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
				int nodeIndex = this.nodeIndex[node];
				constraintsPerNode[nodeIndex] = constraintsPerNode[nodeIndex].Remove(constraint);
			}

			this.constraintsPerNode = constraintsPerNode.ToImmutable();

			this.Version++;
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
			TNodeState?[] src = copyFrom.selectionState;
			TNodeState?[] dest = this.selectionState;
			fixed (void* pSrc = &src[0])
			fixed (void* pDest = &dest[0])
			{
				int bytesToCopy = sizeof(TNodeState?) * src.Length;
				Buffer.MemoryCopy(pSrc, pDest, bytesToCopy, bytesToCopy);
			}

			this.constraints = copyFrom.Constraints;
			this.constraintsPerNode = copyFrom.constraintsPerNode;

			this.Version++;
		}
	}
}
