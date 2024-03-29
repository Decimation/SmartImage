namespace SmartImage.Lib.Utilities;

public sealed class SmartImageException : Exception
{

	public SmartImageException() { }

	public SmartImageException([CBN] string message) : base(message) { }

}