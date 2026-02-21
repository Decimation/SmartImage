#nullable enable
using JetBrains.Annotations;

namespace SmartImage.Lib.Utilities.Diagnostics;

public sealed class SmartImageException : Exception
{

	public SmartImageException() { }

	public SmartImageException(string? message) : base(message) { }


	/*[CA($"{nameof(b)}: false => halt")]
	public static  void Assert(bool b, [CBN] [CAE(nameof(b))] string message = null)
	{
		if (!b) {
			throw new SmartImageException($"Invalid argument {message}");
		}

	}*/

	/*[CA($"{nameof(b)}: false => halt")]
	internal static void Assert<T>(bool b, [CBN] [CAE(nameof(b))] string message = null)
	{
		if (!b) {
			var t = (T) Activator.CreateInstance(typeof(T), [message]);
			throw ((Exception) (object) t);
		}

	}*/

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