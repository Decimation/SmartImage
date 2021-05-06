using JetBrains.Annotations;
using SimpleCore.Utilities;
using SmartImage.Lib.Engines;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace SmartImage.Lib.Searching
{
	public enum ResultStatus
	{
		Success,
		NoResults,
		Unavailable,
		Failure
	}

	public class SearchResult : IFieldView
	{
		/// <summary>
		/// Primary image result
		/// </summary>
		public ImageResult PrimaryResult { get; set; }

		/// <summary>
		/// Other image results
		/// </summary>
		public List<ImageResult> OtherResults { get; set; }

		public Uri RawUri { get; set; }

		public BaseSearchEngine Engine { get; init; }

		public ResultStatus Status { get; set; }

		[CanBeNull]
		public string ErrorMessage { get; set; }

		public bool IsPrimitive
		{
			get
			{
				//todo: WIP
				return PrimaryResult.Url == null;
			}
		}

		public bool IsSuccessful
		{
			get
			{
				switch (Status) {
					case ResultStatus.Failure:
					case ResultStatus.Unavailable:
						return false;

					case ResultStatus.Success:
					case ResultStatus.NoResults:
						return true;

					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		public SearchResult(BaseSearchEngine engine)
		{
			Engine = engine;

			PrimaryResult = new ImageResult();
			OtherResults  = new List<ImageResult>();
		}

		#region UI

		private static readonly string Indent = new string(' ', 3);

		private static readonly string Separator = Indent + new string('-', 20);

		private const string RANK_P = "P";

		private const string RANK_S = "S";

		private static readonly Color Blue = Color.DeepSkyBlue;

		private static string IndentFields(string s)
		{
			//return s.Replace("\n", "\n" + Indent);

			var split = s.Split('\n');

			var j = string.Join($"\n{Indent}", split);

			return Indent + j;
		}

		#endregion UI

		public override string ToString()
		{
			return new DefaultFieldViewHandler().GetString(this);
		}

		public Dictionary<string, object> GetFields()
		{
			var sb = new Dictionary<string, object>();

			var name = $"[{Engine.Name}]".AddColor(Blue);

			sb.Add($"{name}", $"({Status}; {(IsPrimitive ? RANK_P : RANK_S)})");

			if (PrimaryResult.Url != null) {
				
				foreach (var kv in PrimaryResult.GetFields()) {
					sb.Add(kv.Key, kv.Value);
				}

			}

			//========================================================================//


			sb.Add("Raw", RawUri);
			sb.Add("Other image results", $"{OtherResults.Count}");
			sb.Add("Error", ErrorMessage);

			return sb;
		}
	}
}