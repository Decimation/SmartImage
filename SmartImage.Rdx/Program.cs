using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Kantan.Text;
using Novus.Streams;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SmartImage.Lib;
using Spectre.Console;
using Spectre.Console.Cli;
using SmartImage.Rdx.Shell;

namespace SmartImage.Rdx;

/*
 * cd /mnt/c/Users/Deci/RiderProjects/SmartImage/
 * dotnet run --project SmartImage.Rdx/ "$HOME/1654086015521.png"
 * dotnet run -c 'DEBUG' --project SmartImage.Rdx "$HOME/1654086015521.png"
 * dotnet run -lp 'SmartImage.Rdx' -c 'WSL' --project SmartImage.Rdx "$HOME/1654086015521.png"
 * dotnet SmartImage.Rdx/bin/Debug/net8.0/SmartImage.Rdx.dll "/home/neorenegade/1654086015521.png"
 * dotnet run -c Test --project SmartImage.Rdx --  "/home/neorenegade/0c4c80957134d4304538c27499d84dbe.jpeg" -e All -p Auto
 * ./SmartImage.Rdx/bin/Release/net8.0/publish/linux-x64/SmartImage "/home/neorenegade/0c4c80957134d4304538c27499d84dbe.jpeg"
 * dotnet run --project SmartImage.Rdx -- --help
 * dotnet run --project SmartImage.Rdx/ "C:\Users\Deci\Pictures\Epic anime\Kallen_FINAL_1-3.png" --search-engines All --output-format "Delimited" --output-file "output.csv" --read-cookies
 * echo -nE $cx1 | dotnet run -c WSL --project SmartImage.Rdx --
 * "C:\Users\Deci\Pictures\Art\Makima 1-3.png" | dotnet run -c Debug --project SmartImage.Rdx --
 */

public static class Program
{

	private static readonly byte[] Utf8_Bom_Sig = new[]
	{
		(byte) 0xEF, (byte) 0xBB, (byte) 0xBF
	};

	public static bool CopyStreams(Stream stdin, Stream fs, int bufSize)
	{
		var buffer = new byte[bufSize];
		int bytesRead;
		int iter = 0;

		while ((bytesRead = stdin.Read(buffer, 0, buffer.Length)) > 0) {
			if (iter == 0) {

				if (buffer[0]    == Utf8_Bom_Sig[0]
				    && buffer[1] == Utf8_Bom_Sig[1]
				    && buffer[2] == Utf8_Bom_Sig[2]) {

					buffer    =  buffer[3..];
					bytesRead -= Utf8_Bom_Sig.Length;
				}
			}

			fs.Write(buffer, 0, bytesRead);

			iter++;
		}

		fs.Flush();

		return true;
	}

	public static string CopyInputStream(int bufSize = 4096)
	{
		string path = null;

		using Stream stdin = Console.OpenStandardInput();

		var buffer  = new byte[bufSize];
		var buffer2 = new byte[10_000_000];
		int bytesRead;
		int iter  = 0;
		int b2pos = 0;

		while ((bytesRead = stdin.Read(buffer, 0, buffer.Length)) > 0) {
			if (iter == 0) {

				if (buffer[0]    == Utf8_Bom_Sig[0]
				    && buffer[1] == Utf8_Bom_Sig[1]
				    && buffer[2] == Utf8_Bom_Sig[2]) {

					buffer    =  buffer[3..];
					bytesRead -= Utf8_Bom_Sig.Length;
				}
			}

			// fs.Write(buffer, 0, bytesRead);

			Array.Copy(buffer, 0, buffer2, b2pos, bytesRead);
			b2pos += bytesRead;

			iter++;
		}

		// fs.Flush();
		if (buffer2[(b2pos - 1)] == '\n' && buffer2[(b2pos - 2)] == '\r') {
			b2pos -= 2;
		}

		Array.Resize(ref buffer2, b2pos);

		var s = Console.InputEncoding.GetString(buffer2);

		if (File.Exists(s)) {
			// Console.WriteLine("Exists!");
			path = s;
		}
		else {
			// using var fs = File.Open(path, FileMode.Truncate, FileAccess.Write);
			path = Path.GetTempFileName();
			File.WriteAllBytes(path, buffer2);
		}

		// Console.WriteLine($"{s} {buffer2.Length} {b2pos} {bytesRead}");

		return path;
	}

	public static async Task<int> Main(string[] args)
	{
		Debug.WriteLine(AConsole.Profile.Height);
		Debug.WriteLine(Console.BufferHeight);
		Debugger.Launch();

		if (Console.IsInputRedirected) {
			var pipeInput = CopyInputStream();

			var newArgs = new string[args.Length + 1];
			newArgs[0] = pipeInput;
			args.CopyTo(newArgs, 1);

			args = newArgs;
		}

		var ff = CliFormat.LoadFigletFontFromResource(nameof(R2.Fg_larry3d), out var ms);

		// ms?.Dispose();

		var fg = new FigletText(ff, R1.Name)
			.LeftJustified()
			.Color(CliFormat.Clr_Misc1);

		AConsole.Write(fg);

#if DEBUG
		AConsole.WriteLine(args.QuickJoin());
#endif

		Grid grd = CliFormat.CreateInfoGrid();

		AConsole.Write(grd);

		// var env = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process);

		var app = new CommandApp<SearchCommand>();

		app.Configure(c =>
		{
			c.PropagateExceptions();
			var helpProvider = new CustomHelpProvider(c.Settings);
			c.SetHelpProvider(helpProvider);

			/*
			c.SetExceptionHandler((x, i) =>
			{
				AConsole.WriteLine($"{x}");
				Console.ReadKey();

			});
			*/

			//...
		});

		try {
			var x = await app.RunAsync(args);

			if (x != SearchCommand.EC_OK) {
				AConsole.Confirm("Press any key to continue");
			}

			// Console.ReadLine();

			return x;
		}
		catch (Exception e) {
			AConsole.WriteException(e);
			return SearchCommand.EC_ERROR;
		}
	}

}