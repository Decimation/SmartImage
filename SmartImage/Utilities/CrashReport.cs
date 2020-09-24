using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SmartImage.Utilities
{
	internal class CrashReport
	{
		private readonly Exception m_exception;

		internal CrashReport(Exception exception)
		{
			m_exception = exception;
		}

		internal string Dump()
		{
			var sb =new StringBuilder();

			sb.AppendLine(CommonUtilities.CreateSeparator("Exception"));

			sb.AppendFormat("Exception message: {0}\n", m_exception.Message);
			sb.AppendFormat("Exception stack trace: {0}\n", m_exception.StackTrace);
			sb.AppendFormat("Source: {0}\n", m_exception.Source);
			sb.AppendFormat("HResult: {0}\n", m_exception.HResult);
			sb.AppendFormat("Site: {0}\n", m_exception.TargetSite);

			sb.AppendLine();
			sb.AppendLine(CommonUtilities.CreateSeparator("Config"));

			try {
				sb.AppendLine(SearchConfig.Config.Dump());
			}
			catch (Exception) {
				sb.AppendLine("Error adding config");
				
			}

			sb.AppendLine(CommonUtilities.CreateSeparator("Program Info"));
			var versionsInfo = UpdateInfo.CheckForUpdates();
			sb.AppendFormat("Version: {0}", versionsInfo.Current);

			return sb.ToString();
		}

		public override string ToString()
		{
			return Dump();
		}

		internal const string FILENAME = "crash.log";

		internal string WriteToFile()
		{
			var s = Dump();
			var p = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
			var n = FILENAME;

			var n2 = Path.Combine(p, n);
			File.WriteAllText(n2, s);

			return n2;
		}
	}
}
