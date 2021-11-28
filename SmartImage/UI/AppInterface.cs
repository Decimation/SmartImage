using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using Kantan.Cli;
using Kantan.Cli.Controls;
using Kantan.Net;
using Kantan.Numeric;
using Kantan.Text;
using Kantan.Utilities;
using Microsoft.Toolkit.Uwp.Notifications;
using Novus.Utilities;
using SmartImage.Core;
using SmartImage.Lib;
using SmartImage.Lib.Searching;
using SmartImage.Lib.Utilities;
using SmartImage.Properties;
using SmartImage.Utilities;
using static Novus.Utilities.ReflectionOperatorHelpers;
using static Kantan.Diagnostics.LogCategories;
using static SmartImage.Program;
using static SmartImage.UI.ConsoleUIFactory;

// ReSharper disable LocalizableElement
// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable UnusedMember.Global
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable PossibleNullReferenceException

namespace SmartImage.UI;

/// <summary>
/// Handles the main menu interface
/// </summary>
internal static partial class AppInterface
{
	internal static readonly ConsoleOption[] MainMenuOptions =
	{
		new()
		{
			Name  = ">>> Run <<<",
			Color = Elements.ColorMain,
			Function = () =>
			{
				ImageQuery query = ConsoleManager.ReadLine("Image file or direct URL", x =>
				{
					x = x.CleanString();

					(bool url, bool file) = ImageQuery.IsUriOrFile(x);
					return !(url || file);
				}, "Input must be file or direct image link");

				Config.Query = query;
				return true;
			}
		},

		CreateEnumConfigOption(nameof(Config.SearchEngines), "Engines"),
		CreateEnumConfigOption(nameof(Config.PriorityEngines), "Priority engines"),
		CreateConfigOption(nameof(Config.Filtering), "Filter", Program.Config),
		CreateConfigOption(nameof(Config.Notification), "Notification", Program.Config),
		CreateConfigOption(nameof(Config.NotificationImage), "Notification image", Program.Config),

		CreateConfigOption(propertyof(() => AppIntegration.IsContextMenuAdded), "Context menu",
		                   added => AppIntegration.HandleContextMenu(!added)),

		new()
		{
			Name = "Config",
			Function = () =>
			{
				//Console.Clear();

				Console.WriteLine(Config);
				ConsoleManager.WaitForInput();

				return null;
			}
		},
		new()
		{
			Name = "Info",
			Function = () =>
			{
				//Console.Clear();

				Console.WriteLine($"Author: {Resources.Author}");

				Console.WriteLine($"Current version: {AppInfo.Version} ({UpdateInfo.GetUpdateInfo().Status})");
				Console.WriteLine($"Latest version: {ReleaseInfo.GetLatestRelease()}");

				Console.WriteLine();

				var di = new DirectoryInfo(AppInfo.ExeLocation);

				Console.WriteLine($"Executable location: {di.Parent.Name}");
				Console.WriteLine($"In path: {AppInfo.IsAppFolderInPath}");

				Console.WriteLine();
				Console.WriteLine(Strings.Constants.Separator);

				foreach (var utility in ImageHelper.UtilitiesMap) {
					Console.WriteLine(utility);
				}

				Console.WriteLine();
				Console.WriteLine(Strings.Constants.Separator);

				var dependencies = ReflectionHelper.DumpDependencies();

				foreach (var name in dependencies) {
					Console.WriteLine($"{name.Name} ({name.Version})");
				}

				ConsoleManager.WaitForInput();

				return null;
			}
		},
		new()
		{
			Name     = "Update",
			Function = null
		},
		new()
		{
			Name = "Help",
			Function = () =>
			{
				WebUtilities.OpenUrl(Resources.UrlWiki);

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

	internal static readonly ConsoleDialog MainMenuDialog = new()
	{
		Options   = MainMenuOptions,
		Header    = NAME_BANNER,
		Functions = new Dictionary<ConsoleKey, Action>(),
		Status    = "You can also drag and drop a file to run a search."
	};

	/// <summary>
	/// Name in ASCII art
	/// </summary>
	internal const string NAME_BANNER =
		"  ____                       _   ___\n" +
		" / ___| _ __ ___   __ _ _ __| |_|_ _|_ __ ___   __ _  __ _  ___\n" +
		@" \___ \| '_ ` _ \ / _` | '__| __|| || '_ ` _ \ / _` |/ _` |/ _ \" + "\n" +
		"  ___) | | | | | | (_| | |  | |_ | || | | | | | (_| | (_| |  __/\n" +
		@" |____/|_| |_| |_|\__,_|_|   \__|___|_| |_| |_|\__,_|\__, |\___|" + "\n" +
		"                                                     |___/\n";

	static AppInterface()
	{
		// NOTE: Static initializer must be AFTER MainMenuDialog

		var current = UpdateInfo.GetUpdateInfo();

		if (current.Status != VersionStatus.Available) {
			return;
		}

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