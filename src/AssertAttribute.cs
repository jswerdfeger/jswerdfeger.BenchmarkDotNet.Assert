// Copyright (c) 2024 James Swerdfeger
// Licensed under the MIT license.

using System;

namespace jswerdfeger.BenchmarkDotNet.Assert;

/// <summary>
/// States which method to use to assert that your benchmark is operating correctly.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class AssertAttribute : Attribute
{
	/// <summary>
	/// Gets the method name to use to assert this method is operating correctly.
	/// <para>This method must be an instance method within this class. It also must accept the
	/// same parameters your benchmark method does, in the same order, plus an extra parameter
	/// that matches your benchmark method's return type (if it has one). Its return type must
	/// be bool.</para>
	/// </summary>
	public string AssertMethodName { get; }

	/// <summary>
	/// Creates a new <see cref="AssertAttribute"/>.
	/// </summary>
	/// <param name="assertMethodName">The name of the method to use to assert this method is
	/// operating correctly.
	/// <para>This method must be an instance method within this class. It also must accept the
	/// same parameters your benchmark method does, in the same order, plus an extra parameter
	/// that matches your benchmark method's return type (if it has one). Its return type must
	/// be bool.</para></param>
	public AssertAttribute(string assertMethodName)
	{
		AssertMethodName = assertMethodName;
	}

}
