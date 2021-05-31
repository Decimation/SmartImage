using System;
using System.Drawing;
using SimpleCore.Cli;
using SimpleCore.Utilities;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Searching;

namespace SmartImage.Core
{
	public static class MainDialog
	{
		private static readonly NConsoleOption[] MainMenuOptions =
		{
			new()
			{
				Name = "run",
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
				Name = "engines",
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
				Name = "priority engines",
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
				Name = "ctx",
				Function = () =>
				{

					var added = Integration.IsContextMenuAdded;

					Integration.HandleContextMenu(added ? IntegrationOption.Remove : IntegrationOption.Add);

					added = Integration.IsContextMenuAdded;
					Console.WriteLine("Added: {0}", added);
					NConsole.WaitForSecond();
					//todo
					MainMenuOptions[^1].Name = $"ctx {(added ? '*' : '-')}";

					return null;
				}
			},
		};

		static void upd()
		{
			MainMenuOptions[3].Name = "g";

		}

		public static readonly NConsoleDialog MainMenuDialog = new()
		{
			Options = MainMenuOptions,
			Header  = Info.NAME_BANNER
		};

		private static TEnum ReadEnum<TEnum>() where TEnum : Enum
		{
			var e   = NConsoleOption.FromEnum<TEnum>();
			var ex  = NConsole.ReadOptions(new NConsoleDialog() {Options = e, SelectMultiple = true});
			var ex2 = Enums.ReadFromSet<TEnum>(ex);

			return ex2;
		}
	}
}