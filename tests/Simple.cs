// Copyright (c) 2024 James Swerdfeger
// Licensed under the MIT license.

using BenchmarkDotNet.Attributes;
using UnitTestAssert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace jswerdfeger.BenchmarkDotNet.Assert.Tests;

/// <summary>
/// Tests using a simple benchmarking class with no arguments or parameters.
/// </summary>
[TestClass]
public class Simple
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

		#region Instance Assert of int return
		[Benchmark]
		[Assert(nameof(AssertMethod1))]
		public int Method1()
		{
			Method1Count++;
			return 1;
		}
		[ThreadStatic] public static int Method1Count = 0;

		public bool AssertMethod1(int actual)
		{
			AssertMethod1Count++;
			return actual == 1;
		}
		[ThreadStatic] public static int AssertMethod1Count = 0;
		#endregion

		#region Static Assert of string return
		[Benchmark]
		[Assert(nameof(AssertMethod2))]
		public string Method2()
		{
			Method2Count++;
			return "Hello World";
		}
		[ThreadStatic] public static int Method2Count = 0;

		public static bool AssertMethod2(string actual)
		{
			AssertMethod2Count++;
			return actual == "Hello World";
		}
		[ThreadStatic] public static int AssertMethod2Count = 0;
		#endregion

		#region Instance Assert of void return
		[Benchmark]
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
		int method1Arguments = 1;
		int method2Arguments = 1;
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
		UnitTestAssert.AreEqual(method1Iterations, BenchmarkClass.Method1Count);
		UnitTestAssert.AreEqual(method1Iterations, BenchmarkClass.AssertMethod1Count);
		UnitTestAssert.AreEqual(method2Iterations, BenchmarkClass.Method2Count);
		UnitTestAssert.AreEqual(method2Iterations, BenchmarkClass.AssertMethod2Count);
		UnitTestAssert.AreEqual(method3Iterations, BenchmarkClass.Method3Count);
		UnitTestAssert.AreEqual(method3Iterations, BenchmarkClass.AssertMethod3Count);
	}

}