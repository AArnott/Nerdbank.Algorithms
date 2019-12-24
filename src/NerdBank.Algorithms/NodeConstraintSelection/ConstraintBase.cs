// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NerdBank.Algorithms.NodeConstraintSelection
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	[Serializable]
	public abstract class ConstraintBase : IConstraint
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ConstraintBase"/> class.
		/// </summary>
		/// <param name="nodes"></param>
		protected ConstraintBase(IEnumerable<INode> nodes)
		{
			if (nodes is null)
			{
				throw new ArgumentNullException(nameof(nodes));
			}

			if (!nodes.Any())
			{
				throw new ArgumentException(Strings.ListCannotBeEmpty, nameof(nodes));
			}

			this.nodes = nodes.ToList();
		}

		private List<INode> nodes;

		/// <summary>
		/// The nodes involved in the constraint.
		/// </summary>
		public IEnumerable<INode> Nodes
		{
			get { return this.nodes; }
		}

		/// <summary>
		/// Calculates how many possible ending combinations of the selection
		/// of nodes that yet exist, given the already known nodes.
		/// </summary>
		public abstract int PossibleSolutionsCount { get; }

		/// <summary>
		/// Generates all remaining combinations of indeterminate nodes that
		/// could satisfy this constraint.
		/// </summary>
		public abstract IEnumerable<IList<INode>> PossibleSolutions { get; }

		public bool IsResolved
		{
			get { return this.Nodes.All(n => n.IsSelected.HasValue); }
		}

		public abstract bool CanResolve { get; }

		public abstract bool Resolve();

		public abstract bool IsSatisfied { get; }

		public abstract bool IsSatisfiable { get; }

		public virtual bool IsBroken
		{
			get { return !this.IsSatisfiable; }
		}

		public abstract bool IsBreakable { get; }

		public abstract bool IsMinimized { get; }

		public abstract bool IsWorthwhile { get; }

		public abstract bool IsWorthless { get; }

		protected void ThrowIfBroken()
		{
			if (this.IsBroken)
			{
				throw new BrokenConstraintException();
			}
		}

		protected internal static IEnumerable<T[]> ChooseResults<T>(IEnumerable<T> n, int k)
		{
			return chooseResults(n, k, new T[k]);
		}

		private static IEnumerable<T[]> chooseResults<T>(IEnumerable<T> n, int k, T[] l)
		{
			if (k == 0)
			{
				yield return (T[])l.Clone();
			}
			else
			{
				for (var i = 0; i < n.Count(); i++)
				{
					l[l.Length - k] = n.Skip(i).First();
					foreach (T[] r in chooseResults(n.Skip(i + 1), k - 1, l))
					{
						yield return r;
					}
				}
			}
		}

		protected internal static int Choose(int n, int k)
		{
			return Factorial(n) / (Factorial(k) * Factorial(n - k));
		}

		protected internal static int Factorial(int n)
		{
			int result;
			for (result = 1; n > 1; n--)
			{
				result *= n;
			}

			return result;
		}
	}
}
