using SimpleCore.Cli;
using SimpleCore.Utilities;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Searching;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Toolkit.Uwp.Notifications;
using Novus.Utilities;
using Novus.Win32;
using SimpleCore.Net;
using SmartImage.Lib;
using SmartImage.Utilities;

// ReSharper disable PossibleNullReferenceException

namespace SmartImage.Core
{
	/// <summary>
	/// Handles the main menu interface
	/// </summary>
	internal static class MainDialog
	{
		#region Colors

		internal static readonly Color ColorMain  = Color.Yellow;
		internal static readonly Color ColorOther = Color.Aquamarine;
		internal static readonly Color ColorYes   = Color.GreenYellow;
		internal static readonly Color ColorNo    = Color.Red;

		#endregion

		#region Elements

		public const string Description = "Press the result number to open in browser\n" +
		                                     "Ctrl: Load direct | Alt: Show other | Shift: Open raw | Alt+Ctrl: Download";

		private static readonly string Enabled = StringConstants.CHECK_MARK.ToString().AddColor(ColorYes);

		private static readonly string Disabled = StringConstants.MUL_SIGN.ToString().AddColor(ColorNo);

		internal static string ToToggleString(this bool b) => b ? Enabled : Disabled;

		private static string GetFilterName(bool added) => GetName("Filter", added);

		private static string GetNotificationName(bool added) => GetName("Notification", added);

		private static string GetNotificationImageName(bool added) => GetName("Notification image", added);

		private static string GetName(string s, bool added) => $"{s} ({(added.ToToggleString())})";


		private static string GetContextMenuName(bool added) => GetName("Context menu", added);

		#endregion


		private static readonly NConsoleOption[] MainMenuOptions =
		{
			new()
			{
				Name  = ">>> Run <<<",
				Color = ColorMain,
				Function = () =>
				{
					ImageQuery query = NConsole.ReadInput("Image file or direct URL", x =>
					{
						(bool url, bool file) = ImageQuery.IsUriOrFile(x);
						return !(url || file);
					}, "Input must be file or direct image link");

					Program.Config.Query = query;
					return true;
				}
			},

			new()
			{
				Name  = "Engines",
				Color = ColorOther,
				Function = () =>
				{
					Program.Config.SearchEngines = ReadEnum<SearchEngineOptions>();

					Console.WriteLine(Program.Config.SearchEngines);
					NConsole.WaitForSecond();
					SaveAndUpdateConfig();
					return null;
				}
			},

			new()
			{
				Name  = "Priority engines",
				Color = ColorOther,
				Function = () =>
				{
					Program.Config.PriorityEngines = ReadEnum<SearchEngineOptions>();

					Console.WriteLine(Program.Config.PriorityEngines);
					NConsole.WaitForSecond();
					SaveAndUpdateConfig();
					return null;
				}
			},
			new()
			{
				Name = GetFilterName(Program.Config.Filtering),
				Function = () =>
				{
					Program.Config.Filtering = !Program.Config.Filtering;

					MainMenuOptions[3].Name = GetFilterName(Program.Config.Filtering);
					SaveAndUpdateConfig();
					return null;
				}
			},
			new()
			{
				Name = GetNotificationName(Program.Config.Notification),
				Function = () =>
				{
					Program.Config.Notification = !Program.Config.Notification;
					

					MainMenuOptions[4].Name = GetNotificationName(Program.Config.Notification);
					SaveAndUpdateConfig();
					return null;
				}
			},
			new()
			{
				Name = GetNotificationImageName(Program.Config.NotificationImage),
				Function = () =>
				{
					Program.Config.NotificationImage = !Program.Config.NotificationImage;

					MainMenuOptions[5].Name = GetNotificationImageName(Program.Config.NotificationImage);
					SaveAndUpdateConfig();
					return null;
				}
			},
			new()
			{
				Name = GetContextMenuName(OSIntegration.IsContextMenuAdded),
				Function = () =>
				{
					bool added = OSIntegration.IsContextMenuAdded;

					OSIntegration.HandleContextMenu(added ? IntegrationOption.Remove : IntegrationOption.Add);

					added = OSIntegration.IsContextMenuAdded;


					MainMenuOptions[6].Name = GetContextMenuName(added);

					return null;
				}
			},
			new()
			{
				Name = "Config",
				Function = () =>
				{
					//Console.Clear();

					Console.WriteLine(Program.Config);

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

					Console.WriteLine($"Author: {Info.Author}");

					Console.WriteLine($"Current version: {Info.Version} ({UpdateInfo.GetUpdateInfo().Status})");
					Console.WriteLine($"Latest version: {ReleaseInfo.GetLatestRelease()}");

					Console.WriteLine();

					var di = new DirectoryInfo(Info.ExeLocation);

					Console.WriteLine($"Executable location: {di.Parent.Name}");
					Console.WriteLine($"In path: {Info.IsAppFolderInPath}");

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
					WebUtilities.OpenUrl(Info.Wiki);


					return null;
				}
			},
#if DEBUG


			new()
			{
				Name = "Debug",
				Function = () =>
				{

					Program.Config.Query = @"C:\Users\Deci\Pictures\Test Images\Test1.jpg";
					return true;
				}
			},
#endif

		};


		public static readonly NConsoleDialog MainMenuDialog = new()
		{
			Options = MainMenuOptions,
			Header  = Info.NAME_BANNER
		};


		private static void SaveAndUpdateConfig()
		{
			Program.Client.Reload();
			Program.SaveConfigFile();
		}

		private static TEnum ReadEnum<TEnum>() where TEnum : Enum
		{
			var enumOptions = NConsoleOption.FromEnum<TEnum>();

			var selected = NConsole.ReadOptions(new NConsoleDialog
			{
				Options        = enumOptions,
				SelectMultiple = true
			});

			var enumValue = Enums.ReadFromSet<TEnum>(selected);

			return enumValue;
		}
	}
}