// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Nerdbank.Algorithms.NodeConstraintSelection;
using Xunit;
using Xunit.Abstractions;

public class ClueScenarioTests : TestBase
{
	private static readonly CardHolder CaseFile = new("Case file");
	private static readonly ImmutableArray<CardHolder> Players = Enumerable.Range(1, 3).Select(n => new CardHolder($"Player {n}")).ToImmutableArray();
	private static readonly ImmutableArray<Card> Suspects = Enumerable.Range(1, 6).Select(n => new Card($"Suspect {n}")).ToImmutableArray();
	private static readonly ImmutableArray<Card> Weapons = Enumerable.Range(1, 6).Select(n => new Card($"Weapon {n}")).ToImmutableArray();
	private static readonly ImmutableArray<Card> Rooms = Enumerable.Range(1, 9).Select(n => new Card($"Room {n}")).ToImmutableArray();
	private static readonly ImmutableArray<Card> Cards;
	private static readonly ImmutableArray<object> Nodes;
	private static readonly ImmutableArray<IConstraint<bool>> StartingConstraints;
	private static readonly CardHolder InteractivePlayer;
	private static readonly int NumberOfCardsHeldByInteractivePlayer;

	private readonly SolutionBuilder<bool> builder;

	static ClueScenarioTests()
	{
		Cards = Suspects.AddRange(Weapons).AddRange(Rooms);

		ImmutableArray<IConstraint<bool>>.Builder constraints = ImmutableArray.CreateBuilder<IConstraint<bool>>();
		ImmutableArray<object>.Builder nodes = ImmutableArray.CreateBuilder<object>();

		var cardsPerPlayer = new Dictionary<CardHolder, List<object>>();
		foreach (CardHolder player in Players)
		{
			cardsPerPlayer.Add(player, new List<object>());
		}

		void CreateNodesForCategory(ImmutableArray<Card> cards)
		{
			// Start by creating the nodes for the case file.
			var caseFileNodes = new List<object>();
			foreach (Card card in cards)
			{
				var allHolderNodes = new List<object>();
				foreach (CardHolder player in Players)
				{
					object node = (card, player);
					cardsPerPlayer[player].Add(node);
					allHolderNodes.Add(node);
				}

				object caseFileNode = (card, CaseFile);
				allHolderNodes.Add(caseFileNode);
				caseFileNodes.Add(caseFileNode);

				// This card can only appear in one place: a player or the case file.
				constraints.Add(SelectionCountConstraint.ExactSelected(allHolderNodes, 1));
				nodes.AddRange(allHolderNodes);
			}

			// Exactly one card from this whole category of cards will appear in the case file.
			constraints.Add(SelectionCountConstraint.ExactSelected(caseFileNodes, 1));
		}

		CreateNodesForCategory(Suspects);
		CreateNodesForCategory(Weapons);
		CreateNodesForCategory(Rooms);

		// Each player holds a fixed number of cards.
		if (Cards.Length % Players.Length > 0)
		{
			throw new InvalidOperationException("The number of players and cards requires an uneven distribution, which isn't supported in this test.");
		}

		int numberOfCardsPerPlayer = (Cards.Length - 3) / Players.Length;
		foreach (KeyValuePair<CardHolder, List<object>> pair in cardsPerPlayer)
		{
			constraints.Add(SelectionCountConstraint.ExactSelected(pair.Value, numberOfCardsPerPlayer));
		}

		Nodes = nodes.ToImmutable();
		StartingConstraints = constraints.ToImmutable();
		InteractivePlayer = Players[0];
		NumberOfCardsHeldByInteractivePlayer = numberOfCardsPerPlayer;
	}

	public ClueScenarioTests(ITestOutputHelper logger)
		: base(logger)
	{
		this.builder = new SolutionBuilder<bool>(Nodes, ImmutableArray.Create(true, false));
		this.builder.AddConstraints(StartingConstraints);
	}

	[Fact]
	public void CheckForConflictingConstraints_InitialGame()
	{
		(ImmutableArray<Card> chosen, ImmutableArray<Card> _) = ChooseRandomCards(Cards, NumberOfCardsHeldByInteractivePlayer);
		this.builder.AddConstraints(chosen.Select(card => SelectionCountConstraint.ExactSelected(new object[] { (card, InteractivePlayer) }, 1)));
		this.builder.ResolvePartially(this.TimeoutToken);
		this.PrintSolution();

		Assert.Null(this.builder.CheckForConflictingConstraints(this.TimeoutToken));
	}

