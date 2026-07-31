// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Nerdbank.Algorithms.NodeConstraintSelection;

namespace Nerdbank.Algorithms.Benchmarks;

/// <summary>
/// Benchmarks for the node constraint selection solver hot paths.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 1, iterationCount: 8)]
public class NodeConstraintSelectionBenchmarks
{
	private static readonly ImmutableArray<ImmutableArray<object>> SudokuGrid =
		Enumerable.Range(1, 9)
			.Select(i => Enumerable.Range(1, 9).Select(j => (object)$"{(char)('A' + i - 1)}{j}").ToImmutableArray())
			.ToImmutableArray();

	private static readonly ImmutableArray<int> SudokuStates = Enumerable.Range(1, 9).ToImmutableArray();

	/// <summary>
	/// Full solution analysis of a hard sudoku (unique solution).
	/// Stresses deep search, cascading resolve, and backtracking.
	/// </summary>
	/// <returns>The number of viable solutions found.</returns>
	[Benchmark]
	public long SudokuHard_AnalyzeSolutions()
	{
		SolutionBuilder<int> builder = CreateSudokuBuilder();
		ApplyHardSudokuGivens(builder);
		return builder.AnalyzeSolutions(CancellationToken.None).ViableSolutionsFound;
	}

	/// <summary>
	/// Partial resolve + conflict check on a hard sudoku without full enumeration.
	/// </summary>
	/// <returns>0 when no conflict is found; otherwise 1.</returns>
	[Benchmark]
	public int SudokuHard_ResolveAndCheck()
	{
		SolutionBuilder<int> builder = CreateSudokuBuilder();
		ApplyHardSudokuGivens(builder);
		builder.ResolvePartially(CancellationToken.None);
		return builder.CheckForConflictingConstraints(CancellationToken.None) is null ? 0 : 1;
	}

	/// <summary>
	/// Enumerates 2^10 independent pair solutions and records per-node stats.
	/// Stresses solution counting / stats recording.
	/// </summary>
	/// <returns>The number of viable solutions found.</returns>
	[Benchmark]
	public long BoolPairs10_AnalyzeSolutions() => AnalyzeBoolPairs(10);

	/// <summary>
	/// Enumerates 2^14 independent pair solutions and records per-node stats.
	/// Larger stats-recording workload.
	/// </summary>
	/// <returns>The number of viable solutions found.</returns>
	[Benchmark]
	public long BoolPairs14_AnalyzeSolutions() => AnalyzeBoolPairs(14);

	/// <summary>
	/// Builds a probable multi-state assignment via repeated analysis.
	/// </summary>
	/// <returns>A checksum over the chosen node states.</returns>
	[Benchmark]
	public long MultiState_GetProbableSolution()
	{
		// 3 nodes and 4 states (with 'a' forbidden) matches the unit-test scenario and has viable solutions.
		const int nodeCount = 3;
		ImmutableArray<object>.Builder nodesBuilder = ImmutableArray.CreateBuilder<object>(nodeCount);
		for (int i = 0; i < nodeCount; i++)
		{
			nodesBuilder.Add(i);
		}

		ImmutableArray<object> nodes = nodesBuilder.MoveToImmutable();
		var builder = new SolutionBuilder<char>(nodes, ImmutableArray.Create('a', 'b', 'c', 'd'));
		builder.AddConstraint(new NoAConstraint(nodes));
		builder.AddConstraint(new NoDuplicatesConstraint(nodes));
		Scenario<char> scenario = builder.GetProbableSolution(CancellationToken.None);

		long sum = 0;
		foreach (char? state in scenario.NodeStates)
		{
			sum += state!.Value;
		}

		return sum;
	}

