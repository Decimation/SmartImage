using System.Drawing;

namespace SmartImage.Engines.Other
{
	public sealed class TinEyeEngine : BasicSearchEngine
	{
		public TinEyeEngine() : base("https://www.tineye.com/search?url=") { }

		public override string Name => "TinEye";
		public override Color Color => Color.DarkCyan;

		public override SearchEngineOptions Engine => SearchEngineOptions.TinEye;


		/*
		 * https://github.com/Jabeyjabes/TinEye-API/blob/master/TinEye_API
		 * https://github.com/mkroman/tineye/blob/master/library/tineye/client.rb
		 * https://stackoverflow.com/questions/704956/getting-the-redirected-url-from-the-original-url
		 * https://github.com/search?p=3&q=TinEye&type=Repositories
		 */

		
	}
}