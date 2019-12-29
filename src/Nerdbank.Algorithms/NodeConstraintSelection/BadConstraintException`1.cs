// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.Algorithms.NodeConstraintSelection
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// An exception thrown when some <see cref="IConstraint{TNodeState}"/> misbehaves.
	/// </summary>
	/// <typeparam name="TNodeState">The type of value that a node may be set to.</typeparam>
	[Serializable]
#pragma warning disable CA1032 // Implement standard exception constructors
	public class BadConstraintException<TNodeState> : Exception
		where TNodeState : struct
#pragma warning restore CA1032 // Implement standard exception constructors
	{
		/// <inheritdoc cref="BadConstraintException{TNodeState}(IConstraint{TNodeState}, string, Exception)"/>
		public BadConstraintException(IConstraint<TNodeState> constraint)
		{
			this.Constraint = constraint ?? throw new ArgumentNullException(nameof(constraint));
		}

		/// <inheritdoc cref="BadConstraintException{TNodeState}(IConstraint{TNodeState}, string, Exception)"/>
		public BadConstraintException(IConstraint<TNodeState> constraint, string message)
			: base(message)
		{
			this.Constraint = constraint ?? throw new ArgumentNullException(nameof(constraint));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BadConstraintException{TNodeState}"/> class.
		/// </summary>
		/// <param name="constraint">The constraint that misbehaved.</param>
		/// <param name="message">A message about how the constraint misbehaved.</param>
		/// <param name="inner">An inner exception.</param>
		public BadConstraintException(IConstraint<TNodeState> constraint, string message, Exception inner)
			: base(message, inner)
		{
			this.Constraint = constraint ?? throw new ArgumentNullException(nameof(constraint));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BadConstraintException{TNodeState}"/> class.
		/// </summary>
		/// <param name="info">Serialization info.</param>
		/// <param name="context">Serialization context.</param>
		protected BadConstraintException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			this.Constraint = (IConstraint<TNodeState>)info.GetValue(nameof(this.Constraint), typeof(IConstraint<TNodeState>));
		}

		/// <summary>
		/// Gets the bad constraint.
		/// </summary>
		public IConstraint<TNodeState> Constraint { get; }

		/// <inheritdoc/>
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue(nameof(this.Constraint), this.Constraint);
		}
	}
}
