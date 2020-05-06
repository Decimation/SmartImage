using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CommandLine;
using JetBrains.Annotations;
using Neocmd;
using SmartImage.Engines.SauceNao;
using SmartImage.Model;
using SmartImage.Searching;

namespace SmartImage
{
	public static class CliParse
	{
		public static List<Type> LoadVerbs()
		{
			return Assembly.GetExecutingAssembly().GetTypes()
			               .Where(t => t.GetCustomAttribute<VerbAttribute>() != null).ToList();
		}

		[Verb("image", true)]
		[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
		public sealed class Img
		{
			[Value(0, Required = true)]
			public string Location { get; set; }
		}

		[Verb("ctx-menu")]
		[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
		public sealed class ContextMenu : IIntegrated
		{
			[Value(0, Required = true)]
			public string AddOrRem { get; set; }

			public static void Remove()
			{
				// reg delete HKEY_CLASSES_ROOT\*\shell\SmartImage

				// const string DEL = @"reg delete HKEY_CLASSES_ROOT\*\shell\SmartImage";

				string[] code =
				{
					"@echo off",
					String.Format(@"reg.exe delete {0} /f >nul", Core.REG_SHELL)
				};

				Cli.CreateRunBatchFile("rem_from_menu.bat", code);
			}

			public static void Add()
			{
				string fullPath = Core.ExeLocation;

				if (!Core.IsExeInAppFolder) {
					bool v = CliOutput.ReadConfirm("Could not find exe in system path. Add now?");

					if (v) {
						Path.Add();
						return;
					}

					if (fullPath == null) {
						throw new ApplicationException();
					}
				}


				// Add command
				string[] commandCode =
				{
					"@echo off",
					String.Format("reg.exe add {0} /ve /d \"{1} \"\"%%1\"\"\" /f >nul", Core.REG_SHELL_CMD,
					              fullPath)
				};

				Cli.CreateRunBatchFile("add_to_menu.bat", commandCode);


				// Add icon
				string[] iconCode =
				{
					"@echo off",
					String.Format("reg.exe add {0} /v Icon /d \"{1}\" /f >nul", Core.REG_SHELL, fullPath),
				};

				Cli.CreateRunBatchFile("add_icon_to_menu.bat", iconCode);
			}
		}

		[Verb("path")]
		[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
		public sealed class Path : IIntegrated
		{
			[Value(0, Required = true)]
			public string AddOrRem { get; set; }
			
			public static void Add()
			{
				string oldValue = ExplorerSystem.EnvironmentPath;

				string appFolder = Core.AppFolder;

				if (Core.IsAppFolderInPath) {
//				CliOutput.WriteInfo("Executable is already in path: {0}", ExeLocation);
					return;
				}


				bool appFolderInPath = oldValue.Split(ExplorerSystem.PATH_DELIM).Any(p => p == appFolder);

				string cd  = Environment.CurrentDirectory;
				string exe = System.IO.Path.Combine(cd, Core.NAME_EXE);

				if (appFolderInPath) {
//				CliOutput.WriteInfo("App folder already in path: {0}", appFolder);
				}
				else {
					string newValue = oldValue + ExplorerSystem.PATH_DELIM + cd;
					ExplorerSystem.EnvironmentPath = newValue;
//				CliOutput.WriteInfo("Added {0} to path", cd);
				}
			}

			public static void Remove() => ExplorerSystem.RemoveFromPath(Core.AppFolder);
		}

		[Verb("create-saucenao")]
		[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
		public sealed class CreateSauceNao
		{
			[Option]
			public bool Auto { get; set; }
		}

		[Verb("reset")]
		[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
		public sealed class Reset
		{
			[Option]
			public bool All { get; set; }

			public static void RunReset(bool all = false)
			{
				Core.Config.Engines         = SearchEngines.All;
				Core.Config.PriorityEngines = SearchEngines.SauceNao;
				Core.Config.ImgurAuth       = String.Empty;
				Core.Config.SauceNaoAuth    = String.Empty;

				// Computer\HKEY_CLASSES_ROOT\*\shell\SmartImage

				ContextMenu.Remove();

				if (all) {
					Path.Remove();
					CliOutput.WriteSuccess("Removed from path");
					return;
				}
			}
		}

		[Verb("info")]
		[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
		public sealed class Info
		{
			[Option]
			public bool Full { get; set; }

			public override string ToString()
			{
				var sb = new StringBuilder();
				sb.AppendFormat("Full: {0}\n", Full);
				return sb.ToString();
			}

			internal static void ShowInfo(bool full)
			{
				Console.Clear();

				CliOutput.WriteInfo("Search engines: {0}", Core.Config.Engines);
				CliOutput.WriteInfo("Priority engines: {0}", Core.Config.PriorityEngines);

				var sn     = Core.Config.SauceNaoAuth;
				var snNull = String.IsNullOrWhiteSpace(sn);

				CliOutput.WriteInfo("SauceNao authentication: {0} ({1})", snNull ? CliOutput.MUL_SIGN.ToString() : sn,
				                    snNull ? "Basic" : "Advanced");

				var  imgur     = Core.Config.ImgurAuth;
				bool imgurNull = String.IsNullOrWhiteSpace(imgur);
				CliOutput.WriteInfo("Imgur authentication: {0}", imgurNull ? CliOutput.MUL_SIGN.ToString() : imgur);

				CliOutput.WriteInfo("Image upload service: {0}", imgurNull ? "ImgOps" : "Imgur");

				CliOutput.WriteInfo("Application folder: {0}", Core.AppFolder);
				CliOutput.WriteInfo("Executable location: {0}", Core.ExeLocation);
				CliOutput.WriteInfo("Config location: {0}", Core.ConfigLocation);
				CliOutput.WriteInfo("Context menu integrated: {0}", Core.IsContextMenuAdded);
				CliOutput.WriteInfo("In path: {0}\n", Core.IsAppFolderInPath);

				//

				// CliOutput.WriteInfo("Supported search engines: {0}\n", SearchEngines.All);

				//

				CliOutput.WriteInfo("Readme: {0}", Core.Readme);

				var asm            = typeof(Core).Assembly.GetName();
				var currentVersion = asm.Version;
				CliOutput.WriteInfo("Current version: {0}", currentVersion);

				var release = ReleaseInfo.LatestRelease();
				CliOutput.WriteInfo("Latest version: {0} (tag {1}) ({2})", release.Version, release.TagName,
				                    release.PublishedAt);

				int vcmp = currentVersion.CompareTo(release.Version);

				if (vcmp < 0) {
					CliOutput.WriteInfo("Update available");
				}
				else if (vcmp == 0) {
					CliOutput.WriteInfo("Up to date");
				}
				else if (vcmp > 0) {
					CliOutput.WriteInfo("(preview)");
				}
			}
		}

		public static void ReadFuncs(string[] args)
		{
			/*
			 * Verbs 
			 */

			var result = Parser.Default.ParseArguments<ContextMenu, Path,
				CreateSauceNao, Reset, Info>(args);

			result.WithParsed<ContextMenu>(c1 =>
			{
				var c = (IIntegrated) c1;
				if (c.Add) {
					ContextMenu.Add();
				}
				else {
					ContextMenu.Remove();
				}
			});
			result.WithParsed<Path>(c1 =>
			{
				var c = (IIntegrated) c1;
				if (c.Add) {
					Path.Add();
				}
				else {
					Path.Remove();
				}
			});
			result.WithParsed<CreateSauceNao>(c => { SauceNao.CreateAccount(c.Auto); });
			result.WithParsed<Reset>(c => { Reset.RunReset(c.All); });
			result.WithParsed<Info>(c => { Info.ShowInfo(c.Full); });

			//ReadFuncs(args);
		}

		public static Config ReadConfig(string[] args)
		{
			/*
			 * Options 
			 */

			//
			var cfg = new Config();
			
			var result = Parser.Default.ParseArguments<Config>(args);

			result.WithParsed<Config>(p =>
			{
				//
				cfg = p;
			});
			
			if (cfg.IsEmpty) {
				Config.ReadFromFile(cfg, Core.ConfigLocation);
			}


			return cfg;
		}
	}
}