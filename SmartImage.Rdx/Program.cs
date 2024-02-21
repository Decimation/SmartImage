using System.Diagnostics;
using System.Reflection;
using Kantan.Text;
using Microsoft.Extensions.DependencyInjection;
using SmartImage.Rdx.Cli;
using Spectre.Console;
using Spectre.Console.Cli;
using Microsoft.Extensions.Hosting;
using Spectre.Console.Cli.Help;

namespace SmartImage.Rdx;

/*
 * cd /mnt/c/Users/Deci/RiderProjects/SmartImage/
 * dotnet run --project SmartImage.Rdx/ "$HOME/1654086015521.png"
 * dotnet run -c 'DEBUG' --project SmartImage.Rdx "$HOME/1654086015521.png"
 * dotnet run -lp 'SmartImage.Rdx' -c 'WSL' --project SmartImage.Rdx "$HOME/1654086015521.png"
 * dotnet SmartImage.Rdx/bin/Debug/net8.0/SmartImage.Rdx.dll "/home/neorenegade/1654086015521.png"
 * dotnet run -c Test --project SmartImage.Rdx --  "/home/neorenegade/0c4c80957134d4304538c27499d84dbe.jpeg" -e All -p Auto
 * ./SmartImage.Rdx/bin/Release/net8.0/publish/linux-x64/SmartImage "/home/neorenegade/0c4c80957134d4304538c27499d84dbe.jpeg"
 *
 */

public static class Program
{

	internal static readonly bool IsLinux   = OperatingSystem.IsLinux();
	internal static readonly bool IsWindows = OperatingSystem.IsWindows();
	internal static readonly bool IsMacOs   = OperatingSystem.IsMacOS();

	public static async Task<int> Main(string[] args)
	{
		Debug.WriteLine(AConsole.Profile.Height);
		Debug.WriteLine(Console.BufferHeight);

		var ff = CliFormat.LoadFigletFontFromResource(nameof(R2.Fg_larry3d), out var ms);

		// ms?.Dispose();

		var fg = new FigletText(ff, R1.Name)
			.LeftJustified()
			.Color(CliFormat.Color1);

		AConsole.Write(fg);

#if DEBUG
		AConsole.WriteLine(args.QuickJoin());

#endif
		string? os = null;

		if (IsLinux) {
			os = "Linux";

		}
		else if (IsWindows) {
			os = "Windows";
		}
		else if (IsMacOs) {
			os = "Mac";
		}

		var grd = new Grid();
		grd.AddColumns(2);

		grd.AddRow("OS", $"{os} / {Environment.OSVersion}");
		grd.AddRow("Runtime", $"{Environment.Version}");

		grd.AddRow("Terminal ANSI", $"{CliFormat.ProfileCapabilities.Ansi}");
		grd.AddRow("Terminal colors", $"{CliFormat.ProfileCapabilities.ColorSystem}");
		grd.AddRow("Terminal links", $"{CliFormat.ProfileCapabilities.Links}");
		grd.AddRow("Terminal Unicode", $"{CliFormat.ProfileCapabilities.Unicode}");

		var env = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process);

		AConsole.Write(grd);

		var app = new CommandApp<SearchCommand>();

		app.Configure(c =>
		{
			c.PropagateExceptions();
			c.SetHelpProvider(new HelpProvider(c.Settings));

			//...
		});

		try {
		res:
			var x = await app.RunAsync(args);

			switch ((Act) x) {
				case Act.Restart:
					goto res;

					break;
			}

			return x;
		}
		catch (Exception e) {
			AConsole.WriteException(e);

			return SearchCommand.EC_ERROR;
		}
	}

}