// Copyright (c) 2024 James Swerdfeger
// Licensed under the MIT license.

using System;

namespace jswerdfeger.BenchmarkDotNet.Assert;

/// <summary>
/// Provides a means of asserting benchmarking code.
/// </summary>
public static class BenchmarkAssert
{
	/// <summary>
	/// Runs an assertion of all benchmark methods in your supplied type.
	/// </summary>
	public static void Run<T>()
	{
		AssertType(typeof(T));
	}

	/// <summary>
	/// Runs an assertion of all benchmark methods in your supplied type.
	/// </summary>
	public static void Run(Type type)
	{
		AssertType(type);
	}

	/// <summary>
	/// Runs an assertion of all benchmark methods in all your supplied types.
	/// </summary>
	public static void Run(Type[] types)
	{
		foreach (var type in types)
		{
			AssertType(type);
		}
	}

	private static void AssertType(Type type)
	{
		BenchmarkType benchmarkType = new(type);
		foreach (var testCase in benchmarkType.BuildTestCases())
		{
			testCase.Run();
		}
	}

}
