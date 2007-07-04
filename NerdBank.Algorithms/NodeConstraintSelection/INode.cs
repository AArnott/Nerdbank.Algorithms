using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NerdBank.Algorithms.NodeConstraintSelection {
	public interface INode : IComparable, INotifyPropertyChanged {
		/// <summary>
		/// Gets/sets the selection state of a node.
		/// </summary>
		/// <remarks>
		/// Once a node's state is determined (changes from null to either true or false),
		/// it cannot be changed again.  Call <see cref="Reset"/> to revert back to the null state.
		/// </remarks>
		bool? IsSelected { get; set; }
		/// <summary>
		/// Forces the node back into its indeterminate state.
		/// </summary>
		void Reset();
		/// <summary>
		/// Gets whether the node is currently in a simulation mode.
		/// </summary>
		bool IsSimulating { get; }
		/// <summary>
		/// Pushes one level deeper in the simulation.
		/// </summary>
		void PushSimulation();
		/// <summary>
		/// Pops off the deepest simulation level.
		/// </summary>
		/// <returns>
		/// True if the simulation level is 0 (no simulation).
		/// </returns>
		/// <exception cref="InvalidOperationException">
		/// Thrown when no simulation is on the stack.
		/// </exception>
		bool PopSimulation();
	}
}
