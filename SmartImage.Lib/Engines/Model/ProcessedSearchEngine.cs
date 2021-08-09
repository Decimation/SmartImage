using System;
using System.Diagnostics;
using AngleSharp.Dom;
using RestSharp;
using SmartImage.Lib.Searching;

namespace SmartImage.Lib.Engines.Model
{
	public abstract class ProcessedSearchEngine : BaseSearchEngine
	{
		protected ProcessedSearchEngine(string baseUrl) : base(baseUrl) { }

		public abstract override SearchEngineOptions EngineOption { get; }

		public abstract override string Name { get; }

		/// <summary>
		/// Processes engine results
		/// </summary>
		/// <param name="obj">Content upon which to operate</param>
		/// <param name="sr"><see cref="SearchResult"/> to build</param>
		/// <returns>Final <see cref="SearchResult"/></returns>
		protected abstract SearchResult Process(object obj, SearchResult sr);


		[DebuggerHidden]
		public override SearchResult GetResult(ImageQuery query)
		{
			// HACK: this is questionable, but it resolves the edge case with polymorphism

			SearchResult result;

			ClientSearchEngine c;
			WebSearchEngine    w;

			IRestResponse response = null;

			switch (this) {
				case ClientSearchEngine:
					result = base.GetResult(query);
					c      = this as ClientSearchEngine;
					w      = null;
					break;
				case WebSearchEngine:
					result = GetResult(query, out response);
					w      = this as WebSearchEngine;
					c      = null;
					break;
				default:
					throw new InvalidOperationException();
			}

			return TryProcess(result, sr =>
			{
				object o;

				if (c != null) {
					o = query;
				}
				else if (w != null) {
					o = w.GetContent(response);
				}
				else {
					throw new InvalidOperationException();
				}


				sr = Process(o, sr);
				return sr;
			});
		}
	}
}