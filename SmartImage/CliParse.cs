using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CommandLine;
using JetBrains.Annotations;
using SimpleCore;
using SimpleCore.Utilities;
using SmartImage.Engines.SauceNao;
using SmartImage.Model;
using SmartImage.Searching;

// ReSharper disable ParameterTypeCanBeEnumerable.Global

namespace SmartImage
{
	public static class CliParse
	{
		private static void RunIntegrated(IIntegrated c, Action add, Action remove)
		{
			if (c.Add) {
				add();
			}
			else if (c.Remove) {
				remove();
			}
			else {
				CliOutput.WriteError("Option unknown: {0}", c.Option);
			}
		}
		
		private static void RunCommands(object obj)
		{
			// todo: copied code

			switch (obj) {
				case ContextMenuCommand c1:
				{
					var c = (IIntegrated) c1;
					RunIntegrated(c, ContextMenuCommand.Add, ContextMenuCommand.Remove);

					break;
				}

				case PathCommand c1:
				{
					var c = (IIntegrated) c1;
					RunIntegrated(c, PathCommand.Add, PathCommand.Remove);

					break;
				}
				case CreateSauceNaoCommand c:
				{
					var acc = SauceNao.CreateAccount(c.Auto);

					CliOutput.WriteInfo("Account information:");

					var accStr = acc.ToString();
					var output = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
					             + "\\saucenao_account.txt";

					File.WriteAllText(output, accStr);

					Console.WriteLine(accStr);

					CliOutput.WriteInfo("Adding key to cfg file");
					RuntimeInfo.Config.SauceNaoAuth = acc.ApiKey;
					RuntimeInfo.Config.WriteToFile();
					break;
				}
				case ResetCommand c:
				{
					ResetCommand.RunReset(c.All);
					break;
				}
				case InfoCommand c:
				{
					InfoCommand.Show();
					break;
				}
			}
		}

		public static void ReadArguments(string[] args)
		{
			/*
			 * Verbs 
			 */

			var cfgCli = new SearchConfig();

			var result = Parser.Default.ParseArguments<SearchConfig, ContextMenuCommand, PathCommand,
				CreateSauceNaoCommand, ResetCommand, InfoCommand>(args);


			// todo

			result.WithParsed<SearchConfig>(c =>
			{
				//Console.WriteLine("cfg parsed func");
				cfgCli = c;
				//Console.WriteLine(c);
			});

			// Overrides cfg with cfg from cli
			/*if (cfg.IsEmpty) {
				UserConfig.Update(cfg, RuntimeInfo.ConfigLocation);
			}*/
			var cfgFile = SearchConfig.ReadFromFile(RuntimeInfo.ConfigLocation);
			SearchConfig.Update(cfgCli, cfgFile);

			/*Console.WriteLine(cfgCli);
			Console.WriteLine();
			Console.WriteLine(cfgFile);
			Console.ReadLine();*/

			// UserConfig.ReadFromFile(cfg, RuntimeInfo.ConfigLocation);

			RuntimeInfo.Config = cfgCli;

			result.WithParsed(RunCommands);
		}

