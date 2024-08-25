// Copyright (c) 2024 James Swerdfeger
// Licensed under the MIT license.

using BenchmarkDotNet.Attributes;
using UnitTestAssert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace jswerdfeger.BenchmarkDotNet.Assert.Tests;

/// <summary>
/// Tests using <see cref="ArgumentsAttribute"/>.
/// </summary>
[TestClass]
public class Arguments
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

		#region Single attribute of single argument
		[Benchmark]
		[Arguments(1)]
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

		#region Multiple attributes on single argument, including null
		[Benchmark]
		[Arguments(null)]
		[Arguments("Hello")]
		[Arguments("World")]
		[Assert(nameof(AssertMethod2))]
		public string Method2(string? argument)
		{
			var result = $"{argument}";
			UnitTestAssert.IsTrue(Method2Permutations.Add(result), $"A permutation has shown up more than once.");
			return result;
		}
		[ThreadStatic] public static HashSet<string> Method2Permutations = [];

		public bool AssertMethod2(string? argument, string actual)
		{
			AssertMethod2Count++;
			return actual == (argument ?? "");
		}
		[ThreadStatic] public static int AssertMethod2Count = 0;
		#endregion

		#region Multiple attributes of multiple arguments
		[Benchmark]
		[Arguments(1, "One", 1.1)]
		[Arguments(2, "Two", 2.2)]
		[Arguments(3, "Three", 3.3)]
		[Assert(nameof(AssertMethod3))]
		public string Method3(int arg1, string arg2, double arg3)
		{
			var result = $"{arg1} {arg2} {arg3}";
			UnitTestAssert.IsTrue(Method3Permutations.Add(result), $"A permutation has shown up more than once.");
			return result;
		}
		[ThreadStatic] public static HashSet<string> Method3Permutations = [];

		public bool AssertMethod3(int arg1, string arg2, double arg3, string actual)
		{
			AssertMethod3Count++;
			return actual == arg1 + " " + arg2 + " " + arg3;
		}
		[ThreadStatic] public static int AssertMethod3Count = 0;
		#endregion

		#region Empty method arguments are ignored
		[Benchmark]
		[Arguments(1, 3, "hello", true)]
		[Assert(nameof(AssertMethod4))]
		public void Method4()
		{
			Method4Count++;
		}
		[ThreadStatic] public static int Method4Count = 0;

		public bool AssertMethod4()
		{
			return Method4Count == ++AssertMethod4Count;
		}
		[ThreadStatic] public static int AssertMethod4Count = 0;
		#endregion
	}

	[TestMethod]
	public void Test()
	{
		int paramPermutations = 1;
		int method1Arguments = 1;
		int method2Arguments = 3;
		int method3Arguments = 3;
		int method4Arguments = 1;

		int method1Iterations = method1Arguments * paramPermutations;
		int method2Iterations = method2Arguments * paramPermutations;
		int method3Iterations = method3Arguments * paramPermutations;
		int method4Iterations = method4Arguments * paramPermutations;
		int totalIterations = method1Iterations + method2Iterations + method3Iterations
			+ method4Iterations;
		BenchmarkAssert.Run<BenchmarkClass>();

		UnitTestAssert.AreEqual(totalIterations, BenchmarkClass.GlobalSetupCount);
		UnitTestAssert.AreEqual(totalIterations, BenchmarkClass.GlobalCleanupCount);
		UnitTestAssert.AreEqual(totalIterations, BenchmarkClass.IterationSetupCount);
		UnitTestAssert.AreEqual(totalIterations, BenchmarkClass.IterationCleanupCount);
		UnitTestAssert.AreEqual(method1Iterations, BenchmarkClass.Method1Permutations.Count);
		UnitTestAssert.AreEqual(method1Iterations, BenchmarkClass.AssertMethod1Count);
		UnitTestAssert.AreEqual(method2Iterations, BenchmarkClass.Method2Permutations.Count);
		UnitTestAssert.AreEqual(method2Iterations, BenchmarkClass.AssertMethod2Count);
		UnitTestAssert.AreEqual(method3Iterations, BenchmarkClass.Method3Permutations.Count);
		UnitTestAssert.AreEqual(method3Iterations, BenchmarkClass.AssertMethod3Count);
		UnitTestAssert.AreEqual(method4Iterations, BenchmarkClass.Method4Count);
		UnitTestAssert.AreEqual(method4Iterations, BenchmarkClass.AssertMethod4Count);
	}

}