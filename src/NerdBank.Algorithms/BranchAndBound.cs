// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NerdBank.Algorithms
{
	using System;
	using System.Diagnostics;
	using System.Threading;

	/// <summary>
	/// Implements the branch and bound algorithm.
	/// </summary>
	public static class BranchAndBound
	{
		/// <summary>
		/// Searches for the optimal solution to a problem using the branch and bound algorithm.
		/// </summary>
		/// <typeparam name="T">The type of problem or solution to solve.</typeparam>
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
		/// <param name="cancellationToken">A token whose cancellation should abort the search and return the best solution found so far.</param>
		/// <param name="optimalFound">
		/// Whether the optimal solution was found before returning
		/// the best found so far.  See <paramref name="cancellationToken"/>.
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
		public static T? Search<T>(
			T start,
			T? quickSolution,
			bool pruning,
			CancellationToken cancellationToken,
			out bool optimalFound,
			out int solutionsConsidered,
			out int nodesExplored,
			out int maxNodes,
			out int prunedNodes)
			where T : class, IBranchAndBoundNode
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
			T? bestSoFar = quickSolution;
			C5.IPriorityQueue<T> queue = new C5.IntervalHeap<T>();
			////Wintellect.PowerCollections.OrderedBag<T> queue = new Wintellect.PowerCollections.OrderedBag<T>();
			if (quickSolution is object)
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
					if (bestSoFar is null || child.CompareTo(bestSoFar) < 0)
					{
						////Trace.WriteLine("Enqueue: " + child.ToString());
						queue.Add(child);
					}
					////else
					////    Trace.WriteLine("Not enqueuing: " + child.ToString());
					if (child.IsSolution)
					{
						solutionsConsidered++;
						if (bestSoFar is null || child.CompareTo(bestSoFar) < 0)
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

				if (cancellationToken.IsCancellationRequested)
				{
					optimalFound = false;
					break;
				}
			}

			////Trace.WriteLine("Finishing with these solutions in the queue:");
			////foreach (T runthru in queue)
			////    Trace.WriteLine(runthru);

			////Debug.Assert(queue.GetFirst().CompareTo(bestSoFar) == 0);
			return bestSoFar;
		}
	}
}
