// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NerdBank.Algorithms.NodeConstraintSelection
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Globalization;
	using System.Linq;

	[Serializable]
	public class SelectionCountConstraint : ConstraintBase
	{
		/// <summary>
		/// Builds a new constraint that verifies that a given set of nodes
		/// has no more than some given number with their <see cref="INode.IsSelected"/>
		/// state set to some value.
		/// </summary>
		/// <param name="maxNodes">
		/// The maximum number of nodes that can be <paramref name="selectionState"/>
		/// while satisfying the constraint.
		/// </param>
		/// <param name="minNodes">
		/// The minimum number of nodes that must be <paramref name="selectionState"/>
		/// in order to satisfy this constraint.
		/// </param>
		/// <param name="selectionState">
		/// The value of <see cref="INode.IsSelected"/> for nodes to count toward or against this constraint.
		/// </param>
		/// <param name="nodes">The nodes involved in the constraint.</param>
		public SelectionCountConstraint(int minNodes, int maxNodes, bool selectionState, IEnumerable<INode> nodes)
			: base(nodes)
		{
			if (minNodes < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(minNodes), Strings.NonNegativeRequired);
			}

			if (maxNodes < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(maxNodes), Strings.NonNegativeRequired);
			}

			if (maxNodes < minNodes)
			{
				throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Strings.Arg1GreaterThanArg2Required, nameof(maxNodes), nameof(minNodes)));
			}

			if (maxNodes > nodes.Count())
			{
				throw new ArgumentOutOfRangeException(
					nameof(maxNodes),
					string.Format(CultureInfo.CurrentCulture, Strings.Arg1NoGreaterThanElementsInArg2Required, nameof(maxNodes), nameof(nodes)));
			}

			this.selectionState = selectionState;
			this.min = minNodes;
			this.max = maxNodes;
		}

		public static SelectionCountConstraint MaxSelected(int max, IEnumerable<INode> nodes)
		{
			return new SelectionCountConstraint(0, max, true, nodes);
		}

		public static SelectionCountConstraint MinSelected(int min, IEnumerable<INode> nodes)
		{
			return new SelectionCountConstraint(min, nodes.Count(), true, nodes);
		}

		public static SelectionCountConstraint RangeSelected(int minSelected, int maxSelected, IEnumerable<INode> nodes)
		{
			return new SelectionCountConstraint(minSelected, maxSelected, true, nodes);
		}

		public static SelectionCountConstraint ExactSelected(int countSelected, IEnumerable<INode> nodes)
		{
			return RangeSelected(countSelected, countSelected, nodes);
		}

		private readonly bool selectionState;

		/// <summary>
		/// The node state being counted as part of the constraint.
		/// </summary>
		public bool SelectionState
		{
			get { return this.selectionState; }
		}

		private readonly int min;
		private readonly int max;

		/// <summary>
		/// The minimum number of nodes in the nodes collection
		/// with <see cref="INode.IsSelected"/> = <see cref="SelectionState"/>
		/// for this constraint to be satisfied.
		/// </summary>
		public int Min
		{
			get { return this.min; }
		}

		/// <summary>
		/// The maximum number of nodes in the nodes collection
		/// with <see cref="INode.IsSelected"/> = <see cref="SelectionState"/>
		/// for this constraint to be satisfied.
		/// </summary>
		public int Max
		{
			get { return this.max; }
		}

		/// <summary>
		/// Gets all the nodes that have selectionState.
		/// </summary>
		private int countedNodesCount
		{
			get
			{
				return (from n in this.Nodes
						where n.IsSelected.HasValue && n.IsSelected.Value == this.SelectionState
						select n).Count();
			}
		}

		/// <summary>
		/// Gets all the nodes that have !selectionState.
		/// </summary>
		private int discountedNodesCount
		{
			get
			{
				return (from n in this.Nodes
						where n.IsSelected.HasValue && n.IsSelected.Value == !this.SelectionState
						select n).Count();
			}
		}

		/// <summary>
		/// Gets the nodes whose selection status has not yet been determined.
		/// </summary>
		private IEnumerable<INode> indeterminateNodes
		{
			get { return from n in this.Nodes where !n.IsSelected.HasValue select n; }
		}

		public override int PossibleSolutionsCount
		{
			get
			{
				var possibilities = 0;
				var countedNodes = this.countedNodesCount;
				var unknownNodes = this.indeterminateNodes.Count();

				// Iterate through each allowed number of selected nodes and add up the choose possibilities
				// for each one.
				for (var i = this.Min; i <= this.Max; i++)
				{
					// The possibilities for i selected nodes is the n_C_k combination,
					// where n = the nodes left to choose from, and k = the nodes still to be selected.
					possibilities += Choose(unknownNodes, i - countedNodes);
				}

				return possibilities;
			}
		}

		public override IEnumerable<IList<INode>> PossibleSolutions
		{
			get
			{
				// If the constraint is already satisfied, then indicate that no changes is a solution.
				if (this.IsSatisfiable)
				{
					var countedNodes = this.countedNodesCount;
					INode[] unknownNodes = this.indeterminateNodes.ToArray();

					// Iterate through each allowed number of selected nodes and add up the choose possibilities
					// for each one.
					for (var i = this.Min - countedNodes; i <= Math.Min(this.Max - countedNodes, unknownNodes.Length); i++)
					{
						foreach (INode[] result in ChooseResults(unknownNodes, i))
						{
							yield return result;
						}
					}
				}
			}
		}

		public override string ToString()
		{
			return string.Format(
				CultureInfo.CurrentCulture,
				"{0}({1}, {2}, {3}, [{4}])",
				typeof(SelectionCountConstraint).Name,
				this.Min,
				this.Max,
				this.SelectionState,
				string.Join(", ", this.Nodes.Select(n => n.ToString()).ToArray()));
		}

		public override bool CanResolve
		{
			get
			{
				return this.IsSatisfiable && !this.IsResolved && (this.countedNodesCount == this.Max || this.discountedNodesCount == this.Nodes.Count() - this.Min);
			}
		}

		public override bool Resolve()
		{
			this.ThrowIfBroken();
			if (this.IsResolved)
			{
				return false;
			}

			if (this.countedNodesCount == this.Max)
			{
				foreach (INode n in this.indeterminateNodes)
				{
					n.IsSelected = !this.SelectionState;
				}
			}
			else if (this.discountedNodesCount == this.Nodes.Count() - this.Min)
			{
				foreach (INode n in this.indeterminateNodes)
				{
					n.IsSelected = this.SelectionState;
				}
			}
			else
			{
				return false;
			}

			Debug.Assert(this.IsResolved, "IsResolved");
			return true;
		}

		public override bool IsSatisfied
		{
			get { return this.Min <= this.countedNodesCount && this.countedNodesCount <= this.Max; }
		}

		public override bool IsSatisfiable
		{
			get { return this.Min <= this.countedNodesCount + this.indeterminateNodes.Count() && this.countedNodesCount <= this.Max; }
		}

		public override bool IsBreakable
		{
			get
			{
				return

					// it is already broken...
					this.IsBroken ||

					// or there are enough indeterminate nodes to not count toward the minimum...
					this.countedNodesCount < this.Min ||

					// or the number of selected nodes may yet exceed the maximum...
					this.countedNodesCount + this.indeterminateNodes.Count() > this.Max;
			}
		}

		public override bool IsMinimized
		{
			get { return this.Max == this.Nodes.Count() && this.Min == this.Max; }
		}

		public override bool IsWorthwhile
		{
			get { return !this.IsWorthless; }
		}

		public override bool IsWorthless
		{
			get { return this.Min == 0 && this.Max >= this.Nodes.Count(); }
		}
	}
}
