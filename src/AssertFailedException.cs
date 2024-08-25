// Copyright (c) 2024 James Swerdfeger
// Licensed under the MIT license.

using System;

namespace jswerdfeger.BenchmarkDotNet.Assert;

/// <summary>
/// Exception thrown when a benchmark assert test fails.
/// </summary>
public class AssertFailedException : Exception
{
	/// <summary>
	/// Creates a new <see cref="AssertFailedException"/>.
	/// </summary>
	public AssertFailedException(string message)
		: base(message)
	{
	}

}
