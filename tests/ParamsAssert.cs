// Copyright (c) 2024 James Swerdfeger
// Licensed under the MIT license.

using BenchmarkDotNet.Attributes;
using System.Reflection;
using UnitTestAssert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace jswerdfeger.BenchmarkDotNet.Assert.Tests;

internal static class ParamsAssert
{
	/// <summary>
	/// Asserts that the member's value is one of the legal options based on its Params attributes.
	/// </summary>
	public static void Assert(object instance, string memberName)
	{
		var type = instance.GetType();
		var member = (MemberInfo?)type.GetProperty(memberName)
			?? type.GetField(memberName)
			?? throw new ArgumentException($"Member {memberName} was not found in type {type}.");

		object? value = member.GetValue(instance);

		object?[]? values;
		if (member.TryGetCustomAttribute<ParamsAttribute>(out var paramsAttribute))
		{
			values = paramsAttribute.Values;
		}
		else if (member.TryGetCustomAttribute<ParamsAllValuesAttribute>(out var allValuesAttribute))
		{
			values = allValuesAttribute.GetValues(member);
		}
		else if (member.TryGetCustomAttribute<ParamsSourceAttribute>(out var sourceAttribute))
		{
			values = sourceAttribute.GetValues(instance);
		}
		else
		{
			throw new ArgumentException($"Member {memberName} does not have a Params attribute.");
		}

		if (values == null || values.Length == 0)
		{
			UnitTestAssert.IsTrue(value == null ||
				value.Equals(Activator.CreateInstance(member.GetReturnType())),
				$"Member {member} has a value that is not in its Params attribute.");
		}
		else
		{
			UnitTestAssert.IsTrue(values.Contains(value), $"Member {member} has a value that is not in its Params attribute.");
		}
	}


}