using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using JetBrains.Annotations;
using Newtonsoft.Json;
using RestSharp;
using SmartImage.Lib.Clients;
using SmartImage.Lib.Engines.Model;
using SmartImage.Lib.Searching;
using SmartImage.Lib.Utilities;
using static Kantan.Diagnostics.LogCategories;

// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006, IDE0051
namespace SmartImage.Lib.Engines.Impl
{
	/// <summary>
	/// 
	/// </summary>
	/// <a href="https://soruly.github.io/trace.moe/#/">Documentation</a>
	public sealed class TraceMoeEngine : ClientSearchEngine
	{
		public TraceMoeEngine() : base("https://trace.moe/?url=", "https://api.trace.moe") { }

		//public override TimeSpan Timeout => TimeSpan.FromSeconds(4);

		/// <summary>
		/// Used to retrieve more information about results
		/// </summary>
		private readonly AnilistClient m_anilistClient = new();

		public override string Name => "trace.moe";

		public override EngineSearchType SearchType => EngineSearchType.External | EngineSearchType.Metadata;

		public override SearchEngineOptions EngineOption => SearchEngineOptions.TraceMoe;

		protected override SearchResult Process(object obj, SearchResult r)
		{

			//var r = base.GetResult(url);
			var query = (ImageQuery) obj;
			// https://soruly.github.io/trace.moe/#/

			var rq = new RestRequest("search");
			rq.AddQueryParameter("url", query.UploadUri.ToString(), true);
			//rq.AddQueryParameter("anilistInfo", "");
			rq.OnBeforeDeserialization = resp => { resp.ContentType = "application/json"; };
			rq.Timeout                 = Timeout.Milliseconds;
			rq.RequestFormat           = DataFormat.Json;

			var now  = Stopwatch.GetTimestamp();
			var re   = Client.Execute<TraceMoeRootObject>(rq, Method.GET);
			var tm   = re.Data;
			var diff = TimeSpan.FromTicks(Stopwatch.GetTimestamp() - now);
			r.RetrievalTime = diff;

			//var tm=JsonConvert.DeserializeObject<TraceMoeRootObject>(re.Content);

			if (tm?.result != null) {
				// Most similar to least similar

				try {
					var results = ConvertResults(tm).ToList();
					var best    = results[0];

					/*r = new SearchResult(this)
					{
						PrimaryResult = best,
						RawUri        = new Uri(BaseUrl + query.UploadUri),

					};*/
					r.PrimaryResult = best;
					r.RawUri        = new Uri(BaseUrl + query.UploadUri);
					r.OtherResults.AddRange(results);
				}
				catch (Exception e) {
					r              = GetResult(query);
					r.ErrorMessage = e.Message;
					r.Status       = ResultStatus.Failure;
					//return r;
					goto ret;
				}

			}
			else {
				Debug.WriteLine($"{Name}: API error", C_ERROR);
				r.ErrorMessage = $"{re.ErrorMessage} {re.StatusCode}";
			}

			ret:

			r.PrimaryResult.Quality = r.PrimaryResult.Similarity switch
			{
				null                => ResultQuality.Indeterminate,
				>= FILTER_THRESHOLD => ResultQuality.High,
				_                   => ResultQuality.Low,
			};
			return r;
		}

		private IEnumerable<ImageResult> ConvertResults(TraceMoeRootObject obj)
		{
			var docs    = obj.result;
			var results = new ImageResult[docs.Count];

			for (int i = 0; i < results.Length; i++) {
				var   doc = docs[i];
				float sim = MathF.Round((float) (doc.similarity * 100.0f), 2);


				string anilistUrl = ANILIST_URL + doc.anilist;

				string name = m_anilistClient.GetTitle((int) doc.anilist);

				var result = new ImageResult
				{
					Url         = new Uri(anilistUrl),
					Similarity  = sim,
					Source      = name,
					Description = $"Episode #{doc.episode} @ {TimeSpan.FromSeconds(doc.from)}"
				};

				if (result.Similarity < FILTER_THRESHOLD) {
					result.OtherMetadata.Add("Note", $"Result may be inaccurate " +
					                                 $"({result.Similarity.Value.AsPercent()} < {FILTER_THRESHOLD.AsPercent()})");
				}

				results[i] = result;
			}

			return results;
		}

		/// <summary>
		/// https://anilist.co/anime/{id}/
		/// </summary>
		private const string ANILIST_URL = "https://anilist.co/anime/";

		/// <summary>
		/// Threshold at which results become inaccurate
		/// </summary>
		private const float FILTER_THRESHOLD = 87.00F;


		#region API Objects

		[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
		private class TraceMoeDoc
		{
			public double from { get; set; }

			public double to { get; set; }

			public long anilist { get; set; }

			public string filename { get; set; }


			/// <remarks>Episode field may contain multiple possible results delimited by <c>|</c></remarks>
			public string episode { get; set; }

			public double similarity { get; set; }

			public string video { get; set; }

			public string image { get; set; }
		}

		[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
		private class TraceMoeRootObject
		{
			public long              frameCount { get; set; }
			public string            error      { get; set; }
			public List<TraceMoeDoc> result     { get; set; }
		}

		#endregion
	}
}