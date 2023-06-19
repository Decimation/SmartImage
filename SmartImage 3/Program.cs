// Read S SmartImage Program.cs
// 2022-07-02 @ 11:39 PM

#nullable disable

#region

global using static Kantan.Diagnostics.LogCategories;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Text;
using Kantan.Text;
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
using Microsoft.Extensions.Logging;
using SmartImage.Lib.Utilities;

#endregion

// ReSharper disable InconsistentNaming

namespace SmartImage;

// # Notes
/*
 * ...
 */

public static class Program
{
	private static readonly ILogger Logger = LogUtil.Factory.CreateLogger(nameof(SearchClient));

	static Program() { }

	[ModuleInitializer]
	public static void Init()
	{
		Global.Setup();
		Trace.WriteLine("Init", R2.Name);
		// Gui.Init();
		Console.Title = R2.Name;

		if (Compat.IsWin) {
			ConsoleUtil.SetConsoleMode();
			// System.Console.OutputEncoding = Encoding.Unicode;
		}

		AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
		{
			Trace.WriteLine($"Exiting", R2.Name);
		};

		AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
		{
			var ex = args.ExceptionObject as Exception;
			File.WriteAllLines($"smartimage.log", new []
			{
				$"Message: {ex.Message}",
				$"Source: {ex.Source}",
				$"Stack trace: {ex.StackTrace}",

			});
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

		/*
		 * & .\bin\Test\net7.0\win10-x64\SmartImage.exe --noui --i C:\Users\Deci\Pictures\lilith___the_maid_i_hired_recently_is_mysterious_by_sciamano240_dfnpdmn.png
		 *
		 * dotnet run -c Test --project .\SmartImage.csproj --interactive --noui --i C:\Users\Deci\Pictures\lilith___the_maid_i_hired_recently_is_mysterious_by_sciamano240_dfnpdmn.png
		 */
#endif

		bool c = Global.IsCompatible;

		if (!c) {
			Logger.LogCritical("{Lib} incompatible!", Global.LIB_NAME);
		}

		bool cli = args is { } && args.Any();

		if (cli && args.Contains(R2.Arg_NoUI)) {
			var main = new CliMode();

			/*var rc = new RootCommand()
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

			return i;*/

			var r = await main.RunAsync(args[0]);

			return 0;
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

			return ConsoleUtil.CODE_OK;
		}

	}
}