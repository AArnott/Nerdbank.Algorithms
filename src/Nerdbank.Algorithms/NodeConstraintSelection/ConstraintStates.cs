// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.Algorithms.NodeConstraintSelection;

/// <summary>
/// Describes the present state of a constraint.
/// </summary>
[Flags]
public enum ConstraintStates
{
	/// <summary>
	/// The lack of any other flags.
	/// </summary>
	None = 0x0,

	/// <summary>
	/// Indicates that the constraint is satisfied given the current node state or may be satisfied if certain indeterminate nodes resolve to particular states.
	/// </summary>
	Satisfiable = 0x1,

	/// <summary>
	/// Indicates that the constraint is satisfied given the nodes that are already selected or unselected.
	/// </summary>
	Satisfied = Satisfiable | 0x2,

	/// <summary>
	/// Indicates that the constraint can set one or more indeterminate nodes as selected or unselected.
	/// </summary>
	Resolvable = 0x4,

	/// <summary>
	/// Indicates that none nodes in the constraint are in an indeterminate state.
	/// </summary>
	Resolved = 0x8,

	/// <summary>
	/// Indicates that changes to indeterminate nodes could render the constraint broken.
	/// </summary>
	Breakable = 0x10,
}
