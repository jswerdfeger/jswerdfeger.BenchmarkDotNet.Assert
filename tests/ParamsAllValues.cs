// Copyright (c) 2024 James Swerdfeger
// Licensed under the MIT license.

using BenchmarkDotNet.Attributes;
using UnitTestAssert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace jswerdfeger.BenchmarkDotNet.Assert.Tests;

/// <summary>
/// Tests using <see cref="ParamsAllValuesAttribute"/>.
/// </summary>
[TestClass]
public class ParamsAllValues
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
		[ParamsAllValues]
		public bool MyBool { get; set; }

		[ParamsAllValues]
		public bool? MyNullableBool;

		private void AssertParameters()
		{
			ParamsAssert.Assert(this, nameof(MyBool));
			ParamsAssert.Assert(this, nameof(MyNullableBool));
		}
		#endregion

		public bool AssertMethod(string actual)
		{
			AssertMethodCount++;
			return actual == (MyBool + " " + (MyNullableBool?.ToString() ?? "null"));
		}
		[ThreadStatic] public static int AssertMethodCount = 0;

		[Benchmark]
		[Assert(nameof(AssertMethod))]
		public string Method1()
		{
			AssertParameters();
			var result = $"{MyBool} {MyNullableBool?.ToString() ?? "null"}";
			UnitTestAssert.IsTrue(Method1Permutations.Add(result), $"A permutation has shown up more than once.");
			return result;
		}
		[ThreadStatic] public static HashSet<string> Method1Permutations = [];

		[Benchmark]
		[Assert(nameof(AssertMethod))]
		public string Method2()
		{
			AssertParameters();
			var result = string.Format("{0} {1}", MyBool, MyNullableBool?.ToString() ?? "null");
			UnitTestAssert.IsTrue(Method2Permutations.Add(result), $"A permutation has shown up more than once.");
			return result;
		}
		[ThreadStatic] public static HashSet<string> Method2Permutations = [];
	}

	[TestMethod]
	public void Test()
	{
		int paramPermutations = 2 * 3;
		int method1Arguments = 1;
		int method2Arguments = 1;

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