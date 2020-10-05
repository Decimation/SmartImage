using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using SmartImage.Searching.Model;

namespace SmartImage.Searching.Engines.SauceNao
{
	public abstract class BaseSauceNaoClient : BasicSearchEngine
	{
		protected const string BASE_URL = "https://saucenao.com/";

		protected const string BASIC_RESULT = "https://saucenao.com/search.php?url=";

		public override string Name => "SauceNao";


		public override SearchEngines Engine => SearchEngines.SauceNao;

		

		public override Color Color => Color.OrangeRed;

		protected struct SauceNaoSimpleResult : ISearchResult
		{
			public string? Caption { get; set; }
			public string Url { get; set; }
			public float? Similarity { get; set; }
			public int? Width { get; set; }
			public int? Height { get; set; }

			public SauceNaoSimpleResult(string? title, string url, float? similarity)
			{
				Caption = title;
				Url = url;
				Similarity = similarity;
				Width = null;
				Height = null;
			}

			public override string ToString()
			{
				return string.Format("{0} {1} {2}", Caption, Url, Similarity);
			}
		}

		protected BaseSauceNaoClient() : base(BASIC_RESULT) { }
	}
}
