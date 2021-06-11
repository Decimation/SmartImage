using SimpleCore.Cli;
using SimpleCore.Utilities;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Searching;
using System;
using System.Drawing;
using System.Linq;
using Novus.Utilities;
using SmartImage.Lib;

namespace SmartImage.Core
{
	/// <summary>
	/// Handles the main menu interface
	/// </summary>
	public static class MainDialog
	{
		#region Colors

		private static readonly Color ColorMain  = Color.Yellow;
		private static readonly Color ColorOther = Color.Aquamarine;
		private static readonly Color ColorYes   = Color.GreenYellow;
		private static readonly Color ColorNo    = Color.Red;

		#endregion

		#region Elements

		private static readonly string Enabled = StringConstants.CHECK_MARK.ToString().AddColor(ColorYes);

		private static readonly string Disabled = StringConstants.MUL_SIGN.ToString().AddColor(ColorNo);


		private static string GetFilterName(bool added) => $"Filter ({(added ? Enabled : Disabled)})";

		private static string GetContextMenuName(bool added) => $"Context menu ({(added ? Enabled : Disabled)})";

		#endregion


		private static readonly NConsoleOption[] MainMenuOptions =
		{
			new()
			{
				Name = ">>> Run <<<".AddColor(ColorMain),
				Function = () =>
				{
					ImageQuery query = NConsole.ReadInput("Image file or direct URL", x =>
					{
						(bool url, bool file) = ImageQuery.IsUriOrFile(x);
						return !(url || file);
					});

					SearchCli.Config.Query = query;
					return true;
				}
			},

			new()
			{
				Name = "Engines".AddColor(ColorOther),
				Function = () =>
				{
					SearchCli.Config.SearchEngines = ReadEnum<SearchEngineOptions>();

					Console.WriteLine(SearchCli.Config.SearchEngines);
					NConsole.WaitForSecond();
					SaveUpdateConfig();
					return null;
				}
			},

			new()
			{
				Name = "Priority engines".AddColor(ColorOther),
				Function = () =>
				{
					SearchCli.Config.PriorityEngines = ReadEnum<SearchEngineOptions>();

					Console.WriteLine(SearchCli.Config.PriorityEngines);
					NConsole.WaitForSecond();
					SaveUpdateConfig();
					return null;
				}
			},
			new()
			{
				Name = GetFilterName(SearchCli.Config.Filter),
				Function = () =>
				{
					SearchCli.Config.Filter = !SearchCli.Config.Filter;

					//hack: hacky 
					MainMenuOptions[3].Name = GetFilterName(SearchCli.Config.Filter);
					SaveUpdateConfig();
					return null;
				}
			},
			new()
			{
				Name = GetContextMenuName(OSIntegration.IsContextMenuAdded),
				Function = () =>
				{
					var added = OSIntegration.IsContextMenuAdded;

					OSIntegration.HandleContextMenu(added ? IntegrationOption.Remove : IntegrationOption.Add);

					added = OSIntegration.IsContextMenuAdded;


					//hack: hacky 
					MainMenuOptions[4].Name = GetContextMenuName(added);

					return null;
				}
			},
			new()
			{
				Name = "Config",
				Function = () =>
				{
					Console.Clear();

					Console.WriteLine(SearchCli.Config);

					NConsole.WaitForInput();
					return null;
				}
			},
			new()
			{
				Name = "Info",
				Function = () =>
				{
					Console.Clear();

					Console.WriteLine($"Author: {Info.Author}");
					Console.WriteLine($"Version: {Info.Version}");

					Console.WriteLine($"Executable location: {Info.ExeLocation}");
					Console.WriteLine($"In path: {Info.IsAppFolderInPath}");
					Console.WriteLine($"Context menu added: {OSIntegration.IsContextMenuAdded}");

					Console.WriteLine(Strings.Separator);

					var dependencies = ReflectionHelper.DumpDependencies();

					foreach (var name in dependencies) {
						Console.WriteLine($"{name.Name} ({name.Version})");
					}

					NConsole.WaitForInput();
					return null;
				}
			},
#if DEBUG


			new()
			{
				Name = "Debug",
				Function = () =>
				{

					SearchCli.Config.Query = @"C:\Users\Deci\Pictures\Test Images\Test1.jpg";
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



		private static void SaveUpdateConfig()
		{
			SearchCli.Client.Update();
			SearchCli.SaveConfigFile();
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