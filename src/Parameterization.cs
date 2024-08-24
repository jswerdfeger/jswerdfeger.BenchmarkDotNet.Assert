// Copyright (c) 2024 James Swerdfeger
// Licensed under the MIT license.

using BenchmarkDotNet.Attributes;
using System;
using System.Reflection;

namespace jswerdfeger.BenchmarkDotNet.Assert;

internal record struct Parameterization
{
	private static readonly object?[] AllBoolValues = [true, false];
	private static readonly object?[] AllNullableBoolValues = [true, false, null];

	public MemberInfo Member { get; }
	public object?[] Values { get; }

	internal static Parameterization ForAllValues(MemberInfo member)
	{
		var memberType = member.GetReturnType();
		if (memberType == typeof(bool)) return new(member, AllBoolValues);
		else if (memberType == typeof(bool?)) return new(member, AllNullableBoolValues);
		else
		{
			if (memberType.IsEnum
				|| (Nullable.GetUnderlyingType(memberType)?.IsEnum ?? false))
			{
				throw new NotSupportedException($"Sorry, at this time assert does not support {nameof(ParamsAllValuesAttribute)} on enums.");
			}
		}

		throw new ArgumentException($"{nameof(ParamsAllValuesAttribute)} can only be used on bool, or bool? properties or fields.");
	}

	internal Parameterization(MemberInfo member, object?[] values)
	{
		if (member is PropertyInfo pi)
		{
			if (!pi.CanWrite)
			{
				throw new ArgumentException($"Property {member} must have a public setter in order to utilize a Params attribute.");
			}
		}
		else if (member is FieldInfo fi)
		{
			if (!fi.IsInitOnly)
			{
				throw new ArgumentException($"Field {member} cannot be readonly if you wish to use it for a Params attribute.");
			}
		}
		else
		{
			throw new ArgumentException($"Member {member} cannot be used with a Params attribute; only Fields and Properties are supported.");
		}

		Member = member;
		Values = values;
	}

}
