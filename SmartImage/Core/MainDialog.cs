using SimpleCore.Cli;
using SimpleCore.Utilities;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Searching;
using System;
using System.Drawing;
using System.Linq;
using Novus.Utilities;

namespace SmartImage.Core
{
	public static class MainDialog
	{
		#region Colors

		private static readonly Color ColorMain  = Color.Yellow;
		private static readonly Color ColorOther = Color.Aquamarine;
		private static readonly Color ColorYes   = Color.GreenYellow;
		private static readonly Color ColorNo    = Color.Red;

		#endregion

		#region Elements

		private static readonly string Yes = "Y".AddColor(ColorYes);

		private static readonly string No = "N".AddColor(ColorNo);
		private static          string GetDirectName(bool added) => $"Direct URI ({(added ? Yes : No)})";

		private static string GetFilterName(bool added) => $"Filter ({(added ? Yes : No)})";

		private static string GetContextMenuName(bool added) => $"Context menu ({(added ? Yes : No)})";

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

					Program.Config.Query = query;
					return true;
				}
			},

			new()
			{
				Name = "Engines".AddColor(ColorOther),
				Function = () =>
				{
					Program.Config.SearchEngines = ReadEnum<SearchEngineOptions>();

					Console.WriteLine(Program.Config.SearchEngines);
					NConsole.WaitForSecond();
					return null;
				}
			},

			new()
			{
				Name = "Priority engines".AddColor(ColorOther),
				Function = () =>
				{
					Program.Config.PriorityEngines = ReadEnum<SearchEngineOptions>();

					Console.WriteLine(Program.Config.PriorityEngines);
					NConsole.WaitForSecond();
					return null;
				}
			},
			new()
			{
				Name = GetFilterName(Program.Config.Filter),
				Function = () =>
				{
					Program.Config.Filter = !Program.Config.Filter;

					//hack: hacky 
					MainMenuOptions[3].Name = GetFilterName(Program.Config.Filter);
					
					return null;
				}
			},
			new()
			{
				Name = GetDirectName(Program.Config.DirectUri),
				Function = () =>
				{
					Program.Config.DirectUri = !Program.Config.DirectUri;

					//hack: hacky 
					MainMenuOptions[4].Name = GetDirectName(Program.Config.DirectUri);

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
					MainMenuOptions[5].Name = GetContextMenuName(added);

					return null;
				}
			},
			new()
			{
				Name = "Config",
				Function = () =>
				{
					Console.Clear();

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
					Console.Clear();

					Console.WriteLine($"Version: {Info.Version}");
					Console.WriteLine($"Executable location: {Info.ExeLocation}");

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

		private static TEnum ReadEnum<TEnum>() where TEnum : Enum
		{
			var enumOptions = NConsoleOption.FromEnum<TEnum>();

			var selected = NConsole.ReadOptions(new NConsoleDialog
			{
				Options = enumOptions, SelectMultiple = true
			});

			var enumValue = Enums.ReadFromSet<TEnum>(selected);

			return enumValue;
		}
	}
}