	[Fact(Skip = "Slow test")]
	public void AnalyzeSolutions_InitialGame()
	{
		(ImmutableArray<Card> chosen, ImmutableArray<Card> _) = ChooseRandomCards(Cards, NumberOfCardsHeldByInteractivePlayer);
		this.builder.AddConstraints(chosen.Select(card => SelectionCountConstraint.ExactSelected(new object[] { (card, InteractivePlayer) }, 1)));
		this.builder.ResolvePartially(this.TimeoutToken);

		// This next step takes 3-12 seconds on a really fast machine.
		using var longTimeoutCts = new CancellationTokenSource(20 * 1024);
		SolutionBuilder<bool>.SolutionsAnalysis analysis = this.builder.AnalyzeSolutions(longTimeoutCts.Token);
		this.Logger.WriteLine("Identified {0} unique solutions.", analysis.ViableSolutionsFound);
		this.PrintSolutionAnalysis(analysis);
	}

	private static (ImmutableArray<Card> Chosen, ImmutableArray<Card> Remaining) ChooseRandomCards(ImmutableArray<Card> source, int count)
	{
		ImmutableArray<Card>.Builder chosen = ImmutableArray.CreateBuilder<Card>();
		var remaining = source.ToBuilder();
		var random = new Random();
		while (chosen.Count < count)
		{
			int index = random.Next(0, remaining.Count - 1);
			chosen.Add(remaining[index]);
			remaining.RemoveAt(index);
		}

		return (chosen.ToImmutable(), remaining.ToImmutable());
	}

	private void PrintSolution()
	{
		IEnumerable<CardHolder> cardHolders = Players.Concat(new[] { CaseFile });
		var sb = new StringBuilder();

		sb.AppendFormat(CultureInfo.CurrentCulture, "{0,-15}", string.Empty);
		foreach (CardHolder holder in cardHolders)
		{
			sb.AppendFormat(CultureInfo.CurrentCulture, "{0,-15}", holder);
		}

		sb.AppendLine();

		foreach (Card card in Cards)
		{
			sb.AppendFormat(CultureInfo.CurrentCulture, "{0,-15}", card);

			foreach (CardHolder cardHolder in Players.Concat(new[] { CaseFile }))
			{
				bool? state = this.builder[(card, cardHolder)];
				sb.AppendFormat(CultureInfo.CurrentCulture, "{0,7}{1,8}", state is bool selected ? (selected ? "X" : "O") : "?", string.Empty);
			}

			sb.AppendLine();
		}

		this.Logger.WriteLine(sb.ToString());
	}

	private void PrintSolutionAnalysis(SolutionBuilder<bool>.SolutionsAnalysis analysis)
	{
		IEnumerable<CardHolder> cardHolders = Players.Concat(new[] { CaseFile });
		var sb = new StringBuilder();

		sb.AppendFormat(CultureInfo.CurrentCulture, "{0,-15}", string.Empty);
		foreach (CardHolder holder in cardHolders)
		{
			sb.AppendFormat(CultureInfo.CurrentCulture, "{0,-15}", holder);
		}

		sb.AppendLine();

		foreach (Card card in Cards)
		{
			sb.AppendFormat(CultureInfo.CurrentCulture, "{0,-15}", card);

			foreach (CardHolder cardHolder in Players.Concat(new[] { CaseFile }))
			{
				double count = (double)analysis.GetNodeValueCount((card, cardHolder), true) / analysis.ViableSolutionsFound;
				sb.AppendFormat(CultureInfo.CurrentCulture, "{0,-15}", (int)(count * 100) + "%");
			}

			sb.AppendLine();
		}

		this.Logger.WriteLine(sb.ToString());
	}

	private class CardHolder
	{
		internal CardHolder(string name) => this.Name = name;

		internal string Name { get; }

		public override string ToString() => this.Name;
	}

	private class Card
	{
		internal Card(string title) => this.Title = title;

		internal string Title { get; }

		public override string ToString() => this.Title;
	}
}
