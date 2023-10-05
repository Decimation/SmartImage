using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Kantan.Text;
using Spectre.Console;
using Spectre.Console.Cli;
using SmartImage.Linux.Cli;
using Spectre.Console.Cli.Internal.Configuration;

namespace SmartImage.Linux;

public static class Program
{
	internal static readonly bool IsLinux   = OperatingSystem.IsLinux();
	internal static readonly bool IsWindows = OperatingSystem.IsWindows();
	internal static readonly bool IsMacOs   = OperatingSystem.IsMacOS();

	public static async Task<int> Main(string[] args)
	{
		//dotnet run --project SmartImage.Linux/ "$HOME/1654086015521.png"
		//dotnet run -c 'DEBUG' --project SmartImage.Linux "$HOME/1654086015521.png"
		//dotnet run -lp 'SmartImage.Linux' -c 'WSL' --project SmartImage.Linux "$HOME/1654086015521.png"

		var fs = R2.ResourceManager.GetObject(nameof(R2.Fg_Cybermedium));

		var fg = new FigletText(FigletFont.Load(new MemoryStream((byte[]) fs)), R1.Name)
			.LeftJustified()
			.Color(new Color(0x80, 0xFF, 0x80));

		AC.Write(fg);
#if DEBUG
		AC.WriteLine(args.QuickJoin());

#endif
		AC.WriteLine($"{IsLinux}|{IsWindows}|{IsMacOs}");

		var app = new CommandApp<SearchCommand>();
		app.Configure(c => { });

		var x = await app.RunAsync(args);

		return x;
	}
}