// Copyright (c) 2024 James Swerdfeger
// Licensed under the MIT license.

using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace jswerdfeger.BenchmarkDotNet.Assert;

internal class BenchmarkType
{
	private readonly Type _type;
	private readonly MethodInfo? _globalSetup;
	private readonly MethodInfo? _globalCleanup;
	private readonly MethodInfo? _iterationSetup;
	private readonly MethodInfo? _iterationCleanup;

	private readonly List<Parameterization> _parameterizations;

	#region Construction
	internal BenchmarkType(Type type)
	{
		_type = type;
		_globalSetup = type.GetInstanceMethodWithAttribute(typeof(GlobalSetupAttribute));
		_globalCleanup = type.GetInstanceMethodWithAttribute(typeof(GlobalCleanupAttribute));
		_iterationSetup = type.GetInstanceMethodWithAttribute(typeof(IterationSetupAttribute));
		_iterationCleanup = type.GetInstanceMethodWithAttribute(typeof(IterationCleanupAttribute));

		_parameterizations = GetParameterizations(type);
	}

	private static List<Parameterization> GetParameterizations(Type type)
	{
		List<Parameterization> results = [];
		foreach (var pi in type.GetProperties())
		{
			if (pi.TryGetCustomAttribute<ParamsAttribute>(out var paramsAttribute))
			{
				if (!pi.CanWrite)
				{
					throw new ArgumentException($"Property {pi} must have a public setter in order to use {nameof(ParamsAttribute)}.");
				}

				var values = paramsAttribute.Values;
				if (values.Length == 0)
				{
					throw new ArgumentException($"{nameof(ParamsAttribute)} on property {pi} cannot be empty.");
				}

				results.Add(new(pi, paramsAttribute.Values));
			}
			else if (pi.TryGetCustomAttribute<ParamsAllValuesAttribute>(out var allValuesAttribute))
			{
				if (pi.PropertyType == typeof(bool))
				{
					results.Add(new(pi, [true, false]));
				}
				else if (pi.PropertyType == typeof(bool?))
				{
					results.Add(new(pi, [true, false, null]));
				}
				else
				{
					if (pi.PropertyType.IsEnum
						|| (Nullable.GetUnderlyingType(pi.PropertyType)?.IsEnum ?? false))
					{
						throw new NotSupportedException($"Sorry, at this time assert does not support {nameof(ParamsAllValuesAttribute)} on enums.");
					}

					throw new InvalidOperationException($"{nameof(ParamsAllValuesAttribute)} can only be used on bool, or bool? properties.");
				}
			}
			else if (pi.TryGetCustomAttribute<ParamsSourceAttribute>(out _))
			{
				throw new NotSupportedException($"Sorry, at this time {nameof(ParamsSourceAttribute)} is not supported.");
			}
		}

		return results;
	}
	#endregion

	/// <summary>
	/// Asserts all benchmark methods in this type.
	/// </summary>
	public void AssertAll()
	{
		var benchmarkMethods = _type.GetInstanceMethodsWithAttribute(typeof(BenchmarkAttribute));
		foreach (var benchmarkMethod in benchmarkMethods)
		{
			Assert(benchmarkMethod);
		}
	}

	private void Assert(MethodInfo method)
	{
		BenchmarkMethod benchmarkMethod = new(this, method);
		var globalSetup = _globalSetup;
		var globalCleanup = _globalCleanup;
		var iterationSetup = _iterationSetup;
		var iterationCleanup = _iterationCleanup;

		// BenchmarkDotNet will permit you to have ArgumentsSource as an instance variable, but
		// if you do so, it WILL NOT make use of your [Params] attributes, nor will it call
		// GlobalSetup. Welp, that's probably for the best, considering that opens the door to
		// some potential horrid circular dependency, where GlobalSetup depends on params depends
		// on arguments depends on blah blah blah. I'll just do as they do.
		var arguments = benchmarkMethod.GetArguments(CreateInstance()).ToList();

		var parameterizations = _parameterizations;
		int[] valuesIndex = new int[parameterizations.Count];
		bool hasMore = true;
		while (hasMore)
		{
			foreach (object?[]? argumentSet in arguments)
			{
				var instance = CreateInstance();
				for (int i = 0; i < parameterizations.Count; i++)
				{
					var parameterization = parameterizations[i];
					var valueIndex = valuesIndex[i];
					Debug.Assert(valueIndex < parameterization.Values.Length);
					parameterization.Property.SetValue(instance, parameterization.Values[valueIndex]);
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

	private object CreateInstance()
	{
		return Activator.CreateInstance(_type)
			?? throw new ArgumentException($"Failed to create an instance of {_type}.");
	}

}
