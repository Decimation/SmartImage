#region

using System;
using SmartImage.Searching.Model;

#endregion

namespace SmartImage.Searching.Engines.Simple
{
	public sealed class TinEyeClient : SimpleSearchEngine
	{
		public TinEyeClient() : base("https://www.tineye.com/search?url=") { }

		public override string Name => "TinEye";
		public override ConsoleColor Color => ConsoleColor.DarkCyan;

		public override SearchEngines Engine => SearchEngines.TinEye;


		/*
		 * https://github.com/Jabeyjabes/TinEye-API/blob/master/TinEye_API
		 * https://github.com/mkroman/tineye/blob/master/library/tineye/client.rb
		 * https://stackoverflow.com/questions/704956/getting-the-redirected-url-from-the-original-url
		 * https://github.com/search?p=3&q=TinEye&type=Repositories
		 */

		
	}
}