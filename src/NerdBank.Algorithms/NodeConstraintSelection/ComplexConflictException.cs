// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NerdBank.Algorithms.NodeConstraintSelection
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// An exception thrown when a solution has constraints that are so conflicted
	/// that no subset of constraints could be found which if removed would remove the conflict.
	/// </summary>
	[Serializable]
#pragma warning disable CA1032 // Implement standard exception constructors
	public class ComplexConflictException : Exception
#pragma warning restore CA1032 // Implement standard exception constructors
	{
		/// <inheritdoc cref="ComplexConflictException(string, Exception)"/>
		public ComplexConflictException()
		{
		}

		/// <inheritdoc cref="ComplexConflictException(string, Exception)"/>
		public ComplexConflictException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ComplexConflictException"/> class.
		/// </summary>
		/// <param name="message">A message about how the constraint misbehaved.</param>
		/// <param name="inner">An inner exception.</param>
		public ComplexConflictException(string message, Exception inner)
			: base(message, inner)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ComplexConflictException"/> class.
		/// </summary>
		/// <param name="info">Serialization info.</param>
		/// <param name="context">Serialization context.</param>
		protected ComplexConflictException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
