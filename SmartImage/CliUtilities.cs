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
		public static void HandleErrors(object obj)
		{
			Console.WriteLine("error: {0}", obj);
		}

		private static void ReadFuncs(string[] args)
		{
			/*
			 * Verbs 
			 */

			var types = LoadVerbs();
			types.Add(typeof(ArgConfig));


			Parser.Default.ParseArguments(args, types.ToArray())
			      .WithParsed(o =>
			       {
				       Console.WriteLine("parse: {0}", o);
				       var obj = Run(o, args);
			       })
			      .WithNotParsed(CliUtilities.HandleErrors);
		}

		public static ArgConfig ReadConfig(string[] args)
		{
			/*
			 * Options 
			 */

			//
			ArgConfig cfg = new ArgConfig();


			/*var r1 = Parser.Default.ParseArguments<ArgConfig>(args);
			r1.WithParsed(p => { cfg = p; });
			if (cfg != null) {
				if (cfg.__simple) {
					Console.WriteLine("using cfg file fallback");

					bool imgOnly = args.Length == 1;
					if (!imgOnly) {
						Console.WriteLine("????");
					}

					ArgConfig.ReadFromFile(cfg, AltConfig.ConfigLocation);
				}
			}*/
			var r1 = Parser.Default.ParseArguments<ArgConfig, ContextMenu, Path,
				CreateSauceNao, Reset, Info>(args);

			r1.WithParsed<ArgConfig>(p =>
			{
				cfg = p;

				
			});
			
			
			
			if (cfg != null) {
				if (cfg.__simple) {
					Console.WriteLine("using cfg file fallback");

					bool imgOnly = args.Length == 1;
					if (!imgOnly) {
						Console.WriteLine("????");
					}

					ArgConfig.ReadFromFile(cfg, AltConfig.ConfigLocation);
				}
			}

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

			return cfg;
		}
	}
}