using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// ReSharper disable UnusedMember.Global

namespace SmartImage.Lib
{
	public class SauceNaoEngine : SearchEngine
	{
		private const string BASE_URL = "https://saucenao.com/";

		private const string BASIC_RESULT = "https://saucenao.com/search.php?url=";

		private const string ENDPOINT = BASE_URL + "search.php";
		

		public SauceNaoEngine() : base(BASIC_RESULT) { }

		public override SearchEngineOptions Engine => SearchEngineOptions.SauceNao;
	}
}
