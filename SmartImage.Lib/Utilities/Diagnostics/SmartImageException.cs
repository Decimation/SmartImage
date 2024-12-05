namespace SmartImage.Lib.Utilities.Diagnostics;

public sealed class SmartImageException : Exception
{

	public SmartImageException() { }

	public SmartImageException([CBN] string message) : base(message) { }

}