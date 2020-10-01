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

#pragma warning disable IDE0052


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
		internal static NConsoleUI Interface => new NConsoleUI(AllOptions, RuntimeInfo.NAME_BANNER, false);

		/// <summary>
		///     Runs when no arguments are given (and when the executable is double-clicked)
		/// </summary>
		/// <remarks>
		///     More user-friendly menu
		/// </remarks>
		internal static void Run() => NConsole.IO.HandleOptions(ConsoleMainMenu.Interface);

		private static readonly NConsoleOption RunSelectImage = new NConsoleOption()
		{
			Name = ">>> Select image <<<",
			Color = Color.Yellow,
			Function = () =>
			{
				Console.WriteLine("Drag and drop the image here.");
				Console.Write("Image: ");

				string img = Console.ReadLine();
				img = Strings.CleanString(img);

				SearchConfig.Config.Image = img;

				return true;
			}
		};


		private static readonly NConsoleOption ConfigSearchEnginesOption = new NConsoleOption()
		{
			Name = "Configure search engines",
			Function = () =>
			{
				var rgEnum = NConsoleOption.CreateOptionsFromEnum<SearchEngines>();
				var values = NConsole.IO.HandleOptions(rgEnum, true);

				var newValues = Enums.ReadEnumFromSet<SearchEngines>(values);

				NConsole.WriteInfo(newValues);

				SearchConfig.Config.SearchEngines = newValues;

				NConsole.IO.WaitForInput();

				return null;
			},
		};


		private static readonly NConsoleOption ConfigPriorityEnginesOption = new NConsoleOption()
		{
			Name = "Configure priority engines",
			Function = () =>
			{
				var rgEnum = NConsoleOption.CreateOptionsFromEnum<SearchEngines>();
				var values = NConsole.IO.HandleOptions(rgEnum, true);

				var newValues = Enums.ReadEnumFromSet<SearchEngines>(values);

				NConsole.WriteInfo(newValues);

				SearchConfig.Config.PriorityEngines = newValues;

				NConsole.IO.WaitForSecond();

				return null;
			}
		};


		private static readonly NConsoleOption ConfigSauceNaoAuthOption = new NConsoleOption()
		{
			Name = "Configure SauceNao API authentication",
			Function = () =>
			{
				SearchConfig.Config.SauceNaoAuth = NConsole.IO.GetInput("API key");

				NConsole.IO.WaitForSecond();
				return null;
			}
		};

		private static readonly NConsoleOption ConfigImgurAuthOption = new NConsoleOption()
		{
			Name = "Configure Imgur API authentication",
			Function = () =>
			{

				SearchConfig.Config.ImgurAuth = NConsole.IO.GetInput("API key");

				NConsole.IO.WaitForSecond();
				return null;
			}
		};

		private static readonly NConsoleOption ConfigUpdateOption = new NConsoleOption()
		{
			Name = "Update configuration file",
			Function = () =>
			{
				SearchConfig.Config.WriteToFile();

				NConsole.IO.WaitForSecond();
				return null;
			}
		};

		private static readonly NConsoleOption ContextMenuOption = new NConsoleOption()
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

				NConsole.IO.WaitForSecond();
				return null;
			}
		};

		private static readonly NConsoleOption ShowInfoOption = new NConsoleOption()
		{
			Name = "Show info",
			Function = () =>
			{
				RuntimeInfo.ShowInfo();

				NConsole.IO.WaitForInput();
				return null;
			}
		};

		private static readonly NConsoleOption CheckForUpdateOption = new NConsoleOption()
		{
			Name = "Check for updates",
			Function = () =>
			{
				// TODO: WIP

				var v = UpdateInfo.CheckForUpdates();

				if ((v.Status == VersionStatus.Available)) {

					UpdateInfo.Update();

					// No return
					Environment.Exit(0);

				}
				else {
					NConsole.WriteSuccess("{0}", v.Status);
				}

				NConsole.IO.WaitForSecond();
				return null;
			}
		};

		private static readonly NConsoleOption ResetOption = new NConsoleOption()
		{
			Name = "Reset all configuration and integrations",
			Function = () =>
			{
				Integration.ResetIntegrations();

				NConsole.IO.WaitForSecond();
				return null;
			}
		};

		private static readonly NConsoleOption UninstallOption = new NConsoleOption()
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
			//"Test2.jpg",
			"Test3.png"
		};

		private static readonly NConsoleOption DebugTestOption = new NConsoleOption()
		{
			Name = "[DEBUG] Run test",
			Function = () =>
			{
				var cd = new DirectoryInfo(Environment.CurrentDirectory);
				var cd2 = cd.Parent.Parent.Parent.Parent.ToString();


				var testImg = Collections.GetRandomElement(TestImages);
				var img = Path.Combine(cd2, testImg);

				SearchConfig.Config.Image = img;
				SearchConfig.Config.PriorityEngines = SearchEngines.None;
				//SearchConfig.Config.ImgurAuth = "6c97880bf8754c5";
				//SearchConfig.Config.SearchEngines &= ~SearchEngines.TraceMoe;


				return true;
			}
		};
#endif
		
	}
}