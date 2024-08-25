// Copyright (c) 2024 James Swerdfeger
// Licensed under the MIT license.

using BenchmarkDotNet.Attributes;
using UnitTestAssert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace jswerdfeger.BenchmarkDotNet.Assert.Tests;

/// <summary>
/// Tests for an assertion failure.
/// </summary>
[TestClass]
public class AssertFailure
{
	public class BenchmarkClass
	{
		public bool AssertMethod1(bool actual)
		{
			return actual == false;
		}

		[Benchmark]
		[Assert(nameof(AssertMethod1))]
		public bool Method1()
		{
			return true;
		}
	}

	[TestMethod]
	public void Test()
	{
		UnitTestAssert.ThrowsException<AssertFailedException>(()
			=> BenchmarkAssert.Run<BenchmarkClass>());
	}

}