using System;
using System.Diagnostics;
using System.IO;
using Kantan.Cli;
using Kantan.Diagnostics;
using Kantan.Net;
using Kantan.Utilities;
using Microsoft.Toolkit.Uwp.Notifications;
using Novus.Utilities;
using SmartImage.Core;
using SmartImage.Lib;
using SmartImage.Lib.Searching;
using SmartImage.Lib.Utilities;
using SmartImage.Utilities;
using static Novus.Utilities.ReflectionOperatorHelpers;
using static Kantan.Diagnostics.LogCategories;
using static SmartImage.Program;

// ReSharper disable UnusedMember.Global
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

// ReSharper disable PossibleNullReferenceException

namespace SmartImage.UI
{
	/// <summary>
	/// Handles the main menu interface
	/// </summary>
	internal static class AppInterface
	{
		internal static readonly NConsoleOption[] MainMenuOptions =
		{
			new()
			{
				Name  = ">>> Run <<<",
				Color = Elements.ColorMain,
				Function = () =>
				{
					ImageQuery query = NConsole.ReadInput("Image file or direct URL", x =>
					{
						x = x.CleanString();

						(bool url, bool file) = ImageQuery.IsUriOrFile(x);
						return !(url || file);
					}, "Input must be file or direct image link");

					Config.Query = query;
					return true;
				}
			},

			NConsoleFactory.CreateConfigOption(memberof(() => Config.SearchEngines), "Engines"),
			NConsoleFactory.CreateConfigOption(memberof(() => Config.PriorityEngines), "Priority engines"),
			NConsoleFactory.CreateConfigOption(propertyof(() => Config.Filtering), "Filter", 3),
			NConsoleFactory.CreateConfigOption(propertyof(() => Config.Notification), "Notification", 4),
			NConsoleFactory.CreateConfigOption(propertyof(() => Config.NotificationImage), "Notification image", 5),
			NConsoleFactory.CreateConfigOption(propertyof(() => AppIntegration.IsContextMenuAdded), "Context menu", 6,
			                                   (added) => AppIntegration.HandleContextMenu(
				                                   added ? IntegrationOption.Remove : IntegrationOption.Add)),

			new()
			{
				Name = "Config",
				Function = () =>
				{
					//Console.Clear();

					Console.WriteLine(Config);
					NConsole.WaitForInput();

					return null;
				}
			},
			new()
			{
				Name = "Info",
				Function = () =>
				{
					//Console.Clear();

					Console.WriteLine($"Author: {AppInfo.AUTHOR}");

					Console.WriteLine($"Current version: {AppInfo.Version} ({UpdateInfo.GetUpdateInfo().Status})");
					Console.WriteLine($"Latest version: {ReleaseInfo.GetLatestRelease()}");

					Console.WriteLine();

					var di = new DirectoryInfo(AppInfo.ExeLocation);

					Console.WriteLine($"Executable location: {di.Parent.Name}");
					Console.WriteLine($"In path: {AppInfo.IsAppFolderInPath}");

					Console.WriteLine();
					Console.WriteLine(Strings.Separator);

					foreach (var utility in ImageHelper.UtilitiesMap) {
						Console.WriteLine(utility);
					}

					Console.WriteLine();
					Console.WriteLine(Strings.Separator);

					var dependencies = ReflectionHelper.DumpDependencies();

					foreach (var name in dependencies) {
						Console.WriteLine($"{name.Name} ({name.Version})");
					}

					NConsole.WaitForInput();

					return null;
				}
			},
			new()
			{
				Name = "Update",
				Function = () =>
				{
					UpdateInfo.AutoUpdate();

					return null;
				}
			},
			new()
			{
				Name = "Help",
				Function = () =>
				{
					WebUtilities.OpenUrl(AppInfo.URL_WIKI);

					return null;
				}
			},
#if DEBUG

			new()
			{
				Name = "Debug",
				Function = () =>
				{
					Config.Query = @"C:\Users\Deci\Pictures\Test Images\Test1.jpg";
					return true;
				}
			},
#endif

		};

		internal static readonly NConsoleDialog MainMenuDialog = new()
		{
			Options   = MainMenuOptions,
			Header    = AppInfo.NAME_BANNER,
			Functions = Array.Empty<Action>()
		};

		/*
		/// <summary>Returns true if the current application has focus, false otherwise</summary>
		internal static bool ApplicationIsActivated()
		{
			//https://stackoverflow.com/questions/7162834/determine-if-current-application-is-activated-has-focus
			var activatedHandle = Native.GetForegroundWindow();

			if (activatedHandle == IntPtr.Zero) {
				return false; // No window is currently activated
			}

			var p1     = Process.GetCurrentProcess();
			var procId = p1.Id;
			int activeProcId;
			Native.GetWindowThreadProcessId(activatedHandle, out activeProcId);
			var p2 = Process.GetProcessById(activeProcId);

			return activeProcId == procId;
		}*/

		#region Toast

		private const string ARG_KEY_ACTION    = "action";
		private const string ARG_VALUE_DISMISS = "dismiss";

		public static void ShowToast(object sender, SearchCompletedEventArgs args)
		{
			var bestResult = args.Best;

			var builder = new ToastContentBuilder();
			var button  = new ToastButton();
			var button2 = new ToastButton();

			button2.SetContent("Dismiss")
			       .AddArgument(ARG_KEY_ACTION, ARG_VALUE_DISMISS);

			button.SetContent("Open")
			      .AddArgument(ARG_KEY_ACTION, $"{bestResult.Value.Url}");

			builder.AddButton(button)
			       .AddButton(button2)
			       .AddText("Search complete")
			       .AddText($"{bestResult}")
			       .AddText($"Results: {Program.Client.Results.Count}");

			var direct = args.Direct?.Value?.Direct;

			if (direct != null) {
				var path = Path.GetTempPath();
				var file = ImageHelper.Download(direct, path);

				Debug.WriteLine($"Downloaded {file}", LogCategories.C_INFO);

				builder.AddHeroImage(new Uri(file));

				AppDomain.CurrentDomain.ProcessExit += (sender2, args2) =>
				{
					File.Delete(file);
				};
			}

			builder.SetBackgroundActivation();

			//...

			builder.Show();

			//ToastNotificationManager.CreateToastNotifier();
		}

		public static void OnToastActivated(ToastNotificationActivatedEventArgsCompat compat)
		{
			// NOTE: Does not return if invoked from background

			// Obtain the arguments from the notification

			var arguments = ToastArguments.Parse(compat.Argument);

			foreach (var argument in arguments) {
				Debug.WriteLine($"Toast argument: {argument}", C_DEBUG);

				if (argument.Key == ARG_KEY_ACTION) {

					if (argument.Value == ARG_VALUE_DISMISS) {
						break;
					}

					WebUtilities.OpenUrl(argument.Value);
				}
			}

			if (ToastNotificationManagerCompat.WasCurrentProcessToastActivated()) {
				//
				Environment.Exit(0);
			}
		}

		#endregion
	}
}