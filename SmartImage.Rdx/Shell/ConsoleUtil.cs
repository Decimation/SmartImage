global using STable = Spectre.Console.Table;
global using DTable = System.Data.DataTable;
using System.Data;
using Kantan.Utilities;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;

// $User.Name $File.ProjectName $File.FileName
// $File.CreatedYear-$File.CreatedMonth-$File.CreatedDay @ $File.CreatedHour:$File.CreatedMinute

namespace SmartImage.Rdx.Shell;

[Flags]
public enum OutputFields
{

	None = 0,

	Name       = 1 << 0,
	Url        = 1 << 1,
	Similarity = 1 << 2,
	Artist     = 1 << 3,
	Site       = 1 << 4,

	// Default = Name | Url | Similarity

}

public enum OutputFileFormat
{

	None = 0,
	Delimited,

}

internal static class ConsoleUtil
{

	private static readonly byte[] s_utf8BomSig =
	[
		0xEF, 0xBB, 0xBF
	];

	public static string ParseInputStream(int bufSize = 4096, int maxSize = 10_000_000)
	{
		string path = null;

		using Stream stdin = Console.OpenStandardInput();

		var buffer  = new byte[bufSize];
		var buffer2 = new byte[maxSize];
		int bytesRead;
		int iter  = 0;
		int b2pos = 0;

		while ((bytesRead = stdin.Read(buffer, 0, buffer.Length)) > 0) {
			if (iter == 0) {

				if (buffer[0]    == s_utf8BomSig[0]
				    && buffer[1] == s_utf8BomSig[1]
				    && buffer[2] == s_utf8BomSig[2]) {

					buffer    =  buffer[3..];
					bytesRead -= s_utf8BomSig.Length;
				}
			}

			Array.Copy(buffer, 0, buffer2, b2pos, bytesRead);
			b2pos += bytesRead;

			iter++;

			// prog?.Report(b2pos);
		}

		if (buffer2[(b2pos - 1)] == '\n' && buffer2[(b2pos - 2)] == '\r') {
			b2pos -= 2;
		}

		Array.Resize(ref buffer2, b2pos);

		var s = Console.InputEncoding.GetString(buffer2);

		if (File.Exists(s)) {
			path = s;
		}
		else {
			path = Path.GetTempFileName();
			File.WriteAllBytes(path, buffer2);
		}

		return path;
	}

}