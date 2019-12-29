// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.Algorithms.NodeConstraintSelection
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;

	/// <summary>
	/// A constraint that sets a specific node to a specific value.
	/// </summary>
	/// <typeparam name="TNodeState">The type of value the node can be set to.</typeparam>
	public class SetOneNodeValueConstraint<TNodeState> : IConstraint<TNodeState>
		where TNodeState : struct, IEquatable<TNodeState>
	{
		/// <summary>
		/// The node whose <see cref="value"/> is to be set.
		/// </summary>
		private readonly object node;

		/// <summary>
		/// The value to set the <see cref="node"/> to.
		/// </summary>
		private readonly TNodeState value;

		/// <summary>
		/// Initializes a new instance of the <see cref="SetOneNodeValueConstraint{TNodeState}"/> class.
		/// </summary>
		/// <param name="node">The node whose value is to be set.</param>
		/// <param name="value">The value to set the node to.</param>
		public SetOneNodeValueConstraint(object node, TNodeState value)
		{
			this.node = node ?? throw new ArgumentNullException(nameof(node));
			this.value = value;
			this.Nodes = ImmutableArray.Create(node);
		}

		/// <inheritdoc/>
		public IReadOnlyCollection<object> Nodes { get; }

		/// <inheritdoc/>
		public ConstraintStates GetState(Scenario<TNodeState> scenario)
		{
			if (scenario is null)
			{
				throw new ArgumentNullException(nameof(scenario));
			}

			TNodeState? state = scenario[this.node];
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

			if (scenario[this.node] is null)
			{
				scenario[this.node] = this.value;
				return true;
			}

			return false;
		}

		/// <inheritdoc/>
		public override string ToString() => $"{this.GetType().Name}(Set {{{this.node}}} to {this.value})";
	}
}
