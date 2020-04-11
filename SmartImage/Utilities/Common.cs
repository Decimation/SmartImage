using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using RestSharp;

namespace SmartImage.Utilities
{
	internal static class Common
	{
		internal static string Truncate(this string value, int maxLength)
		{
			if (String.IsNullOrEmpty(value)) return value;
			return value.Length <= maxLength ? value : value.Substring(0, maxLength);
		}

		/// <summary>Convert a word that is formatted in pascal case to have splits (by space) at each upper case letter.</summary>
		internal static string SplitPascalCase(string convert)
		{
			return Regex.Replace(Regex.Replace(convert, @"(\P{Ll})(\P{Ll}\p{Ll})", "$1 $2"),
			                     @"(\p{Ll})(\P{Ll})", "$1 $2");
		}

		public static void KillProc(Process p)
		{
			p.WaitForExit();
			p.Dispose();

			try {
				if (!p.HasExited) {
					p.Kill();
				}
			}
			catch (InvalidOperationException e) {
				// todo
			}
		}

		public static void TryMove(string exe, string dest)
		{
			for (int i = 0; i < 3; i++) {
				try {
					CliOutput.WriteInfo("Moving executable from {0} to {1}", exe, dest);
					File.Move(exe, dest);
					CliOutput.WriteSuccess("Success. Relaunch the program for changes to take effect.");
					return;
				}
				catch (IOException exception) {
					CliOutput.WriteError("Could not move file: {0}", exception.Message);
					Console.ReadLine();
				}
			}
		}

		public static List<Process> GetProcessesAssociatedToFile(string file)
		{
			return Process.GetProcesses()
			              .Where(x => !x.HasExited
			                          && x.Modules.Cast<ProcessModule>().ToList()
			                              .Exists(y => y.FileName.ToLowerInvariant() == file.ToLowerInvariant())
			               ).ToList();
		}
	}
}