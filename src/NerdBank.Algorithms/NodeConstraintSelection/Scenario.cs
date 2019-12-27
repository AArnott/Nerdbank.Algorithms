// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NerdBank.Algorithms.NodeConstraintSelection
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Collections.ObjectModel;

	/// <summary>
	/// A scenario where nodes are considered to be selected or not.
	/// </summary>
	/// <remarks>
	/// Thread safety: Instance members on this class are not thread safe.
	/// </remarks>
	public class Scenario
	{
		/// <summary>
		/// The selection state for each node.
		/// </summary>
		private readonly bool?[] selectionState;

		/// <summary>
		/// The nodes in the solution.
		/// </summary>
		private readonly IReadOnlyList<object> nodes;

		/// <summary>
		/// A map of nodes to their index into <see cref="nodes"/>.
		/// </summary>
		private readonly IReadOnlyDictionary<object, int> nodeIndex;

		/// <summary>
		/// Initializes a new instance of the <see cref="Scenario"/> class.
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

			this.selectionState = new bool?[nodes.Count];
			this.nodes = nodes;
			this.nodeIndex = CreateNodeIndex(nodes);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Scenario"/> class.
		/// </summary>
		/// <param name="nodes">The nodes in the problem/solution.</param>
		/// <param name="nodeIndex">A map of nodes to their index into <paramref name="nodes"/>.</param>
		internal Scenario(IReadOnlyList<object> nodes, IReadOnlyDictionary<object, int> nodeIndex)
		{
			this.selectionState = new bool?[nodes.Count];
			this.nodes = nodes;
			this.nodeIndex = nodeIndex;
		}

		/// <summary>
		/// Gets the number of nodes in the problem/solution.
		/// </summary>
		public int NodeCount => this.nodes.Count;

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
		public bool? this[int index]
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
		public bool? this[object node]
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
		/// This method can be used by <see cref="IConstraint"/> implementations to translate and cache nodes into indexes
		/// for improved performance in <see cref="IConstraint.GetState(Scenario)"/>.
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
				lookup[nodes[i]] = i;
			}

			return new ReadOnlyDictionary<object, int>(lookup);
		}

		/// <summary>
		/// Sets the selection state of a given node, even if it is already set.
		/// </summary>
		/// <param name="index">The index of the node to change.</param>
		/// <param name="selected">The new state.</param>
		internal void ResetNode(int index, bool? selected)
		{
			this.selectionState[index] = selected;
			this.Version++;
		}

		/// <summary>
		/// Applies the selection state of another scenario to this one.
		/// </summary>
		/// <param name="copyFrom">The template scenario.</param>
		/// <exception cref="ArgumentException">Thrown if the <paramref name="copyFrom"/> scenario does not have the same number of nodes as this one.</exception>
		internal void CopyFrom(Scenario copyFrom)
		{
			if (copyFrom.selectionState.Length != this.selectionState.Length)
			{
				throw new ArgumentException(Strings.NodeCountMismatch);
			}

			bool?[] src = copyFrom.selectionState;
			bool?[] dest = this.selectionState;
			for (int i = 0; i < src.Length; i++)
			{
				dest[i] = src[i];
			}

			this.Version++;
		}
	}
}
