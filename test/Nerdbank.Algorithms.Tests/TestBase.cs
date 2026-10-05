// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

public abstract class TestBase
{
	public TestBase()
    {
    }

	protected static TimeSpan UnexpectedTimeout => Debugger.IsAttached ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(5);

	protected CancellationToken TimeoutToken => TestContext.Current?.Execution.CancellationToken ?? new CancellationTokenSource(UnexpectedTimeout).Token;

	protected DefaultLogger Logger => TestContext.Current?.GetDefaultLogger() ?? throw new InvalidOperationException();
}
