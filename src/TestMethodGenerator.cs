// Copyright (c) 2024 James Swerdfeger
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace jswerdfeger.BenchmarkDotNet.Assert;

internal static class TestMethodGenerator
{
#if (NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
	internal static TestMethod Generate(MethodInfo benchmarkMethod, MethodInfo assertMethod)
	{
		if (!benchmarkMethod.HasByRefLikeTypes() && !assertMethod.HasByRefLikeTypes())
		{
			return GenerateDirectTestMethod(benchmarkMethod, assertMethod);
		}

		Debug.Assert(!benchmarkMethod.IsStatic);
		Debug.Assert(assertMethod.ReturnType == typeof(bool));

		var benchmarkParameters = benchmarkMethod.GetParameters();

		// You can't create an instance DynamicMethod; static only. So we accept the instance as
		// a parameter.
		Type[] newParameterTypes = [benchmarkMethod.DeclaringType!,
			..benchmarkParameters.Select(p => p.ParameterType.AsReflectionSafe())];

		DynamicMethod newMethod = new DynamicMethod(string.Empty, assertMethod.ReturnType,
			newParameterTypes, true);
		ILGenerator gen = newMethod.GetILGenerator();

		CallBenchmarkMethod(gen, benchmarkMethod);
		var benchmarkReturnType = benchmarkMethod.ReturnType;
		if (benchmarkReturnType != typeof(void))
		{
			var local = gen.DeclareLocal(benchmarkReturnType);
			gen.Emit(OpCodes.Stloc, local);
		}

		CallAssertMethod(gen, assertMethod, benchmarkMethod);
		gen.Emit(OpCodes.Ret);

		return (object instance, object?[]? arguments) =>
		{
			// Remember, DynamicMethod is a static method that accepts the instance as the first
			// argument.
			return (bool)newMethod.Invoke(null, [instance, .. (arguments ?? [])])!;
		};
	}

	private static Type AsReflectionSafe(this Type type)
	{
		if (!type.IsByRefLike) return type;

		if (type.IsGenericType)
		{
			var genericTypeArgument = type.GetGenericArguments()[0];
			var genericTypeDefinition = type.GetGenericTypeDefinition();
			if (genericTypeDefinition == typeof(ReadOnlySpan<>))
			{
				// A ReadOnlySpan<char> is basically a string. You won't be working with char[].
				if (genericTypeArgument == typeof(char))
				{
					return typeof(string);
				}
				else
				{
					return genericTypeArgument.MakeArrayType();
				}
			}
			else if (genericTypeDefinition == typeof(Span<>))
			{
				return genericTypeArgument.MakeArrayType();
			}
		}

		throw new NotSupportedException($"The Assert library does not support the type {type} because it is a ref struct. We only support Span<> and ReadOnlySpan<>.");
	}

	private static void CallBenchmarkMethod(ILGenerator gen, MethodInfo benchmarkMethod)
	{
		Debug.Assert(!benchmarkMethod.IsStatic);

		var parameters = benchmarkMethod.GetParameters();

		gen.Emit(OpCodes.Ldarg_0); // The instance

		for (int i = 0; i < parameters.Length; i++)
		{
			// parameter might be by-ref-like, but our method's arguments are not. Hence, ldarg_S.
			gen.Emit(OpCodes.Ldarg_S, i + 1);

			var parameterType = parameters[i].ParameterType;
			if (parameterType.IsByRefLike)
			{
				AddILConversionToSpan(gen, parameterType);
				// Dup it so that we can store it in a local, but leave a copy on the stack to
				// pass as an arg to the benchmark method.
				gen.Emit(OpCodes.Dup);
				var local = gen.DeclareLocal(parameterType);
				gen.Emit(OpCodes.Stloc, local);
			}
		}

		gen.Emit(OpCodes.Call, benchmarkMethod);
	}

	private static void AddILConversionToSpan(ILGenerator gen, Type spanType)
	{
		Debug.Assert(spanType.IsGenericType && spanType.GetGenericTypeDefinition() == typeof(Span<>)
			|| spanType.GetGenericTypeDefinition() == typeof(ReadOnlySpan<>));

		if (spanType == typeof(ReadOnlySpan<char>))
		{
			var asSpanMethod = typeof(MemoryExtensions)
				.GetMethod(nameof(MemoryExtensions.AsSpan), [typeof(string)])
				?? throw new Exception($"Failed to find the {nameof(MemoryExtensions)}.{nameof(MemoryExtensions.AsSpan)} method!");
			gen.Emit(OpCodes.Call, asSpanMethod);
		}
		else
		{
			var spanArgument = spanType.GetGenericArguments()[0];
			var spanConstructor = spanType.GetConstructor([spanArgument.MakeArrayType()])
				?? throw new Exception($"Failed to find the (Array) constructor in {spanType}!");
			gen.Emit(OpCodes.Newobj, spanConstructor);
		}
	}

	private static void CallAssertMethod(ILGenerator gen, MethodInfo assertMethod,
		MethodInfo benchmarkMethod)
	{
		var parameters = assertMethod.GetParameters();

		if (!assertMethod.IsStatic) gen.Emit(OpCodes.Ldarg_0); // The instance

		// Being as arg0 is the instance, whether or not we load it, the first arg we would load
		// in the loop below would be arg1. Thus, argPos starts at 1.
		int argPos = 1;
		int localPos = 0;
		// If the benchmark method returns something, that'll be the last parameter on the assert
		// method, and we must always load that one from the locals. Hence, we iterate only until
		// the last parameter.
		for (int i = 0; i < parameters.Length - 1; i++)
		{
			var parameterType = parameters[i].ParameterType;
			if (parameterType.IsByRefLike) gen.Emit(OpCodes.Ldloc_S, localPos++);
			else gen.Emit(OpCodes.Ldarg_S, argPos++);
		}

		if (benchmarkMethod.ReturnType != typeof(void) || parameters[^1].ParameterType.IsByRefLike)
		{
			gen.Emit(OpCodes.Ldloc_S, localPos++);
		}
		else
		{
			gen.Emit(OpCodes.Ldarg_S, argPos++);
		}

		gen.Emit(OpCodes.Call, assertMethod);
	}

#else
	internal static TestMethod Generate(MethodInfo benchmarkMethod, MethodInfo assertMethod)
		=> GenerateDirectTestMethod(benchmarkMethod, assertMethod);
#endif

	private static TestMethod GenerateDirectTestMethod(MethodInfo benchmarkMethod, MethodInfo assertMethod)
	{
		Debug.Assert(!benchmarkMethod.IsStatic);
		Debug.Assert(assertMethod.ReturnType == typeof(bool));

		return (object instance, object?[]? arguments) =>
		{
			var benchmarkResult = benchmarkMethod.Invoke(instance, arguments);
			object?[]? assertParameters;
			if (benchmarkMethod.ReturnType == typeof(void)) assertParameters = [.. (arguments ?? [])];
			else assertParameters = [.. (arguments ?? []), benchmarkResult];

			return (bool)assertMethod.Invoke(assertMethod.IsStatic ? null : instance,
				assertParameters)!;
		};
	}

}
