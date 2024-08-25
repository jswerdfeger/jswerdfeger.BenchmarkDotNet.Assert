// Copyright (c) 2024 James Swerdfeger
// Licensed under the MIT license.

using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace jswerdfeger.BenchmarkDotNet.Assert;

/// <summary>
/// Stores assert information for a <see cref="Type"/> that has benchmark methods.
/// </summary>
internal class BenchmarkType
{
	private readonly Type _type;

	private readonly MethodInfo? _globalSetup;
	private readonly MethodInfo? _globalCleanup;
	private readonly MethodInfo? _iterationSetup;
	private readonly MethodInfo? _iterationCleanup;

	// The sets of possible parameters you can initialize this type with are treated as constant.
	// That is, they are created on a fresh instance without running GlobalSetup. (This makes
	// sense, since otherwise you could wind up with an impossible situation of not knowing what
	// depends on what.) Thus, we create and cache all possible parameterizations right away.
	private readonly ParameterSets _parameterSets;

	private readonly List<BenchmarkMethod> _benchmarkMethods;

	#region Construction
	internal BenchmarkType(Type type)
	{
		_type = type;

		_globalSetup = type.GetInstanceMethodWithAttribute(typeof(GlobalSetupAttribute));
		_globalCleanup = type.GetInstanceMethodWithAttribute(typeof(GlobalCleanupAttribute));
		_iterationSetup = type.GetInstanceMethodWithAttribute(typeof(IterationSetupAttribute));
		_iterationCleanup = type.GetInstanceMethodWithAttribute(typeof(IterationCleanupAttribute));

		var instance = CreateDefaultInstance();
		_parameterSets = GetParameterSets(instance);
		_benchmarkMethods = GetBenchmarkMethods(instance);
	}

	private static ParameterSets GetParameterSets(object instance)
	{
		var type = instance.GetType();
		ParameterSets parameterSets = new ParameterSets();
		foreach (var member in type.GetFieldsAndProperties())
		{
			// btw, AllowMultiple is false on all the Params attributes.
			// Also, BenchmarkDotNet only supports one at a time.
			bool hasParams = false;

			if (member.TryGetCustomAttribute<ParamsAttribute>(out var paramsAttribute))
			{
				hasParams = true;
				parameterSets.Add(member, paramsAttribute.Values);
			}

			if (member.TryGetCustomAttribute<ParamsAllValuesAttribute>(out var allValuesAttribute))
			{
				if (hasParams)
				{
					throw new ArgumentException($"You cannot use more than one of ({nameof(ParamsAttribute)}, {nameof(ParamsAllValuesAttribute)}, {nameof(ParamsSourceAttribute)}) on a single property/field.");
				}
				hasParams = true;
				parameterSets.Add(member, allValuesAttribute.GetValues(member));
			}

			if (member.TryGetCustomAttribute<ParamsSourceAttribute>(out var sourceAttribute))
			{
				if (hasParams)
				{
					throw new ArgumentException($"You cannot use more than one of ({nameof(ParamsAttribute)}, {nameof(ParamsAllValuesAttribute)}, {nameof(ParamsSourceAttribute)}) on a single property/field.");
				}
				hasParams = true;

				parameterSets.Add(member, sourceAttribute.GetValues(instance));
			}
		}

		return parameterSets;
	}

	private static List<BenchmarkMethod> GetBenchmarkMethods(object instance)
	{
		var type = instance.GetType();
		var benchmarkMethods = type.GetInstanceMethodsWithAttribute(typeof(BenchmarkAttribute));
		List<BenchmarkMethod> results = [];
		foreach (var benchmarkMethod in benchmarkMethods)
		{
			results.Add(new(benchmarkMethod, instance));
		}

		return results;
	}
	#endregion

	public IEnumerable<TestCase> BuildTestCases()
	{
		return _parameterSets.GetParameterSets()
			.SelectMany(parameterSet => BuildTestCases(parameterSet));
	}

	private IEnumerable<TestCase> BuildTestCases(MemberValue[] parameterSet)
	{
		return _benchmarkMethods
			.SelectMany((benchmarkMethod) => BuildTestCases(parameterSet, benchmarkMethod));
	}

	private IEnumerable<TestCase> BuildTestCases(MemberValue[] parameterSet, BenchmarkMethod benchmarkMethod)
	{
		foreach (var argumentSet in benchmarkMethod.GetArgumentSets())
		{
			yield return new TestCase(this, parameterSet, benchmarkMethod, argumentSet);
		}
	}

	private object CreateDefaultInstance()
	{
		return Activator.CreateInstance(_type)
			?? throw new ArgumentException($"Failed to create an instance of {_type}.");
	}

	/// <summary>
	/// Creates a new instance, with parameters set matching <paramref name="parameterSet"/>,
	/// and initialized with GlobalSetup and IterationSetup.
	/// <para>Do not forget to call Dispose when you're done (in order to call GlobalCleanup and
	/// IterationCleanup.)</para>
	/// </summary>
	internal DisposableInstance CreateInstance(MemberValue[] parameterSet)
	{
		var instance = CreateDefaultInstance();
		foreach (var memberValue in parameterSet)
		{
			memberValue.Member.SetValue(instance, memberValue.Value);
		}

		_globalSetup?.Invoke(instance, null);
		_iterationSetup?.Invoke(instance, null);
		return new(instance, _globalCleanup, _iterationCleanup);
	}

}
