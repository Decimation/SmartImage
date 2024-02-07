using System.Diagnostics;
using System.Reflection;
using Kantan.Text;
using Microsoft.Extensions.DependencyInjection;
using SmartImage.Rdx.Cli;
using Spectre.Console;
using Spectre.Console.Cli;
using Microsoft.Extensions.Hosting;

namespace SmartImage.Rdx;

// dotnet run --project SmartImage.Rdx/ "$HOME/1654086015521.png"
// dotnet run -c 'DEBUG' --project SmartImage.Rdx "$HOME/1654086015521.png"
// dotnet run -lp 'SmartImage.Rdx' -c 'WSL' --project SmartImage.Rdx "$HOME/1654086015521.png"
// dotnet SmartImage.Rdx/bin/Debug/net8.0/SmartImage.Rdx.dll "/home/neorenegade/1654086015521.png"

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
		string os = null;

		if (IsLinux) {
			os = "Linux";

		}
		else if (IsWindows) {
			os = "Windows";
		}
		else if (IsMacOs) {
			os = "Mac";
		}

		AConsole.WriteLine($"OS: {os} {Environment.OSVersion} | {Environment.Version}");
		AConsole.WriteLine($"{AConsole.Profile.Capabilities.Ansi} | {AConsole.Profile.Capabilities.ColorSystem}");

		var app = new CommandApp<SearchCommand>();

		app.Configure(c =>
		{
			c.PropagateExceptions();

			//...
		});

		try {
			var x = await app.RunAsync(args);

			return x;
		}
		catch (Exception e) {
			AConsole.WriteException(e);
			return -1;
		}
	}

}
