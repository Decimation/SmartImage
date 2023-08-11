// Read S SmartImage.UI LogEntry.cs
// 2023-08-11 @ 12:28 PM

using System;

namespace SmartImage.UI.Model;

#pragma warning disable CS8618
public sealed class LogEntry
{
	public DateTime Time    { get; }
	public string   Message { get; }

	public LogEntry(string message) : this(DateTime.Now, message) { }

	public LogEntry(DateTime time, string message)
	{
		Time    = time;
		Message = message;
	}

	public override string ToString()
	{
		return $"{Time} {Message}";
	}
}