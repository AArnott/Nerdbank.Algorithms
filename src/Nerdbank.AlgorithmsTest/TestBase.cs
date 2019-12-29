// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading;
using Xunit.Abstractions;

public abstract class TestBase
{
	public TestBase(ITestOutputHelper logger)
	{
		this.Logger = logger;
	}

	protected static TimeSpan UnexpectedTimeout => Debugger.IsAttached ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(5);

	protected CancellationToken TimeoutToken { get; } = new CancellationTokenSource(UnexpectedTimeout).Token;

	protected ITestOutputHelper Logger { get; }
}
