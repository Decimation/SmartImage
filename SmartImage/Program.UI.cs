global using CPI = Kantan.Cli.ConsoleManager.UI.ProgressIndicator;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using JetBrains.Annotations;
using Kantan.Cli;
using Kantan.Cli.Controls;
using Kantan.Net;
using Kantan.Text;
using Kantan.Utilities;
using Novus.Utilities;
using SmartImage.App;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Searching;
using SmartImage.Lib.Utilities;
using SmartImage.Properties;
using SmartImage.Utilities;
using static Novus.Utilities.ReflectionOperatorHelpers;

// ReSharper disable LocalizableElement
// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable UnusedMember.Global
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable PossibleNullReferenceException

namespace SmartImage;

public static partial class Program
{
	/// <summary>
	/// Handles the main menu interface
	/// </summary>
	private static partial class UI
	{
		private static readonly ConsoleOption[] MainMenuOptions =
		{
			new()
			{
				Name  = ">>> Run <<<",
				Color = UI.Elements.ColorMain,
				Function = () =>
				{
					ImageQuery query = ConsoleManager.ReadLine("Image file or direct URL", x =>
					{
						x = x.CleanString();

						var m = ImageMedia.GetMediaInfo(x);
						return !(m.IsValid);
					}, "Input must be file or direct image link");

					Program.Config.Query = query;
					return true;
				}
			},

			CreateEnumConfigOption<SearchEngineOptions>(nameof(Program.Config.SearchEngines), "Engines",
			                                            Program.Config),
			CreateEnumConfigOption<SearchEngineOptions>(nameof(Program.Config.PriorityEngines), "Priority engines",
			                                            Program.Config),
			CreateConfigOption(nameof(Program.Config.Filtering), "Filter", Program.Config),
			CreateConfigOption(nameof(Program.Config.Notification), "Notification", Program.Config),
			CreateConfigOption(nameof(Program.Config.NotificationImage), "Notification image", Program.Config),

			CreateConfigOption(propertyof(() => AppIntegration.IsContextMenuAdded), "Context menu",
			                   added => AppIntegration.HandleContextMenu(!added), Program.Config),

			new()
			{
				Name = "Config",
				Function = () =>
				{
					//Console.Clear();

					Console.WriteLine(Program.Config);
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

					Console.WriteLine($"Current version: {AppInfo.AppVersion} ({UpdateInfo.GetUpdateInfo().Status})");
					Console.WriteLine($"Latest version: {ReleaseInfo.GetLatestRelease()}");

					Console.WriteLine();

					var di = new DirectoryInfo(AppInfo.ExeLocation);

					Console.WriteLine($"Executable location: {di.Parent.Name}");
					Console.WriteLine($"In path: {AppInfo.IsAppFolderInPath}");

					Console.WriteLine();
					Console.WriteLine(Strings.Constants.Separator);

					foreach (var utility in AppIntegration.UtilitiesMap) {
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
					WebUtilities.OpenUrl(Resources.U_Wiki);

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

		static UI()
		{
			// NOTE: Static initializer must be AFTER MainMenuDialog

			var current = UpdateInfo.GetUpdateInfo();

			if (current.Status != VersionStatus.Available) {
				return;
			}

			var option = MainMenuOptions.First(f => f.Name == "Update");

			option.Name = Pastel.AddColor(option.Name, (Color) UI.Elements.ColorHighlight);

			var updateStr =
				$"* Update available (latest: {UI.Elements.GetVersionString(current.Latest.Version)};" +
				$" current: {UI.Elements.GetVersionString(current.Current)})";

			updateStr = Pastel.AddColor(updateStr, (Color) UI.Elements.ColorHighlight);

			MainMenuDialog.Description = updateStr;

			option.Function = () =>
			{
				UpdateInfo.Update(current);
				return null;
			};
		}

		private static ConsoleOption CreateConfigOption(string field, string name, object t)
		{
			bool initVal = (bool) t.GetType().GetAnyResolvedField(field)
			                       .GetValue(t);

			var option = new ConsoleOption
			{
				Name = GetName(name, initVal)
			};

			option.Function = () =>
			{
				var fi = t.GetType().GetAnyResolvedField(field);

				object curVal = fi.GetValue(t);
				bool   newVal = !(bool) curVal;
				fi.SetValue(t, newVal);

				option.Name = GetName(name, newVal);

				Debug.Assert((bool) fi.GetValue(t) == newVal);

				Program.Reload(true);

				return null;
			};


			return option;
		}

		[UsedImplicitly]
		private static string GetName(string s, bool added) => $"{s} ({(UI.Elements.GetToggleString(added))})";

		private static ConsoleOption CreateEnumConfigOption<T>(string f, string name, object o) where T : Enum
		{
			return new()
			{
				Name  = name,
				Color = UI.Elements.ColorOther,
				Function = () =>
				{
					var enumOptions = ConsoleOption.FromEnum<T>();

					var selected = (new ConsoleDialog
						               {
							               Options        = enumOptions,
							               SelectMultiple = true
						               }).ReadInput();

					var enumValue = EnumHelper.ReadFromSet<T>(selected.Output);
					var field     = o.GetType().GetAnyResolvedField(f);
					field.SetValue(o, enumValue);

					Console.WriteLine(enumValue);

					ConsoleManager.WaitForSecond();

					Debug.Assert(((T) field.GetValue(o)).Equals(enumValue));

					Program.Reload(true);

					return null;
				}
			};
		}

		private static ConsoleOption CreateConfigOption(PropertyInfo member, string name, Action<bool> fn, object o)
		{
			bool initVal = (bool) member.GetValue(o);

			var option = new ConsoleOption
			{
				Name = GetName(name, initVal)
			};

			option.Function = () =>
			{
				var pi = member.DeclaringType.GetProperty(member.Name);

				bool curVal = (bool) pi.GetValue(null);
				fn(curVal);
				bool newVal = (bool) pi.GetValue(null);
				option.Name = GetName(name, newVal);

				Debug.Assert((bool) pi.GetValue(null) == newVal);

				return null;
			};

			return option;
		}
	}
}