using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NerdBank.Algorithms.NodeConstraintSelection {
	public interface IConstraint {
		/// <summary>
		/// Gets the set of nodes being constrained.
		/// </summary>
		IEnumerable<INode> Nodes { get; }
		/// <summary>
		/// Whether every node in this constraint has a determined selection state.
		/// </summary>
		bool IsResolved { get; }
		/// <summary>
		/// Whether the group of remaining indeterminate nodes (if any) has exactly one
		/// determinate state left that would satisfy this constraint.
		/// </summary>
		bool CanResolve { get; }
		/// <summary>
		/// Forces all related Nodes into a determinate state consistent with
		/// this constraint, if there is only one state left that the indeterminate
		/// ones can be in while keeping this constraint satisfied.
		/// </summary>
		/// <returns>
		/// Whether the operation was successful, and all related nodes are now resolved.
		/// </returns>
		bool Resolve();
		/// <summary>
		/// Whether this constraint is satisfied.
		/// </summary>
		bool IsSatisfied { get; }
		/// <summary>
		/// Whether this constraint may still be satisfied in the future.
		/// </summary>
		bool IsSatisfiable { get; }
		/// <summary>
		/// Whether this constraint is already irrepairably broken.
		/// </summary>
		bool IsBroken { get; }
		/// <summary>
		/// Whether this constraint may still be broken in the future.
		/// </summary>
		bool IsBreakable { get; }
		/// <summary>
		/// Whether this constraint is just as tight as possible.
		/// </summary>
		bool IsMinimized { get; }
		/// <summary>
		/// Whether this constraint contains any useful information.
		/// </summary>
		bool IsWorthwhile { get; }
		/// <summary>
		/// Whether this constraint can be discarded without any information loss.
		/// </summary>
		bool IsWorthless { get; }
	}
}
