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
using SmartImage.Searching;

namespace SmartImage
{
	public static class CmdFunctions
	{
		public static List<Type> LoadVerbs()
		{
			return Assembly.GetExecutingAssembly().GetTypes()
			               .Where(t => t.GetCustomAttribute<VerbAttribute>() != null).ToList();
		}

		[Verb("image", true)]
		public class Img
		{
			[Value(0)]
			public string Path { get; set; }

			public override string ToString()
			{
				var sb = new StringBuilder();
				sb.AppendFormat("path: {0}\n", Path);
				return sb.ToString();
			}
		}

		[Verb("ctx-menu")]
		[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
		public sealed class ContextMenu
		{
			[Option]
			public bool Add { get; set; }

			[Option]
			public bool Remove { get; set; }

			public override string ToString()
			{
				var sb = new StringBuilder();
				sb.AppendFormat("Add: {0}\n", Add);
				sb.AppendFormat("Remove: {0}\n", Remove);
				return sb.ToString();
			}

			public static void RemoveFromContextMenu()
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

			public static void AddToContextMenu()
			{
				string fullPath = Core.ExeLocation;

				if (!Core.IsExeInAppFolder) {
					bool v = CliOutput.ReadConfirm("Could not find exe in system path. Add now?");

					if (v) {
						Path.AddToPath();
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
		public sealed class Path
		{
			[Option]
			public bool Add { get; set; }

			public override string ToString()
			{
				var sb = new StringBuilder();
				sb.AppendFormat("Add: {0}\n", Add);
				return sb.ToString();
			}

			public static void AddToPath()
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

			public static void RemoveFromPath() => ExplorerSystem.RemoveFromPath(Core.AppFolder);
		}

		[Verb("create-saucenao")]
		[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
		public sealed class CreateSauceNao
		{
			[Option]
			public bool Auto { get; set; }

			public override string ToString()
			{
				var sb = new StringBuilder();
				sb.AppendFormat("Auto: {0}\n", Auto);
				return sb.ToString();
			}
		}

		[Verb("reset")]
		[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
		public sealed class Reset
		{
			[Option]
			public bool All { get; set; }

			public override string ToString()
			{
				var sb = new StringBuilder();
				sb.AppendFormat("All: {0}\n", All);
				return sb.ToString();
			}

			public static void RunReset(bool all = false)
			{
				Core.CoreCfg.Engines         = SearchEngines.All;
				Core.CoreCfg.PriorityEngines = SearchEngines.SauceNao;
				Core.CoreCfg.ImgurAuth       = String.Empty;
				Core.CoreCfg.SauceNaoAuth    = String.Empty;

				// Computer\HKEY_CLASSES_ROOT\*\shell\SmartImage

				ContextMenu.RemoveFromContextMenu();

				if (all) {
					Path.RemoveFromPath();
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

				CliOutput.WriteInfo("Search engines: {0}", Core.CoreCfg.Engines);
				CliOutput.WriteInfo("Priority engines: {0}", Core.CoreCfg.PriorityEngines);

				var sn     = Core.CoreCfg.SauceNaoAuth;
				var snNull = String.IsNullOrWhiteSpace(sn);

				CliOutput.WriteInfo("SauceNao authentication: {0} ({1})", snNull ? CliOutput.MUL_SIGN.ToString() : sn,
				                    snNull ? "Basic" : "Advanced");

				var  imgur     = Core.CoreCfg.ImgurAuth;
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

				var release = Core.LatestRelease();
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