// Copyright (c) 2024 James Swerdfeger
// Licensed under the MIT license.

using BenchmarkDotNet.Attributes;
using UnitTestAssert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace jswerdfeger.BenchmarkDotNet.Assert.Test;

/// <summary>
/// Tests using all arguments and parameter options within the same class.
/// </summary>
[TestClass]
public class All
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

		[Params("Hello", "Hola", "Bonjour")]
		public string MyString { get; set; } = default!;

		[ParamsAllValues]
		public bool MyBool { get; set; }

		public int[][] MyArguments =>
		[
			[1, 2],
			[3, 4],
			[5, 6],
			[7, 8],
		];

		public bool AssertMethod(int arg1, int arg2, string actual)
		{
			AssertMethodCount++;
			return actual == (arg1 + arg2) + " " + MyString + " " + MyBool;
		}
		[ThreadStatic] public static int AssertMethodCount = 0;

		[Benchmark]
		[ArgumentsSource(nameof(MyArguments))]
		[Assert(nameof(AssertMethod))]
		public string Method1(int arg1, int arg2)
		{
			var result = $"{(arg1 + arg2)} {MyString} {MyBool}";
			UnitTestAssert.IsTrue(Method1Permutations.Add(result), $"A permutation has shown up more than once.");
			return result;
		}
		[ThreadStatic] public static HashSet<string> Method1Permutations = [];

		[Benchmark]
		[ArgumentsSource(nameof(MyArguments))]
		[Assert(nameof(AssertMethod))]
		public string Method2(int arg1, int arg2)
		{
			var result = string.Format("{0} {1} {2}", arg1 + arg2, MyString, MyBool);
			UnitTestAssert.IsTrue(Method2Permutations.Add(result), $"A permutation has shown up more than once.");
			return result;
		}
		[ThreadStatic] public static HashSet<string> Method2Permutations = [];
	}

	[TestMethod]
	public void Test()
	{
		int paramPermutations = 3 * 2;
		int method1Arguments = 4;
		int method2Arguments = 4;

		int method1Iterations = method1Arguments * paramPermutations;
		int method2Iterations = method2Arguments * paramPermutations;
		int totalIterations = method1Iterations + method2Iterations;
		BenchmarkAssert.Run<BenchmarkClass>();

		UnitTestAssert.AreEqual(totalIterations, BenchmarkClass.GlobalSetupCount);
		UnitTestAssert.AreEqual(totalIterations, BenchmarkClass.GlobalCleanupCount);
		UnitTestAssert.AreEqual(totalIterations, BenchmarkClass.IterationSetupCount);
		UnitTestAssert.AreEqual(totalIterations, BenchmarkClass.IterationCleanupCount);
		UnitTestAssert.AreEqual(method1Iterations, BenchmarkClass.Method1Permutations.Count);
		UnitTestAssert.AreEqual(method2Iterations, BenchmarkClass.Method2Permutations.Count);
		UnitTestAssert.AreEqual(totalIterations, BenchmarkClass.AssertMethodCount);
	}

}