using Novus.Win32;
using SimpleCore.Cli;
using SmartImage.Core;
using System;
using System.IO;
using System.Net;

namespace SmartImage.Utilities
{
	public readonly struct UpdateInfo
	{
		public Version Current { get; }

		public ReleaseInfo Latest { get; }

		public VersionStatus Status { get; }

		private UpdateInfo(Version current, ReleaseInfo info, VersionStatus status)
		{
			Current = current;
			Latest = info;
			Status = status;
		}

		public static void Update(UpdateInfo ui)
		{
			const string NEW_EXE = "SmartImage-new.exe";
			const string UPDATE_BAT = "SmartImage_Updater.bat";


			var destNew = Path.Combine(Info.AppFolder, NEW_EXE);
			var wc = new WebClient();

			NConsole.WriteInfo("Downloading...");

			wc.DownloadFile(ui.Latest.AssetUrl, destNew);


			string exeFileName = Info.ExeLocation;

			//const string WAIT_4_SEC = "ping 127.0.0.1 > nul";

			const string WAIT_4_SEC = "timeout /t 4 /nobreak >nul";

			string[] commands =
			{
				"@echo off",

				/* Wait approximately 4 seconds (so that the process is already terminated) */
				WAIT_4_SEC,

				/* Delete executable */
				"echo y | del /F " + exeFileName,

				/* Rename */
				$"move /Y \"{destNew}\" \"{exeFileName}\" > NUL",

				/* Wait */
				WAIT_4_SEC,
				WAIT_4_SEC,

				/* Open the new SmartImage version */
				$"start /d \"{Info.AppFolder}\" {Info.NAME_EXE}",

				/* Delete this batch file */
				"echo y | del " + UPDATE_BAT,
			};


			// Runs in background
			Command.RunBatch(commands, false, UPDATE_BAT);
		}


		// NOTE: Does not return if a new update is found and the user updates
		public static void AutoUpdate()
		{
			var ui = GetUpdateInfo();

			if (ui.Status == VersionStatus.Available)
			{
				NConsole.WriteSuccess($"Update found: {ui.Latest} ");

				if (NConsole.ReadConfirmation("Update?"))
				{
					try
					{
						Update(ui);
					}
					catch (Exception e)
					{
						Console.WriteLine(e);
						return;
					}

					Environment.Exit(0);
				}
			}

			NConsole.WriteInfo($"Up to date: {ui.Current} [{ui.Latest}]");
			NConsole.WaitForSecond();
		}

		public static UpdateInfo GetUpdateInfo()
		{
			var asm = typeof(Info).Assembly.GetName();
			var currentVersion = asm.Version;
			var release = ReleaseInfo.GetLatestRelease();

			int cmp = currentVersion.CompareTo(release.Version);

			var status = cmp switch
			{
				< 0 => VersionStatus.Available,
				0   => VersionStatus.UpToDate,
				_   => VersionStatus.Preview
			};

			return new UpdateInfo(currentVersion, release, status);
		}
	}

	public enum VersionStatus
	{
		UpToDate,
		Available,
		Preview,
	}
}