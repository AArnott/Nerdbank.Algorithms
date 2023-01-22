// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Nerdbank.Algorithms.NodeConstraintSelection;
using Xunit;
using Xunit.Abstractions;

public class SudokuScenarioTests : TestBase
{
	private static readonly ImmutableArray<ImmutableArray<object>> NodeGrid = Enumerable.Range(1, 9).Select(i => Enumerable.Range(1, 9).Select(j => (object)$"{(char)('A' + i - 1)}{j}").ToImmutableArray()).ToImmutableArray();
	private static readonly ImmutableArray<int> PossibleCellValues = Enumerable.Range(1, 9).ToImmutableArray();

	private readonly SolutionBuilder<int> builder;

	public SudokuScenarioTests(ITestOutputHelper logger)
		: base(logger)
	{
		this.builder = new SolutionBuilder<int>(NodeGrid.SelectMany(a => a).ToImmutableArray(), PossibleCellValues);

		for (int row = 0; row < 9; row++)
		{
			this.builder.AddConstraint(new UniqueValueConstraint(NodeGrid[row]));
		}

		for (int column = 0; column < 9; column++)
		{
			this.builder.AddConstraint(new UniqueValueConstraint(Enumerable.Range(0, 9).Select(row => NodeGrid[row][column]).ToImmutableArray()));
		}

		for (int column = 0; column < 9; column += 3)
		{
			for (int row = 0; row < 9; row += 3)
			{
				IEnumerable<object> nodes = from c in Enumerable.Range(column, 3)
											from r in Enumerable.Range(row, 3)
											select NodeGrid[c][r];
				this.builder.AddConstraint(new UniqueValueConstraint(nodes.ToImmutableArray()));
			}
		}
	}

	[Fact]
	public void Empty()
	{
		this.PrintGrid();
		Assert.Null(this.builder.CheckForConflictingConstraints(this.TimeoutToken));
	}

	[Fact]
	public void HardSudoku()
	{
		this.builder.SetNodeState(NodeGrid[0][3], 8);
		this.builder.SetNodeState(NodeGrid[0][6], 6);
		this.builder.SetNodeState(NodeGrid[0][7], 2);

		this.builder.SetNodeState(NodeGrid[1][2], 9);
		this.builder.SetNodeState(NodeGrid[1][3], 4);
		this.builder.SetNodeState(NodeGrid[1][6], 3);

		this.builder.SetNodeState(NodeGrid[2][1], 6);
		this.builder.SetNodeState(NodeGrid[2][3], 5);
		this.builder.SetNodeState(NodeGrid[2][5], 3);
		this.builder.SetNodeState(NodeGrid[2][6], 9);
		this.builder.SetNodeState(NodeGrid[2][8], 1);

		this.builder.SetNodeState(NodeGrid[3][0], 4);
		this.builder.SetNodeState(NodeGrid[3][7], 9);

		this.builder.SetNodeState(NodeGrid[4][4], 7);

		this.builder.SetNodeState(NodeGrid[5][1], 9);
		this.builder.SetNodeState(NodeGrid[5][8], 8);

		this.builder.SetNodeState(NodeGrid[6][0], 9);
		this.builder.SetNodeState(NodeGrid[6][2], 6);
		this.builder.SetNodeState(NodeGrid[6][3], 7);
		this.builder.SetNodeState(NodeGrid[6][5], 1);
		this.builder.SetNodeState(NodeGrid[6][7], 5);

		this.builder.SetNodeState(NodeGrid[7][2], 4);
		this.builder.SetNodeState(NodeGrid[7][5], 6);
		this.builder.SetNodeState(NodeGrid[7][6], 8);

		this.builder.SetNodeState(NodeGrid[8][1], 1);
		this.builder.SetNodeState(NodeGrid[8][2], 2);
		this.builder.SetNodeState(NodeGrid[8][5], 8);

		this.PrintGrid();
		SolutionBuilder<int>.SolutionsAnalysis analysis = this.builder.AnalyzeSolutions(this.TimeoutToken);
		Assert.Equal(1, analysis.ViableSolutionsFound);

		this.Logger.WriteLine("Solution:");
		analysis.ApplyAnalysisBackToBuilder();
		this.PrintGrid();
	}

	private void PrintGrid()
	{
		this.builder.ResolvePartially(this.TimeoutToken);

		var stringBuilder = new StringBuilder();
		for (int i = 0; i < 9 * 9; i++)
		{
			stringBuilder.Append(this.builder[i]?.ToString(CultureInfo.CurrentCulture) ?? "_");
			stringBuilder.Append(' ');

			if ((i + 1) % 9 == 0)
			{
				stringBuilder.AppendLine();
			}
		}

		this.Logger.WriteLine(stringBuilder.ToString());
	}

	/// <summary>
	/// A constraint that ensures that each integer 1-9 appears exactly once in a given set of exactly 9 nodes.
	/// </summary>
	private class UniqueValueConstraint : IConstraint<int>
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

		public bool Equals(IConstraint<int>? other)
		{
			return other is UniqueValueConstraint uv && this.Nodes.Length == uv.Nodes.Length && this.Nodes.SequenceEqual(uv.Nodes);
		}

		public ConstraintStates GetState(Scenario<int> scenario)
		{
			ConstraintStates result = ConstraintStates.Satisfiable; // assume we're satisfiable till we find otherwise.

			int resolvedNodesCount = 0;
			int uniqueValuesObserved = 0;
			var observed = ArrayPool<bool>.Shared.Rent(9);
			try
			{
				Array.Clear(observed, 0, 9);
				foreach (var node in this.Nodes)
				{
					if (scenario[node] is int value)
					{
						resolvedNodesCount++;
						if (observed[value - 1])
						{
							// We saw the same value twice. That's a violation.
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
			var observed = ArrayPool<bool>.Shared.Rent(9);
			try
			{
				foreach (var node in this.Nodes)
				{
					if (scenario[node] is int value)
					{
						if (observed[value - 1])
						{
							// We saw the same value twice. That's a violation.
							return false;
						}
						else
						{
							observed[value - 1] = true;
						}
					}
					else if (emptyNode is null)
					{
						emptyNode = node;
					}
					else
					{
						// More than one empty node. Cannot resolve.
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
}
