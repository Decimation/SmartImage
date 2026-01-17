// Author: Deci | Project: SmartImage.Lib | Name: BaseSearchEngine.cs
// Date: 2024/06/06 @ 14:06:00


using System.Runtime.CompilerServices;
using Flurl.Http;
using Flurl.Http.Configuration;
using Kantan.Net.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using SmartImage.Lib;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Utilities.Diagnostics;
using SmartImage.Lib.Model;
using SmartImage.Lib.Utilities;
using SmartImage.Shared;

[assembly: InternalsVisibleTo(Common.PROJ_SMARTIMAGE_TEST)]
[assembly: InternalsVisibleTo(Common.PROJ_SMARTIMAGE_UI2)]

namespace SmartImage.Lib.Engines;

#pragma warning disable CA1822
#nullable disable

public abstract class BaseSearchEngine : IDisposable, IEquatable<BaseSearchEngine>, ISearchEngine, IUrl
{

	protected static readonly ILogger Logger = AppSupport.Factory.CreateLogger(nameof(BaseSearchEngine));

	/// <summary>
	/// Base URI
	/// </summary>
	public virtual Url Url { get; }

	public virtual string Name => EngineOption.ToString();

	/// <inheritdoc />
	public abstract SearchEngineOptions EngineOption { get; }

	[JI]
	public TimeSpan Timeout { get; protected init; }

	[JI]
	public long? MaxLength { get; protected init; }

	[JI]
	protected virtual string[] ErrorBodyMessages { get; }

	protected static IFlurlClient Client {get;}


	static BaseSearchEngine()
	{
		Client = FlurlHttp.Clients.GetOrAdd(nameof(BaseSearchEngine), null, static builder =>
		{
			builder.Headers.AddOrReplace(HeaderNames.UserAgent, R1.UserAgent1);

			// builder.Settings.JsonSerializer = new DefaultJsonSerializer();


			builder.Settings.AllowedHttpStatusRange = "*";
			builder.Settings.HttpVersion            = "2.0";

			builder.OnError(static f =>
			{
				// Debugger.Break();
				Logger.LogError(f.Exception, "Request: {Req}", f.Request);

			});

			// builder.AddMiddleware(static () => new HttpLoggingHandler(Logger));

		});
	}

	protected BaseSearchEngine([NN] Url url)
	{
		Url               = url;
		Timeout           = TimeSpan.FromSeconds(30);
		ErrorBodyMessages = [];
		MaxLength         = null;
		
	}


	public virtual Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken token = default)
	{
		var b = VerifyQuery(query);

		var srs = b ? SearchResultStatus.None : SearchResultStatus.IllegalInput;

		var res = GetRawResult(query);
		res.Status = srs;

		Logger.LogInformation("{Engine} with {Query} returned {Status}", Name, query, res.Status);

		return Task.FromResult(res);
	}

	protected virtual SearchResult GetRawResult(SearchQuery query)
	{
		var rawUrl = GetRawUrl(query);

		var res = new SearchResult(this, rawUrl) { };

		return res;
	}

	protected virtual Url GetRawUrl(SearchQuery query)
	{
		if (!query.IsUploaded) {
			throw new SmartImageException($"{query} not uploaded");
		}

		Url u = (Url + query.Upload);

		return u;
	}

	public virtual bool VerifyQuery(SearchQuery q)
	{
		bool b = true;

		if (MaxLength.HasValue) {
			b = q.Source.Length <= MaxLength;
		}

		return b;
	}

	/*public int GetHashCode(BaseSearchEngine obj)
	{
		return (int) EngineOption;
	}*/


	public override string ToString()
	{
		return $"{Name}: {Url} {Timeout}";
	}


	public override bool Equals([CBN] object obj)
	{
		if (obj is null) {
			return false;
		}

		if (ReferenceEquals(this, obj)) {
			return true;
		}

		if (obj.GetType() != GetType()) {
			return false;
		}

		return Equals((BaseSearchEngine) obj);
	}

	/*public override int GetHashCode()
	{
		return (int) EngineOption;
	}*/

	public abstract void Dispose();

	public bool Equals([CBN] BaseSearchEngine other)
	{
		if (other is null) {
			return false;
		}

		if (ReferenceEquals(this, other)) {
			return true;
		}

		return other.Equals(this);
	}

	/// <inheritdoc />
	public override int GetHashCode()
	{
		return (Name != null ? Name.GetHashCode() : 0);
	}

	public static bool operator ==([CBN] BaseSearchEngine left, [CBN] BaseSearchEngine right)
		=> Equals(left, right);

	public static bool operator !=([CBN] BaseSearchEngine left, [CBN] BaseSearchEngine right)
		=> !Equals(left, right);

}