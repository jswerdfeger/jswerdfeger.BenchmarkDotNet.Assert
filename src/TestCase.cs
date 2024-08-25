// Copyright (c) 2024 James Swerdfeger
// Licensed under the MIT license.

using jswerdfeger.BenchmarkDotNet.Assert;

/// <summary>
/// Represents a single test case.
/// </summary>
internal class TestCase
{
	private readonly BenchmarkType _benchmarkType;
	private readonly MemberValue[] _memberValues;
	private readonly BenchmarkMethod _benchmarkMethod;
	private readonly object?[]? _arguments;

	internal TestCase(BenchmarkType benchmarkType, MemberValue[] memberValues,
		BenchmarkMethod benchmarkMethod, object?[]? arguments)
	{
		_benchmarkType = benchmarkType;
		_benchmarkMethod = benchmarkMethod;
		_memberValues = memberValues;
		_arguments = arguments;
	}

	internal void Run()
	{
		var benchmarkType = _benchmarkType;
		using var disposableInstance = benchmarkType.CreateInstance(_memberValues);
		var instance = disposableInstance.Instance;
		bool assertResult = _benchmarkMethod.RunAssert(instance, _arguments);

		if (!assertResult)
		{
			throw new AssertFailedException($"""
				Test failed on {_benchmarkMethod.Name} in {instance.GetType()}.
				Params: {string.Join(", ", _memberValues)}
				Arguments: ({string.Join(", ", _arguments ?? [])})
				""");
		}
	}

}
