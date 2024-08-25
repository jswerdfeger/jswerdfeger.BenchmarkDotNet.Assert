// Copyright (c) 2024 James Swerdfeger
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace jswerdfeger.BenchmarkDotNet.Assert;

internal static class ReflectionExtensions
{
	internal static IEnumerable<MemberInfo> GetFieldsAndProperties(this Type type)
	{
		foreach (var field in type.GetFields()) yield return field;
		foreach (var property in type.GetProperties()) yield return property;
	}

	internal static Type? GetGenericTypeDefinitionOrDefault(this Type type)
	{
		return type.IsGenericType ? type.GetGenericTypeDefinition() : null;
	}

	internal static MethodInfo? GetInstanceMethodWithAttribute(this Type parentType,
		Type attributeType)
	{
		return parentType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.FirstOrDefault(mi => mi.IsDefined(attributeType, inherit: false));
	}

	internal static IEnumerable<MethodInfo> GetInstanceMethodsWithAttribute(this Type parentType,
		Type attributeType)
	{
		return parentType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(mi => mi.IsDefined(attributeType, inherit: false));
	}

	internal static Type GetReturnType(this MemberInfo memberInfo)
	{
		return memberInfo.MemberType switch
		{
			MemberTypes.Property => ((PropertyInfo)memberInfo).PropertyType,
			MemberTypes.Field => ((FieldInfo)memberInfo).FieldType,
			_ => throw new ArgumentException($"Member must be a property or field.")
		};
	}

#if !(NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
	public static MethodInfo? GetMethod(this Type type, string name, BindingFlags bindingAttr, Type[] types)
		=> type.GetMethod(name, bindingAttr, binder: null, types, modifiers: null);
#endif

#if (NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
	/// <summary>
	/// Returns true if this <see cref="MethodInfo"/> has any by-ref-like parameters or returns.
	/// </summary>
	internal static bool HasByRefLikeTypes(this MethodInfo method)
	{
		if (method.ReturnType.IsByRefLike) return true;

		var parameters = method.GetParameters();
		foreach (var parameter in parameters)
		{
			if (parameter.ParameterType.IsByRefLike) return true;
		}

		return false;
	}
#endif

	internal static object? GetValue(this MemberInfo memberInfo, object? instance)
	{
		if (memberInfo.MemberType == MemberTypes.Property)
		{
			return ((PropertyInfo)memberInfo).GetValue(instance);
		}
		else if (memberInfo.MemberType == MemberTypes.Field)
		{
			return ((FieldInfo)memberInfo).GetValue(instance);
		}
		else
		{
			throw new ArgumentException($"Member must be a property or field.");
		}
	}

	/// <summary>
	/// Confirms this <see cref="MemberInfo"/> can be written to. That is, it must be a
	/// non-init-only field, or a writeable property.
	/// </summary>
	internal static bool IsWriteable(this MemberInfo memberInfo)
	{
		return (memberInfo is PropertyInfo pi && pi.CanWrite)
			|| (memberInfo is FieldInfo fi && !fi.IsInitOnly);
	}

	internal static void SetValue(this MemberInfo memberInfo, object? instance, object? value)
	{
		if (memberInfo.MemberType == MemberTypes.Property)
		{
			((PropertyInfo)memberInfo).SetValue(instance, value);
		}
		else if (memberInfo.MemberType == MemberTypes.Field)
		{
			((FieldInfo)memberInfo).SetValue(instance, value);
		}
		else
		{
			throw new ArgumentException($"Member must be a property or field.");
		}
	}

	internal static bool TryGetCustomAttribute<TAttribute>(this MemberInfo memberInfo,
		[MaybeNullWhen(false)] out TAttribute customAttribute)
	where TAttribute : Attribute
	{
		customAttribute = memberInfo.GetCustomAttribute<TAttribute>();
		return customAttribute is not null;
	}

}
