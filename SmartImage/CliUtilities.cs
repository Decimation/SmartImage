using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using CommandLine;
using SmartImage.Engines.SauceNao;
using static SmartImage.CmdFunctions;

namespace SmartImage
{
	public static class CliUtilities
	{
		public static void ReadFuncs(string[] args)
		{
			/*
			 * Verbs 
			 */

			var r1 = Parser.Default.ParseArguments<ContextMenu, Path,
				CreateSauceNao, Reset, Info>(args);

			r1.WithParsed<ContextMenu>(c =>
			{
				if (c.Add) {
					ContextMenu.AddToContextMenu();
				}
				else if (c.Remove) {
					ContextMenu.RemoveFromContextMenu();
				}
			});
			r1.WithParsed<Path>(c =>
			{
				if (c.Add) {
					Path.AddToPath();
				}
				else {
					Path.RemoveFromPath();
				}
			});
			r1.WithParsed<CreateSauceNao>(c => { SauceNao.CreateAccount(c.Auto); });
			r1.WithParsed<Reset>(c => { Reset.RunReset(c.All); });
			r1.WithParsed<Info>(c => { Info.ShowInfo(c.Full); });

			//ReadFuncs(args);
		}

		public static Config ReadConfig(string[] args)
		{
			/*
			 * Options 
			 */

			//
			Config cfg = new Config();


			var r1 = Parser.Default.ParseArguments<Config, Img>(args);

			r1.WithParsed<Config>(p => { cfg = p; });


			if (cfg != null) {
				if (cfg.__simple) {
					Console.WriteLine("using cfg file fallback");


					Config.ReadFromFile(cfg, Core.ConfigLocation);
				}
			}


			return cfg;
		}
	}
}