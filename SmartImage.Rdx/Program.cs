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
 */

public static class Program
{

	private static readonly byte[] Utf8_Bom_Sig = new[]
	{
		(byte) 0xEF, (byte) 0xBB, (byte) 0xBF
	};

	public static string ReadInputStream(out bool isFile)
	{

		var path = Path.GetTempFileName();

		using var fs = File.Open(path, FileMode.Truncate, FileAccess.Write);

		using Stream stdin = Console.OpenStandardInput();

		byte[] buffer = new byte[4096]; // Buffer to hold byte input
		int    bytesRead;
		int    iter = 0;

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

		isFile = File.Exists(path);
		fs.Flush();
		fs.Dispose();

		return path;
	}

	public static async Task<int> Main(string[] args)
	{
		Debug.WriteLine(AConsole.Profile.Height);
		Debug.WriteLine(Console.BufferHeight);

		if (Console.IsInputRedirected) {
			var pipeInput = ReadInputStream(out var isf);

			// AConsole.WriteLine($"[{pipeInput}] {isf}");

			var newargs = new string[args.Length + 1];
			newargs[0] = pipeInput;
			args.CopyTo(newargs, 1);

			args = newargs;
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