	private static long AnalyzeBoolPairs(int pairCount)
	{
		ImmutableArray<object>.Builder nodesBuilder = ImmutableArray.CreateBuilder<object>(pairCount * 2);
		for (int i = 0; i < pairCount * 2; i++)
		{
			nodesBuilder.Add(i);
		}

		ImmutableArray<object> nodes = nodesBuilder.MoveToImmutable();
		var builder = new SolutionBuilder<bool>(nodes, ImmutableArray.Create(true, false));
		for (int p = 0; p < pairCount; p++)
		{
			builder.AddConstraint(SelectionCountConstraint.ExactSelected(
				ImmutableArray.Create(nodes[p * 2], nodes[(p * 2) + 1]),
				1));
		}

		return builder.AnalyzeSolutions(CancellationToken.None).ViableSolutionsFound;
	}

	private static SolutionBuilder<int> CreateSudokuBuilder()
	{
		var builder = new SolutionBuilder<int>(
			SudokuGrid.SelectMany(static a => a).ToImmutableArray(),
			SudokuStates);

		for (int row = 0; row < 9; row++)
		{
			builder.AddConstraint(new UniqueValueConstraint(SudokuGrid[row]));
		}

		for (int column = 0; column < 9; column++)
		{
			builder.AddConstraint(new UniqueValueConstraint(
				Enumerable.Range(0, 9).Select(row => SudokuGrid[row][column]).ToImmutableArray()));
		}

		for (int column = 0; column < 9; column += 3)
		{
			for (int row = 0; row < 9; row += 3)
			{
				ImmutableArray<object> nodes = (from c in Enumerable.Range(column, 3)
												from r in Enumerable.Range(row, 3)
												select SudokuGrid[c][r]).ToImmutableArray();
				builder.AddConstraint(new UniqueValueConstraint(nodes));
			}
		}

		return builder;
	}

	private static void ApplyHardSudokuGivens(SolutionBuilder<int> builder)
	{
		static object Cell(int r, int c) => $"{(char)('A' + r)}{c + 1}";
		void Set(int r, int c, int v) => builder.SetNodeState(Cell(r, c), v);

		Set(0, 3, 8);
		Set(0, 6, 6);
		Set(0, 7, 2);
		Set(1, 2, 9);
		Set(1, 3, 4);
		Set(1, 6, 3);
		Set(2, 1, 6);
		Set(2, 3, 5);
		Set(2, 5, 3);
		Set(2, 6, 9);
		Set(2, 8, 1);
		Set(3, 0, 4);
		Set(3, 7, 9);
		Set(4, 4, 7);
		Set(5, 1, 9);
		Set(5, 8, 8);
		Set(6, 0, 9);
		Set(6, 2, 6);
		Set(6, 3, 7);
		Set(6, 5, 1);
		Set(6, 7, 5);
		Set(7, 2, 4);
		Set(7, 5, 6);
		Set(7, 6, 8);
		Set(8, 1, 1);
		Set(8, 2, 2);
		Set(8, 5, 8);
	}

	private sealed class UniqueValueConstraint : IConstraint<int>
	{
		internal UniqueValueConstraint(ImmutableArray<object> nodes)
		{
			if (nodes.Length != 9)
			{
				throw new ArgumentException("Always applied to 9 nodes.", nameof(nodes));
			}

			this.Nodes = nodes;
		}

		public ImmutableArray<object> Nodes { get; }

		public bool Equals(IConstraint<int>? other) =>
			other is UniqueValueConstraint uv && this.Nodes.SequenceEqual(uv.Nodes);

