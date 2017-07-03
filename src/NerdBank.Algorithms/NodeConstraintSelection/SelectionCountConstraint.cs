using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NerdBank.Algorithms.NodeConstraintSelection {
	[Serializable]
	public class SelectionCountConstraint : ConstraintBase {
		/// <summary>
		/// Builds a new constraint that verifies that a given set of nodes
		/// has no more than some given number with their <see cref="INode.Selected"/>
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
			: base(nodes) {
			if (minNodes < 0) throw new ArgumentOutOfRangeException("minNodes", Strings.NonNegativeRequired);
			if (maxNodes < 0) throw new ArgumentOutOfRangeException("maxNodes", Strings.NonNegativeRequired);
			if (maxNodes < minNodes) throw new ArgumentException(string.Format(Strings.Arg1GreaterThanArg2Required, "maxNodes", "minNodes"));
			if (maxNodes > nodes.Count()) throw new ArgumentOutOfRangeException("maxNodes", string.Format(
														Strings.Arg1NoGreaterThanElementsInArg2Required, "maxNodes", "nodes"));
			this.selectionState = selectionState;
			this.min = minNodes;
			this.max = maxNodes;
		}
		public static SelectionCountConstraint MaxSelected(int max, IEnumerable<INode> nodes) {
			return new SelectionCountConstraint(0, max, true, nodes);
		}
		public static SelectionCountConstraint MinSelected(int min, IEnumerable<INode> nodes) {
			return new SelectionCountConstraint(min, nodes.Count(), true, nodes);
		}
		public static SelectionCountConstraint RangeSelected(int minSelected, int maxSelected, IEnumerable<INode> nodes) {
			return new SelectionCountConstraint(minSelected, maxSelected, true, nodes);
		}
		public static SelectionCountConstraint ExactSelected(int countSelected, IEnumerable<INode> nodes) {
			return RangeSelected(countSelected, countSelected, nodes);
		}

		readonly bool selectionState;
		/// <summary>
		/// The node state being counted as part of the constraint.
		/// </summary>
		public bool SelectionState {
			get { return selectionState; }
		}
		readonly int min, max;
		/// <summary>
		/// The minimum number of nodes in the <see cref="Nodes"/> collection
		/// with <see cref="Node.Selected"/> = <see cref="SelectionState"/>
		/// for this constraint to be satisfied.
		/// </summary>
		public int Min {
			get { return min; }
		}
		/// <summary>
		/// The maximum number of nodes in the <see cref="Nodes"/> collection
		/// with <see cref="Node.Selected"/> = <see cref="SelectionState"/>
		/// for this constraint to be satisfied.
		/// </summary>
		public int Max {
			get { return max; }
		}

		/// <summary>
		/// Gets all the nodes that have selectionState.
		/// </summary>
		int countedNodesCount {
			get {
				return (from n in Nodes
						where n.IsSelected.HasValue && n.IsSelected.Value == SelectionState
						select n).Count();
			}
		}
		/// <summary>
		/// Gets all the nodes that have !selectionState.
		/// </summary>
		int discountedNodesCount {
			get {
				return (from n in Nodes
						where n.IsSelected.HasValue && n.IsSelected.Value == !SelectionState
						select n).Count();
			}
		}
		/// <summary>
		/// Gets the nodes whose selection status has not yet been determined.
		/// </summary>
		IEnumerable<INode> indeterminateNodes {
			get { return from n in Nodes where !n.IsSelected.HasValue select n; }
		}

		public override int PossibleSolutionsCount {
			get {
				int possibilities = 0;
				int countedNodes = countedNodesCount;
				int unknownNodes = indeterminateNodes.Count();
				// Iterate through each allowed number of selected nodes and add up the choose possibilities
				// for each one.
				for (int i = Min; i <= Max; i++) {
					// The possibilities for i selected nodes is the n_C_k combination,
					// where n = the nodes left to choose from, and k = the nodes still to be selected.
					possibilities += Choose(unknownNodes, i - countedNodes);
				}
				return possibilities;
			}
		}
		public override IEnumerable<IList<INode>> PossibleSolutions {
			get {
				// If the constraint is already satisfied, then indicate that no changes is a solution.
				if (IsSatisfiable) {
					int countedNodes = countedNodesCount;
					var unknownNodes = indeterminateNodes.ToArray();
					// Iterate through each allowed number of selected nodes and add up the choose possibilities
					// for each one.
					for (int i = Min - countedNodes; i <= Math.Min(Max - countedNodes, unknownNodes.Length); i++) {
						foreach (var result in ChooseResults(unknownNodes, i))
							yield return result;
					}
				}
			}
		}

		public override string ToString() {
			return string.Format("{0}({1}, {2}, {3}, [{4}])",
				typeof(SelectionCountConstraint).Name,
				Min, Max, SelectionState, string.Join(", ", Nodes.Select(n => n.ToString()).ToArray()));
		}

		#region IConstraint Members

		public override bool CanResolve {
			get {
				return IsSatisfiable && !IsResolved && (countedNodesCount == Max || discountedNodesCount == Nodes.Count() - Min);
			}
		}

		public override bool Resolve() {
			ThrowIfBroken();
			if (IsResolved) return false;
			if (countedNodesCount == Max) {
				foreach (INode n in indeterminateNodes)
					n.IsSelected = !SelectionState;
			} else if (discountedNodesCount == Nodes.Count() - Min) {
				foreach (INode n in indeterminateNodes)
					n.IsSelected = SelectionState;
			} else
				return false;

			Debug.Assert(IsResolved);
			return true;
		}

		public override bool IsSatisfied {
			get { return Min <= countedNodesCount && countedNodesCount <= Max; }
		}

		public override bool IsSatisfiable {
			get { return Min <= countedNodesCount + indeterminateNodes.Count() && countedNodesCount <= Max; }
		}

		public override bool IsBreakable {
			get {
				return
					// it is already broken...
					IsBroken ||
					// or there are enough indeterminate nodes to not count toward the minimum...
					countedNodesCount < Min ||
					// or the number of selected nodes may yet exceed the maximum...
					countedNodesCount + indeterminateNodes.Count() > Max;
			}
		}

		public override bool IsMinimized {
			get { return Max == Nodes.Count() && Min == Max; }
		}

		public override bool IsWorthwhile {
			get { return !IsWorthless; }
		}

		public override bool IsWorthless {
			get { return Min == 0 && Max >= Nodes.Count(); }
		}

		#endregion
	}
}
