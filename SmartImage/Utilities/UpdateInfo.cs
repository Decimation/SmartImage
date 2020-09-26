using System;
using System.Diagnostics;
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

		public static void Update()
		{
			var ui = CheckForUpdates();

			if (ui.Status == VersionStatus.Available) {
				var dest = Path.Combine(RuntimeInfo.AppFolder, "SmartImage-new.exe");
				var wc = new WebClient();
				wc.DownloadFile(ui.Latest.AssetUrl, dest);

				string exeFileName = RuntimeInfo.ExeLocation;
				const string UPDATE_BAT = "SmartImage_Updater.bat";

				string[] commands =
				{
					"@echo off",

					/* Wait approximately 4 seconds (so that the process is already terminated) */
					"ping 127.0.0.1 > nul",

					/* Delete executable */
					"echo y | del /F " + exeFileName,

					/* Rename */
					$"move /Y \"{dest}\" \"{exeFileName}\" > NUL",

					/* Delete this bat file */
					"echo y | del " + UPDATE_BAT
				};

				var dir = Path.Combine(Path.GetTempPath(), UPDATE_BAT);

				File.WriteAllText(dir, commands.QuickJoin("\n"));

				// Runs in background
				Process.Start(dir);
			}


		}

		public static UpdateInfo CheckForUpdates()
		{
			var asm = typeof(RuntimeInfo).Assembly.GetName();
			var currentVersion = asm.Version;


			var release = ReleaseInfo.GetLatestRelease();

			VersionStatus status;

			int cmp = currentVersion.CompareTo(release.Version);

			if (cmp < 0) {
				status = VersionStatus.Available;
			}
			else if (cmp == 0) {
				status = VersionStatus.UpToDate;
			}
			else {
				status = VersionStatus.Preview;
			}

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