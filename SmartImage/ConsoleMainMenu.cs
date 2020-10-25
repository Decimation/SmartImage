using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using SimpleCore.CommandLine;
using SimpleCore.Utilities;
using SmartImage.Searching;
using SmartImage.Utilities;

#pragma warning disable IDE0052, HAA0502, HAA0505, HAA0601, HAA0502, HAA0101
#nullable enable

namespace SmartImage
{
	/// <summary>
	/// Contains <see cref="NConsoleUI"/> and <see cref="NConsoleOption"/> for the main menu
	/// </summary>
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	internal static class ConsoleMainMenu
	{
		private static NConsoleOption[] AllOptions
		{
			get
			{
				var fields = typeof(ConsoleMainMenu).GetFields(
						BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Default)
					.Where(f => f.FieldType == typeof(NConsoleOption))
					.ToArray();


				var options = new NConsoleOption[fields.Length];

				for (int i = 0; i < fields.Length; i++) {
					options[i] = (NConsoleOption) fields[i].GetValue(null);
				}

				return options;
			}
		}

		/// <summary>
		/// Main menu console interface
		/// </summary>
		internal static NConsoleUI Interface => new NConsoleUI(AllOptions, RuntimeInfo.NAME_BANNER, null, false, null);

		/// <summary>
		///     Runs when no arguments are given (and when the executable is double-clicked)
		/// </summary>
		/// <remarks>
		///     More user-friendly menu
		/// </remarks>
		internal static void Run() => NConsoleIO.HandleOptions(Interface);

		private static readonly NConsoleOption RunSelectImage = new NConsoleOption
		{
			Name = ">>> Select image <<<",
			Color = Color.Yellow,
			Function = () =>
			{
				Console.WriteLine("Drag and drop the image here.");
				
				string? img = NConsoleIO.GetInput("Image");

				if (string.IsNullOrWhiteSpace(img)) {
					NConsole.WriteError("Invalid image");
					NConsoleIO.WaitForInput();
					return null;
				}

				img = Strings.CleanString(img);

				SearchConfig.Config.Image = img;

				return true;
			}
		};


		private static readonly NConsoleOption ConfigSearchEnginesOption = new NConsoleOption
		{
			Name = "Configure search engines",
			Function = () =>
			{
				var rgEnum = NConsoleOption.CreateOptionsFromEnum<SearchEngineOptions>();
				var values = NConsoleIO.HandleOptions(rgEnum, true);

				var newValues = Enums.ReadEnumFromSet<SearchEngineOptions>(values);

				NConsole.WriteInfo(newValues);

				SearchConfig.Config.SearchEngines = newValues;

				NConsoleIO.WaitForInput();

				return null;
			},
		};


		private static readonly NConsoleOption ConfigPriorityEnginesOption = new NConsoleOption
		{
			Name = "Configure priority engines",
			Function = () =>
			{
				var rgEnum = NConsoleOption.CreateOptionsFromEnum<SearchEngineOptions>();
				var values = NConsoleIO.HandleOptions(rgEnum, true);

				var newValues = Enums.ReadEnumFromSet<SearchEngineOptions>(values);

				NConsole.WriteInfo(newValues);

				SearchConfig.Config.PriorityEngines = newValues;

				NConsoleIO.WaitForSecond();

				return null;
			}
		};


		private static readonly NConsoleOption ConfigSauceNaoAuthOption = new NConsoleOption
		{
			Name = "Configure SauceNao API authentication",
			Function = () =>
			{
				SearchConfig.Config.SauceNaoAuth = NConsoleIO.GetInput("API key");

				NConsoleIO.WaitForSecond();
				return null;
			}
		};

		private static readonly NConsoleOption ConfigImgurAuthOption = new NConsoleOption
		{
			Name = "Configure Imgur API authentication",
			Function = () =>
			{

				SearchConfig.Config.ImgurAuth = NConsoleIO.GetInput("API key");

				NConsoleIO.WaitForSecond();
				return null;
			}
		};

		private static readonly NConsoleOption ConfigUpdateOption = new NConsoleOption
		{
			Name = "Update configuration file",
			Function = () =>
			{
				SearchConfig.Config.WriteToFile();

				NConsoleIO.WaitForSecond();
				return null;
			}
		};

		private static readonly NConsoleOption ContextMenuOption = new NConsoleOption
		{
			Name = "Add/remove context menu integration",
			Function = () =>
			{
				bool ctx = Integration.IsContextMenuAdded;

				if (!ctx) {
					Integration.HandleContextMenu(IntegrationOption.Add);
					NConsole.WriteSuccess("Added to context menu");
				}
				else {
					Integration.HandleContextMenu(IntegrationOption.Remove);
					NConsole.WriteSuccess("Removed from context menu");
				}

				NConsoleIO.WaitForSecond();
				return null;
			}
		};

		private static readonly NConsoleOption ShowInfoOption = new NConsoleOption
		{
			Name = "Show info",
			Function = () =>
			{
				RuntimeInfo.ShowInfo();

				NConsoleIO.WaitForInput();
				return null;
			}
		};


		private static readonly NConsoleOption CheckForUpdateOption = new NConsoleOption
		{
			Name = "Check for updates",
			Function = () =>
			{
				var v = UpdateInfo.CheckForUpdates();

				if ((v.Status == VersionStatus.Available)) {

					UpdateInfo.Update();
					
					// No return
					Environment.Exit(0);

				}
				else {
					NConsole.WriteInfo("{0}", v.Status);
				}

				NConsoleIO.WaitForSecond();
				return null;
			}
		};

		private static readonly NConsoleOption ResetOption = new NConsoleOption
		{
			Name = "Reset all configuration and integrations",
			Function = () =>
			{
				Integration.ResetIntegrations();

				NConsoleIO.WaitForSecond();
				return null;
			}
		};

		private static readonly NConsoleOption UninstallOption = new NConsoleOption
		{
			Name = "Uninstall",
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
			// "Test1.jpg", 
			"Test2.jpg",
			//"Test3.png"
		};

		private static readonly NConsoleOption DebugTestOption = new NConsoleOption
		{
			Name = "[DEBUG] Run test",
			Function = () =>
			{
				var cd = new DirectoryInfo(Environment.CurrentDirectory);
				var cd2 = cd.Parent.Parent.Parent.Parent.ToString();


				var testImg = TestImages.GetRandomElement();
				var img = Path.Combine(cd2, testImg);

				SearchConfig.Config.Image = img;
				SearchConfig.Config.PriorityEngines = SearchEngineOptions.None;


				return true;
			}
		};
#endif
		
	}
}