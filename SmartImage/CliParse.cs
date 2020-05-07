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
		public static void ReadArguments(string[] args)
		{
			/*
			 * Verbs 
			 */

			var cfg = new UserConfig();
			
			var result = Parser.Default.ParseArguments<UserConfig,ContextMenu, Path,
				CreateSauceNao, Reset, Info>(args);
			

			// todo

			result.WithParsed<UserConfig>(c =>
			{
				//Console.WriteLine("cfg parsed func");
				cfg = c;
				//Console.WriteLine(c);
			});

			if (cfg.IsEmpty) {
				UserConfig.ReadFromFile(cfg, RuntimeInfo.ConfigLocation);
			}
			
			// UserConfig.ReadFromFile(cfg, RuntimeInfo.ConfigLocation);

			RuntimeInfo.Config = cfg;
			
			// todo: copied code
			
			result.WithParsed<ContextMenu>(c1 =>
			{
				var c = (IIntegrated) c1;
				if (c.Add) {
					ContextMenu.Add();
					CliOutput.WriteInfo("Added to context menu");
				}
				else if (c.Remove) {
					ContextMenu.Remove();
					CliOutput.WriteInfo("Removed from context menu");
				}
				else {
					CliOutput.WriteError("Option unknown: {0}", c.Option);
				}
			});
			result.WithParsed<Path>(c1 =>
			{
				var c = (IIntegrated) c1;
				if (c.Add) {
					Path.Add();
					CliOutput.WriteInfo("Added to path");
				}
				else if (c.Remove) {
					Path.Remove();
					CliOutput.WriteInfo("Removed from path");
				}
				else {
					CliOutput.WriteError("Option unknown: {0}", c.Option);
				}
			});
			result.WithParsed<CreateSauceNao>(c =>
			{
				var acc=SauceNao.CreateAccount(c.Auto);
				
				CliOutput.WriteInfo("Account information:");

				var accStr = acc.ToString();
				var output = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) 
				             + "\\saucenao_account.txt";
				
				File.WriteAllText(output, accStr);

				Console.WriteLine(accStr);
				
				CliOutput.WriteInfo("Adding key to cfg file");
				RuntimeInfo.Config.SauceNaoAuth = acc.ApiKey;
				RuntimeInfo.Config.UpdateFile();
			});
			result.WithParsed<Reset>(c =>
			{
				//
				Reset.RunReset(c.All);
			});
			result.WithParsed<Info>(c =>
			{
				//
				Info.Show();
			});
		}

		[Verb("ctx-menu")]
		[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
		public sealed class ContextMenu : IIntegrated
		{
			[Value(0, Required = true)]
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

		[Verb("path")]
		[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
		public sealed class Path : IIntegrated
		{
			[Value(0, Required = true)]
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

		[Verb("create-sn")]
		[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
		public sealed class CreateSauceNao
		{
			[Value(0)]
			public bool Auto { get; set; }
		}

		[Verb("reset")]
		[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
		public sealed class Reset
		{
			[Value(0)]
			public bool All { get; set; }

			public static void RunReset(bool all = false)
			{
				RuntimeInfo.Config.Reset();

				// Computer\HKEY_CLASSES_ROOT\*\shell\SmartImage

				ContextMenu.Remove();
				
				// will be added automatically if run again
				//Path.Remove();
				
				if (all) {
					RuntimeInfo.Config.Reset();
					RuntimeInfo.Config.UpdateFile();
					
					CliOutput.WriteSuccess("Reset cfg");
					return;
				}
			}
		}

		[Verb("info")]
		[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
		public sealed class Info
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

				CliOutput.WriteInfo("Readme: {0}", RuntimeInfo.Readme);

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
			}
		}
	}
}