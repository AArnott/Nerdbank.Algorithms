// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NerdBank.Algorithms.NodeConstraintSelection
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;

	/// <summary>
	/// Describes node selection changes.
	/// </summary>
	public class SelectionChangedEventArgs
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SelectionChangedEventArgs"/> class.
		/// </summary>
		/// <param name="changedNodes">The nodes whose selection state has changed.</param>
		public SelectionChangedEventArgs(IReadOnlyCollection<object> changedNodes) => this.ChangedNodes = changedNodes;

		/// <summary>
		/// Gets the nodes whose selection state has changed.
		/// </summary>
		public IReadOnlyCollection<object> ChangedNodes { get; }
	}
}
