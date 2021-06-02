using SimpleCore.Cli;
using SimpleCore.Utilities;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Searching;
using System;

namespace SmartImage.Core
{
	public static class MainDialog
	{
		private static readonly NConsoleOption[] MainMenuOptions =
		{
			new()
			{
				Name = ">>> Run <<<",
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
				Name = GetContextMenuName(Integration.IsContextMenuAdded),
				Function = () =>
				{
					var added = Integration.IsContextMenuAdded;

					Integration.HandleContextMenu(added ? IntegrationOption.Remove : IntegrationOption.Add);

					added = Integration.IsContextMenuAdded;
					Console.WriteLine("Added: {0}", added);
					NConsole.WaitForSecond();

					//hack: hacky 
					MainMenuOptions[^1].Name = GetContextMenuName(added);

					return null;
				}
			},
		};

		private static string GetContextMenuName(bool added) => $"Context menu {(added ? '*' : '-')}";

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