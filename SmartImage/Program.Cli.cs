using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kantan.Utilities;
using SmartImage.Lib.Engines;
using SmartImage.UI;

namespace SmartImage
{
	public static partial class Program
	{

		static class Cli
		{
			/// <summary>
			/// Command line argument handler
			/// </summary>
			private static readonly CliHandler ArgumentHandler = new()
			{
				Parameters =
				{
					new()
					{
						ArgumentCount = 1,
						ParameterId   = "-se",
						Function = strings =>
						{
							Config.SearchEngines = Enum.Parse<SearchEngineOptions>(strings[0]);
							return null;
						}
					},
					new()
					{
						ArgumentCount = 1,
						ParameterId   = "-pe",
						Function = strings =>
						{
							Config.PriorityEngines = Enum.Parse<SearchEngineOptions>(strings[0]);
							return null;
						}
					},
					new()
					{
						ArgumentCount = 0,
						ParameterId   = "-f",
						Function = delegate
						{
							Config.Filtering = true;
							return null;
						}
					},
					new()
					{
						ArgumentCount = 0,
						ParameterId   = "-output_only",
						Function = delegate
						{
							Config.OutputOnly = true;
							return null;
						}
					}
				},
				Default = new()
				{
					ArgumentCount = 1,
					ParameterId   = null,
					Function = strings =>
					{
						Config.Query = strings[0];
						return null;
					}
				}
			};

			public static async Task<bool> HandleArguments()
			{
				var args = Environment.GetCommandLineArgs();

				// first element is executing assembly
				args = args.Skip(1).ToArray();

				if (!args.Any()) {
					var options = await AppInterface.MainMenuDialog.ReadInputAsync();

					var file = options.DragAndDrop;

					if (file != null) {
						Debug.WriteLine($"Drag and drop: {file}");
						Console.WriteLine($">> {file}".AddColor(AppInterface.Elements.ColorMain));
						Config.Query = file;
						return true;
					}

					if (!options.Output.Any()) {
						return false;
					}
				}
				else {

					/*
			 * Handle CLI args
			 */

					try {

						ArgumentHandler.Run(args);

						Client.Reload();
					}
					catch (Exception e) {
						Console.WriteLine($"Error: {e.Message}");
						return false;
					}
				}

				return true;
			}
		}
	}
}