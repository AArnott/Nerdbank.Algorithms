// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NerdBank.Algorithms.NodeConstraintSelection;
using Xunit;
using Xunit.Abstractions;

public class SolutionBuilderTests : TestBase
{
	private static readonly IReadOnlyList<DummyNode> Nodes = ImmutableList.Create(new DummyNode("a"), new DummyNode("b"), new DummyNode("c"), new DummyNode("d"));

	private readonly SolutionBuilder builder;

	public SolutionBuilderTests(ITestOutputHelper logger)
		: base(logger)
	{
		this.builder = new SolutionBuilder(Nodes);
	}

	[Fact]
	public void Ctor_Null()
	{
		Assert.Throws<ArgumentNullException>("nodes", () => new SolutionBuilder(default!));
	}

	[Fact]
	public void Ctor_EmptyNodeList()
	{
		Assert.Throws<ArgumentException>("nodes", () => new SolutionBuilder(Array.Empty<DummyNode>()));
	}

	[Fact]
	public void Indexers_DefaultState()
	{
		Assert.Null(this.builder[0]);
		Assert.Null(this.builder[Nodes[0]]);
	}

	[Fact]
	public void AddConstraint_Null()
	{
		Assert.Throws<ArgumentNullException>("constraint", () => this.builder.AddConstraint(null!));
	}

	[Fact]
	public void AddConstraints_Null()
	{
		Assert.Throws<ArgumentNullException>("constraints", () => this.builder.AddConstraints(null!));
	}

	[Fact]
	public void AddConstraints_NullElement()
	{
		Assert.Throws<ArgumentException>("constraints", () => this.builder.AddConstraints(new IConstraint[1]));
	}

	[Fact]
	public void AddConstraint()
	{
		this.builder.AddConstraint(SelectionCountConstraint.ExactSelected(Nodes.Take(3), 1));
	}

	[Fact]
	public void AddConstraint_ThrowsOnEmptyNodeSet()
	{
		var badConstraint = new EmptyNodeSetConstraint();
		BadConstraintException ex = Assert.Throws<BadConstraintException>(() => this.builder.AddConstraint(badConstraint));
		Assert.Same(badConstraint, ex.Constraint);
	}

	[Fact]
	public void AddConstraint_IrrelevantNodes()
	{
		Assert.Throws<KeyNotFoundException>(() => this.builder.AddConstraint(SelectionCountConstraint.ExactSelected(new[] { new DummyNode("x") }, 1)));
	}

	[Fact]
	public void AddConstraints()
	{
		this.builder.AddConstraints(new[]
		{
			SelectionCountConstraint.ExactSelected(Nodes.Take(1), 1),
			SelectionCountConstraint.ExactSelected(Nodes.Skip(1).Take(1), 1),
		});

		this.builder.ResolvePartially();
		Assert.True(this.builder[0]);
		Assert.True(this.builder[1]);
		for (int i = 2; i < Nodes.Count; i++)
		{
			Assert.Null(this.builder[i]);
		}
	}

	[Fact]
	public void ResolvePartially_NoOpWithoutConstraints()
	{
		this.builder.ResolvePartially(this.TimeoutToken);
		this.AssertAllNodesIndeterminate();
	}

	/// <summary>
	/// Verifies that constraints are repeatedly resolved so long as any resolve.
	/// </summary>
	[Fact]
	public void ResolvePartially_WithConstraints()
	{
		this.builder.AddConstraint(SelectionCountConstraint.ExactSelected(Nodes.Take(3), 1));

		this.builder.ResolvePartially(this.TimeoutToken);
		this.AssertAllNodesIndeterminate();

		this.builder.AddConstraint(SelectionCountConstraint.ExactSelected(Nodes.Take(1), 1));
		this.AssertAllNodesIndeterminate();

		this.builder.ResolvePartially(this.TimeoutToken);
		Assert.True(this.builder[0]);
		for (int i = 1; i < Nodes.Count - 1; i++)
		{
			Assert.False(this.builder[i]);
		}

		Assert.Null(this.builder[Nodes.Count - 1]);
	}

	[Fact]
	public void ResolvePartially_NoChangesCommittedIfConstraintThrows()
	{
		this.builder.AddConstraint(SelectionCountConstraint.ExactSelected(Nodes.Take(1), 1));
		this.builder.AddConstraint(new ThrowingConstraint());

		Assert.Throws<BadConstraintException>(() => this.builder.ResolvePartially(this.TimeoutToken));
		this.AssertAllNodesIndeterminate();
	}

	[Fact]
	public void ResolvePartially_DoesNotHangWhenConstraintClaimsResolvedWhenNothingChanged()
	{
		var badConstraint = new FalselyNonResolvingConstraint();
		this.builder.AddConstraint(badConstraint);
		BadConstraintException ex = Assert.Throws<BadConstraintException>(() => this.builder.ResolvePartially(this.TimeoutToken));
		Assert.Same(badConstraint, ex.Constraint);
	}

