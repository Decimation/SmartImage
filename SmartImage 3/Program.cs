#nullable disable

global using static Kantan.Diagnostics.LogCategories;
using System.Collections;
using System.ComponentModel;
using System.Configuration;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
// using Windows.UI.Notifications;
// using CommunityToolkit.WinUI.Notifications;
using Flurl.Http;
using Kantan.Console;
using Kantan.Net.Utilities;
using Kantan.Text;
using Microsoft.Extensions.Hosting;
using Rune = System.Text.Rune;
using Microsoft.Extensions.Configuration;
using Novus;
using Novus.FileTypes;
using Novus.Win32;
using Novus.Win32.Structures.Kernel32;
using SmartImage.Lib;
using Terminal.Gui;
using ConfigurationManager = System.Configuration.ConfigurationManager;
using Attribute = Terminal.Gui.Attribute;
using SmartImage.Shell;
using SmartImage.App;

#pragma warning disable CS0168

// ReSharper disable InconsistentNaming

namespace SmartImage;

// # Notes
/*
 * ...
 */

// # Terminology
// Indicators	|	in this codebase and in relevant contexts, refers to the "pointer" or "link" referencing a binary resource
//					(e.g., a file path or direct URI)

public static class Program
{
	private static GuiMain _main;

	[ModuleInitializer]
	public static void Init()
	{
		Global.Setup();
		Trace.WriteLine("Init", Resources.Name);
		// Gui.Init();
		Console.Title = R2.Name;
		ConsoleUtil.SetConsoleMode();
		Application.Init();
	}

	public static async Task Main(string[] args)
	{
		// Console.OutputEncoding = Encoding.Unicode;

		// ToastNotificationManagerCompat.OnActivated += AppNotification.OnActivated;

#if TEST
		// args = new String[] { null };
		args = new[] { R2.Arg_Input, "https://i.imgur.com/QtCausw.png",R2.Arg_AutoSearch };

		// ReSharper disable once ConditionIsAlwaysTrueOrFalse
#endif
		bool cli = args is { } && args.Any();

		_main = new GuiMain(args);

		main1:

		object status;

		var run = _main.RunAsync(null);
		status = await run;

		if (status is bool { } and true) {
			_main.Dispose();
			goto main1;
		}
	}

}
