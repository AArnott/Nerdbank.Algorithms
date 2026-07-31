// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Globalization;

namespace Nerdbank.Algorithms.Benchmarks;

/// <summary>
/// Lightweight A/B harness used when comparing two library builds without full BenchmarkDotNet overhead.
/// Invoke with <c>--quick</c>.
/// </summary>
internal static class QuickCompare
{
	/// <summary>
	/// Runs the quick comparison suite if requested.
	/// </summary>
	/// <param name="args">Command-line arguments.</param>
	/// <returns><see langword="true"/> if the quick suite ran and the process should exit.</returns>
	internal static bool TryRun(string[] args)
	{
		if (!args.Any(static a => string.Equals(a, "--quick", StringComparison.OrdinalIgnoreCase)))
		{
			return false;
		}

		int iterations = 9;
		int warmup = 3;
		for (int i = 0; i < args.Length - 1; i++)
		{
			if (string.Equals(args[i], "--iterations", StringComparison.OrdinalIgnoreCase)
				&& int.TryParse(args[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
			{
				iterations = Math.Max(1, parsed);
			}
		}

		var bench = new NodeConstraintSelectionBenchmarks();
		Console.WriteLine($"Quick compare against {typeof(Nerdbank.Algorithms.NodeConstraintSelection.SolutionBuilder<>).Assembly.GetName().Version}");
		Console.WriteLine($"Location: {typeof(Nerdbank.Algorithms.NodeConstraintSelection.SolutionBuilder<>).Assembly.Location}");
		Console.WriteLine($"Warmup={warmup}, Measured={iterations}");
		Console.WriteLine();

		Measure("SudokuHard_AnalyzeSolutions", iterations, warmup, () => bench.SudokuHard_AnalyzeSolutions());
		Measure("SudokuHard_ResolveAndCheck", iterations, warmup, () => bench.SudokuHard_ResolveAndCheck());
		Measure("BoolPairs10_AnalyzeSolutions", iterations, warmup, () => bench.BoolPairs10_AnalyzeSolutions());
		Measure("BoolPairs14_AnalyzeSolutions", iterations, warmup, () => bench.BoolPairs14_AnalyzeSolutions());
		Measure("MultiState_GetProbableSolution", iterations, warmup, () => bench.MultiState_GetProbableSolution());
		return true;
	}

	private static void Measure(string name, int iterations, int warmup, Func<long> work)
	{
		for (int i = 0; i < warmup; i++)
		{
			_ = work();
		}

		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();

		var timesMs = new double[iterations];
		long checksum = 0;
		long alloc = 0;
		for (int i = 0; i < iterations; i++)
		{
			long before = GC.GetAllocatedBytesForCurrentThread();
			Stopwatch sw = Stopwatch.StartNew();
			checksum += work();
			sw.Stop();
			long after = GC.GetAllocatedBytesForCurrentThread();
			timesMs[i] = sw.Elapsed.TotalMilliseconds;
			alloc += Math.Max(0, after - before);
		}

		Array.Sort(timesMs);
		double mean = timesMs.Average();
		double median = timesMs[timesMs.Length / 2];
		double min = timesMs[0];
		double allocKb = alloc / (double)iterations / 1024.0;

		Console.WriteLine(name);
		Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"  mean={mean,8:F2} ms  median={median,8:F2} ms  min={min,8:F2} ms  alloc/op≈{allocKb,8:F1} KB  checksum={checksum}"));
		Console.WriteLine();
	}
}
