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

		var ff = CliFormat.LoadFigletFontFromResource(nameof(R2.Fg_larry3d), out var ms);

		// ms?.Dispose();

		var fg = new FigletText(ff, R1.Name)
			.LeftJustified()
			.Color(CliFormat.Color1);

		AC.Write(fg);

#if DEBUG
		AC.WriteLine(args.QuickJoin());

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

		AC.WriteLine($"OS: {os} {Environment.OSVersion} | {Environment.Version}");

		var app = new CommandApp<SearchCommand>();

		app.Configure(c =>
		{
		
			//...
		});

		var x = await app.RunAsync(args);

		return x;
	}

}

class SearchConfigCli : ISearchConfig { }

interface ISearchConfig { }