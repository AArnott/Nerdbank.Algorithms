// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;

namespace Nerdbank.Algorithms.NodeConstraintSelection;

/// <summary>
/// A constraint that verifies that the number of selected nodes within some set of nodes falls within a required range.
/// </summary>
/// <remarks>
/// Thread safety: This class is thread safe.
/// </remarks>
public class SelectionCountConstraint : IConstraint<bool>
{
	/// <summary>
	/// Backing field for the <see cref="Nodes"/> property.
	/// </summary>
	private readonly ImmutableArray<object> nodes;

	/// <summary>
	/// The indexes for each node in <see cref="nodes"/>.
	/// </summary>
	/// <remarks>
	/// This value is lazily initialized by <see cref="GetNodeIndexes"/>.
	/// </remarks>
	private int[]? nodeIndexes;

	/// <summary>
	/// Initializes a new instance of the <see cref="SelectionCountConstraint"/> class.
	/// </summary>
	/// <param name="minSelected">The minimum number of nodes that may be selected.</param>
	/// <param name="maxSelected">The maximum number of nodes that may be selected. If this exceeds the count of <paramref name="nodes"/>, the value will be adjusted to equal the total count.</param>
	/// <param name="nodes">The nodes involved in the constraint.</param>
	public SelectionCountConstraint(ImmutableArray<object> nodes, int minSelected, int maxSelected)
	{
		if (nodes.IsDefault)
		{
			throw new ArgumentNullException(nameof(nodes));
		}

		if (minSelected < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(minSelected), Strings.NonNegativeRequired);
		}

