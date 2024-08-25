// Copyright (c) 2024 James Swerdfeger
// Licensed under the MIT license.

using BenchmarkDotNet.Attributes;
using System;
using System.Reflection;

namespace jswerdfeger.BenchmarkDotNet.Assert;

internal static class ParamsAllValuesAttributeExtensions
{
	private static readonly object?[] AllBoolValues = [true, false];
	private static readonly object?[] AllNullableBoolValues = [true, false, null];

	internal static object?[] GetValues(this ParamsAllValuesAttribute allValuesAttribute,
		MemberInfo member)
	{
		var memberType = member.GetReturnType();
		if (memberType == typeof(bool)) return AllBoolValues;
		else if (memberType == typeof(bool?)) return AllNullableBoolValues;

		if (memberType.IsEnum
			|| (Nullable.GetUnderlyingType(memberType)?.IsEnum ?? false))
		{
			throw new NotSupportedException($"Sorry, at this time assert does not support {nameof(ParamsAllValuesAttribute)} on enums.");
		}

		throw new ArgumentException($"{nameof(ParamsAllValuesAttribute)} can only be used on bool, or bool? properties or fields.");
	}

}
