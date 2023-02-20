#nullable disable
global using SConsole = System.Console;
global using static Kantan.Diagnostics.LogCategories;
using System.CommandLine;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Kantan.Threading;
// using Windows.UI.Notifications;
// using CommunityToolkit.WinUI.Notifications;
using Novus;
using Terminal.Gui;
using SmartImage.App;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Results;
using SmartImage.Mode;
using SmartImage.Mode.Shell;
using SmartImage.Utilities;
using Command = System.CommandLine.Command;

#pragma warning disable CS0168

// ReSharper disable InconsistentNaming

namespace SmartImage;

// # Notes
/*
 * ...
 */

public static class Program
{
	[ModuleInitializer]
	public static void Init()
	{
		Global.Setup();
		Trace.WriteLine("Init", R2.Name);
		// Gui.Init();
		System.Console.Title = R2.Name;

		if (Compat.IsWin) {
			ConsoleUtil.SetConsoleMode();
			System.Console.OutputEncoding = Encoding.Unicode;
		}

		AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
		{
			Trace.WriteLine($"Exiting", R2.Name);
		};

	}

	public static async Task<int> Main(string[] args)
	{
		// Console.OutputEncoding = Encoding.Unicode;

		// ToastNotificationManagerCompat.OnActivated += AppNotification.OnActivated;

#if TEST
		// args = new String[] { null };
		// args = new[] { R2.Arg_Input, "https://i.imgur.com/QtCausw.png",R2.Arg_AutoSearch };

		// ReSharper disable once ConditionIsAlwaysTrueOrFalse

		Debug.WriteLine($"TEST");

		/*
		 * & .\bin\Test\net7.0\win10-x64\SmartImage.exe --noui --i C:\Users\Deci\Pictures\lilith___the_maid_i_hired_recently_is_mysterious_by_sciamano240_dfnpdmn.png
		 *
		 * dotnet run -c Test --project .\SmartImage.csproj --interactive --noui --i C:\Users\Deci\Pictures\lilith___the_maid_i_hired_recently_is_mysterious_by_sciamano240_dfnpdmn.png
		 */
#endif

		bool cli = args is { } && args.Any();

		if (cli && args.Contains(R2.Arg_NoUI)) {

			var main = new CliMode();

			var rc = new RootCommand()
				{ };

			var options = new Option[]
			{
				new Option<string>(R2.Arg_Input)
					{ },

				new Option<bool>(R2.Arg_NoUI)
				{
					Arity = ArgumentArity.Zero,

				},
				
			};

			foreach (Option option in options) {
				rc.AddOption(option);
			}

			rc.SetHandler(main.RunAsync, (Option<string>) options[0]);

			var i = await rc.InvokeAsync(args);

			return i;
		}
		else {
			main1:
			Application.Init();

			var    main = new ShellMode(args);
			object status;

			var run = main.RunAsync(null);
			status = (bool?) await run;

			if (status is bool { } and true) {
				main.Dispose();
				goto main1;
			}

			return 0;
		}
	}
}