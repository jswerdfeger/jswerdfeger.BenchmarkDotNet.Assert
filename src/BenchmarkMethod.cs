// Copyright (c) 2024 James Swerdfeger
// Licensed under the MIT license.

using BenchmarkDotNet.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace jswerdfeger.BenchmarkDotNet.Assert;

internal class BenchmarkMethod
{
	private readonly BenchmarkType _benchmarkType;

	private readonly MethodInfo _benchmarkMethod;
	private readonly MethodInfo? _argumentsSource;

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
	// methods but uses the-equivalent managed types instead. And because your benchmark method
	// might return a span, we want to ensure we pass that same span reference to your assert
	// method, otherwise operations like equality checks would fail (they would be different spans
	// due to them pointing to different spots in memory). That's why we create a delegate that
	// both calls the benchmark as well as your assert method.
	private readonly TestMethod _testMethod;

	#region Construction
	internal BenchmarkMethod(BenchmarkType benchmarkType, MethodInfo benchmarkMethod)
	{
		_benchmarkType = benchmarkType;

		if (benchmarkMethod.IsStatic)
		{
			throw new InvalidOperationException($"Benchmark methods cannot be static.");
		}

		_benchmarkMethod = benchmarkMethod;
		_argumentsSource = GetArgumentsSource(benchmarkMethod);

		var assertMethod = GetAssertMethod(benchmarkMethod);
		_testMethod = TestMethodGenerator.Generate(benchmarkMethod, assertMethod);
	}

	private static MethodInfo? GetArgumentsSource(MethodInfo benchmarkMethod)
	{
		var argumentsAttribute = benchmarkMethod.GetCustomAttribute<ArgumentsAttribute>();
		if (argumentsAttribute is not null)
		{
			throw new NotSupportedException($"Sorry, at this time the {nameof(ArgumentsAttribute)} is not supported. Use {nameof(ArgumentsSourceAttribute)} instead.");
		}

		var argumentsSource = benchmarkMethod.GetCustomAttribute<ArgumentsSourceAttribute>();
		if (argumentsSource is null)
		{
			if (benchmarkMethod.GetParameters().Length != 0)
			{
				throw new ArgumentException($"Method {benchmarkMethod} has arguments, but is missing a {nameof(ArgumentsSourceAttribute)}.");
			}

			return null;
		}

		var declaringType = benchmarkMethod.DeclaringType!;
		var argumentsProperty = declaringType.GetProperty(argumentsSource.Name)?.GetGetMethod()
			?? declaringType.GetMethod(argumentsSource.Name, [])
			?? throw new ArgumentException($"No public property or void method was found in {benchmarkMethod.DeclaringType} with the name {argumentsSource.Name}.");

		return argumentsProperty;
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

	public void Assert(object instance, object?[]? arguments)
	{
		bool assertResult = _testMethod(instance, arguments);
		if (!assertResult)
		{
			throw new Exception($"Test failed on {_benchmarkMethod.Name} in {instance.GetType()}.");
		}
	}

	/// <summary>
	/// Gets the arguments to use in calling this benchmark method.
	/// </summary>
	public IEnumerable<object?[]?> GetArguments(object instance)
	{
		var argumentsSource = _argumentsSource;
		if (argumentsSource is null)
		{
			yield return null;
			yield break;
		}

		var value = argumentsSource.Invoke(argumentsSource.IsStatic ? null : instance, null)
			?? throw new ArgumentException($"Arguments defined in {argumentsSource.Name} cannot be null.");
		var arguments = value as IEnumerable
			?? throw new ArgumentException($"Arguments defined in {argumentsSource.Name} must be IEnumerable.");

		var methodParameters = _benchmarkMethod.GetParameters();
		foreach (var argumentSet in arguments)
		{
			if (methodParameters.Length == 1)
			{
				yield return [argumentSet];
				continue;
			}
			else if (argumentSet is not IEnumerable || argumentSet is string)
			{
				throw new ArgumentException($"Arguments defined in {nameof(ArgumentsSourceAttribute)} do not match the parameters in method {_benchmarkMethod}.");
			}

			var array = new ArrayList();
			foreach (var item in (IEnumerable)argumentSet)
			{
				array.Add(item);
			}
			object?[] objectArray = array.ToArray();

			yield return objectArray;
		}
	}

}
