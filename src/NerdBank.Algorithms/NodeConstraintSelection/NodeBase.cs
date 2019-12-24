// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NerdBank.Algorithms.NodeConstraintSelection
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Globalization;

	[Serializable]
#pragma warning disable CA1036 // Override methods on comparable types
	public abstract class NodeBase : INode
#pragma warning restore CA1036 // Override methods on comparable types
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NodeBase"/> class.
		/// </summary>
		public NodeBase()
		{
			this.isSelected.Push(null); // push the actual selection status onto the stack.
		}

		private Stack<bool?> isSelected = new Stack<bool?>();

		/// <summary>
		/// Whether the node is "selected" in a hypothetical solution.
		/// </summary>
		public bool? IsSelected
		{
			get
			{
				return this.isSelected.Peek();
			}

			set
			{
				if (value == this.IsSelected)
				{
					return; // nothing to change
				}

				// if changing a known state (not so known, is it?!)
				if (this.IsSelected.HasValue)
				{
					throw new InvalidOperationException(
						string.Format(
							CultureInfo.CurrentCulture,
							Strings.PropertyChangeFromToError,
							this.IsSelected.Value,
							value.HasValue ? value.Value.ToString(CultureInfo.CurrentCulture) : "<NULL>"));
				}

				var oldValue = this.IsSelected;
				this.isSelected.Pop();
				this.isSelected.Push(value);
				if (!this.IsSimulating)
				{
					this.OnPropertyChanged(nameof(this.IsSelected));
				}
			}
		}

		public void Reset()
		{
			if (this.IsSimulating)
			{
				throw new InvalidOperationException(Strings.CannotResetNodeDuringSimulation);
			}

			this.isSelected.Pop();
			this.isSelected.Push(null);
			this.OnPropertyChanged(nameof(this.IsSelected));
		}

		public bool IsSimulating
		{
			get { return this.isSelected.Count > 1; }
		}

		public void PushSimulation()
		{
			this.isSelected.Push(this.IsSelected);
		}

		public bool PopSimulation()
		{
			if (!this.IsSimulating)
			{
				throw new InvalidOperationException(Strings.NotSimulating);
			}

			this.isSelected.Pop();
			return this.isSelected.Count == 1;
		}

		/// <summary>
		/// Fires when the <see cref="IsSelected"/> property changes.
		/// </summary>
		public event PropertyChangedEventHandler? PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private static object syncLastAssignedUniqueId = new object();
		private static int lastAssignedUniqueNodeId = -1;

		private static int acquireUniqueNodeId()
		{
			lock (syncLastAssignedUniqueId)
			{
				return ++lastAssignedUniqueNodeId;
			}
		}

		private readonly int uniqueId = acquireUniqueNodeId();

		public int CompareTo(object other)
		{
			if (other is null)
			{
				throw new ArgumentNullException(nameof(other));
			}

			var otherNode = (NodeBase)other;
			return this.uniqueId.CompareTo(otherNode.uniqueId);
		}
	}
}
