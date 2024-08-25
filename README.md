# Assert Library for BenchmarkDotNet

For use with [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet), this library adds the
capability to assert that your benchmarks are operating correctly, as you intend. Nothing is worse
than writing a bunch of benchmarks, running them, coming to a conclusion, then finding out later
that your benchmarking code wasn't even working correctly! (Or does that only ever happen to me...)

## Why would you want this?

Simply: no need to write a separate unit test library just for your benchmarks.

# Example

Let's say you're benchmarking different ways to concat a string.

```
using BenchmarkDotNet.Attributes;
using System.Text;

public class ConcatenateBenchmark
{
	public static object[][] Arguments => [
		["String #1. ", DateTime.Now, 3.1415]
	];

	[Benchmark]
	[ArgumentsSource(nameof(Arguments))]
	public string Adding(string value1, DateTime value2, double value3)
	{
		return value1 + value2 + value3;
	}

	[Benchmark]
	[ArgumentsSource(nameof(Arguments))]
	public string StringInterpolation(string value1, DateTime value2, double value3)
	{
		return $"{value1}{value2}{value3}";
	}

	[Benchmark]
	[ArgumentsSource(nameof(Arguments))]
	public string StringBuilders(string value1, DateTime value2, double value3)
	{
		var sb = new StringBuilder();
		sb.Append(value1)
			.Append(value2)
			.Append(value2);
		return sb.ToString();
	}
}
```

Wouldn't it be nice to know that your benchmarks are actually testing what they should be testing?

Using this library, all you need to do is write an `Assert` method to check that the string that's
returned is as you expect it to be. Your `Assert` method accepts the same arguments your method
does, plus an extra argument for what it returns (if anything), and returns a `bool` indicating
whether or not the test succeeded.

Of course, if we made use of properties initialized with `ParamsAttribute`, those would be
available to us in the `Assert` method as well, but that's not applicable to this example.
```
	public bool Assert(string value1, DateTime value2, double value3, string actualResult)
	{
		string expected = value1 + value2 + value3;
		return expected.Equals(actualResult);
	}
```

And you can add these to your Benchmark methods by using the `AssertAttribute` and passing the
name of your method.

So your class becomes:

```
using jswerdfeger.BenchmarkDotNet.Assert;
using BenchmarkDotNet.Attributes;
using System.Text;

public class ConcatenateBenchmark
{
	public static object[][] Arguments => [
		["String #1. ", DateTime.Now, 3.1415]
	];

	public bool Assert(string value1, DateTime value2, double value3, string actualResult)
	{
		string expected = value1 + value2 + value3;
		return expected.Equals(actualResult);
	}

	[Benchmark]
	[Assert(nameof(Assert))]
	[ArgumentsSource(nameof(Arguments))]
	public string Adding(string value1, DateTime value2, double value3)
	{
		return value1 + value2 + value3;
	}

	[Benchmark]
	[Assert(nameof(Assert))]
	[ArgumentsSource(nameof(Arguments))]
	public string StringInterpolation(string value1, DateTime value2, double value3)
	{
		return $"{value1}{value2}{value3}";
	}

	[Benchmark]
	[Assert(nameof(Assert))]
	[ArgumentsSource(nameof(Arguments))]
	public string StringBuilders(string value1, DateTime value2, double value3)
	{
		var sb = new StringBuilder();
		sb.Append(value1)
			.Append(value2)
			.Append(value2);
		return sb.ToString();
	}
}
```

In our case we only need the one `Assert` method, but your benchmark class might very well have
methods that return different things, or perform different operations. For example, maybe you're
trying to find the bottleneck across multiple methods that serve different purposes. Hence, you
can absolutely write different `Assert` methods for each of your benchmark methods.

Then, to execute the assert, in your main method where you likely call
`BenchmarkRunner.Run<ConcatenateBenchmark>()`, just call
`BenchmarkAssert.Run<ConcatenateBenchmark>()` first.

```
public static void Main(string args[])
{
	BenchmarkAssert.Run<ConcatenateBenchmark>();
	BenchmarkRunner.Run<ConcatenateBenchmark>();
}
```

Now when you run your program, whether in release mode or debug mode, it will confirm all your
benchmarks are working as intended, and ergo actually mean something. Should any assert method
fail, it will raise an `AssertFailedException`.

Give it a test, and you'll find out:
```
Unhandled exception. jswerdfeger.BenchmarkDotNet.Assert.AssertFailedException: Test failed on
StringBuilders in ConcatenateBenchmark.
Params: 
Arguments: (String #1. , 2024-08-25 5:02:26 PM, 3.1415)
```

Oops, our `StringBuilders` method added value2 twice, instead of adding value3. So that wouldn't
be a fair benchmark. Now we know!

_For the academic; the fastest way of concatenating values into a string, if you know the final
length in advance, is [string.Create](https://learn.microsoft.com/en-us/dotnet/api/system.string.create#system-string-create-1(system-int32-0-system-buffers-spanaction((system-char-0))))._

# What is supported?

We support BenchmarkDotNet's [Setup and Cleanup](https://benchmarkdotnet.org/articles/features/setup-and-cleanup.html)
attributes:
- [GlobalSetup]
- [IterationSetup]
- [GlobalCleanup]
- [IterationCleanup]

Like in BenchmarkDotNet, each will be called once per assertion test.

We support most of BenchmarkDotNet's [Parameterization](https://benchmarkdotnet.org/articles/features/parameterization.html)
attributes.
- [ParamsAttribute](https://benchmarkdotnet.org/articles/features/parameterization.html#sample-introparams)
- [ParamsSourceAttribute](https://benchmarkdotnet.org/articles/features/parameterization.html#sample-introparamssource)
- [ParamsAllValues](https://benchmarkdotnet.org/articles/features/parameterization.html#sample-introparamsallvalues)
  - NOTE: Enums, at this time, are not supported.
- [ArgumentsAttribute](https://benchmarkdotnet.org/articles/features/parameterization.html#sample-introarguments)
- [ArgumentsSourceAttribute](https://benchmarkdotnet.org/articles/features/parameterization.html#sample-introargumentssource)
