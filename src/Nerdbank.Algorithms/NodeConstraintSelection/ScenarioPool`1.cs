// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace Nerdbank.Algorithms.NodeConstraintSelection;

/// <summary>
/// Object pooling for <see cref="Scenario{TNodeState}"/> objects.
/// </summary>
/// <typeparam name="TNodeState">The type of value that a node may be set to.</typeparam>
/// <remarks>
/// Thread safety: This class is thread-safe.
/// </remarks>
internal class ScenarioPool<TNodeState>
	where TNodeState : unmanaged
{
	private readonly ConcurrentBag<Scenario<TNodeState>> bag = new();
	private readonly Configuration<TNodeState> configuration;

	/// <summary>
	/// Initializes a new instance of the <see cref="ScenarioPool{TNodeState}"/> class.
	/// </summary>
	/// <param name="configuration">The problem space configuration.</param>
	internal ScenarioPool(Configuration<TNodeState> configuration)
	{
		this.configuration = configuration;
	}

	/// <summary>
	/// Acquires a recycled or new <see cref="Scenario{TNodeState}"/> instance.
	/// </summary>
	/// <param name="copyFrom">The scenario that the returned one will copy from.</param>
	/// <returns>An instance of <see cref="Scenario{TNodeState}"/>.</returns>
	internal Scenario<TNodeState> Take(Scenario<TNodeState> copyFrom)
	{
		Scenario<TNodeState> result = this.bag.TryTake(out Scenario<TNodeState>? scenario) ? scenario : new(this.configuration);
		result.CopyFrom(copyFrom);
		return result;
	}

	/// <summary>
	/// Returns a <see cref="Scenario{TNodeState}"/> for recycling.
	/// </summary>
	/// <param name="scenario">The instance to recycle.</param>
	internal void Return(Scenario<TNodeState> scenario) => this.bag.Add(scenario);
}
