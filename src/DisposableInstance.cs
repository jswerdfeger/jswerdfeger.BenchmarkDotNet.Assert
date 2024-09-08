// Copyright (c) 2024 James Swerdfeger
// Licensed under the MIT license.

using System;
using System.Reflection;

namespace jswerdfeger.BenchmarkDotNet.Assert;

internal readonly record struct DisposableInstance : IDisposable
{
	public object Instance { get; }
	private readonly MethodInfo? _globalCleanup;
	private readonly MethodInfo? _iterationCleanup;

	internal DisposableInstance(object instance, MethodInfo? globalCleanup,
		MethodInfo? iterationCleanup)
	{
		Instance = instance;
		_globalCleanup = globalCleanup;
		_iterationCleanup = iterationCleanup;
	}

	public void Dispose()
	{
		_iterationCleanup?.Invoke(Instance, null);
		_globalCleanup?.Invoke(Instance, null);
	}

}
