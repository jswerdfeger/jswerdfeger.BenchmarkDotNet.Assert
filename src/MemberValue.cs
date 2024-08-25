// Copyright (c) 2024 James Swerdfeger
// Licensed under the MIT license.

using System.Diagnostics;
using System.Reflection;

namespace jswerdfeger.BenchmarkDotNet.Assert;

/// <summary>
/// Stores a member and a value you wish to set to it.
/// </summary>
internal readonly record struct MemberValue
{
	public MemberInfo Member { get; }
	public object? Value { get; }

	public MemberValue(MemberInfo member, object? value)
	{
		Debug.Assert(member.IsWriteable());
		Member = member;
		Value = value;
	}

	public override string ToString()
	{
		return $"({Member.Name}={Value})";
	}
}
