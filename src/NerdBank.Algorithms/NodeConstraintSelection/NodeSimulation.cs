// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NerdBank.Algorithms.NodeConstraintSelection
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// Puts a set of nodes into simulation mode and guarantees that Dispose pops all the
	/// same nodes out of simulation mode.
	/// </summary>
	public class NodeSimulation : IDisposable
	{
		/// <summary>
		/// Constructs the node simulator and immediately puts the provided set of nodes
		/// into simulation mode.
		/// </summary>
		public NodeSimulation(IEnumerable<INode> nodes)
		{
			if (nodes == null)
			{
				throw new ArgumentNullException(nameof(nodes));
			}

			// To ensure integrity between nodes we push and pop, we need to be sure to
			// turn the enumerable into an array and push and pop from that array.
			// If we were to keep using the enumerable there's a chance the elements
			// we enumerate over would change from push to pop time.
			this.nodes = nodes.ToArray();
			foreach (INode n in this.nodes)
			{
				n.PushSimulation();
			}
		}

		private INode[] nodes;

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			foreach (INode n in this.nodes)
			{
				n.PopSimulation();
			}
		}
	}
}
