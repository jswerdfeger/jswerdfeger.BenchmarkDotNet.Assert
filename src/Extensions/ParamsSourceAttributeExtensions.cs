// Copyright (c) 2024 James Swerdfeger
// Licensed under the MIT license.

using BenchmarkDotNet.Attributes;
using System;
using System.Collections;
using System.Linq;

namespace jswerdfeger.BenchmarkDotNet.Assert;

internal static class ParamsSourceAttributeExtensions
{
	internal static object?[] GetValues(this ParamsSourceAttribute sourceAttribute, object instance)
	{
		var type = instance.GetType();
		var name = sourceAttribute.Name;
		var method = type.GetProperty(name)?.GetGetMethod()
			?? type.GetMethod(name, [])
			?? throw new ArgumentException($"No public property or void method was found in {type} with the name {name}.");

		var value = method.Invoke(method.IsStatic ? null : instance, null)
			?? throw new ArgumentException($"Parameters supplied by {name} cannot be null.");
		var enumerable = value as IEnumerable
			?? throw new ArgumentException($"Parameters supplied by {name} must be IEnumerable.");

		return enumerable.Cast<object?>().ToArray();
	}

}
