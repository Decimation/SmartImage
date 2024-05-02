using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Kantan.Text;
using Novus.Streams;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
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

	public static async Task<int> Main(string[] args)
	{
		Debug.WriteLine(AConsole.Profile.Height);
		Debug.WriteLine(Console.BufferHeight);

		if (Console.IsInputRedirected) {
			// using var str = Console.OpenStandardInput(500);
			Stream stdin = Console.OpenStandardInput(1000);
			/*byte[] inBuffer = new byte[1_000_000];

			int    outLen   = stdin.Read(inBuffer, 0, inBuffer.Length);
			char[] chars    = Encoding.ASCII.GetChars(inBuffer, 0, outLen);
			var cmd = new string(chars);
			if ((cmd[cmd.Length - 2] == '\r') && (cmd[cmd.Length - 1] == '\n'))
			{
				cmd = cmd.Substring(0, cmd.Length - 2);
			}*/
			var          sr = new StreamReader(stdin);
			int          n1 = 0, n2 = 0;
			var sb = new char[8_000_000];

			while (!sr.EndOfStream) {
				/*if (sb[^2] == '\r' && sb[^1] == '\n') {
					break;
				}*/
				n1 += (n2 = sr.Read(sb, n1, sb.Length - n1));

				if (n2 == 0) {
					break;
				}

			}

			sb = sb[..n1];

			if (sb[^2] == '\r' && sb[^1] == '\n') {
				sb = sb[0..^2];
			}

			IImageFormat fmt;
			AConsole.WriteLine($"{sb.Length}");

			/*try {
				fmt = await Image.DetectFormatAsync();
			}
			catch (Exception e) {
				AConsole.WriteLine($"{e.Message}");
			}
			finally {
				str.TrySeek();
			}*/
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