		public ConstraintStates GetState(Scenario<int> scenario)
		{
			ConstraintStates result = ConstraintStates.Satisfiable;
			int resolvedNodesCount = 0;
			int uniqueValuesObserved = 0;
			bool[] observed = ArrayPool<bool>.Shared.Rent(9);
			try
			{
				Array.Clear(observed, 0, 9);
				foreach (object node in this.Nodes)
				{
					if (scenario[node] is int value)
					{
						resolvedNodesCount++;
						if (observed[value - 1])
						{
							result &= ~ConstraintStates.Satisfiable;
						}
						else
						{
							uniqueValuesObserved++;
							observed[value - 1] = true;
						}
					}
				}

				if (resolvedNodesCount == this.Nodes.Length)
				{
					result |= ConstraintStates.Resolved;
					if (uniqueValuesObserved == 9)
					{
						result |= ConstraintStates.Satisfied;
					}
				}
				else
				{
					if (resolvedNodesCount < 9)
					{
						result |= ConstraintStates.Breakable;
					}

					if (resolvedNodesCount == 8 && (result & ConstraintStates.Satisfiable) == ConstraintStates.Satisfiable)
					{
						result |= ConstraintStates.Resolvable;
					}
				}
			}
			finally
			{
				ArrayPool<bool>.Shared.Return(observed);
			}

			return result;
		}

		public bool Resolve(Scenario<int> scenario)
		{
			object? emptyNode = null;
			bool[] observed = ArrayPool<bool>.Shared.Rent(9);
			try
			{
				Array.Clear(observed, 0, 9);
				foreach (object node in this.Nodes)
				{
					if (scenario[node] is int value)
					{
						if (observed[value - 1])
						{
							return false;
						}

						observed[value - 1] = true;
					}
					else if (emptyNode is null)
					{
						emptyNode = node;
					}
					else
					{
						return false;
					}
				}

				if (emptyNode is object)
				{
					for (int i = 0; i < 9; i++)
					{
						if (!observed[i])
						{
							scenario[emptyNode] = i + 1;
							break;
						}
					}

					return true;
				}

				return false;
			}
			finally
			{
				ArrayPool<bool>.Shared.Return(observed);
			}
		}
	}

	private sealed class NoAConstraint : IConstraint<char>
	{
		internal NoAConstraint(ImmutableArray<object> nodes) => this.Nodes = nodes;

		public ImmutableArray<object> Nodes { get; }

		public bool Equals(IConstraint<char>? other) =>
			other is NoAConstraint n && this.Nodes.SequenceEqual(n.Nodes);

		public ConstraintStates GetState(Scenario<char> scenario)
		{
			ConstraintStates result = ConstraintStates.None;
			if (this.Nodes.All(n => scenario[n].HasValue))
			{
				result |= ConstraintStates.Resolved;
			}

			foreach (object node in this.Nodes)
			{
				if (scenario[node] == 'a')
				{
					return result;
				}
			}

			result |= ConstraintStates.Satisfiable;
			if ((result & ConstraintStates.Resolved) == ConstraintStates.Resolved)
			{
				result |= ConstraintStates.Satisfied;
			}
			else
			{
				result |= ConstraintStates.Breakable;
			}

			return result;
		}

		public bool Resolve(Scenario<char> scenario) => false;
	}

	private sealed class NoDuplicatesConstraint : IConstraint<char>
	{
		internal NoDuplicatesConstraint(ImmutableArray<object> nodes) => this.Nodes = nodes;

		public ImmutableArray<object> Nodes { get; }

		public bool Equals(IConstraint<char>? other) =>
			other is NoDuplicatesConstraint n && this.Nodes.SequenceEqual(n.Nodes);

		public ConstraintStates GetState(Scenario<char> scenario)
		{
			ConstraintStates result = ConstraintStates.None;
			if (this.Nodes.All(n => scenario[n].HasValue))
			{
				result |= ConstraintStates.Resolved;
			}

			var usedChars = new HashSet<char>();
			foreach (object node in this.Nodes)
			{
				if (scenario[node] is char ch && !usedChars.Add(ch))
				{
					return result;
				}
			}

			result |= ConstraintStates.Satisfiable;
			if ((result & ConstraintStates.Resolved) == ConstraintStates.Resolved)
			{
				result |= ConstraintStates.Satisfied;
			}
			else
			{
				result |= ConstraintStates.Breakable;
			}

			return result;
		}

		public bool Resolve(Scenario<char> scenario) => false;
	}
}
