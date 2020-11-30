using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using SimpleCore.Console.CommandLine;
using SimpleCore.Utilities;
using SmartImage.Engines;
using SmartImage.Searching;
using SmartImage.Utilities;

// ReSharper disable ArrangeAccessorOwnerBody

#pragma warning disable IDE0052, HAA0502, HAA0505, HAA0601, HAA0502, HAA0101, RCS1213, RCS1036
#nullable enable

namespace SmartImage.Core
{
	/// <summary>
	/// Contains <see cref="NConsoleUI"/> and <see cref="NConsoleOption"/> for the main menu
	/// </summary>
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	internal static class MainMenu
	{
		private static NConsoleOption[] AllOptions
		{
			get
			{
				var fields = typeof(MainMenu).GetFields(
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
		/// Main menu console interface
		/// </summary>
		internal static NConsoleUI Interface
		{
			get
			{
				//
				return new(AllOptions, Info.NAME_BANNER, null, false, null);
			}
		}

		/// <summary>
		///     Runs when no arguments are given (and when the executable is double-clicked)
		/// </summary>
		/// <remarks>
		///     More user-friendly menu
		/// </remarks>
		internal static void Run()
		{
			//
			NConsoleIO.HandleOptions(Interface);
		}

		private static readonly NConsoleOption RunSelectImage = new()
		{
			Name  = ">>> Select image <<<",
			Color = Color.Yellow,
			Function = () =>
			{
				Console.WriteLine("Drag and drop the image here.");

				string? img = NConsoleIO.ReadInput("Image");

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


		private static readonly NConsoleOption ConfigSearchEnginesOption = new()
		{
			Name = "Configure search engines",
			Color = Color.CadetBlue,
			Function = () =>
			{
				var rgEnum = NConsoleOption.FromEnum<SearchEngineOptions>();
				var values = NConsoleIO.HandleOptions(rgEnum, true);

				var newValues = Enums.ReadFromSet<SearchEngineOptions>(values);

				NConsole.WriteInfo(newValues);

				SearchConfig.Config.SearchEngines = newValues;

				NConsoleIO.WaitForInput();

				return null;
			},
		};


		private static readonly NConsoleOption ConfigPriorityEnginesOption = new()
		{
			Name  = "Configure priority engines",
			Color = Color.CadetBlue,
			Function = () =>
			{
				var rgEnum = NConsoleOption.FromEnum<SearchEngineOptions>();
				var values = NConsoleIO.HandleOptions(rgEnum, true);

				var newValues = Enums.ReadFromSet<SearchEngineOptions>(values);

				NConsole.WriteInfo(newValues);

				SearchConfig.Config.PriorityEngines = newValues;

				NConsoleIO.WaitForSecond();

				return null;
			}
		};


		private static readonly NConsoleOption ConfigSauceNaoAuthOption = new()
		{
			Name  = "Configure SauceNao API authentication",
			Color = Color.CadetBlue,
			Function = () =>
			{
				SearchConfig.Config.SauceNaoAuth = NConsoleIO.ReadInput("API key");

				NConsoleIO.WaitForSecond();
				return null;
			}
		};

		private static readonly NConsoleOption ConfigImgurAuthOption = new()
		{
			Name  = "Configure Imgur API authentication",
			Color = Color.CadetBlue,
			Function = () =>
			{

				SearchConfig.Config.ImgurAuth = NConsoleIO.ReadInput("API key");

				NConsoleIO.WaitForSecond();
				return null;
			}
		};

		private static readonly NConsoleOption ConfigUpdateOption = new()
		{
			Name  = "Update configuration file",
			Color = Color.CadetBlue,
			Function = () =>
			{
				SearchConfig.Config.WriteToFile();

				NConsoleIO.WaitForSecond();
				return null;
			}
		};

		private static readonly NConsoleOption ShowInfoOption = new()
		{
			Name  = "Show info",
			Color = Color.MediumPurple,
			Function = () =>
			{
				Info.ShowInfo();

				NConsoleIO.WaitForInput();
				return null;
			}
		};

		private static readonly NConsoleOption ContextMenuOption = new()
		{
			Name  = "Add/remove context menu integration",
			Color = Color.DarkOrange,
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

		


		private static readonly NConsoleOption CheckForUpdateOption = new()
		{
			Name  = "Check for updates",
			Color = Color.DarkOrange,
			Function = () =>
			{
				var v = UpdateInfo.CheckForUpdates();

				if ((v.Status == VersionStatus.Available)) {
					Console.WriteLine($"Updating to {v.Latest}...");

					try {
						UpdateInfo.Update();
					}
					catch (Exception e) {
						Console.WriteLine(e);

					}

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

		private static readonly NConsoleOption ResetOption = new()
		{
			Name  = "Reset all configuration and integrations",
			Color = Color.DarkOrange,
			Function = () =>
			{
				Integration.ResetIntegrations();

				NConsoleIO.WaitForSecond();
				return null;
			}
		};

		private static readonly NConsoleOption UninstallOption = new()
		{
			Name  = "Uninstall",
			Color = Color.DarkOrange,
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
				var cd  = new DirectoryInfo(Environment.CurrentDirectory);
				var cd2 = cd.Parent.Parent.Parent.Parent.ToString();

				var rgOption = NConsoleOption.FromArray(TestImages, s => s);

				var testImg =(string) NConsoleIO.HandleOptions(rgOption).First();

				var img     = Path.Combine(cd2, testImg);

				SearchConfig.Config.Image           = img;
				SearchConfig.Config.PriorityEngines = SearchEngineOptions.None;

				return true;
			}
		};
#endif
	}
}