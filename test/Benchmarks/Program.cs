// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Running;

namespace Nerdbank.Algorithms.Benchmarks;

/// <summary>
/// Entry point for microbenchmarks.
/// </summary>
public static class Program
{
	/// <summary>
	/// Runs BenchmarkDotNet.
	/// </summary>
	/// <param name="args">BenchmarkDotNet arguments.</param>
	public static void Main(string[] args)
	{
		if (QuickCompare.TryRun(args))
		{
			return;
		}

		BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
	}
}
