using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using HtmlAgilityPack;
using JetBrains.Annotations;
using SimpleCore.Win32.Cli;
using SmartImage.Searching;
using SmartImage.Utilities;

#pragma warning disable IDE0052


namespace SmartImage.Shell
{
	/// <summary>
	/// Contains <see cref="ConsoleInterface"/> and <see cref="ConsoleOption"/> for the main menu
	/// </summary>
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	internal static class ConsoleMainMenu
	{
		private static ConsoleOption[] AllOptions
		{
			get
			{
				var fields = typeof(ConsoleMainMenu).GetFields(
						BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Default)
					.Where(f => f.FieldType == typeof(ConsoleOption))
					.ToArray();


				var options = new ConsoleOption[fields.Length];

				for (int i = 0; i < fields.Length; i++) {
					options[i] = (ConsoleOption) fields[i].GetValue(null);
				}

				return options;
			}
		}

		/// <summary>
		/// Main menu console interface
		/// </summary>
		internal static ConsoleInterface MainInterface => new ConsoleInterface(AllOptions, RuntimeInfo.NAME_BANNER, false);

		/// <summary>
		///     Runs when no arguments are given (and when the executable is double-clicked)
		/// </summary>
		/// <remarks>
		///     More user-friendly menu
		/// </remarks>
		internal static void RunMainMenu() => ConsoleIO.HandleOptions(ConsoleMainMenu.MainInterface);

		private static readonly ConsoleOption RunSelectImage = new ConsoleOption()
		{
			Name = ">>> Select image <<<",
			Color = ConsoleColor.Yellow,
			Function = () =>
			{
				Console.WriteLine("Drag and drop the image here.");
				Console.Write("Image: ");

				string img = Console.ReadLine();
				img = Common.CleanString(img);

				SearchConfig.Config.Image = img;

				return true;
			}
		};


		private static readonly ConsoleOption ConfigSearchEnginesOption = new ConsoleOption()
		{
			Name = "Configure search engines",
			Function = () =>
			{
				var rgEnum = ConsoleOption.CreateOptionsFromEnum<SearchEngines>();
				var values = ConsoleIO.HandleOptions(rgEnum, true);

				var newValues = Common.ReadEnumFromSet<SearchEngines>(values);

				CliOutput.WriteInfo(newValues);

				SearchConfig.Config.SearchEngines = newValues;

				ConsoleIO.WaitForInput();

				return null;
			},
		};


		private static readonly ConsoleOption ConfigPriorityEnginesOption = new ConsoleOption()
		{
			Name = "Configure priority engines",
			Function = () =>
			{
				var rgEnum = ConsoleOption.CreateOptionsFromEnum<SearchEngines>();
				var values = ConsoleIO.HandleOptions(rgEnum, true);

				var newValues = Common.ReadEnumFromSet<SearchEngines>(values);

				CliOutput.WriteInfo(newValues);

				SearchConfig.Config.PriorityEngines = newValues;

				ConsoleIO.WaitForSecond();

				return null;
			}
		};


		private static readonly ConsoleOption ConfigSauceNaoAuthOption = new ConsoleOption()
		{
			Name = "Configure SauceNao API authentication",
			Function = () =>
			{
				SearchConfig.Config.SauceNaoAuth = ConsoleIO.GetInput("API key");

				ConsoleIO.WaitForSecond();
				return null;
			}
		};

		private static readonly ConsoleOption ConfigImgurAuthOption = new ConsoleOption()
		{
			Name = "Configure Imgur API authentication",
			Function = () =>
			{

				SearchConfig.Config.ImgurAuth = ConsoleIO.GetInput("API key");

				ConsoleIO.WaitForSecond();
				return null;
			}
		};

		private static readonly ConsoleOption ConfigUpdateOption = new ConsoleOption()
		{
			Name = "Update configuration file",
			Function = () =>
			{
				SearchConfig.Config.WriteToFile();

				ConsoleIO.WaitForSecond();
				return null;
			}
		};

		private static readonly ConsoleOption ContextMenuOption = new ConsoleOption()
		{
			Name = "Add/remove context menu integration",
			Function = () =>
			{
				bool ctx = Integration.IsContextMenuAdded;

				if (!ctx) {
					Integration.HandleContextMenu(IntegrationOption.Add);
					CliOutput.WriteSuccess("Added to context menu");
				}
				else {
					Integration.HandleContextMenu(IntegrationOption.Remove);
					CliOutput.WriteSuccess("Removed from context menu");
				}

				ConsoleIO.WaitForSecond();
				return null;
			}
		};

		private static readonly ConsoleOption ShowInfoOption = new ConsoleOption()
		{
			Name = "Show info",
			Function = () =>
			{
				RuntimeInfo.ShowInfo();

				ConsoleIO.WaitForInput();
				return null;
			}
		};

		private static readonly ConsoleOption CheckForUpdateOption = new ConsoleOption()
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
					CliOutput.WriteSuccess("{0}", v.Status);
				}

				ConsoleIO.WaitForSecond();
				return null;
			}
		};

		private static readonly ConsoleOption ResetOption = new ConsoleOption()
		{
			Name = "Reset all configuration and integrations",
			Function = () =>
			{
				Integration.ResetIntegrations();

				ConsoleIO.WaitForSecond();
				return null;
			}
		};


		private static readonly ConsoleOption LegacyCleanupOption = new ConsoleOption()
		{
			Name = "Legacy cleanup",
			Function = () =>
			{
				Integration.RemoveOldRegistry();

				ConsoleIO.WaitForInput();

				return null;
			}
		};

		private static readonly ConsoleOption UninstallOption = new ConsoleOption()
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

		private static readonly ConsoleOption DebugTestOption = new ConsoleOption()
		{
			Name = "[DEBUG] Run test",
			Function = () =>
			{
				var cd = new DirectoryInfo(Environment.CurrentDirectory);
				var cd2 = cd.Parent.Parent.Parent.Parent.ToString();


				var testImg = Common.GetRandomElement(TestImages);
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