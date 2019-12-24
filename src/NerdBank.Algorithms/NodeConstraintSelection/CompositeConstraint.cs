// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NerdBank.Algorithms.NodeConstraintSelection
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using NodeSolution = System.Collections.Generic.Dictionary<NerdBank.Algorithms.NodeConstraintSelection.INode, bool>;

	public class CompositeConstraint : IConstraint
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CompositeConstraint"/> class.
		/// </summary>
		/// <param name="constraints"></param>
		public CompositeConstraint(IEnumerable<IConstraint> constraints)
		{
			if (constraints == null)
			{
				throw new ArgumentNullException(nameof(constraints));
			}

			if (!constraints.Any())
			{
				throw new ArgumentException(Strings.ListCannotBeEmpty, nameof(constraints));
			}

			this.constraints = constraints;
		}

		private Func<INode, bool>? nodeAffinity;

		/// <summary>
		/// While searching for solutions, if the caller prefers solutions with certain nodes set to certain
		/// states, this function can provide the desired state.
		/// Note that this does not guarantee the solution found will have the node in the preferred state.
		/// </summary>
		public Func<INode, bool>? NodeAffinity
		{
			get
			{
				return this.nodeAffinity ?? (n => true); // default to trying to select nodes first.
			}

			set
			{
				this.nodeAffinity = value;
			}
		}

		private readonly IEnumerable<IConstraint> constraints;

		public IEnumerable<IConstraint> Constraints
		{
			get { return this.constraints; }
		}

		private INode[]? sortedNodes;

		private INode[] SortedNodes
		{
			get
			{
				if (this.sortedNodes == null)
				{
					this.sortedNodes = this.Nodes.ToArray();
					Array.Sort(this.sortedNodes);
				}

				return this.sortedNodes;
			}
		}

		/// <summary>
		/// Whether any contained constraints can resolve.
		/// </summary>
		public bool CanResolvePartially
		{
			get { return this.constraints.Any(c => c.CanResolve); }
		}

		/// <summary>
		/// Resolves any contained constraints that can be.
		/// </summary>
		/// <returns>
		/// Whether any resolving took place.
		/// </returns>
		/// <remarks>
		/// Whenever a constraint resolves its nodes, other constraints can potentially be affected.
		/// Because of this, this method gives all unresolved constraints a chance to resolve repeatedly
		/// until none of them can be resolved.
		/// </remarks>
		public bool ResolvePartially()
		{
			var anyResolved = false;
			while (this.constraints.Any(c => c.CanResolve && c.Resolve()))
			{
				anyResolved = true;
			}

			return anyResolved;
		}

		/// <summary>
		/// Whether any contained constraint is broken, not taking into account mutually exclusive constraint interactions.
		/// </summary>
		private bool isBrokenShallow
		{
			get
			{
				return this.constraints.Any(c => c.IsBroken);
			}
		}

		/// <summary>
		/// Searches for just one possible solution to the current state.
		/// </summary>
		/// <returns>
		/// A dictionary of nodes and the state they were in for the solution found,
		/// or null if no solution exists given the current state of nodes.
		/// </returns>
		internal NodeSolution? FindOnePossibleSolution()
		{
			return this.findOnePossibleSolution(null);
		}

		/// <summary>
		/// Finds whether there is actually a solution given the current state of nodes (which may
		/// already be in simulation mode), rather than just test whether the contained constraints
		/// can individually be satisfied, which leaves error where the constraints may be mutually exclusive.
		/// </summary>
		private NodeSolution? findOnePossibleSolution(INode? onlyNodesAfter)
		{
			if (this.IsSatisfied && this.IsResolved)
			{
				////Debug.Write("(");
				////foreach (INode node in Nodes.Where(n => n.IsSelected.HasValue && n.IsSelected.Value)) {
				////    Debug.Write(node.ToString());
				////}
				////Debug.Write(") ~(");
				////foreach (INode node in Nodes.Where(n => n.IsSelected.HasValue && !n.IsSelected.Value)) {
				////    Debug.Write(node.ToString());
				////}
				////Debug.WriteLine(") satisfied");

				// Construct a dictionary with the solution.
				var solution = new NodeSolution(this.Nodes.Count());
				foreach (INode node in this.Nodes)
				{
					solution[node] = node.IsSelected.Value;
				}

				return solution;
			}

			if (this.isBrokenShallow)
			{
				////Debug.Write("(");
				////foreach (INode node in Nodes.Where(n => n.IsSelected.HasValue && n.IsSelected.Value)) {
				////    Debug.Write(node.ToString());
				////}
				////Debug.Write(") ~(");
				////foreach (INode node in Nodes.Where(n => n.IsSelected.HasValue && !n.IsSelected.Value)) {
				////    Debug.Write(node.ToString());
				////}
				////Debug.WriteLine(") broken");
				return null;
			}

			IEnumerable<INode> indeterminateNodes = this.SortedNodes.Where(n => !n.IsSelected.HasValue);
			INode[] indeterminateNodesArray = indeterminateNodes.ToArray();
			INode testNode = indeterminateNodesArray.FirstOrDefault(n => onlyNodesAfter == null || n.CompareTo(onlyNodesAfter) > 0);
			if (testNode == null)
			{
				return null;
			}

			// Consider the next indeterminate node's selection states, deeply.
			var firstTestNodeState = this.NodeAffinity(testNode);
			return this.findOnePossibleSolution(testNode, indeterminateNodesArray, firstTestNodeState) ??
				this.findOnePossibleSolution(testNode, indeterminateNodesArray, !firstTestNodeState);
		}

		/// <summary>
		/// Simulates an individual node's selection state and tests whether it could lead to a solution to the game.
		/// </summary>
		/// <returns>Whether the given node and selection state leads to a valid solution.</returns>
		private NodeSolution? findOnePossibleSolution(INode testNode, INode[] indeterminateNodes, bool testState)
		{
			// Future optimization: Only recurse into testing further nodes that share a constraint with the testNode.
			// Reasoning:  I'm interested in whether setting testNode can invalidate a future solution.
			//             The only constraints that testNode could possibly invalidate are the ones that contain testNode.
			//             Therefore instead of testing _every_ indeterminate node recursively after setting this node,
			//             all we need to do is test each "nearby" node and see that no constraints have been broken.
			// Discussion: But when the affected constraints are partially resolved though, the "nearby" nodes will be affected.
			//             Since those nodes could potentially belong to additional constraints which will also partially
			//             resolve and affect others in a daisy chain effect,
			//             Will we be missing important cascade effects if we implement this optimization???
			////var constraintsContainingTestNode = new CompositeConstraint(Constraints.Where(c => c.Nodes.Contains(testNode)));
			////IEnumerable<INode> testNodeAndCousins = constraintsContainingTestNode.Nodes;

			Debug.Assert(indeterminateNodes.Contains(testNode), "indeterminateNodes.Contains(testNode)");
			using (var sim = new NodeSimulation(indeterminateNodes))
			{
				testNode.IsSelected = testState;
				this.ResolvePartially();
				////INode[] indeterminateNodesNow = Nodes.Where(n => !n.IsSelected.HasValue).ToArray();
				////INode[] resolvedNodes = indeterminateNodes.Where(n => !indeterminateNodesNow.Contains(n)).ToArray();
				////Debug.WriteLine("Simulated resolved nodes: " + string.Join(", ", resolvedNodes.Select(n => n.ToString()).ToArray()));
				NodeSolution? result = this.findOnePossibleSolution(testNode);
				////Debug.WriteLine(string.Format("Node {0} simulated to be {1} and {2}", testNode, testState, (result != null ? "SUCCESSFUL" : "FAILED")));
				return result;
			}
		}

		/// <summary>
		/// Gets a list of unique nodes that are involved in the contained constraints.
		/// </summary>
		public IEnumerable<INode> Nodes
		{
			get
			{
				return (from c in this.constraints
						from n in c.Nodes
						select n).Distinct();
			}
		}

		public bool IsResolved
		{
			get { return this.constraints.All(c => c.IsResolved); }
		}

		public bool CanResolve
		{
			get { return this.constraints.All(c => c.CanResolve); }
		}

		public bool Resolve()
		{
			// Make sure that all constraints can be resolved now before starting.
			if (this.CanResolve)
			{
				this.Resolve();
				return true;
			}
			else
			{
				return false;
			}
		}

		public bool IsSatisfied
		{
			get { return this.constraints.All(c => c.IsSatisfied); }
		}

		public bool IsSatisfiable
		{
			get { return this.FindOnePossibleSolution() != null; }
		}

		public bool IsBroken
		{
			get { return !this.IsSatisfiable; }
		}

		public bool IsBreakable
		{
			get { return this.constraints.Any(c => c.IsBreakable); }
		}

		public bool IsMinimized
		{
			get { return this.constraints.All(c => c.IsMinimized); }
		}

		public bool IsWorthwhile
		{
			get { return this.constraints.Any(c => c.IsWorthwhile); }
		}

		public bool IsWorthless
		{
			get { return this.constraints.All(c => c.IsWorthless); }
		}
	}
}
