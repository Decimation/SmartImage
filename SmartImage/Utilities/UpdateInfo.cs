using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using JetBrains.Annotations;
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
		[ContractAnnotation("=> stop")]
		public static void Update(UpdateInfo ui)
		{
			const string NEW_EXE    = "SmartImage-new.exe";
			const string UPDATE_BAT = "SmartImage_Updater.bat";


			var destNew = Path.Combine(AppInfo.AppFolder, NEW_EXE);

			using var wc = new WebClient();


			Console.WriteLine("Downloading...");
			Console.WriteLine("Program will reopen automatically after update!");

			wc.DownloadFile(ui.Latest.AssetUrl, destNew);

			

			string exeFileName = AppInfo.ExeLocation;

			//const string WAIT_4_SEC = "ping 127.0.0.1 > nul";

			const string WAIT_2_SEC = "timeout /t 2 /nobreak >nul";
			const string WAIT_1_SEC = "timeout /t 1 /nobreak >nul";

			string[] commands =
			{
				"@echo off",

				/* Wait approximately 2 seconds (so that the process is already terminated) */
				WAIT_2_SEC,

				/* Delete executable */
				"echo y | del /F " + exeFileName,

				/* Rename */
				$"move /Y \"{destNew}\" \"{exeFileName}\" > NUL",

				/* Wait */
				WAIT_1_SEC,

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