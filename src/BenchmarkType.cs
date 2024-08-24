// Copyright (c) 2024 James Swerdfeger
// Licensed under the MIT license.

using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace jswerdfeger.BenchmarkDotNet.Assert;

internal class BenchmarkType
{
	private readonly Type _type;

	private readonly MethodInfo? _globalSetup;
	private readonly MethodInfo? _globalCleanup;
	private readonly MethodInfo? _iterationSetup;
	private readonly MethodInfo? _iterationCleanup;

	// Parameterizations are constant per type. That is, they are created on a fresh instance
	// without running GlobalSetup. (This makes sense, since otherwise you could wind up with an
	// impossible situation of not knowing what depends on what.) Thus, we create and cache all
	// possible parameterizations right away.
	private readonly List<Parameterization> _parameterizations;

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
		_parameterizations = GetParameterizations(instance);
		_benchmarkMethods = GetBenchmarkMethods(instance);
	}

	private static List<Parameterization> GetParameterizations(object instance)
	{
		var type = instance.GetType();
		List<Parameterization> results = [];
		foreach (var pi in type.GetProperties())
		{
			// btw, AllowMultiple is false on all the Params attributes.
			// Also, BenchmarkDotNet only supports one at a time.
			bool hasParams = false;

			if (pi.TryGetCustomAttribute<ParamsAttribute>(out var paramsAttribute))
			{
				hasParams = true;
				var values = paramsAttribute.Values;
				if (values.Length > 0) results.Add(new(pi, values));
			}

			if (pi.IsDefined(typeof(ParamsAllValuesAttribute), inherit: false))
			{
				if (hasParams)
				{
					throw new ArgumentException($"You cannot use more than one of ({nameof(ParamsAttribute)}, {nameof(ParamsAllValuesAttribute)}, {nameof(ParamsSourceAttribute)}) on a single property/field.");
				}
				hasParams = true;

				results.Add(Parameterization.ForAllValues(pi));
			}

			if (pi.TryGetCustomAttribute<ParamsSourceAttribute>(out var sourceAttribute))
			{
				if (hasParams)
				{
					throw new ArgumentException($"You cannot use more than one of ({nameof(ParamsAttribute)}, {nameof(ParamsAllValuesAttribute)}, {nameof(ParamsSourceAttribute)}) on a single property/field.");
				}
				hasParams = true;

				results.Add(new(pi, sourceAttribute.GetValues(instance)));
			}
		}

		return results;
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

	/// <summary>
	/// Asserts all benchmark methods in this type.
	/// </summary>
	public void AssertAll()
	{
		foreach (var benchmarkMethod in _benchmarkMethods)
		{
			Assert(benchmarkMethod);
		}
	}

	private void Assert(BenchmarkMethod benchmarkMethod)
	{
		var globalSetup = _globalSetup;
		var globalCleanup = _globalCleanup;
		var iterationSetup = _iterationSetup;
		var iterationCleanup = _iterationCleanup;

		var methodArgumentSets = benchmarkMethod.GetArgumentSets();

		var parameterizations = _parameterizations;
		int[] valuesIndex = new int[parameterizations.Count];
		bool hasMore = true;
		while (hasMore)
		{
			foreach (object?[]? argumentSet in methodArgumentSets)
			{
				var instance = CreateDefaultInstance();
				for (int i = 0; i < parameterizations.Count; i++)
				{
					var parameterization = parameterizations[i];
					var valueIndex = valuesIndex[i];
					Debug.Assert(valueIndex < parameterization.Values.Length);
					parameterization.Member.SetValue(instance, parameterization.Values[valueIndex]);
				}

				globalSetup?.Invoke(instance, null);
				iterationSetup?.Invoke(instance, null);

				benchmarkMethod.Assert(instance, argumentSet);

				iterationCleanup?.Invoke(instance, null);
				globalCleanup?.Invoke(instance, null);
			}

			for (int i = 0; (hasMore = i < parameterizations.Count); i++)
			{
				if (++valuesIndex[i] < parameterizations[i].Values.Length) break;
				valuesIndex[i] = 0;
			}
		}
	}

	private object CreateDefaultInstance()
	{
		return Activator.CreateInstance(_type)
			?? throw new ArgumentException($"Failed to create an instance of {_type}.");
	}

}
