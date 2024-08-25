// Copyright (c) 2024 James Swerdfeger
// Licensed under the MIT license.

using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace jswerdfeger.BenchmarkDotNet.Assert;

/// <summary>
/// Stores assert information for a method with the <see cref="BenchmarkAttribute"/>.
/// </summary>
internal class BenchmarkMethod
{
	private static readonly List<object?[]?> NoArguments = [null];

	private readonly MethodInfo _benchmarkMethod;

	// Like for Params attributes, the Arguments attributes are setup on an empty instance; they
	// cannot have any dependency on a parameter or GlobalSetup. Thus, we can create and cache it
	// immediately.
	//
	// This is designed to be exactly what you pass to the MethodInfo.Invoke method. Thus, if you
	// have no arguments to pass, this will store a single "null" entry.
	private readonly List<object?[]?> _argumentSets;

	// You may wonder why we bother going to the effort to create a delegate to perform the test.
	// Couldn't we just store the MethodInfo for benchmarkMethod and assertMethod, and call them
	// individually?
	//
	// The reason is because your benchmark and/or assert methods might very well make use of
	// types that reflection does not support, namely by-ref-like types like span. (And yes,
	// BenchmarkDotNet does support them, so we have to too.). Specifically, you cannot Invoke a
	// method that accepts by-ref-like parameters or returns a by-ref-like.
	//
	// But we can still test them by simply creating a new method that effectively calls those
	// methods but uses the equivalent managed types instead. And because your benchmark method
	// might return a span, we want to ensure we pass that same span reference to your assert
	// method, otherwise operations like equality checks would fail (they would be different spans
	// due to them pointing to different spots in memory). That's why we create a delegate that
	// both calls the benchmark as well as your assert method.
	private readonly TestMethod _testMethod;

	public string Name => _benchmarkMethod.Name;

	#region Construction
	internal BenchmarkMethod(MethodInfo benchmarkMethod, object instance)
	{
		if (benchmarkMethod.IsStatic)
		{
			throw new InvalidOperationException($"Benchmark methods cannot be static.");
		}

		_benchmarkMethod = benchmarkMethod;
		_argumentSets = GetArgumentsSets(benchmarkMethod, instance);

		var assertMethod = GetAssertMethod(benchmarkMethod);
		_testMethod = TestMethodGenerator.Generate(benchmarkMethod, assertMethod);
	}

	private static List<object?[]?> GetArgumentsSets(MethodInfo benchmarkMethod, object instance)
	{
		// BenchmarkDotNet will just ignore any Arguments attribute if your method has no
		// parameters.
		int benchmarkParametersLength = benchmarkMethod.GetParameters().Length;
		if (benchmarkParametersLength == 0) return NoArguments;

		// Beyond this point, since the benchmark method has arguments, you must supply them to
		// us one way or another.
		List<object?[]?> results = [];
		if (benchmarkMethod.TryGetCustomAttribute<ArgumentsSourceAttribute>(out var sourceAttribute))
		{
			results.AddRange(sourceAttribute.GetValues(instance, benchmarkMethod));
		}

		var argumentsAttributes = benchmarkMethod.GetCustomAttributes<ArgumentsAttribute>();
		foreach (var argumentsAttribute in argumentsAttributes)
		{
			var values = argumentsAttribute.Values;
			if (values.Length != benchmarkParametersLength)
			{
				throw new ArgumentException($"Arguments defined in {nameof(ArgumentsAttribute)} do not match the parameters in method {benchmarkMethod}.");
			}

			results.Add(values);
		}

		if (results.Count == 0)
		{
			throw new ArgumentException($"No arguments were supplied for method {benchmarkMethod}.");
		}
		return results;
	}

	private static MethodInfo GetAssertMethod(MethodInfo benchmarkMethod)
	{
		var assertAttribute = benchmarkMethod.GetCustomAttribute<AssertAttribute>()
			?? throw new ArgumentException($"Benchmark method {benchmarkMethod} must have an [{nameof(AssertAttribute)}] defined.");

		var declaringType = benchmarkMethod.DeclaringType!;
		var assertMethod = declaringType.GetMethod(assertAttribute.AssertMethodName)
			?? declaringType.DeclaringType?.GetMethod(assertAttribute.AssertMethodName)
			?? throw new ArgumentException($"No public method was found in {benchmarkMethod.DeclaringType} with the name {assertAttribute.AssertMethodName}.");

		if (assertMethod.ReturnType != typeof(bool))
		{
			throw new ArgumentException($"Assert method {assertAttribute.AssertMethodName} in type {benchmarkMethod.DeclaringType} must return a bool.");
		}

		if (!CheckAssertMethodParameters(benchmarkMethod, assertMethod))
		{
			var expectedArguments = benchmarkMethod.GetParameters().Select(p => p.ParameterType)
				.Append(benchmarkMethod.ReturnType);
			throw new ArgumentException($"Assert method {assertAttribute.AssertMethodName} in type {benchmarkMethod.DeclaringType} must accept the following parameters: ({string.Join(", ", expectedArguments)}). This matches the method's signature, including what the method returns (if anything).");
		}

		return assertMethod;
	}

	private static bool CheckAssertMethodParameters(MethodInfo benchmarkMethod, MethodInfo assertMethod)
	{
		var benchmarkParameters = benchmarkMethod.GetParameters();
		var assertParameters = assertMethod.GetParameters();

		var returnType = benchmarkMethod.ReturnType;
		if (returnType != typeof(void))
		{
			if (assertParameters.Length != benchmarkParameters.Length + 1
				|| !assertParameters[^1].ParameterType.IsAssignableFrom(returnType))
			{
				return false;
			}
		}
		else if (assertParameters.Length != benchmarkParameters.Length) return false;

		for (int i = 0; i < benchmarkParameters.Length; i++)
		{
			var assertParameterType = assertParameters[i].ParameterType;
			var benchmarkParameterType = benchmarkParameters[i].ParameterType;
			if (!assertParameterType.IsAssignableFrom(benchmarkParameterType))
			{
				return false;
			}
		}

		return true;
	}
	#endregion

	public IReadOnlyList<object?[]?> GetArgumentSets()
	{
		Debug.Assert(_argumentSets.Count >= 1);
		return _argumentSets;
	}

	public bool RunAssert(object instance, object?[]? arguments)
	{
		return _testMethod(instance, arguments);
	}


}
