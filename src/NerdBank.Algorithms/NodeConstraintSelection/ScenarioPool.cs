// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NerdBank.Algorithms.NodeConstraintSelection
{
	using System.Collections.Generic;
	using System.Collections.Immutable;

	/// <summary>
	/// Object pooling for <see cref="Scenario"/> objects.
	/// </summary>
	/// <remarks>
	/// Thread safety: Instance members on this class are not thread safe.
	/// </remarks>
	internal class ScenarioPool
	{
		private readonly Stack<Scenario> bag = new Stack<Scenario>();
		private readonly IReadOnlyList<object> nodes;
		private readonly IReadOnlyDictionary<object, int> nodeIndex;

		/// <summary>
		/// Initializes a new instance of the <see cref="ScenarioPool"/> class.
		/// </summary>
		/// <param name="nodes">The nodes in the problem/solution.</param>
		/// <param name="nodeIndex">A map of nodes to their index into <paramref name="nodes"/>.</param>
		internal ScenarioPool(IReadOnlyList<object> nodes, IReadOnlyDictionary<object, int> nodeIndex)
		{
			this.nodes = nodes;
			this.nodeIndex = nodeIndex;
		}

		/// <summary>
		/// Acquires a recycled or new <see cref="Scenario"/> instance.
		/// </summary>
		/// <returns>An instance of <see cref="Scenario"/>.</returns>
		internal Scenario Take()
		{
			if (this.bag.Count > 0)
			{
				return this.bag.Pop();
			}

			return new Scenario(this.nodes, this.nodeIndex);
		}

		/// <summary>
		/// Returns a <see cref="Scenario"/> for recycling.
		/// </summary>
		/// <param name="scenario">The instance to recycle.</param>
		internal void Return(Scenario scenario) => this.bag.Push(scenario);
	}
}
