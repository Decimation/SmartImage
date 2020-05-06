using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using CommandLine;

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

			var types = CmdFunctions.LoadVerbs();
			types.Add(typeof(ArgConfig));


			Parser.Default.ParseArguments(args, types.ToArray())
			      .WithParsed(o =>
			       {
				       Console.WriteLine("parse: {0}", o);
				       var obj = CmdFunctions.Run(o, args);
			       })
			      .WithNotParsed(CliUtilities.HandleErrors);
		}

		public static ArgConfig ReadConfig(string[] args)
		{
			/*
			 * Options 
			 */

			//
			ArgConfig cfg = null;


			var r1 = Parser.Default.ParseArguments<ArgConfig>(args);
			r1.WithParsed(p =>
			{
				
				cfg = p;
			});
			if (cfg!=null) {
				if (cfg.__simple) {
					Console.WriteLine("using cfg file fallback");

					bool imgOnly = args.Length == 1;
					if (!imgOnly) {
						Console.WriteLine("????");
					}

					ArgConfig.ReadFromFile(cfg,AltConfig.ConfigLocation);
				}
			}

			
			
			
			//ReadFuncs(args);

			return cfg;
		}
	}
}