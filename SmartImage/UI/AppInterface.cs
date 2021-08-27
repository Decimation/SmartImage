using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using Kantan.Cli;
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
using static SmartImage.UI.NConsoleFactory;

// ReSharper disable AssignNullToNotNullAttribute

// ReSharper disable UnusedMember.Global
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

// ReSharper disable PossibleNullReferenceException

namespace SmartImage.UI
{
	/// <summary>
	/// Handles the main menu interface
	/// </summary>
	internal static partial class AppInterface
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

			CreateConfigOption(nameof(Config.SearchEngines), "Engines"),
			CreateConfigOption(nameof(Config.PriorityEngines), "Priority engines"),
			CreateConfigOption(nameof(Config.Filtering), "Filter", 3),
			CreateConfigOption(nameof(Config.Notification), "Notification", 4),
			CreateConfigOption(nameof(Config.NotificationImage), "Notification image", 5),

			CreateConfigOption(propertyof(() => AppIntegration.IsContextMenuAdded), "Context menu", 6,
			                   added => AppIntegration.HandleContextMenu(
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
			Functions = new Dictionary<ConsoleKey, Action>(),
			Status    = "You can also drag and drop a file to run a search."
		};

		static AppInterface()
		{
			// NOTE: Static initializer must be AFTER MainMenuDialog

			var current = UpdateInfo.GetUpdateInfo();

			if (current.Status == VersionStatus.Available) {

				var option = MainMenuOptions.First(f => f.Name == "Update");

				option.Name = option.Name.AddColor(Elements.ColorHighlight);

				var updateStr = $"* Update available (latest: {Elements.ToVersionString(current.Latest.Version)};" +
				                $" current: {Elements.ToVersionString(current.Current)})";

				updateStr = updateStr.AddColor(Elements.ColorHighlight);

				MainMenuDialog.Description = updateStr;

				option.Function = () =>
				{
					UpdateInfo.Update(current);
					return null;
				};
			}
		}

		#region Toast

		public static void ShowToast(object sender, SearchCompletedEventArgs args)
		{
			var bestResult = args.Best;

			var builder = new ToastContentBuilder();
			var button  = new ToastButton();
			var button2 = new ToastButton();

			button2.SetContent("Dismiss")
			       .AddArgument(Elements.ARG_KEY_ACTION, Elements.ARG_VALUE_DISMISS);

			button.SetContent("Open")
			      .AddArgument(Elements.ARG_KEY_ACTION, $"{bestResult.Value.Url}");

			builder.AddButton(button)
			       .AddButton(button2)
			       .AddText("Search complete")
			       .AddText($"{bestResult}")
			       .AddText($"Results: {Client.Results.Count}");

			var direct = args.Direct?.Value?.Direct;

			if (direct != null) {
				var path = Path.GetTempPath();
				var file = ImageHelper.Download(direct, path);

				Debug.WriteLine($"Downloaded {file}", C_INFO);

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

				if (argument.Key == Elements.ARG_KEY_ACTION) {

					if (argument.Value == Elements.ARG_VALUE_DISMISS) {
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