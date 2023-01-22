// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.Algorithms.NodeConstraintSelection;

/// <content>
/// Contains the <see cref="SolutionsAnalysis"/> nested type.
/// </content>
public partial class SolutionBuilder<TNodeState>
{
	/// <summary>
	/// Describes the set of viable solutions available.
	/// </summary>
	public class SolutionsAnalysis
	{
		private readonly SolutionBuilder<TNodeState> owner;

		/// <summary>
		/// The count that each state appears in a viable solution for each node.
		/// </summary>
		private readonly Dictionary<TNodeState, long>?[]? nodeValueCount;

		/// <summary>
		/// Initializes a new instance of the <see cref="SolutionsAnalysis"/> class.
		/// </summary>
		/// <param name="owner">The <see cref="SolutionBuilder{T}"/> that created this instance.</param>
		/// <param name="viableSolutionsFound">The number of viable solutions that exist.</param>
		/// <param name="nodeValueCount">The number of times each value was used for a given node in any viable solution.</param>
		/// <param name="conflicts">Information about the conflicting constraints that prevent any viable solution from being found.</param>
		internal SolutionsAnalysis(SolutionBuilder<TNodeState> owner, long viableSolutionsFound, Dictionary<TNodeState, long>?[]? nodeValueCount, SolutionBuilder<TNodeState>.ConflictedConstraints? conflicts)
		{
			this.owner = owner;
			this.ViableSolutionsFound = viableSolutionsFound;
			this.nodeValueCount = nodeValueCount;
			this.Conflicts = conflicts;
		}

		/// <summary>
		/// Gets the number of viable solutions that exist.
		/// </summary>
		public long ViableSolutionsFound { get; }

		/// <summary>
		/// Gets information about the conflicting constraints that prevent any viable solution from being found.
		/// </summary>
		/// <value>Null if there were no conflicting constraints.</value>
		public SolutionBuilder<TNodeState>.ConflictedConstraints? Conflicts { get; }

		/// <summary>
		/// Gets the number of solutions in which a given node had a given value.
		/// </summary>
		/// <param name="nodeIndex">The index of the node of interest.</param>
		/// <param name="value">The value of the node for which a count is requested.</param>
		/// <returns>
		/// The number of viable solutions where the node at <paramref name="nodeIndex"/> was assigned the given <paramref name="value"/>.
		/// May be -1 if the node at <paramref name="nodeIndex"/> is not constrained by anything and therefore can be anything in any solution.
		/// </returns>
		/// <exception cref="IndexOutOfRangeException">Thrown if <paramref name="nodeIndex"/> is negative or exceeds the number of nodes in the solution.</exception>
		public long GetNodeValueCount(int nodeIndex, TNodeState value)
		{
			if (this.nodeValueCount is { } valueAndCounts)
			{
				if (valueAndCounts[nodeIndex] is { } counts)
				{
					counts.TryGetValue(value, out long count);
					return count;
				}
				else
				{
					return -1;
				}
			}
			else
			{
				throw new InvalidOperationException(Strings.ViableSolutionStatsNotAvailable);
			}
		}

		/// <summary>
		/// Gets the number of solutions in which a given node had a given value.
		/// </summary>
		/// <param name="node">The node of interest.</param>
		/// <param name="value">The value of the node for which a count is requested.</param>
		/// <returns>
		/// The number of viable solutions where the <paramref name="node"/> was assigned the given <paramref name="value"/>.
		/// May be -1 if the <paramref name="node"/> is not constrained by anything and therefore can be anything in any solution.
		/// </returns>
		/// <exception cref="KeyNotFoundException">Thrown if the <paramref name="node"/> is not among the nodes in the solution.</exception>
		public long GetNodeValueCount(object node, TNodeState value) => this.GetNodeValueCount(this.owner.CurrentScenario.GetNodeIndex(node), value);

		/// <summary>
		/// Select or unselect any indeterminate nodes which are unconditionally in just one state in all viable solutions.
		/// </summary>
		/// <remarks>
		/// Constraints can interact as they narrow the field of viable solutions such that
		/// some nodes may be effectively constrained to a certain value even though that value
		/// isn't explicitly required by any individual constraint.
		/// When these interactions are understood, applying them back to the <see cref="SolutionBuilder{T}"/>
		/// can speed up future analyses and lead each node's value to reflect the remaining possibilities.
		/// </remarks>
		public void ApplyAnalysisBackToBuilder()
		{
			if (this.ViableSolutionsFound == 0 || this.nodeValueCount is null)
			{
				throw new InvalidOperationException(Strings.ViableSolutionStatsNotAvailable);
			}

			if (this.ViableSolutionsFound > 0)
			{
				for (int i = 0; i < this.nodeValueCount.Length; i++)
				{
					if (this.owner.CurrentScenario[i] is null && this.nodeValueCount[i] is { } valuesAndCounts)
					{
						foreach (TNodeState value in this.owner.resolvedNodeStates)
						{
							if (valuesAndCounts.TryGetValue(value, out long counts) && counts == this.ViableSolutionsFound)
							{
								this.owner.CurrentScenario[i] = value;
								break;
							}
						}
					}
				}
			}
		}
	}
}
