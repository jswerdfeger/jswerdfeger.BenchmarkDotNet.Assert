// Copyright (c) 2024 James Swerdfeger
// Licensed under the MIT license.

using BenchmarkDotNet.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace jswerdfeger.BenchmarkDotNet.Assert;

internal static class ArgumentsSourceAttributeExtensions
{
	internal static IEnumerable<object?[]> GetValues(this ArgumentsSourceAttribute sourceAttribute,
		object instance, MethodInfo benchmarkMethod)
	{
		var type = instance.GetType();
		var name = sourceAttribute.Name;
		var method = type.GetProperty(name)?.GetGetMethod()
			?? type.GetMethod(name, [])
			?? throw new ArgumentException($"No public property or void method was found in {type} with the name {name}.");

		var value = method.Invoke(method.IsStatic ? null : instance, null)
			?? throw new ArgumentException($"Arguments supplied by {name} cannot be null.");
		var enumerable = value as IEnumerable
			?? throw new ArgumentException($"Arguments supplied by {name} must be IEnumerable.");

		var methodParameterLength = benchmarkMethod.GetParameters().Length;
		if (methodParameterLength == 0)
		{
			// BenchmarkDotNet will just ignore any Arguments attribute if your method has no
			// parameters.
			yield break;
		}
		else if (methodParameterLength == 1)
		{
			foreach (var argumentSet in enumerable)
			{
				yield return [argumentSet];
			}
		}
		else
		{
			foreach (var argumentSet in enumerable)
			{
				if (argumentSet is not IEnumerable || argumentSet is string)
				{
					throw new ArgumentException($"Arguments defined in {nameof(ArgumentsSourceAttribute)} do not match the parameters in method {benchmarkMethod}.");
				}

				var array = new ArrayList();
				foreach (var item in (IEnumerable)argumentSet)
				{
					array.Add(item);
				}
				object?[] objectArray = array.ToArray();
				if (objectArray.Length != methodParameterLength)
				{
					throw new ArgumentException($"Each row of arguments supplied by {name} must have {methodParameterLength} total arguments.");
				}

				yield return objectArray;
			}
		}

	}

}
