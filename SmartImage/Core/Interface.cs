using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using SimpleCore.Console.CommandLine;
using SimpleCore.Utilities;
using SmartImage.Engines;
using SmartImage.Utilities;
using Novus.Win32;

// ReSharper disable ArrangeAccessorOwnerBody

#pragma warning disable IDE0052, HAA0502, HAA0505, HAA0601, HAA0502, HAA0101, RCS1213, RCS1036, CS8602
#nullable enable

namespace SmartImage.Core
{
	/// <summary>
	///     User interface; contains <see cref="NConsoleInterface" /> and <see cref="NConsoleOption" /> for the main menu
	/// </summary>
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	internal static class Interface
	{
		// TODO: refactor, optimize

		private static NConsoleOption[] AllOptions
		{
			get
			{
				var fields = typeof(Interface).GetFields(
						BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Default)
					.Where(f => f.FieldType == typeof(NConsoleOption))
					.ToArray();


				var options = new NConsoleOption[fields.Length];

				for (int i = 0; i < fields.Length; i++) {
					options[i] = (NConsoleOption) fields[i].GetValue(null)!;
				}

				return options;
			}
		}

		/// <summary>
		///     Main menu console interface
		/// </summary>
		internal static NConsoleInterface MainMenu => new(AllOptions, Info.NAME_BANNER, null, false, null);

		/// <summary>
		///     Runs when no arguments are given (and when the executable is double-clicked)
		/// </summary>
		/// <remarks>
		///     User-friendly menu
		/// </remarks>
		internal static void Run() => NConsole.ReadOptions(MainMenu);


		/// <summary>
		/// Primary color
		/// </summary>
		internal static readonly Color ColorMain = Color.Yellow;

		/// <summary>
		/// Config color
		/// </summary>
		internal static readonly Color ColorConfig = Color.DeepSkyBlue;

		/// <summary>
		/// Utility color
		/// </summary>
		internal static readonly Color ColorUtility = Color.DarkOrange;

		/// <summary>
		/// Misc color
		/// </summary>
		internal static readonly Color ColorMisc = Color.MediumPurple;

		/// <summary>
		/// Version color
		/// </summary>
		internal static readonly Color ColorVersion = Color.LightGreen;


		/// <summary>
		/// Console window width
		/// </summary>
		internal const int ConsoleWindowWidth = 120;


		/// <summary>
		/// Console window height
		/// </summary>
		internal const int ConsoleWindowHeight = 50;

		/// <summary>
		/// Main option
		/// </summary>
		private static readonly NConsoleOption RunSelectImage = new()
		{
			Name  = ">>> Select image <<<",
			Color = ColorMain,
			Function = () =>
			{
				Console.WriteLine("Drag and drop the image here.");

				string? img = NConsole.ReadInput("Image");

				if (String.IsNullOrWhiteSpace(img)) {
					NConsole.WriteError("Invalid image");
					NConsole.WaitForInput();
					return null;
				}

				img = Strings.CleanString(img);

				SearchConfig.Config.Image = img;

				return true;
			}
		};


		private static readonly NConsoleOption ConfigSearchEnginesOption = new()
		{
			Name  = "Configure search engines",
			Color = ColorConfig,
			Function = () =>
			{
				SearchConfig.Config.SearchEngines = ReadSearchEngineOptions();
				SearchConfig.Config.EnsureConfig();
				NConsole.WriteSuccess(SearchConfig.Config.SearchEngines);
				NConsole.WaitForSecond();
				return null;
			}
		};


		private static readonly NConsoleOption ConfigPriorityEnginesOption = new()
		{
			Name  = "Configure priority engines",
			Color = ColorConfig,
			Function = () =>
			{
				SearchConfig.Config.PriorityEngines = ReadSearchEngineOptions();
				SearchConfig.Config.EnsureConfig();
				NConsole.WriteSuccess(SearchConfig.Config.PriorityEngines);
				NConsole.WaitForSecond();
				return null;
			}
		};


		private static SearchEngineOptions ReadSearchEngineOptions()
		{
			var rgEnum = NConsoleOption.FromEnum<SearchEngineOptions>();
			var values = NConsole.ReadOptions(rgEnum, true);

			var newValues = Enums.ReadFromSet<SearchEngineOptions>(values);

			return newValues;
		}


		private static readonly NConsoleOption ConfigSauceNaoAuthOption = new()
		{
			Name  = "Configure SauceNao API authentication",
			Color = ColorConfig,
			Function = () =>
			{
				SearchConfig.Config.SauceNaoAuth = NConsole.ReadInput("API key");

				NConsole.WaitForSecond();
				return null;
			}
		};

		private static readonly NConsoleOption ConfigImgurAuthOption = new()
		{
			Name  = "Configure Imgur API authentication",
			Color = ColorConfig,
			Function = () =>
			{

				SearchConfig.Config.ImgurAuth = NConsole.ReadInput("API key");

				NConsole.WaitForSecond();
				return null;
			}
		};

		private static readonly NConsoleOption ConfigAutoFilter = new()
		{
			Name  = GetAutoFilterString(),
			Color = ColorConfig,
			Function = () =>
			{

				SearchConfig.Config.FilterResults = !SearchConfig.Config.FilterResults;
				ConfigAutoFilter.Name             = GetAutoFilterString();
				return null;
			}
		};

		private static string GetAutoFilterString()
		{
			var x = SearchConfig.Config.FilterResults;
			return $"Filter results: {x}";
		}

		private static readonly NConsoleOption ConfigUpdateOption = new()
		{
			Name  = "Update configuration file",
			Color = ColorConfig,
			Function = () =>
			{
				SearchConfig.Config.SaveFile();

				NConsole.WaitForSecond();
				return null;
			}
		};

		private static readonly NConsoleOption ShowInfoOption = new()
		{
			Name  = "Show info and config",
			Color = ColorMisc,
			Function = () =>
			{
				Info.ShowInfo();

				NConsole.WaitForInput();
				return null;
			}
		};


		private static readonly NConsoleOption ContextMenuOption = new()
		{
			Name  = GetContextMenuString(Integration.IsContextMenuAdded),
			Color = ColorUtility,
			Function = () =>
			{
				bool ctx = Integration.IsContextMenuAdded;

				var io = !ctx ? IntegrationOption.Add : IntegrationOption.Remove;

				Integration.HandleContextMenu(io);
				bool added = io == IntegrationOption.Add;

				NConsole.WriteInfo($"Context menu integrated: {added}");

				ContextMenuOption.Name = GetContextMenuString(added);

				NConsole.WaitForSecond();
				return null;
			}
		};

		private static string GetContextMenuString(bool added) =>
			(!added ? "Add" : "Remove") + " context menu integration";


		private static readonly NConsoleOption CheckForUpdateOption = new()
		{
			Name  = "Check for updates",
			Color = ColorUtility,
			Function = () =>
			{
				UpdateInfo.AutoUpdate();

				NConsole.WaitForSecond();
				return null;
			}
		};

		private static readonly NConsoleOption ResetOption = new()
		{
			Name  = "Reset all configuration and integrations",
			Color = ColorUtility,
			Function = () =>
			{
				Integration.ResetIntegrations();

				NConsole.WaitForSecond();
				return null;
			}
		};


		private static readonly NConsoleOption CleanupLegacy = new()
		{
			Name  = "Legacy cleanup",
			Color = ColorUtility,

			Function = () =>
			{
				bool ok = LegacyIntegration.LegacyCleanup();

				NConsole.WriteInfo($"Legacy cleanup: {ok}");
				NConsole.WaitForInput();

				return null;
			}
		};

		private static readonly NConsoleOption UninstallOption = new()
		{
			Name  = "Uninstall",
			Color = ColorUtility,
			Function = () =>
			{
				Integration.ResetIntegrations();
				Integration.HandlePath(IntegrationOption.Remove);

				File.Delete(SearchConfig.ConfigLocation);

				Integration.Uninstall();

				// No return

				Environment.Exit(0);

				return null;
			}
		};


#if DEBUG
		private static readonly string[] TestImages =
		{
			"Test1.jpg",
			"Test2.jpg",
			"Test3.png"
		};

		private static readonly NConsoleOption DebugTestOption = new()
		{
			Name = "[DEBUG] Run test",
			Function = () =>
			{

				//var cd  = new DirectoryInfo(Environment.CurrentDirectory);
				//var cd2 = cd.Parent.Parent.Parent.Parent.ToString();
				//var cd2 = cd.GetParentLevel(4).ToString();

				var cd2 = FileSystem.GetParentLevel(Environment.CurrentDirectory, 4);

				var rgOption = NConsoleOption.FromArray(TestImages, s => s);

				var testImg = (string) NConsole.ReadOptions(rgOption).First();

				var img = Path.Combine(cd2, testImg);

				SearchConfig.Config.Image = img;
				//SearchConfig.Config.PriorityEngines = SearchEngineOptions.None;

				return true;
			}
		};
#endif
	}
}