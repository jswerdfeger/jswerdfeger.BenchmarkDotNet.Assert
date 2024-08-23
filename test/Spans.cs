// Copyright (c) 2024 James Swerdfeger
// Licensed under the MIT license.

#if (NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER)

using BenchmarkDotNet.Attributes;
using UnitTestAssert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace jswerdfeger.BenchmarkDotNet.Assert.Test;

/// <summary>
/// Tests using spans in the method arguments. This confirms that methods with by-ref-like
/// arguments can still be tested.
/// </summary>
[TestClass]
public class Spans
{
	public class BenchmarkClass
	{
		[GlobalSetup]
		public void GlobalSetup()
		{
			GlobalSetupCount++;
		}
		[ThreadStatic] public static int GlobalSetupCount = 0;

		[GlobalCleanup]
		public void GlobalCleanup()
		{
			GlobalCleanupCount++;
		}
		[ThreadStatic] public static int GlobalCleanupCount = 0;

		[IterationSetup]
		public void IterationSetup()
		{
			IterationSetupCount++;
		}
		[ThreadStatic] public static int IterationSetupCount = 0;

		[IterationCleanup]
		public void IterationCleanup()
		{
			IterationCleanupCount++;
		}
		[ThreadStatic] public static int IterationCleanupCount = 0;

		#region Method1
		// Test a method with no arguments and a span return.
		[Benchmark]
		[Assert(nameof(AssertMethod1))]
		public ReadOnlySpan<char> Method1()
		{
			Method1Count++;
			return "Hello";
		}
		[ThreadStatic] public static int Method1Count = 0;

		public bool AssertMethod1(ReadOnlySpan<char> actual)
		{
			AssertMethod1Count++;
			return actual.Equals("Hello", StringComparison.Ordinal);
		}
		[ThreadStatic] public static int AssertMethod1Count = 0;
		#endregion

		#region Method2
		// Test a method that uses ReadOnlySpan<char> arguments passed by strings, and returns
		// a string.
		public IEnumerable<string> Arguments2 =>
		[
			"Hello", "Hola", "Bonjour"
		];

		[Benchmark]
		[ArgumentsSource(nameof(Arguments2))]
		[Assert(nameof(AssertMethod2))]
		public string Method2(ReadOnlySpan<char> chars)
		{
			Method2Count++;
			return string.Concat(chars, "!");
		}
		[ThreadStatic] public static int Method2Count = 0;

		public bool AssertMethod2(ReadOnlySpan<char> chars, string actual)
		{
			AssertMethod2Count++;
			return actual == string.Concat(chars, "!");
		}
		[ThreadStatic] public static int AssertMethod2Count = 0;
		#endregion

		#region Method3
		// Test a method that uses Span<int> arguments, and returns a Span<int>.
		public IEnumerable<int[]> Arguments3 =>
		[
			[1, 2, 3],
			[1, 2, 3, 4, 5],
			[1, 2, 3, 4, 5, 6, 7, 8, 9, 10],
		];

		[Benchmark]
		[ArgumentsSource(nameof(Arguments3))]
		[Assert(nameof(AssertMethod3))]
		public Span<int> Method3(Span<int> ints)
		{
			Method3Count++;
			return ints[0..2];
		}
		[ThreadStatic] public static int Method3Count = 0;

		// Test a static assert method.
		public static bool AssertMethod3(Span<int> ints, Span<int> actual)
		{
			AssertMethod3Count++;
			return actual == ints.Slice(0, 2);
		}
		[ThreadStatic] public static int AssertMethod3Count = 0;
		#endregion
	}

	[TestMethod]
	public void Test()
	{
		int paramPermutations = 1;
		int method1Arguments = 1;
		int method2Arguments = 3;
		int method3Arguments = 3;

		int method1Iterations = method1Arguments * paramPermutations;
		int method2Iterations = method2Arguments * paramPermutations;
		int method3Iterations = method3Arguments * paramPermutations;
		int totalIterations = method1Iterations + method2Iterations + method3Iterations;
		BenchmarkAssert.Run<BenchmarkClass>();

		UnitTestAssert.AreEqual(totalIterations, BenchmarkClass.GlobalSetupCount);
		UnitTestAssert.AreEqual(totalIterations, BenchmarkClass.GlobalCleanupCount);
		UnitTestAssert.AreEqual(totalIterations, BenchmarkClass.IterationSetupCount);
		UnitTestAssert.AreEqual(totalIterations, BenchmarkClass.IterationCleanupCount);
		UnitTestAssert.AreEqual(method1Iterations, BenchmarkClass.Method1Count);
		UnitTestAssert.AreEqual(method1Iterations, BenchmarkClass.AssertMethod1Count);
		UnitTestAssert.AreEqual(method2Iterations, BenchmarkClass.Method2Count);
		UnitTestAssert.AreEqual(method2Iterations, BenchmarkClass.AssertMethod2Count);
		UnitTestAssert.AreEqual(method3Iterations, BenchmarkClass.Method2Count);
		UnitTestAssert.AreEqual(method3Iterations, BenchmarkClass.AssertMethod2Count);
	}

}
#endif