using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using SmartImage.Searching.Model;

namespace SmartImage.Searching.Engines.SauceNao
{
	public abstract class BaseSauceNaoClient : ISearchEngine
	{
		protected const string BASE_URL = "https://saucenao.com/";

		public string Name => "SauceNao";


		public SearchEngines Engine => SearchEngines.SauceNao;

		public abstract SearchResult GetResult(string url);

		public Color Color => Color.OrangeRed;


	}
}
