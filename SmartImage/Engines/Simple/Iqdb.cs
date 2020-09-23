#region

using System;
using SmartImage.Searching;

#endregion

namespace SmartImage.Engines.Simple
{
	public sealed class Iqdb : SimpleSearchEngine
	{
		public Iqdb() : base("https://iqdb.org/?url=") { }

		public override string Name => "IQDB";
		public override ConsoleColor Color => ConsoleColor.DarkMagenta;

		public override SearchEngines Engine => SearchEngines.Iqdb;
	}
}