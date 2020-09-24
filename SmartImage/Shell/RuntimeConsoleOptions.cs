using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;
using SimpleCore.Win32.Cli;
using SmartImage.Searching;
using SmartImage.Utilities;

namespace SmartImage.Shell
{
	/// <summary>
	/// Contains <see cref="ConsoleOption"/> for the main menu
	/// </summary>
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	internal static class RuntimeConsoleOptions
	{
		internal static ConsoleOption[] AllOptions
		{
			get
			{
				var fields = typeof(RuntimeConsoleOptions).GetFields(
					BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Default);


				var options = new ConsoleOption[fields.Length];

				for (int i = 0; i < fields.Length; i++) {
					options[i] = (ConsoleOption) fields[i].GetValue(null);
				}

				return options;
			}
		}

		internal static readonly ConsoleOption RunSelectImage = new ConsoleOption(">>> Select image <<<",
			ConsoleColor.Yellow,
			() =>
			{
				Console.WriteLine("Drag and drop the image here.");
				Console.Write("Image: ");

				string img = Console.ReadLine();
				img = CommonUtilities.CleanString(img);

				SearchConfig.Config.Image = img;

				return true;
			});


		internal static readonly ConsoleOption ConfigSearchEnginesOption = new ConsoleOption("Configure search engines",
			() =>
			{
				var rgEnum = ConsoleOption.CreateOptionsFromEnum<SearchEngines>();
				var values = Commands.HandleConsoleOptions(rgEnum, true);

				var newValues = CommonUtilities.ReadEnumFromSet<SearchEngines>(values);

				CliOutput.WriteInfo(newValues);

				SearchConfig.Config.SearchEngines = newValues;

				Commands.WaitForInput();

				return null;
			});

		internal static readonly ConsoleOption ConfigPriorityEnginesOption = new ConsoleOption(
			"Configure priority engines", () =>
			{
				var rgEnum = ConsoleOption.CreateOptionsFromEnum<SearchEngines>();
				var values = Commands.HandleConsoleOptions(rgEnum, true);

				var newValues = CommonUtilities.ReadEnumFromSet<SearchEngines>(values);

				CliOutput.WriteInfo(newValues);

				SearchConfig.Config.PriorityEngines = newValues;

				Commands.WaitForSecond();

				return null;
			});


		internal static readonly ConsoleOption ConfigSauceNaoAuthOption = new ConsoleOption(
			"Configure SauceNao API key", () =>
			{
				Console.Write("API key: ");

				string sauceNaoAuth = Console.ReadLine();


				SearchConfig.Config.SauceNaoAuth = sauceNaoAuth;

				Commands.WaitForSecond();
				return null;
			});

		internal static readonly ConsoleOption ConfigImgurAuthOption = new ConsoleOption("Configure Imgur API key",
			() =>
			{
				Console.Write("API key: ");

				string imgurAuth = Console.ReadLine();

				SearchConfig.Config.ImgurAuth = imgurAuth;

				Commands.WaitForSecond();
				return null;
			});

		internal static readonly ConsoleOption ConfigUpdateOption = new ConsoleOption("Update config file", () =>
		{
			SearchConfig.Config.WriteToFile();

			Commands.WaitForSecond();
			return null;
		});

		internal static readonly ConsoleOption ContextMenuOption = new ConsoleOption(
			"Add/remove context menu integration", () =>
			{
				bool ctx = RuntimeInfo.IsContextMenuAdded;

				if (!ctx)
				{
					Integration.HandleContextMenu(IntegrationOption.Add);
					CliOutput.WriteSuccess("Added to context menu");
				}
				else
				{
					Integration.HandleContextMenu(IntegrationOption.Remove);
					CliOutput.WriteSuccess("Removed from context menu");
				}

				Commands.WaitForSecond();
				return null;
			});

		internal static readonly ConsoleOption ShowInfoOption = new ConsoleOption("Show info", () =>
		{
			RuntimeInfo.ShowInfo();

			Commands.WaitForInput();
			return null;
		});

		internal static readonly ConsoleOption CheckForUpdateOption = new ConsoleOption("Check for updates", () =>
		{
			// TODO: WIP

			var v = UpdateInfo.CheckForUpdates();

			if ((v.Status == VersionStatus.Available))
			{
				NetworkUtilities.OpenUrl(v.Latest.AssetUrl);
			}

			Commands.WaitForSecond();
			return null;
		});

		internal static readonly ConsoleOption ResetOption = new ConsoleOption("Reset all configuration", () =>
		{
			Integration.ResetIntegrations();

			Commands.WaitForSecond();
			return null;
		});

		
		internal static readonly ConsoleOption LegacyCleanupOption = new ConsoleOption("Legacy cleanup", () =>
		{
			Integration.RemoveOldRegistry();

			Commands.WaitForInput();

			return null;
		});

		internal static readonly ConsoleOption UninstallOption = new ConsoleOption("Uninstall", () =>
		{
			Integration.ResetIntegrations();
			Integration.HandlePath(IntegrationOption.Remove);

			File.Delete(SearchConfig.ConfigLocation);
			
			Integration.Uninstall();

			// No return

			Environment.Exit(0);

			return null;
		});

		


#if DEBUG


		internal static readonly ConsoleOption DebugTestOption = new ConsoleOption("Debug: Run test", () =>
		{
			var cd = new DirectoryInfo(Environment.CurrentDirectory);
			var cd2 = cd.Parent.Parent.Parent.Parent.ToString();

			var testImages = new[]
			{
				// "Test1.jpg", 
				"Test2.jpg"
			};


			var testImg = CommonUtilities.GetRandomElement(testImages);
			var img = Path.Combine(cd2, testImg);

			SearchConfig.Config.Image = img;
			SearchConfig.Config.PriorityEngines = SearchEngines.None;
			SearchConfig.Config.SearchEngines &= ~SearchEngines.TraceMoe;
			return true;
		});
#endif
	}
}