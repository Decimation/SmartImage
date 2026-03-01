#nullable enable

using JetBrains.Annotations;

namespace SmartImage.Lib.Utilities.Diagnostics;

public sealed class SmartImageException : Exception
{

	public SmartImageException() { }

	public SmartImageException(string? message) : base(message) { }

}

public sealed class ParseException : Exception
{

	[ContractAnnotation($"{nameof(val)}: null => halt")]
	public static void ThrowIfNull<T>([CBN] T? val, [CAE(nameof(val))] string? cae = null)
	{
		throw new ParseException($"Type {nameof(T)} parse failed");

	}

	public ParseException(string? message = null) : base(message) { }

}