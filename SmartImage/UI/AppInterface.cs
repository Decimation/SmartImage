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

namespace SmartImage.UI
{
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

			CreateConfigOption(nameof(Config.SearchEngines), "Engines"),
			CreateConfigOption(nameof(Config.PriorityEngines), "Priority engines"),
			CreateConfigOption(nameof(Config.Filtering), "Filter", 3),
			CreateConfigOption(nameof(Config.Notification), "Notification", 4),
			CreateConfigOption(nameof(Config.NotificationImage), "Notification image", 5),

			CreateConfigOption(propertyof(() => AppIntegration.IsContextMenuAdded), "Context menu", 6,
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
			Debug.WriteLine($"Building toast", C_DEBUG);
			var bestResult = args.Detailed;

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

			if (Config.Notification && Config.NotificationImage) {

				var imageResult = args.FirstDirect.Value;

				if (imageResult != null) {
					var path = Path.GetTempPath();

					string file = ImageHelper.Download(imageResult.Direct, path);

					if (file == null) {
						int i = 0;

						var imageResults = args.Direct.Value;

						do {
							file = ImageHelper.Download(imageResults[i++].Direct, path);

						} while (String.IsNullOrWhiteSpace(file) && i < imageResults.Length);

					}

					if (file != null) {

						file = GetHeroImage(path, file);

						Debug.WriteLine($"{nameof(AppInterface)}: Downloaded {file}", C_INFO);

						builder.AddHeroImage(new Uri(file));

						AppDomain.CurrentDomain.ProcessExit += (sender2, args2) =>
						{
							File.Delete(file);
						};
					}

				}


			}

			builder.SetBackgroundActivation();

			//...

			builder.Show();

			// ToastNotificationManager.CreateToastNotifier();
		}

		private static string GetHeroImage(string path, string file)
		{
			var  bytes     = File.ReadAllBytes(file).Length;
			var  kiloBytes = MathHelper.ConvertToUnit(bytes, MetricPrefix.Kilo);
			bool tooBig    = kiloBytes >= MAX_IMG_SIZE_KB;

			if (tooBig) {
				var    bitmap  = new Bitmap(file);
				var    newSize = new Size(Convert.ToInt32(bitmap.Width / 2), Convert.ToInt32(bitmap.Height / 2));
				Bitmap bitmap2 = ImageHelper.ResizeImage(bitmap, newSize);

				if (bitmap2 != null) {
					string s = Path.Combine(path, Path.GetTempFileName());
					bitmap2.Save(s, System.Drawing.Imaging.ImageFormat.Jpeg);
					bytes     = File.ReadAllBytes(file).Length;
					kiloBytes = MathHelper.ConvertToUnit(bytes, MetricPrefix.Kilo);

					Debug.WriteLine($"-> {bytes} {kiloBytes} | {s}");
					file = s;
				}
				
			}

			return file;
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

		/// <summary>
		/// Name in ASCII art
		/// </summary>
		public const string NAME_BANNER =
			"  ____                       _   ___\n" +
			" / ___| _ __ ___   __ _ _ __| |_|_ _|_ __ ___   __ _  __ _  ___\n" +
			@" \___ \| '_ ` _ \ / _` | '__| __|| || '_ ` _ \ / _` |/ _` |/ _ \" + "\n" +
			"  ___) | | | | | | (_| | |  | |_ | || | | | | | (_| | (_| |  __/\n" +
			@" |____/|_| |_| |_|\__,_|_|   \__|___|_| |_| |_|\__,_|\__, |\___|" + "\n" +
			"                                                     |___/\n";

		private const int MAX_IMG_SIZE_KB = 200;
	}
}