// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Nerdbank.Algorithms.NodeConstraintSelection;

/// <summary>
/// Configuration for a node constraint selection problem space.
/// </summary>
/// <typeparam name="TNodeState">The type of nodes in the problem space.</typeparam>
public class Configuration<TNodeState>
	where TNodeState : unmanaged
{
	/// <summary>
	/// Initializes a new instance of the <see cref="Configuration{TNodeState}"/> class.
	/// </summary>
	/// <param name="nodes">The nodes in the problem/solution.</param>
	/// <param name="resolvedNodeStates">An array of allowed values for each node.</param>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="nodes"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentException">Thrown if <paramref name="nodes"/> is empty or contains duplicates.</exception>
	public Configuration(ImmutableArray<object> nodes, ImmutableArray<TNodeState> resolvedNodeStates)
	{
		if (nodes.IsDefault)
		{
			throw new ArgumentNullException(nameof(nodes));
		}

		if (nodes.IsEmpty)
		{
			throw new ArgumentException(Strings.NonEmptyArrayRequired, nameof(nodes));
		}

		if (resolvedNodeStates.IsDefaultOrEmpty)
		{
			throw new ArgumentException(Strings.NonEmptyArrayRequired, nameof(resolvedNodeStates));
		}

		if (resolvedNodeStates.Length < 2)
		{
			throw new ArgumentException(Strings.AtLeastTwoNodeStatesRequired, nameof(resolvedNodeStates));
		}

		this.Nodes = nodes;
		this.ResolvedNodeStates = resolvedNodeStates;
		this.Index = CreateNodeIndex(nodes);
		this.ScenarioPool = new ScenarioPool<TNodeState>(this);
	}

	/// <summary>
	/// Gets the nodes in the problem space.
	/// </summary>
	public ImmutableArray<object> Nodes { get; }

	/// <summary>
	/// Gets a map of nodes to their index within <see cref="Nodes"/>.
	/// </summary>
	public ReadOnlyDictionary<object, int> Index { get; }

	/// <summary>
	/// Gets the values a resolved node may take.
	/// </summary>
	public ImmutableArray<TNodeState> ResolvedNodeStates { get; }

	/// <summary>
	/// Gets the scenario pool to use.
	/// </summary>
	internal ScenarioPool<TNodeState> ScenarioPool { get; }

	/// <summary>
	/// Writes all the nodes with their current state as text.
	/// </summary>
	/// <param name="writer">The writer to render the nodes to.</param>
	/// <param name="scenario">The scenario to pull node values from.</param>
	public virtual void WriteScenario(TextWriter writer, Scenario<TNodeState> scenario)
	{
		if (writer is null)
		{
			throw new ArgumentNullException(nameof(writer));
		}

		if (scenario is null)
		{
			throw new ArgumentNullException(nameof(scenario));
		}

		for (int i = 0; i < this.Nodes.Length; i++)
		{
			writer.Write(this.Nodes[i].ToString());
			writer.Write(": ");
			writer.WriteLine(scenario[i]);
		}
	}

	/// <inheritdoc cref="WriteScenario(TextWriter, Scenario{TNodeState})"/>
	public string ToString(Scenario<TNodeState> scenario)
	{
		StringWriter sw = new();
		this.WriteScenario(sw, scenario);
		return sw.ToString();
	}

	/// <summary>
	/// Creates a map of nodes to the index in a list.
	/// </summary>
	/// <param name="nodes">The list of nodes.</param>
	/// <returns>The map of nodes to where they are found in the <paramref name="nodes"/> list.</returns>
	private static ReadOnlyDictionary<object, int> CreateNodeIndex(ImmutableArray<object> nodes)
	{
		var lookup = new Dictionary<object, int>(nodes.Length);
		for (int i = 0; i < nodes.Length; i++)
		{
			lookup.Add(nodes[i], i);
		}

		return new ReadOnlyDictionary<object, int>(lookup);
	}
}
