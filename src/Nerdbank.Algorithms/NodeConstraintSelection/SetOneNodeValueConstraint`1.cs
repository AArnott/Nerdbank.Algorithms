// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Nerdbank.Algorithms.NodeConstraintSelection;

/// <summary>
/// A constraint that sets a specific node to a specific value.
/// </summary>
/// <typeparam name="TNodeState">The type of value the node can be set to.</typeparam>
public class SetOneNodeValueConstraint<TNodeState> : IConstraint<TNodeState>
	where TNodeState : unmanaged, IEquatable<TNodeState>
{
	/// <summary>
	/// The value to set the <see cref="Node"/> to.
	/// </summary>
	private readonly TNodeState value;

	/// <summary>
	/// Initializes a new instance of the <see cref="SetOneNodeValueConstraint{TNodeState}"/> class.
	/// </summary>
	/// <param name="node">The node whose value is to be set.</param>
	/// <param name="value">The value to set the node to.</param>
	public SetOneNodeValueConstraint(object node, TNodeState value)
	{
		this.value = value;
		this.Nodes = ImmutableArray.Create(node ?? throw new ArgumentNullException(nameof(node)));
	}

	/// <summary>
	/// Gets the one node impacted by this constraint.
	/// </summary>
	public object Node => this.Nodes[0];

	/// <inheritdoc/>
	public ImmutableArray<object> Nodes { get; }

	/// <inheritdoc/>
	public ConstraintStates GetState(Scenario<TNodeState> scenario)
	{
		if (scenario is null)
		{
			throw new ArgumentNullException(nameof(scenario));
		}

		TNodeState? state = scenario[this.Node];
		if (state is null)
		{
			return ConstraintStates.Resolvable | ConstraintStates.Breakable | ConstraintStates.Satisfiable;
		}
		else if (state.Value.Equals(this.value))
		{
			return ConstraintStates.Satisfied | ConstraintStates.Resolved;
		}
		else
		{
			return ConstraintStates.Resolved;
		}
	}

	/// <inheritdoc/>
	public bool Resolve(Scenario<TNodeState> scenario)
	{
		if (scenario is null)
		{
			throw new ArgumentNullException(nameof(scenario));
		}

		if (scenario[this.Node] is null)
		{
			scenario[this.Node] = this.value;
			return true;
		}

		return false;
	}

	/// <inheritdoc/>
	public override string ToString() => $"{this.GetType().Name}(Set {{{this.Node}}} to {this.value})";

	/// <inheritdoc/>
	public bool Equals(IConstraint<TNodeState>? other) => other is SetOneNodeValueConstraint<TNodeState> sonv && this.Node == sonv.Node && this.value.Equals(sonv.value);
}
