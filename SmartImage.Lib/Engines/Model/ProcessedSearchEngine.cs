﻿using System;
using System.Diagnostics;
using System.Net.Http;
using AngleSharp.Dom;
using RestSharp;
using SmartImage.Lib.Searching;
using static Kantan.Diagnostics.LogCategories;

namespace SmartImage.Lib.Engines.Model
{
	public abstract class ProcessedSearchEngine : BaseSearchEngine
	{
		protected ProcessedSearchEngine(string baseUrl) : base(baseUrl) { }

		public abstract override SearchEngineOptions EngineOption { get; }

		public abstract override string Name { get; }

		public abstract override EngineResultType ResultType { get; }


		/// <summary>
		/// Processes engine results
		/// </summary>
		/// <param name="obj">Content upon which to operate</param>
		/// <param name="sr"><see cref="SearchResult"/> to build</param>
		/// <returns>Final <see cref="SearchResult"/></returns>
		protected abstract SearchResult Process(object obj, SearchResult sr);

		[DebuggerHidden]
		public sealed override SearchResult GetResult(ImageQuery query)
		{
			// HACK: this is questionable, but it resolves the edge case with polymorphism

			ClientSearchEngine c = null;
			WebSearchEngine    w = null;

			var sr = GetResult(query, out IRestResponse response);

			switch (this) {
				case ClientSearchEngine:
					c = this as ClientSearchEngine;
					break;
				case WebSearchEngine:
					w = this as WebSearchEngine;
					break;
			}

			if (!sr.IsSuccessful) {
				return sr;
			}

			try {

				object o = null;

				if (c != null) {
					o = query;
				}
				else if (w != null) {
					o = w.GetContent(response);
				}

				sr = Process(o, sr);

				return sr;
			}
			catch (Exception e) {

				sr.Status       = ResultStatus.Failure;
				sr.ErrorMessage = e.Message;

				Trace.WriteLine($"{sr.Engine.Name}: {e.Message}", C_ERROR);
			}

			return sr;

		}
	}
}