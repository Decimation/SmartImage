using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using Kantan.Cli;
using Novus.Win32;
using SmartImage.Core;

namespace SmartImage.Utilities
{
	public enum VersionStatus
	{
		UpToDate,
		Available,
		Preview,
	}

	public readonly struct UpdateInfo
	{
		public Version Current { get; }

		public ReleaseInfo Latest { get; }

		public VersionStatus Status { get; }

		private UpdateInfo(Version current, ReleaseInfo info, VersionStatus status)
		{
			Current = current;
			Latest  = info;
			Status  = status;
		}

		[DoesNotReturn]
		public static void Update(UpdateInfo ui)
		{
			const string NEW_EXE    = "SmartImage-new.exe";
			const string UPDATE_BAT = "SmartImage_Updater.bat";


			var destNew = Path.Combine(AppInfo.AppFolder, NEW_EXE);
			var wc      = new WebClient();

			Console.WriteLine("Downloading...");

			wc.DownloadFile(ui.Latest.AssetUrl, destNew);


			string exeFileName = AppInfo.ExeLocation;

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
				//WAIT_4_SEC,

				/* Open the new SmartImage version */
				$"start /d \"{AppInfo.AppFolder}\" {AppInfo.NAME_EXE}",

				/* Delete this batch file */
				"echo y | del " + UPDATE_BAT,
			};


			// Runs in background
			var proc = Command.Batch(commands, UPDATE_BAT);

			proc.Start();
			Environment.Exit(0);
		}


		// NOTE: Does not return if a new update is found and the user updates
		public static void AutoUpdate()
		{
			var ui = GetUpdateInfo();

			if (ui.Status == VersionStatus.Available) {
				Console.WriteLine($"Update found: {ui.Latest} ");

				if (NConsole.ReadConfirmation("Update?")) {
					try {
						Update(ui);
						
					}
					catch (Exception e) {
						Console.WriteLine(e);
						
					}
				}
			}

			Console.WriteLine($"Up to date: {ui.Current} [{ui.Latest}]");
			NConsole.WaitForSecond();
			
		}

		public static UpdateInfo GetUpdateInfo()
		{
			var currentVersion = AppInfo.Version;

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
}