using SimpleCore.Cli;
using SimpleCore.Utilities;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Searching;
using System;
using System.Drawing;

namespace SmartImage.Core
{
	public static class MainDialog
	{
		private static readonly NConsoleOption[] MainMenuOptions =
		{
			new()
			{
				Name = ">>> Run <<<".AddColor(Color.Yellow),
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
				Name = "Engines",
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
				Name = "Priority engines",
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
				Name = GetContextMenuName(OSIntegration.IsContextMenuAdded),
				Function = () =>
				{
					var added = OSIntegration.IsContextMenuAdded;

					OSIntegration.HandleContextMenu(added ? IntegrationOption.Remove : IntegrationOption.Add);

					added = OSIntegration.IsContextMenuAdded;
					Console.WriteLine("Added: {0}", added);
					NConsole.WaitForSecond();

					//hack: hacky 
					MainMenuOptions[^1].Name = GetContextMenuName(added);

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



		private static string GetContextMenuName(bool added) => $"Context menu {(added ? '*' : '-')}";

		public static readonly NConsoleDialog MainMenuDialog = new()
		{
			Options = MainMenuOptions,
			Header  = Info.NAME_BANNER
		};

		private static TEnum ReadEnum<TEnum>() where TEnum : Enum
		{
			var enumOptions   = NConsoleOption.FromEnum<TEnum>();
			var selected  = NConsole.ReadOptions(new NConsoleDialog
			{
				Options = enumOptions, SelectMultiple = true
			});

			var enumValue = Enums.ReadFromSet<TEnum>(selected);

			return enumValue;
		}
	}
}