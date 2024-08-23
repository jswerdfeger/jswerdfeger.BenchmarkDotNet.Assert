// Copyright (c) 2024 James Swerdfeger
// Licensed under the MIT license.

using System.Reflection;

namespace jswerdfeger.BenchmarkDotNet.Assert;

internal record struct Parameterization(
	PropertyInfo Property,
	object?[] Values);
