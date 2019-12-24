// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NerdBank.Algorithms.NodeConstraintSelection
{
	using System;

	[Serializable]
	public class BrokenConstraintException : InvalidOperationException
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BrokenConstraintException"/> class.
		/// </summary>
		public BrokenConstraintException()
			: this(Strings.BrokenConstraint)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BrokenConstraintException"/> class.
		/// </summary>
		/// <param name="message"></param>
		public BrokenConstraintException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BrokenConstraintException"/> class.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="inner"></param>
		public BrokenConstraintException(string message, Exception inner)
			: base(message, inner)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BrokenConstraintException"/> class.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		protected BrokenConstraintException(
			System.Runtime.Serialization.SerializationInfo info,
			System.Runtime.Serialization.StreamingContext context)
			: base(info, context)
		{
		}
	}
}
