// Read ​​ SmartImage FileLogger.cs
// 2023-04-28 @ 1:02 PM

namespace SmartImage.Lib.Utilities;

// TODO: TEMPORARY

public class FileLogger : IDisposable
{
	internal static readonly FileLogger Fl = new FileLogger("smartimage.log");

	public TextWriter Writer { get; }

	public FileLogger(string s)
	{
		// var f = File.Open(s, FileMode.Append);

		Writer = StreamWriter.Synchronized(new StreamWriter(s));
		
	}

	public void Dispose()
	{
		Writer.Flush();
		Writer.Dispose();
	}
}