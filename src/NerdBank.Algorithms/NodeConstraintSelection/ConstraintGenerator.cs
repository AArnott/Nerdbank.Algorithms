// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NerdBank.Algorithms.NodeConstraintSelection
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using NodeSolution = System.Collections.Generic.Dictionary<NerdBank.Algorithms.NodeConstraintSelection.INode, bool>;

	public class ConstraintGenerator
	{
		[Flags]
		private enum NodePossibility
		{
			None = 0x0,
			TriedSelectedInSolution = 0x1,
			FoundSelectedInSolution = TriedSelectedInSolution | 0x2,
			TriedUnselectedInSolution = 0x4,
			FoundUnselectedInSolution = TriedUnselectedInSolution | 0x8,
		}

		private CompositeConstraint cc;

		/// <summary>
		/// A dictionary of nodes and whether they are selected in any remaining solutions.
		/// </summary>
		private Dictionary<INode, NodePossibility> nodeSolutionPossibilities;
		private bool trySelectingNodes;
		private bool tryUnselectingNodes;
		private INode[] indeterminateNodes;

		private ConstraintGenerator(IEnumerable<IConstraint> constraints, bool trySelectingNodes, bool tryUnselectingNodes)
		{
			this.cc = new CompositeConstraint(constraints);
			this.trySelectingNodes = trySelectingNodes;
			this.tryUnselectingNodes = tryUnselectingNodes;

			if (this.cc.IsBroken)
			{
				throw new BrokenConstraintException();
			}

			this.indeterminateNodes = this.cc.Nodes.Where(n => !n.IsSelected.HasValue).ToArray();

			this.nodeSolutionPossibilities = new Dictionary<INode, NodePossibility>(this.indeterminateNodes.Length);
			foreach (INode n in this.indeterminateNodes)
			{
				this.nodeSolutionPossibilities.Add(n, NodePossibility.None);
			}

			// Set the node affinity to always try an unproven path when searching for a solution.
			// This logic says to always try selecting the node first, unless both the selected and unselected states
			// are being searched in which case try selecting the node until we already know the answer to that.
			this.cc.NodeAffinity = n => (trySelectingNodes && tryUnselectingNodes) ?
				(this.nodeSolutionPossibilities[n] & NodePossibility.TriedSelectedInSolution) == NodePossibility.None : trySelectingNodes;
		}

		/// <summary>
		/// Analyzes a set of constraints and generates any additional constraints that may be inferred.
		/// </summary>
		/// <returns>The sequence of additional constraints.</returns>
		/// <remarks>
		/// The strategy is to enumerate through each indeterminate node and experiment with each of its
		/// possible selection states.  With each value, we shake out the repercussions of the selection
		/// and notice if it ends up with the constraints in a broken state.  If so, then we know the node
		/// must adopt the opposite selection state.
		/// </remarks>
		public static IEnumerable<IConstraint> GenerateDeducedConstraints(IEnumerable<IConstraint> constraints, bool trySelectingNodes, bool tryUnselectingNodes)
		{
			if (constraints == null)
			{
				throw new ArgumentNullException(nameof(constraints));
			}

			if (!trySelectingNodes && !tryUnselectingNodes)
			{
				throw new ArgumentException(string.Format(
					CultureInfo.CurrentCulture,
					Strings.AtLeastOneOfTwoArgumentsMustBeSet,
					nameof(trySelectingNodes),
					nameof(tryUnselectingNodes),
					true.ToString(CultureInfo.CurrentCulture)));
			}

			var cg = new ConstraintGenerator(constraints, trySelectingNodes, tryUnselectingNodes);
			return cg.deduceConstraints();
		}

		private IEnumerable<IConstraint> deduceConstraints()
		{
			// First ensure that there is a solution at all
			NodeSolution? solution = this.cc.FindOnePossibleSolution();
			if (solution == null)
			{
				throw new BrokenConstraintException();
			}

			// As long as we went to the trouble of finding a solution, let's learn all we can from it.
			this.learnFromSolution(solution);

			// Now drill down and learn everything we need to in order to make some good deductions.
			if (this.trySelectingNodes)
			{
				this.findAllSelectionPossibilities(true);
			}

			if (this.tryUnselectingNodes)
			{
				this.findAllSelectionPossibilities(false);
			}

			// Now make as many deductions as possible.
			var nodesForcedSelected = new List<INode>();
			var nodesForcedUnselected = new List<INode>();
			foreach (KeyValuePair<INode, NodePossibility> pair in this.nodeSolutionPossibilities)
			{
				if ((pair.Value & NodePossibility.FoundSelectedInSolution) == NodePossibility.TriedSelectedInSolution)
				{
					// We tried selecting this node and couldn't find any solutions, so it must be unselected in
					// every remaining solution.
					nodesForcedUnselected.Add(pair.Key);
				}
				else if ((pair.Value & NodePossibility.FoundUnselectedInSolution) == NodePossibility.TriedUnselectedInSolution)
				{
					// We tried unselecting this node and couldn't find any solutions, so it must be selected in
					// every remaining solution.
					nodesForcedSelected.Add(pair.Key);
				}
			}

			if (nodesForcedSelected.Count > 0)
			{
				yield return SelectionCountConstraint.ExactSelected(nodesForcedSelected.Count, nodesForcedSelected);
			}

			if (nodesForcedUnselected.Count > 0)
			{
				yield return SelectionCountConstraint.ExactSelected(0, nodesForcedUnselected);
			}
		}

		private INode findNodeWithUnknownSolutionPossibility(bool selectedPossibility)
		{
			NodePossibility possibility = selectedPossibility ? NodePossibility.TriedSelectedInSolution : NodePossibility.TriedUnselectedInSolution;
			return this.nodeSolutionPossibilities.FirstOrDefault(p => (p.Value & possibility) == NodePossibility.None).Key;
		}

		private void findAllSelectionPossibilities(bool selectionStateToCheck)
		{
			INode focusedNode;
			while ((focusedNode = this.findNodeWithUnknownSolutionPossibility(selectionStateToCheck)) != null)
			{
				using (var sim = new NodeSimulation(this.indeterminateNodes))
				{
					// Force the focusedNode into being selected or unselected (depending on argument) and search for a solution.
					focusedNode.IsSelected = selectionStateToCheck;
					NodeSolution? solution = this.cc.FindOnePossibleSolution();
					if (solution != null)
					{
						this.learnFromSolution(solution);
					}
					else
					{
						// Since we could not find a single solution, the selection state we tried for this node must be out of the
						// set of possible solutions.
						// Record the attempt for just the focused node.
						this.nodeSolutionPossibilities[focusedNode] |= selectionStateToCheck ? NodePossibility.TriedSelectedInSolution : NodePossibility.TriedUnselectedInSolution;

						// Since we already know that a solution exists to the actual problem, it's safe to assume that focusedNode
						// MUST be able to be in the opposite selection state, since the one we tried is not possible.
						// This little optimization can save us time if both selected and unselected states for each node are being sought.
						this.nodeSolutionPossibilities[focusedNode] |= !selectionStateToCheck ? NodePossibility.FoundSelectedInSolution : NodePossibility.FoundUnselectedInSolution;
					}
				}
			}
		}

		private void learnFromSolution(NodeSolution solution)
		{
			if (solution == null)
			{
				throw new ArgumentNullException(nameof(solution));
			}

			// Since we found a solution, record ALL nodes' selection states -- not just the one we were specifically testing.
			// By doing this, we avoid having to redo work later to check the other nodes' possible selection states.
			foreach (KeyValuePair<INode, bool> pair in solution.Where(p => this.nodeSolutionPossibilities.ContainsKey(p.Key)))
			{
				this.nodeSolutionPossibilities[pair.Key] |= pair.Value ? NodePossibility.FoundSelectedInSolution : NodePossibility.FoundUnselectedInSolution;
			}
		}
	}
}
