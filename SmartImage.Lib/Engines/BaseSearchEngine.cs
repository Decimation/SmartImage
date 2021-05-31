﻿using SimpleCore.Net;
using SmartImage.Lib.Searching;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using static SimpleCore.Diagnostics.LogCategories;

namespace SmartImage.Lib.Engines
{
	public abstract class BaseSearchEngine
	{
		public string BaseUrl { get; }

		protected BaseSearchEngine(string baseUrl)
		{
			BaseUrl = baseUrl;
		}

		public abstract SearchEngineOptions Engine { get; }

		public virtual string Name => Engine.ToString();

		public virtual SearchResult GetResult(ImageQuery query)
		{
			var rawUrl = GetRawResultUrl(query);

			var sr = new SearchResult(this);

			if (rawUrl == null) {
				sr.Status = ResultStatus.Unavailable;
			}
			else {
				sr.RawUri = rawUrl;
				sr.Status = ResultStatus.Success;
			}

			return sr;
		}

		public async Task<SearchResult> GetResultAsync(ImageQuery query)
		{
			// todo: use cts?

			var task = Task.Run(delegate
			{
				Debug.WriteLine($"{Name}: getting result async",C_INFO);
				var sw  = Stopwatch.StartNew();

				var res = GetResult(query);
				
				sw.Stop();
				
				Debug.WriteLine($"{Name}: result done {sw.Elapsed.TotalSeconds}",C_SUCCESS);

				return res;
			});

			return await task;

			//if (!task.Wait(span)) TokenSource.Cancel();

			//await task.AwaitWithTimeout(10, () => { }, () => { Debug.WriteLine($"cancel {Name}"); });

			//return task.Result;
		}

		public Uri GetRawResultUrl(ImageQuery query)
		{
			var uri = new Uri(BaseUrl + query.Uri);

			bool ok = Network.IsUriAlive(uri);

			if (!ok) {
				Debug.WriteLine($"{uri.Host} is unavailable",C_WARN);
				return null;
			}

			return uri;
		}
	}
}