	[Fact]
	public void CheckForConflictingConstraints_NoConflictsExist()
	{
		this.builder.AddConstraint(SelectionCountConstraint.ExactSelected(Nodes.Take(2), 1));
		this.builder.AddConstraint(SelectionCountConstraint.ExactSelected(Nodes.Skip(2), 1));
		Assert.Null(this.builder.CheckForConflictingConstraints(this.TimeoutToken));

		this.builder.AddConstraint(SelectionCountConstraint.ExactSelected(Nodes.Take(1), 1));
		Assert.Null(this.builder.CheckForConflictingConstraints(this.TimeoutToken));

		this.builder.AddConstraint(SelectionCountConstraint.ExactSelected(Nodes.Reverse().Take(1), 1));
		Assert.Null(this.builder.CheckForConflictingConstraints(this.TimeoutToken));

		this.builder.ResolvePartially(this.TimeoutToken);
		Assert.Null(this.builder.CheckForConflictingConstraints(this.TimeoutToken));
		for (int i = 0; i < Nodes.Count; i++)
		{
			Assert.NotNull(this.builder[i]);
		}
	}

	[Fact]
	public void CheckForConflictingConstraints_ConflictsExist()
	{
		Assert.Null(this.builder.CheckForConflictingConstraints(this.TimeoutToken));

		SelectionCountConstraint[] constraints = new[]
		{
			SelectionCountConstraint.ExactSelected(Nodes.Take(2), 1),
			SelectionCountConstraint.ExactSelected(Nodes.Skip(2), 1),
			SelectionCountConstraint.ExactSelected(Nodes, 1),
			SelectionCountConstraint.RangeSelected(Nodes, 1, Nodes.Count),
		};
		this.builder.AddConstraints(constraints);

		// Verify that ResolvePartially doesn't notice or care about conflicting constraints.
		this.builder.ResolvePartially(this.TimeoutToken);
		this.AssertAllNodesIndeterminate();

		IReadOnlyCollection<IConstraint>? conflictingConstraints = this.builder
			.CheckForConflictingConstraints(this.TimeoutToken)
			?.GetConflictingConstraints(this.TimeoutToken);
		Assert.NotNull(conflictingConstraints);

		Assert.Equal(3, conflictingConstraints?.Count);
		Assert.Contains(constraints[0], conflictingConstraints);
		Assert.Contains(constraints[1], conflictingConstraints);
		Assert.Contains(constraints[2], conflictingConstraints);

		this.builder.RemoveConstraint(constraints[1]);
		Assert.Null(this.builder.CheckForConflictingConstraints(this.TimeoutToken));
	}

	[Fact]
	public void AnalyzeSolution_NoConstraints()
	{
		SolutionsAnalysis analysis = this.builder.AnalyzeSolutions(this.TimeoutToken);
		Assert.NotNull(analysis);

		Assert.Null(analysis.Conflicts);
		Assert.Equal(1, analysis.ViableSolutionsFound);

		for (int i = 0; i < Nodes.Count; i++)
		{
			Assert.Equal(-1, analysis.GetNodeSelectedCount(0));
		}
	}

	[Fact]
	public void AnalyzeSolution_WorthlessConstraint()
	{
		this.builder.AddConstraint(SelectionCountConstraint.RangeSelected(Nodes, 0, Nodes.Count));
		SolutionsAnalysis analysis = this.builder.AnalyzeSolutions(this.TimeoutToken);
		Assert.NotNull(analysis);

		Assert.Null(analysis.Conflicts);
		Assert.Equal(16, analysis.ViableSolutionsFound);

		for (int i = 0; i < Nodes.Count; i++)
		{
			Assert.Equal(analysis.ViableSolutionsFound / 2, analysis.GetNodeSelectedCount(0));
		}
	}

	[Fact]
	public void AnalyzeSolution_ConstraintInteractionsLeadToNodeSelections()
	{
		// Set up a constraint system in which exactly one specific node must be selected in the remaining viable solutions.
		this.builder.AddConstraints(new[]
		{
			// [1-3]: exactly one
			SelectionCountConstraint.ExactSelected(Nodes.Take(3), 1),

			// [1-2] and [2-3]: each exactly one
			SelectionCountConstraint.ExactSelected(Nodes.Take(2), 1),
			SelectionCountConstraint.ExactSelected(Nodes.Skip(1).Take(2), 1),
		});

		this.builder.ResolvePartially(this.TimeoutToken);
		this.AssertAllNodesIndeterminate();

		SolutionsAnalysis analysis = this.builder.AnalyzeSolutions(this.TimeoutToken);

		// viable solutions are: 010x
		Assert.Equal(1, analysis.ViableSolutionsFound);
		Assert.Equal(0, analysis.GetNodeSelectedCount(0));
		Assert.Equal(analysis.ViableSolutionsFound, analysis.GetNodeSelectedCount(1));
		Assert.Equal(0, analysis.GetNodeSelectedCount(2));
		Assert.Equal(-1, analysis.GetNodeSelectedCount(3));

		// Verify that analysis didn't impact any node selections.
		this.AssertAllNodesIndeterminate();

		// Verify that applying the analysis results back to the builder
		// lead to 010x results.
		analysis.ApplyAnalysisBackToBuilder();
		for (int i = 0; i < Nodes.Count - 1; i++)
		{
			Assert.Equal(i == 1, this.builder[i]);
		}

		Assert.Null(this.builder[Nodes.Count - 1]);

		SolutionsAnalysis analysis2 = this.builder.AnalyzeSolutions(this.TimeoutToken);
		Assert.Equal(analysis.ViableSolutionsFound, analysis2.ViableSolutionsFound);
		Assert.Null(analysis2.Conflicts);
	}

