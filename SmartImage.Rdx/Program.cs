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
 * $cx2=[System.IO.File]::ReadAllBytes($(Resolve-Path "..\..\Pictures\Art\fucking_epic.jpg"))
 *
 */

public static class Program
{

	public static async Task<int> Main(string[] args)
	{
		Debug.WriteLine(AConsole.Profile.Height);
		Debug.WriteLine(Console.BufferHeight);

#if DEBUG

		Debugger.Launch();
#endif

		if (Console.IsInputRedirected) {
			var pipeInput = ConsoleUtil.ParseInputStream();

			var newArgs = new string[args.Length + 1];
			newArgs[0] = pipeInput;
			args.CopyTo(newArgs, 1);

			args = newArgs;
			
			AConsole.WriteLine($"Received input from stdin");
		}

		var ff = ConsoleFormat.LoadFigletFontFromResource(nameof(R2.Fg_larry3d), out var ms);

		// ms?.Dispose();

		var fg = new FigletText(ff, R1.Name)
			.LeftJustified()
			.Color(ConsoleFormat.Clr_Misc1);

		AConsole.Write(fg);

#if DEBUG
		Trace.WriteLine(args.QuickJoin());
#endif

		Grid grd = ConsoleFormat.CreateInfoGrid();

		AConsole.Write(grd);

		// var env = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process);

		var app = new CommandApp<SearchCommand>();

		app.Configure(c =>
		{
			c.PropagateExceptions();
			var helpProvider = new CustomHelpProvider(c.Settings);
			c.SetHelpProvider(helpProvider);
		});

		try {
			var x = await app.RunAsync(args);

			if (x != SearchCommand.EC_OK) {
				AConsole.Confirm("Press any key to continue");
			}

			return x;
		}
		catch (Exception e) {
			AConsole.WriteException(e);
			return SearchCommand.EC_ERROR;
		}
	}

}