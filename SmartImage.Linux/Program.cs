global using R2 = SmartImage.Linux.Resources;
global using R1 = SmartImage.Lib.Resources;
global using AC = Spectre.Console.AnsiConsole;
global using AConsole = Spectre.Console.AnsiConsole;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Kantan.Text;
using Spectre.Console;
using Spectre.Console.Cli;
using SmartImage.Linux.Cli;

namespace SmartImage.Linux;

public static class Program
{
	public static async Task<int> Main(string[] args)
	{
		//dotnet run --project SmartImage.Linux/ "$HOME/1654086015521.png"

		AC.WriteLine(args.QuickJoin());
		AC.Write(new FigletText(R1.Name)
			         .LeftJustified()
			         .Color(Color.Red));
#if DEBUG
		
#endif
		AC.WriteLine($"{OperatingSystem.IsLinux()}|{OperatingSystem.IsWindows()}|{OperatingSystem.IsMacOS()}");

		var app = new CommandApp<SearchCommand>();
		return await app.RunAsync(args);
	}
}