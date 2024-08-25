// Copyright (c) 2024 James Swerdfeger
// Licensed under the MIT license.

using BenchmarkDotNet.Attributes;
using UnitTestAssert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace jswerdfeger.BenchmarkDotNet.Assert.Tests;

/// <summary>
/// Tests using <see cref="ParamsAttribute"/>.
/// </summary>
[TestClass]
public class Params
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

		#region Params
		// Confirm empty works, and is treated as a single iteration.
		[Params()]
		public int MyBlank { get; set; }

		// Confirm one value works.
		[Params(1)]
		public int MyInt { get; set; }

		// Confirm multiple class values work.
		[Params("Hello", "World")]
		public string MyString { get; set; } = default!;

		[Params(3.14, 2.718, 1.609)]
		public double MyDouble { get; set; }

		private void AssertParameters()
		{
			ParamsAssert.Assert(this, nameof(MyBlank));
			ParamsAssert.Assert(this, nameof(MyInt));
			ParamsAssert.Assert(this, nameof(MyString));
			ParamsAssert.Assert(this, nameof(MyDouble));
		}
		#endregion

		public bool AssertMethod(string actual)
		{
			AssertMethodCount++;
			return actual == (MyInt + " " + MyString + " " + MyDouble);
		}
		[ThreadStatic] public static int AssertMethodCount = 0;

		[Benchmark]
		[Assert(nameof(AssertMethod))]
		public string Method1()
		{
			AssertParameters();
			var result = MyInt + " " + MyString + " " + MyDouble;
			UnitTestAssert.IsTrue(Method1Permutations.Add(result), $"A permutation has shown up more than once.");
			return result;
		}
		[ThreadStatic] public static HashSet<string> Method1Permutations = [];

		[Benchmark]
		[Assert(nameof(AssertMethod))]
		public string Method2()
		{
			AssertParameters();
			var result = $"{MyInt} {MyString} {MyDouble}";
			UnitTestAssert.IsTrue(Method2Permutations.Add(result), $"A permutation has shown up more than once.");
			return result;
		}
		[ThreadStatic] public static HashSet<string> Method2Permutations = [];

		[Benchmark]
		[Assert(nameof(AssertMethod))]
		public string Method3()
		{
			AssertParameters();
			var result = string.Format("{0} {1} {2}", MyInt, MyString, MyDouble);
			UnitTestAssert.IsTrue(Method3Permutations.Add(result), $"A permutation has shown up more than once.");
			return result;
		}
		[ThreadStatic] public static HashSet<string> Method3Permutations = [];
	}

	[TestMethod]
	public void Test()
	{
		int paramPermutations = 1 * 1 * 2 * 3;
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
		UnitTestAssert.AreEqual(method1Iterations, BenchmarkClass.Method1Permutations.Count);
		UnitTestAssert.AreEqual(method2Iterations, BenchmarkClass.Method2Permutations.Count);
		UnitTestAssert.AreEqual(method3Iterations, BenchmarkClass.Method3Permutations.Count);
		UnitTestAssert.AreEqual(totalIterations, BenchmarkClass.AssertMethodCount);
	}

}