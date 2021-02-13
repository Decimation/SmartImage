using System;
using System.IO;
using System.Text;
using SimpleCore.Utilities;
using SmartImage.Configuration;
using SmartImage.Core;

// ReSharper disable UnusedMember.Global

namespace SmartImage.Utilities
{
	internal class CrashReport
	{
		private readonly Exception m_exception;

		internal CrashReport(Exception exception)
		{
			m_exception = exception;
		}

		public override string ToString()
		{
			var sb = new StringBuilder();

			sb.AppendLine(Strings.CreateSeparator("Exception"));

			sb.AppendFormat("Exception message: {0}\n", m_exception.Message);
			sb.AppendFormat("Exception stack trace: {0}\n", m_exception.StackTrace);
			sb.AppendFormat("Source: {0}\n", m_exception.Source);
			sb.AppendFormat("HResult: {0}\n", m_exception.HResult);
			sb.AppendFormat("Site: {0}\n", m_exception.TargetSite);

			sb.AppendLine();
			sb.AppendLine(Strings.CreateSeparator("Config"));

			try
			{
				sb.AppendLine(SearchConfig.Config.ToString());
			}
			catch (Exception)
			{
				sb.AppendLine("Error adding config");

			}

			sb.AppendLine(Strings.CreateSeparator("Program Info"));
			var versionsInfo = UpdateInfo.GetUpdateInfo();
			sb.AppendFormat("Version: {0}", versionsInfo.Current);

			return sb.ToString();
		}

		internal const string FILENAME = "crash.log";

		internal string WriteToFile()
		{
			var s = ToString();
			var p = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
			var n = FILENAME;

			var n2 = Path.Combine(p, n);
			File.WriteAllText(n2, s);

			return n2;
		}
	}
}