	[Fact]
	public void AnalyzeSolution_ConflictsExist()
	{
		this.builder.AddConstraints(new[]
		{
			SelectionCountConstraint.ExactSelected(Nodes.Take(3), 1),
			SelectionCountConstraint.ExactSelected(Nodes.Take(3), 2),
		});
		SolutionsAnalysis analysis = this.builder.AnalyzeSolutions(this.TimeoutToken);
		Assert.Equal(0, analysis.ViableSolutionsFound);
		Assert.NotNull(analysis.Conflicts);
		Assert.Throws<InvalidOperationException>(() => analysis.GetNodeSelectedCount(0));
		Assert.Throws<InvalidOperationException>(() => analysis.ApplyAnalysisBackToBuilder());
	}

	/// <summary>
	/// Verifies that checking for conflicting constraints can quickly find a conflict even in a very large problem space.
	/// </summary>
	[Fact]
	public void CheckForConflictingConstraints_VeryLargeProblemSpace()
	{
		SolutionBuilder conflictedBuilder = CreateBuilderWithNonObviousConflictInVeryLargeProblemSpace();
		SolutionBuilder.ConflictedConstraints? conflicts = conflictedBuilder.CheckForConflictingConstraints(this.TimeoutToken);
		Assert.NotNull(conflicts);
	}

	/// <summary>
	/// Verifies that solution analysis can quickly find a conflict even in a very large problem space.
	/// </summary>
	[Fact]
	public void AnalyzeSolution_VeryLargeProblemSpace()
	{
		SolutionBuilder conflictedBuilder = CreateBuilderWithNonObviousConflictInVeryLargeProblemSpace();
		SolutionsAnalysis analysis = conflictedBuilder.AnalyzeSolutions(this.TimeoutToken);
		Assert.Equal(0, analysis.ViableSolutionsFound);
		Assert.NotNull(analysis.Conflicts);
	}

	private static SolutionBuilder CreateBuilderWithNonObviousConflictInVeryLargeProblemSpace()
	{
		DummyNode[] nodes = Enumerable.Range(1, 100).Select(n => new DummyNode(n)).ToArray();
		var builder = new SolutionBuilder(nodes);
		builder.AddConstraints(new[]
		{
			SelectionCountConstraint.ExactSelected(nodes.Skip(90).Take(3), 1),
			SelectionCountConstraint.ExactSelected(nodes.Skip(90).Take(3), 2),
		});
		return builder;
	}

	private void AssertAllNodesIndeterminate()
	{
		for (int i = 0; i < Nodes.Count; i++)
		{
			Assert.Null(this.builder[i]);
		}
	}

	/// <summary>
	/// A constraint that returns true from <see cref="IConstraint.Resolve(Scenario)"/>
	/// even though it doesn't change anything.
	/// </summary>
	private class FalselyNonResolvingConstraint : IConstraint
	{
		public IReadOnlyCollection<object> Nodes { get; } = SolutionBuilderTests.Nodes;

		public bool IsEmpty => throw new NotImplementedException();

		public ConstraintStates GetState(Scenario scenario)
		{
			throw new NotImplementedException();
		}

		public bool Resolve(Scenario scenario) => true;
	}

	/// <summary>
	/// A constraint that throws from everything.
	/// </summary>
	private class ThrowingConstraint : IConstraint
	{
		public IReadOnlyCollection<object> Nodes { get; } = SolutionBuilderTests.Nodes;

		public bool IsEmpty => throw new NotImplementedException();

		public ConstraintStates GetState(Scenario scenario)
		{
			throw new NotImplementedException();
		}

		public bool Resolve(Scenario scenario)
		{
			throw new NotImplementedException();
		}
	}

	private class EmptyNodeSetConstraint : IConstraint
	{
		public IReadOnlyCollection<object> Nodes => Array.Empty<object>();

		public bool IsEmpty => throw new NotImplementedException();

		public ConstraintStates GetState(Scenario scenario)
		{
			throw new NotImplementedException();
		}

		public bool Resolve(Scenario scenario)
		{
			throw new NotImplementedException();
		}
	}
}
