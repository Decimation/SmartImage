global using Url = Flurl.Url;
using Image = System.Drawing.Image;
using System.Diagnostics;
using System.Drawing;
using System.Json;
using System.Resources;
using Novus.Utilities;
using SmartImage.Lib.Results;

namespace SmartImage.Lib.Engines;
#nullable enable

public abstract class BaseSearchEngine : IDisposable
{
	public const int NA_SIZE = -1;

	/// <summary>
	/// The corresponding <see cref="SearchEngineOptions"/> of this engine
	/// </summary>
	public abstract SearchEngineOptions EngineOption { get; }

	/// <summary>
	/// Name of this engine
	/// </summary>
	public virtual string Name => EngineOption.ToString();

	public virtual Url BaseUrl { get; }

	protected TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(3);

	protected long MaxSize { get; set; } = NA_SIZE;

	protected BaseSearchEngine(string baseUrl)
	{
		BaseUrl = baseUrl;
	}

	static BaseSearchEngine() { }

	public override string ToString()
	{
		return $"{Name}: {BaseUrl} {Timeout}";
	}

	protected virtual bool Verify(SearchQuery q)
	{
		if (q.Upload is not { }) {
			return false;
		}

		bool b = q.LoadImage(), b2;

		if (b && OperatingSystem.IsWindows()) {
			b = VerifyImage(q.Image);
		}

		if (MaxSize == NA_SIZE || q.Size == NA_SIZE) {
			b2 = true;
		}

		else b2 = q.Size <= MaxSize;

		return b && b2;
	}

	protected virtual bool VerifyImage(Image i)
	{
		return true;
	}

	public virtual async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken? token = null)
	{
		token ??= CancellationToken.None;

		bool b = Verify(query);

		/*if (!b) {
			throw new SmartImageException($"{query}");
		}*/

		var res = new SearchResult(this)
		{
			RawUrl = await GetRawUrlAsync(query),
			Status = !b ? SearchResultStatus.IllegalInput : SearchResultStatus.None,
			ErrorMessage = $"Engine restriction"
		};

		Debug.WriteLine($"{query} - {res.Status}", nameof(GetResultAsync));

		return res;
	}

	protected virtual ValueTask<Url> GetRawUrlAsync(SearchQuery query)
	{
		//
		Url u = ((BaseUrl + query.Upload));

		return ValueTask.FromResult(u);
	}

	public abstract void Dispose();

	public static readonly BaseSearchEngine[] All =
		ReflectionHelper.CreateAllInAssembly<BaseSearchEngine>(TypeProperties.Subclass).ToArray();
}