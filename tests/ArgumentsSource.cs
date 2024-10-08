// Copyright (c) 2024 James Swerdfeger
// Licensed under the MIT license.

using BenchmarkDotNet.Attributes;
using UnitTestAssert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace jswerdfeger.BenchmarkDotNet.Assert.Tests;

/// <summary>
/// Tests using <see cref="ArgumentsSourceAttribute"/>.
/// </summary>
[TestClass]
public class ArgumentsSource
{
	public class BenchmarkClass
	{
		#region Init and Cleanup
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
		#endregion

		#region Single argument via instance property
		public int[] Method1Arguments => [1, 3, 5, 7, 9];

		[Benchmark]
		[ArgumentsSource(nameof(Method1Arguments))]
		[Assert(nameof(AssertMethod1))]
		public string Method1(int argument)
		{
			var result = $"{argument}";
			UnitTestAssert.IsTrue(Method1Permutations.Add(result), $"A permutation has shown up more than once.");
			return result;
		}
		[ThreadStatic] public static HashSet<string> Method1Permutations = [];

		public bool AssertMethod1(int argument, string actual)
		{
			AssertMethod1Count++;
			return actual == argument.ToString();
		}
		[ThreadStatic] public static int AssertMethod1Count = 0;
		#endregion

		#region Multiple arguments via static method
		public static IEnumerable<object[]> Method2Arguments() =>
		[
			[1, "one", 1.1],
			[2, "two", 2.2],
			[3, "three", 3.3],
			[4, "four", 4.4],
			[5, "five", 5.5],
		];

		[Benchmark]
		[ArgumentsSource(nameof(Method2Arguments))]
		[Assert(nameof(AssertMethod2))]
		public string Method2(int arg1, string arg2, double arg3)
		{
			var result = $"{arg1} {arg2} {arg3}";
			UnitTestAssert.IsTrue(Method2Permutations.Add(result), $"A permutation has shown up more than once.");
			return result;
		}
		[ThreadStatic] public static HashSet<string> Method2Permutations = [];

		public bool AssertMethod2(int arg1, string arg2, double arg3, string actual)
		{
			AssertMethod2Count++;
			return actual == arg1 + " " + arg2 + " " + arg3;
		}
		[ThreadStatic] public static int AssertMethod2Count = 0;
		#endregion

		#region Empty method arguments are ignored
		public int[] Method3Arguments => [1, 2, 3, 4, 5];

		[Benchmark]
		[ArgumentsSource(nameof(Method3Arguments))]
		[Assert(nameof(AssertMethod3))]
		public void Method3()
		{
			Method3Count++;
		}
		[ThreadStatic] public static int Method3Count = 0;

		public bool AssertMethod3()
		{
			return Method3Count == ++AssertMethod3Count;
		}
		[ThreadStatic] public static int AssertMethod3Count = 0;
		#endregion
	}

	[TestMethod]
	public void Test()
	{
		int paramPermutations = 1;
		int method1Arguments = 5;
		int method2Arguments = 5;
		int method3Arguments = 1;

		int method1Iterations = method1Arguments * paramPermutations;
		int method2Iterations = method2Arguments * paramPermutations;
		int method3Iterations = method3Arguments * paramPermutations;
		int totalIterations = method1Iterations + method2Iterations + method3Iterations;
		BenchmarkAssert.Run<BenchmarkClass>();

		UnitTestAssert.AreEqual(totalIterations, BenchmarkClass.GlobalSetupCount);
		UnitTestAssert.AreEqual(totalIterations, BenchmarkClass.GlobalCleanupCount);
		UnitTestAssert.AreEqual(totalIterations, BenchmarkClass.IterationSetupCount);
		UnitTestAssert.AreEqual(totalIterations, BenchmarkClass.IterationCleanupCount);
		UnitTestAssert.AreEqual(method1Iterations, BenchmarkClass.Method1Permutations.Count);
		UnitTestAssert.AreEqual(method1Iterations, BenchmarkClass.AssertMethod1Count);
		UnitTestAssert.AreEqual(method2Iterations, BenchmarkClass.Method2Permutations.Count);
		UnitTestAssert.AreEqual(method2Iterations, BenchmarkClass.AssertMethod2Count);
		UnitTestAssert.AreEqual(method3Iterations, BenchmarkClass.Method3Count);
		UnitTestAssert.AreEqual(method3Iterations, BenchmarkClass.AssertMethod3Count);
	}

}