// Copyright (c) 2024 James Swerdfeger
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace jswerdfeger.BenchmarkDotNet.Assert;

/// <summary>
/// Represents all possible parameter sets that a type has.
/// </summary>
internal class ParameterSets
{


	// We'll store each member and the values for them. We can later turn that into lists of all
	// the possible parameter sets. (It's easier to do when you know you don't have further ones
	// to add.)
	private readonly List<(MemberInfo Member, object?[] Values)> _parameterizations = [];

	internal void Add(MemberInfo member, object?[] values)
	{
		if (values.Length == 0) return;

		if (!member.IsWriteable())
		{
			throw new ArgumentException($"Member {member} must have a public setter in order to utilize a Params attribute.");
		}

		_parameterizations.Add((member, values));
	}

	internal IEnumerable<MemberValue[]> GetParameterSets()
	{
		// So, we have parameterizations that have values. To make sure we create a parameter set
		// for every possibility (ie, O^n), we'll store an index with each parameterization's
		// values. We'll increment the first until we run out of values, then set it back to zero
		// and increment the second, and so on, until the last index cannot be incremented.
		var parameterizations = _parameterizations;
		int[] valuesIndex = new int[parameterizations.Count];
		bool hasMore = true;
		while (hasMore)
		{
			yield return parameterizations
				.Select((p, i) => new MemberValue(p.Member, p.Values[valuesIndex[i]]))
				.ToArray(); ;

			for (int i = 0; (hasMore = i < parameterizations.Count); i++)
			{
				if (++valuesIndex[i] < parameterizations[i].Values.Length) break;
				valuesIndex[i] = 0;
			}
		}
	}

}
