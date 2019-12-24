// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NerdBank.Algorithms
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;

	public static class BranchAndBound<T>
		where T : BranchAndBound<T>.IBranchAndBoundNode
	{
		/// <summary>
		/// Represents one solution or partial solution to the problem.
		/// </summary>
		/// <remarks>
		/// Each node must be able to enumerate any child nodes.
		/// Each node must be able to compare itself with other nodes
		/// so that the priority queue may do its job.
		/// The CompareTo method must consider cost/value to be the
		/// first criteria, and <see cref="IsSolution"/> to be the second,
		/// giving preference among equal cost nodes to those nodes that
		/// are solutions.
		/// </remarks>
		public interface IBranchAndBoundNode : IComparable<T>
		{
			/// <summary>
			/// Gets the child nodes of this node.
			/// </summary>
			/// <returns>
			/// An enumerator capable of enumerating over the child nodes.
			/// </returns>
			/// <remarks>
			/// If no child nodes exist, this enumerator should operate on
			/// an empty list.
			/// </remarks>
			IEnumerable<T> ChildNodes { get; }

			/// <summary>
			/// Gets a value indicating whether this node represents a solution.
			/// </summary>
			bool IsSolution { get; }
		}

		/// <summary>
		/// Searches for the optimal solution to a problem within
		/// some given time limit, using the branch and bound algorithm.
		/// </summary>
		/// <param name="start">
		/// The starting node from which all solutions can be derived.
		/// </param>
		/// <param name="quickSolution">
		/// Optional. A quick-and-dirty solution that is not necessarily optimal.
		/// </param>
		/// <param name="pruning">
		/// Whether to prune out disqualified nodes along the way to keep
		/// memory usage down, at the expense of some performance.
		/// </param>
		/// <param name="maxDuration">
		/// The maximum time to spend on the problem before returning the
		/// best solution found thus far.
		/// </param>
		/// <param name="optimalFound">
		/// Whether the optimal solution was found before returning
		/// the best found so far.  See <paramref name="maxDuration"/>.
		/// </param>
		/// <param name="solutionsConsidered">
		/// How many solutions were found during the search.
		/// </param>
		/// <param name="nodesExplored">
		/// The total number of nodes explored for possible solutions.
		/// </param>
		/// <param name="maxNodes">
		/// The maximum number of nodes that were in the priority queue.
		/// </param>
		/// <param name="prunedNodes">
		/// The number of nodes that were pruned from the priority queue
		/// having never been explored.
		/// </param>
		/// <returns>
		/// The node that represents the optimal solution.
		/// </returns>
		public static T Search(
			T start,
			T quickSolution,
			bool pruning,
			TimeSpan maxDuration,
			out bool optimalFound,
			out int solutionsConsidered,
			out int nodesExplored,
			out int maxNodes,
			out int prunedNodes)
		{
			if (start is null)
			{
				throw new ArgumentNullException(nameof(start));
			}

			maxNodes = 0;
			prunedNodes = 0;
			nodesExplored = 0;
			solutionsConsidered = 0;
			optimalFound = true; // assume yes
			DateTime startTime = DateTime.Now;
			T bestSoFar = quickSolution;
			C5.IPriorityQueue<T> queue = new C5.IntervalHeap<T>();
			////Wintellect.PowerCollections.OrderedBag<T> queue = new Wintellect.PowerCollections.OrderedBag<T>();
			if (quickSolution != null)
			{
				queue.Add(quickSolution);
				////Trace.WriteLine("Enqueue: " + quickSolution.ToString());
			}
			////Trace.WriteLine("Enqueue: " + start.ToString());
			queue.Add(start);
			while (!queue.FindMin().IsSolution)
			{
				T node = queue.DeleteMin();
				////Trace.WriteLine("Dequeued " + node.ToString());
				nodesExplored++;
				Debug.Assert(!node.IsSolution, "!node.IsSolution");
				foreach (T child in node.ChildNodes)
				{
					if (child.CompareTo(bestSoFar) < 0)
					{
						////Trace.WriteLine("Enqueue: " + child.ToString());
						queue.Add(child);
					}
					////else
					////    Trace.WriteLine("Not enqueuing: " + child.ToString());
					if (child.IsSolution)
					{
						solutionsConsidered++;
						if (bestSoFar == null || child.CompareTo(bestSoFar) < 0)
						{
							bestSoFar = child;
						}
					}
				}

				maxNodes = Math.Max(maxNodes, queue.Count);
				while (pruning && bestSoFar != null && queue.FindMax().CompareTo(bestSoFar) > 0)
				{
					queue.DeleteMax();
					prunedNodes++;
				}

				if ((DateTime.Now - startTime) >= maxDuration)
				{
					optimalFound = false;
					break;
				}
			}

			////Trace.WriteLine("Finishing with these solutions in the queue:");
			////foreach (T runthru in queue)
			////    Trace.WriteLine(runthru);

			////Debug.Assert(queue.GetFirst().CompareTo(bestSoFar) == 0);
			Debug.Assert(bestSoFar.IsSolution, "bestSoFar.IsSolution");
			return bestSoFar;
		}

		/// <summary>
		/// Finds the optimal solution to a problem using the
		/// branch and bound algorithm.
		/// </summary>
		/// <param name="start">
		/// The starting node from which all solutions can be derived.
		/// </param>
		/// <param name="quickSolution">
		/// Optional. A quick-and-dirty solution that is not necessarily optimal.
		/// </param>
		/// <param name="pruning">
		/// Whether to prune out disqualified nodes along the way to keep
		/// memory usage down, at the expense of some performance.
		/// </param>
		/// <returns>
		/// The node that represents the optimal solution.
		/// </returns>
		public static T FindOptimal(T start, T quickSolution, bool pruning)
		{
			bool optimalFound;
			int solutionsConsidered, nodesExplored, maxNodes, prunedNodes;
			return Search(
				start,
				quickSolution,
				pruning,
				TimeSpan.MaxValue,
				out optimalFound,
				out solutionsConsidered,
				out nodesExplored,
				out maxNodes,
				out prunedNodes);
		}
	}
}
