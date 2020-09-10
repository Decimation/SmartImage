using System;

namespace SmartImage.Utilities
{
	public readonly struct VersionsInfo
	{
		public Version Current { get; }
		public ReleaseInfo Latest { get; }

		public VersionStatus Status { get; }

		private VersionsInfo(Version current, ReleaseInfo info, VersionStatus status)
		{
			Current = current;
			Latest = info;
			Status = status;
		}

		public static VersionsInfo Create()
		{
			var asm = typeof(RuntimeInfo).Assembly.GetName();
			var currentVersion = asm.Version;


			var release = ReleaseInfo.LatestRelease();

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

			return new VersionsInfo(currentVersion, release, status);
		}
	}

	public enum VersionStatus
	{
		UpToDate,
		Available,
		Preview,
	}
}