		[Verb("ctx-menu", HelpText = "Adds or removes context menu integration.")]
		[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
		public sealed class ContextMenuCommand : IIntegrated
		{
			[Value(0, Required = true, HelpText = "<add/remove>")]
			public string Option { get; set; }

			public static void Remove()
			{
				// reg delete HKEY_CLASSES_ROOT\*\shell\SmartImage

				// const string DEL = @"reg delete HKEY_CLASSES_ROOT\*\shell\SmartImage";

				string[] code =
				{
					"@echo off",
					String.Format(@"reg.exe delete {0} /f >nul", RuntimeInfo.REG_SHELL)
				};

				Cli.CreateRunBatchFile("rem_from_menu.bat", code);
			}

			public static void Add()
			{
				string fullPath = RuntimeInfo.ExeLocation;

				if (!RuntimeInfo.IsExeInAppFolder) {
					bool v = CliOutput.ReadConfirm("Could not find exe in system path. Add now?");

					if (v) {
						PathCommand.Add();
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
					String.Format("reg.exe add {0} /ve /d \"{1} \"\"%%1\"\"\" /f >nul", RuntimeInfo.REG_SHELL_CMD,
					              fullPath)
				};

				Cli.CreateRunBatchFile("add_to_menu.bat", commandCode);


				// Add icon
				string[] iconCode =
				{
					"@echo off",
					String.Format("reg.exe add {0} /v Icon /d \"{1}\" /f >nul", RuntimeInfo.REG_SHELL, fullPath),
				};

				Cli.CreateRunBatchFile("add_icon_to_menu.bat", iconCode);
			}
		}

		[Verb("path", HelpText = "Adds or removes executable path to path environment variable.")]
		[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
		public sealed class PathCommand : IIntegrated
		{
			[Value(0, Required = true, HelpText = "<add/remove>")]
			public string Option { get; set; }

			public static void Add()
			{
				string oldValue = ExplorerSystem.EnvironmentPath;

				string appFolder = RuntimeInfo.AppFolder;

				if (RuntimeInfo.IsAppFolderInPath) {
//				CliOutput.WriteInfo("Executable is already in path: {0}", ExeLocation);
					return;
				}


				bool appFolderInPath = oldValue.Split(ExplorerSystem.PATH_DELIM).Any(p => p == appFolder);

				string cd  = Environment.CurrentDirectory;
				string exe = System.IO.Path.Combine(cd, RuntimeInfo.NAME_EXE);

				if (appFolderInPath) {
//				CliOutput.WriteInfo("App folder already in path: {0}", appFolder);
				}
				else {
					string newValue = oldValue + ExplorerSystem.PATH_DELIM + cd;
					ExplorerSystem.EnvironmentPath = newValue;
//				CliOutput.WriteInfo("Added {0} to path", cd);
				}
			}

			public static void Remove() => ExplorerSystem.RemoveFromPath(RuntimeInfo.AppFolder);
		}

		[Verb("create-sn", HelpText = "Creates a SauceNao account (for API keys).")]
		[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
		public sealed class CreateSauceNaoCommand
		{
			[Value(0, Default = true,
			       HelpText   = "Specify true to automatically autofill account registration fields.")]
			public bool Auto { get; set; }
		}

		[Verb("reset", HelpText = "Removes integrations")]
		[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
		public sealed class ResetCommand
		{
			[Value(0, HelpText = "Specify [all] to reset configuration in addition to removing integrations")]
			public string Option { get; set; }

			public bool All => Option == "all";

			public static void RunReset(bool all = false)
			{
				RuntimeInfo.Config.Reset();

				// Computer\HKEY_CLASSES_ROOT\*\shell\SmartImage

				ContextMenuCommand.Remove();

				// will be added automatically if run again
				//Path.Remove();

				if (all) {
					RuntimeInfo.Config.Reset();
					RuntimeInfo.Config.WriteToFile();

					CliOutput.WriteSuccess("Reset cfg");
					return;
				}
			}
		}

		[Verb("info", HelpText = "Displays information about the program and its configuration.")]
		[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
		public sealed class InfoCommand
		{
			internal static void Show()
			{
				Console.Clear();

				CliOutput.WriteInfo("Search engines: {0}", RuntimeInfo.Config.Engines);
				CliOutput.WriteInfo("Priority engines: {0}", RuntimeInfo.Config.PriorityEngines);

				var sn     = RuntimeInfo.Config.SauceNaoAuth;
				var snNull = String.IsNullOrWhiteSpace(sn);

				CliOutput.WriteInfo("SauceNao authentication: {0} ({1})", snNull ? CliOutput.MUL_SIGN.ToString() : sn,
				                    snNull ? "Basic" : "Advanced");

				var  imgur     = RuntimeInfo.Config.ImgurAuth;
				bool imgurNull = String.IsNullOrWhiteSpace(imgur);
				CliOutput.WriteInfo("Imgur authentication: {0}", imgurNull ? CliOutput.MUL_SIGN.ToString() : imgur);

				CliOutput.WriteInfo("Image upload service: {0}", imgurNull ? "ImgOps" : "Imgur");

				CliOutput.WriteInfo("Application folder: {0}", RuntimeInfo.AppFolder);
				CliOutput.WriteInfo("Executable location: {0}", RuntimeInfo.ExeLocation);
				CliOutput.WriteInfo("Config location: {0}", RuntimeInfo.ConfigLocation);
				CliOutput.WriteInfo("Context menu integrated: {0}", RuntimeInfo.IsContextMenuAdded);
				CliOutput.WriteInfo("In path: {0}\n", RuntimeInfo.IsAppFolderInPath);

				//

				// CliOutput.WriteInfo("Supported search engines: {0}\n", SearchEngines.All);

				//

				var asm            = typeof(RuntimeInfo).Assembly.GetName();
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

				CliOutput.WriteInfo("Readme: {0}", RuntimeInfo.Readme);
				CliOutput.WriteInfo("Author: {0}", RuntimeInfo.Author);
			}
		}
	}
}