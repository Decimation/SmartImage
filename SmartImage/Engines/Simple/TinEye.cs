#region

using SmartImage.Searching;

#endregion

namespace SmartImage.Engines.Simple
{
	public sealed class TinEye : SimpleSearchEngine
	{
		public TinEye() : base("https://www.tineye.com/search?url=") { }

		public override string Name => "TinEye";

		public override SearchEngines Engine => SearchEngines.TinEye;


		/*
		 * https://github.com/Jabeyjabes/TinEye-API/blob/master/TinEye_API
		 * https://github.com/mkroman/tineye/blob/master/library/tineye/client.rb
		 * https://stackoverflow.com/questions/704956/getting-the-redirected-url-from-the-original-url
		 * https://github.com/search?p=3&q=TinEye&type=Repositories
		 */

	}
}