		if (maxSelected < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(maxSelected), Strings.NonNegativeRequired);
		}

		if (maxSelected < minSelected)
		{
			throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Strings.Arg1GreaterThanArg2Required, nameof(maxSelected), nameof(minSelected)));
		}

		this.nodes = nodes;

		if (this.nodes.Length == 0)
		{
			throw new ArgumentException(Strings.ListCannotBeEmpty, nameof(nodes));
		}

		maxSelected = Math.Min(this.nodes.Length, maxSelected);

		this.Minimum = minSelected;
		this.Maximum = maxSelected;
	}

	/// <inheritdoc/>
	public ImmutableArray<object> Nodes => this.nodes;

	/// <summary>
	/// Gets the minimum number of nodes that must be selected.
	/// </summary>
	public int Minimum { get; }

	/// <summary>
	/// Gets the maximum number of nodes that must be selected.
	/// </summary>
	public int Maximum { get; }

	/// <summary>
	/// Creates a new constraint with the specified maximum for nodes that must be selected in the solution.
	/// </summary>
	/// <param name="nodes">The nodes that the constraint applies to.</param>
	/// <param name="maximum">The maximum nodes that must be selected in the solution.</param>
	/// <returns>The new constraint.</returns>
	public static SelectionCountConstraint MaxSelected(ImmutableArray<object> nodes, int maximum) => new(nodes, 0, maximum);

	/// <inheritdoc cref="MaxSelected(ImmutableArray{object}, int)"/>
	public static SelectionCountConstraint MaxSelected(IEnumerable<object> nodes, int maximum) => MaxSelected(nodes.ToImmutableArray(), maximum);

	/// <summary>
	/// Creates a new constraint with the specified minimum for nodes that must be selected in the solution.
	/// </summary>
	/// <param name="nodes">The nodes that the constraint applies to.</param>
	/// <param name="minimum">The minimum nodes that must be selected in the solution.</param>
	/// <returns>The new constraint.</returns>
	public static SelectionCountConstraint MinSelected(ImmutableArray<object> nodes, int minimum) => new(nodes, minimum, int.MaxValue);

	/// <inheritdoc cref="MinSelected(ImmutableArray{object}, int)"/>
	public static SelectionCountConstraint MinSelected(IEnumerable<object> nodes, int minimum) => MinSelected(nodes.ToImmutableArray(), minimum);

	/// <summary>
	/// Creates a new constraint with the specified number of nodes that must be selected in the solution.
	/// </summary>
	/// <param name="nodes">The nodes that the constraint applies to.</param>
	/// <param name="selectedCount">The number of nodes that must be selected in the solution.</param>
	/// <returns>The new constraint.</returns>
	public static SelectionCountConstraint ExactSelected(ImmutableArray<object> nodes, int selectedCount) => new(nodes, selectedCount, selectedCount);

	/// <inheritdoc cref="ExactSelected(ImmutableArray{object}, int)"/>
	public static SelectionCountConstraint ExactSelected(IEnumerable<object> nodes, int selectedCount) => ExactSelected(nodes.ToImmutableArray(), selectedCount);

	/// <summary>
	/// Creates a new constraint with the specified minimum and maximum for nodes that must be selected in the solution.
	/// </summary>
	/// <param name="nodes">The nodes that the constraint applies to.</param>
	/// <param name="minimum">The minimum nodes that must be selected in the solution.</param>
	/// <param name="maximum">The maximum nodes that must be selected in the solution.</param>
	/// <returns>The new constraint.</returns>
	public static SelectionCountConstraint RangeSelected(ImmutableArray<object> nodes, int minimum, int maximum) => new(nodes, minimum, maximum);

	/// <inheritdoc cref="RangeSelected(ImmutableArray{object}, int, int)"/>
	public static SelectionCountConstraint RangeSelected(IEnumerable<object> nodes, int minimum, int maximum) => RangeSelected(nodes.ToImmutableArray(), minimum, maximum);

	/// <inheritdoc/>
	public override string ToString() => $"{this.GetType().Name}({this.Minimum}-{this.Maximum} from {{{string.Join(", ", this.Nodes)}}})";

	/// <inheritdoc/>
	public ConstraintStates GetState(Scenario<bool> scenario)
	{
		if (scenario is null)
		{
			throw new ArgumentNullException(nameof(scenario));
		}

		NodeStats stats = this.GetNodeStates(scenario);
		ConstraintStates states = ConstraintStates.None;

		if (this.IsSatisfiable(stats))
		{
			states |= ConstraintStates.Satisfiable;
			if (this.IsSatisfied(stats))
			{
				states |= ConstraintStates.Satisfied;
			}
		}

		if (this.IsResolved(stats))
		{
			states |= ConstraintStates.Resolved;
		}
		else
		{
			if (this.CanResolveBySelecting(stats) || this.CanResolveByUnselecting(stats))
			{
				states |= ConstraintStates.Resolvable;
			}

			if (this.IsBreakable(stats))
			{
				states |= ConstraintStates.Breakable;
			}
		}

		return states;
	}

	/// <inheritdoc/>
	public bool Resolve(Scenario<bool> scenario)
	{
		if (scenario is null)
		{
			throw new ArgumentNullException(nameof(scenario));
		}

		NodeStats stats = this.GetNodeStates(scenario);

		// If the maximum nodes have already been selected, unselect the rest.
		if (this.CanResolveByUnselecting(stats))
		{
			return this.MarkIndeterminateNodes(scenario, select: false);
		}

		// If so many nodes have been UNselected that the remaining nodes equal the minimum allowed selected nodes, select the rest.
		if (this.CanResolveBySelecting(stats))
		{
			return this.MarkIndeterminateNodes(scenario, select: true);
		}

		// We can't resolve yet.
		return false;
	}

	/// <inheritdoc/>
	public bool Equals(IConstraint<bool>? other) => other is SelectionCountConstraint scc && this.Maximum == scc.Maximum && this.Minimum == scc.Minimum && this.Nodes.Length == scc.Nodes.Length && this.Nodes.SequenceEqual(scc.Nodes);

	/// <summary>
	/// Gets a value indicating whether we have so many UNselected nodes that the remaining nodes equal the minimum allowed selected nodes
	/// and we can resolve by selecting the rest.
	/// </summary>
	/// <param name="stats">The node stats.</param>
	/// <returns><see langword="true"/> if we can resolve by selecting the indeterminate nodes; <see langword="false"/> otherwise.</returns>
	private bool CanResolveBySelecting(in NodeStats stats) => stats.Unselected == this.Nodes.Length - this.Minimum;

	/// <summary>
	/// Gets a value indicating whether the selected nodes equal the max allowable
	/// and we can resolve by UNselecting the rest.
	/// </summary>
	/// <param name="stats">The node stats.</param>
	/// <returns><see langword="true"/> if we can resolve by unselecting the indeterminate nodes; <see langword="false"/> otherwise.</returns>
	private bool CanResolveByUnselecting(in NodeStats stats) => stats.Selected == this.Maximum;

	/// <summary>
	/// Gets a value indicating whether a given scenario already fully satisifies this constraint.
	/// </summary>
	/// <param name="stats">The node selection stats.</param>
	/// <returns>A boolean value.</returns>
	private bool IsSatisfied(in NodeStats stats) => this.Minimum <= stats.Selected && this.Maximum >= stats.Selected;

	/// <summary>
	/// Gets a value indicating whether this constraint may still be satisfied in the future.
	/// </summary>
	/// <param name="stats">The node selection stats.</param>
	/// <returns>A boolean value.</returns>
	private bool IsSatisfiable(in NodeStats stats) => this.Minimum <= stats.Selected + stats.Indeterminate && this.Maximum >= stats.Selected;

	/// <summary>
	/// Gets a value indicating whether every node in this constraint has a determined selection state.
	/// </summary>
	/// <param name="stats">The node selection stats.</param>
	/// <returns>A boolean value.</returns>
	/// <remarks>
	/// This check does not test whether the constraint it actually satisfied.
	/// </remarks>
	private bool IsResolved(in NodeStats stats) => stats.Indeterminate == 0;

	/// <summary>
	/// Gets a value indicating whether this constraint may still be broken in the future.
	/// </summary>
	/// <param name="stats">The node selection stats.</param>
	/// <returns>A boolean value.</returns>
	private bool IsBreakable(in NodeStats stats)
	{
		return

			// it is already broken...
			!this.IsSatisfiable(stats) ||

			// or there are enough indeterminate nodes to not count toward the minimum...
			stats.Selected < this.Minimum ||

			// or the number of selected nodes may yet exceed the maximum...
			stats.Selected + stats.Indeterminate > this.Maximum;
	}

	/// <summary>
	/// Collect aggregate data on the selection state of the nodes involved in this constraint.
	/// </summary>
	/// <param name="scenario">The scenario to consider.</param>
	/// <returns>The aggregate stats.</returns>
	private NodeStats GetNodeStates(Scenario<bool> scenario)
	{
		int selectedCount = 0;
		int unselectedCount = 0;
		int indeterminateCount = 0;
		int[] nodeIndexes = this.nodeIndexes ?? this.GetNodeIndexes(scenario);
		foreach (int nodeIndex in nodeIndexes)
		{
			bool? state = scenario[nodeIndex];
			if (state is bool isSelected)
			{
				if (isSelected)
				{
					selectedCount++;
				}
				else
				{
					unselectedCount++;
				}
			}
			else
			{
				indeterminateCount++;
			}
		}

		return new NodeStats(selectedCount, unselectedCount, indeterminateCount);
	}

	/// <summary>
	/// Mark all indeterminate nodes in a scenario as either selected or unselected.
	/// </summary>
	/// <param name="scenario">The scenario to alter.</param>
	/// <param name="select"><see langword="true"/> to select indeterminate nodes; <see langword="false"/> to unselect them.</param>
	/// <returns><see langword="true"/> if any nodes were actually changed; <see langword="false"/> if there were no indeterminate nodes.</returns>
	private bool MarkIndeterminateNodes(Scenario<bool> scenario, bool select)
	{
		bool changed = false;
		int[] nodeIndexes = this.nodeIndexes ?? this.GetNodeIndexes(scenario);
		foreach (int nodeIndex in nodeIndexes)
		{
			if (!scenario[nodeIndex].HasValue)
			{
				scenario[nodeIndex] = select;
				changed = true;
			}
		}

		return changed;
	}

	/// <summary>
	/// Retrieves an array of the indexes to nodes included in this constraint.
	/// </summary>
	/// <param name="scenario">A scenario from which to derive the indexes if it has not already been cached.</param>
	/// <returns>An array of node indexes.</returns>
	private int[] GetNodeIndexes(Scenario<bool> scenario)
	{
		var nodeIndexes = this.nodeIndexes;
		if (nodeIndexes is null)
		{
			this.nodeIndexes = nodeIndexes = new int[this.nodes.Length];

			for (int i = 0; i < this.nodes.Length; i++)
			{
				nodeIndexes[i] = scenario.GetNodeIndex(this.nodes[i]);
			}
		}

		return nodeIndexes;
	}

	private struct NodeStats
	{
		internal NodeStats(int selected, int unselected, int indeterminate)
		{
			this.Selected = selected;
			this.Unselected = unselected;
			this.Indeterminate = indeterminate;
		}

		internal int Selected { get; }

		internal int Unselected { get; }

		internal int Indeterminate { get; }
	}
}
