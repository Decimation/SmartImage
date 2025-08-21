
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using SmartImage.Lib.Engines;

namespace SmartImage.Lib.Utilities.Diagnostics;

public sealed class SmartImageException : Exception
{

	public SmartImageException() { }

	public SmartImageException([CBN] string message) : base(message) { }


	[ContractAnnotation($"{nameof(b)}:false => halt")]
	internal static void Assert(bool b, [CAE(nameof(b))] string s = null)
	{
		if (!b) {
			throw new SmartImageException($"Invalid argument {s}");
		}

	}

}