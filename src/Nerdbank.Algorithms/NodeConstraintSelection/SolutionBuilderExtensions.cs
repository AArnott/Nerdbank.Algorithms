// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.Algorithms.NodeConstraintSelection;

/// <summary>
/// Extension methods for the <see cref="SolutionBuilder{TNodeState}"/> class.
/// </summary>
public static class SolutionBuilderExtensions
{
	/// <summary>
	/// Adds a <see cref="SetOneNodeValueConstraint{TNodeState}"/> that forces a given node to a particular value.
	/// </summary>
	/// <typeparam name="TNodeState">The type of value the node can be set to.</typeparam>
	/// <param name="builder">The <see cref="SolutionBuilder{TNodeState}"/> to add a constraint to.</param>
	/// <param name="node">The node to modify.</param>
	/// <param name="value">The value to set on the node.</param>
	/// <returns>The constraint that was created and added to the solution to set the node to the desired state.</returns>
	/// <remarks>
	/// The new constraint's value is not applied to the node immediately.
	/// The caller must use <see cref="SolutionBuilder{TNodeState}.ResolvePartially(System.Threading.CancellationToken)"/> to activate the constraint.
	/// </remarks>
	public static IConstraint<TNodeState> SetNodeState<TNodeState>(this SolutionBuilder<TNodeState> builder, object node, TNodeState value)
		where TNodeState : unmanaged, IEquatable<TNodeState>
	{
		if (builder is null)
		{
			throw new ArgumentNullException(nameof(builder));
		}

		if (node is null)
		{
			throw new ArgumentNullException(nameof(node));
		}

		var constraint = new SetOneNodeValueConstraint<TNodeState>(node, value);
		builder.AddConstraint(constraint);
		return constraint;
	}
}
