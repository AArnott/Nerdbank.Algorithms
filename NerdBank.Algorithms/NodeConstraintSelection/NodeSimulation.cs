using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NerdBank.Algorithms.NodeConstraintSelection {
	/// <summary>
	/// Puts a set of nodes into simulation mode and guarantees that Dispose pops all the 
	/// same nodes out of simulation mode.
	/// </summary>
	public class NodeSimulation : IDisposable {
		/// <summary>
		/// Constructs the node simulator and immediately puts the provided set of nodes 
		/// into simulation mode.
		/// </summary>
		public NodeSimulation(IEnumerable<INode> nodes) {
			if (nodes == null) throw new ArgumentNullException("nodes");

			// To ensure integrity between nodes we push and pop, we need to be sure to 
			// turn the enumerable into an array and push and pop from that array.
			// If we were to keep using the enumerable there's a chance the elements
			// we enumerate over would change from push to pop time.

			this.nodes = nodes.ToArray();
			foreach (INode n in this.nodes) {
				n.PushSimulation();
			}
		}

		INode[] nodes;

		#region IDisposable Members

		public void Dispose() {
			foreach (INode n in this.nodes) {
				n.PopSimulation();
			}
		}

		#endregion
	}
}
