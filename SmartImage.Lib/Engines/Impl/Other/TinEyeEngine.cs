using SmartImage.Lib.Engines.Model;

namespace SmartImage.Lib.Engines.Impl.Other
{
	public sealed class TinEyeEngine : BaseSearchEngine
	{
		public TinEyeEngine() : base("https://www.tineye.com/search?url=") { }
		

		public override SearchEngineOptions EngineOption => SearchEngineOptions.TinEye;


		/*
		 * https://github.com/Jabeyjabes/TinEye-API/blob/master/TinEye_API
		 * https://github.com/mkroman/tineye/blob/master/library/tineye/client.rb
		 * https://stackoverflow.com/questions/704956/getting-the-redirected-url-from-the-original-url
		 * https://github.com/search?p=3&q=TinEye&type=Repositories
		 */


	}
}