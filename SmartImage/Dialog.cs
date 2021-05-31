using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleCore.Cli;
using SimpleCore.Utilities;
using SmartImage.Core;
using SmartImage.Lib.Engines;

namespace SmartImage
{
	public static class Dialog
	{
		private static readonly NConsoleOption[] MainMenuOptions =
		{
			new()
			{
				Name = "run",
				Function = () =>
				{
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

					return null;
				}
			},

		};

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