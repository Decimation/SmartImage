#nullable disable
using System.Collections.Concurrent;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using AngleSharp.Html.Parser;
using Flurl.Http;
using Kantan.Diagnostics;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp.Formats;
using SmartImage.Lib.Engines.Search;
using SmartImage.Lib.Images;
using SmartImage.Lib.Images.Uni;
using SmartImage.Lib.Model;

namespace SmartImage.Lib.Engines.Results;

public class SearchResultItem : UniImageUri, IComparable<SearchResultItem>, IComparable, IEquatable<SearchResultItem>
{

	/// <summary>
	///     Result containing this result item
	/// </summary>
	[NN]
	[JI]
	public SearchResult Root { get; }

	[CBN]
	[JI]
	public SearchResultItem Parent { get; private set; }

	[MNNW(true, nameof(Parent))]
	public bool HasParent => Parent != null;

	// [MN]
	// [JPN("url")]
	// public Url Url { get; protected set; }

	/// <summary>
	///     Title/caption of this result
	/// </summary>
	[CBN]
	public string Title { get; internal set; }

	/// <summary>
	///     Media source of this result (e.g., anime, movie, game, etc.)
	/// </summary>
	[CBN]
	public string Source { get; internal set; }

	/// <summary>
	///     Image width
	/// </summary>
	public int? Width { get; internal set; }

	/// <summary>
	///     Image height
	/// </summary>
	public int? Height { get; internal set; }

	[MNNW(true, nameof(Width), nameof(Height))]
	public bool HasDimensions => Width.HasValue && Height.HasValue;

	/// <summary>
	///     Artist or author
	/// </summary>
	[CBN]
	public string Artist { get; internal set; }

	/// <summary>
	///     Image description
	/// </summary>
	[CBN]
	[JPN("description")]
	public string Description { get; internal set; }

	/// <summary>
	///     Character(s) depicted in the image
	/// </summary>
	[CBN]
	public string Character { get; internal set; }

	/// <summary>
	///     Site which returned this result
	/// </summary>
	[CBN]
	public string Site { get; internal set; }


	/// <summary>
	///     Timestamp of the image.
	/// </summary>
	public DateTime? Time { get; internal set; }

	/// <summary>
	///     Additional metadata.
	/// </summary>
	[JI]
	public object Metadata { get; internal set; }

	[JI]
	public List<SearchResultItem> ScannedItems { get; }

	[MNNW(true, nameof(ScannedItems))]
	public bool HasScannedItems => ScannedItems?.Count > 0;

	/// <summary>
	/// Whether this is <see cref="SearchResult.RawResultItem"/>
	/// </summary>
	[JI]
	public bool IsRaw { get; }

	[MNNW(true, nameof(Similarity))]
	public bool HasSimilarity => Similarity.HasValue;

	public double Score
	{
		get
		{
			if (IsRaw)
				return 0;

			var s = 0d;

			if (HasImage) {
				s++;
			}

			if (HasHash) {
				s++;
			}

			if (Similarity.HasValue)
				s += Similarity.Value * 0.66d;

			if (Url.IsValid(Url))
				s++;

			if (HasThumbnail)
				s += 2;

			int?[] ir = [Width, Height];
			s += ir.Count(static c => c.HasValue);

			string[] p = [Title, Source, Artist, Description, Character, Site, ThumbnailTitle];
			s += p.Count(static c => !String.IsNullOrWhiteSpace(c));

			if (Time.HasValue)
				s++;

			if (Metadata is not null)
				s++;

			s += ScannedItems.Count;

			return s;
		}
	}

#region

	[CBN]
	public Url Thumbnail { get; internal set; }

	[CBN]
	public ISImage ThumbnailImage { get; internal set; }

	[CBN]
	public string ThumbnailTitle { get; internal set; }

	[MNNW(true, nameof(Thumbnail), nameof(ThumbnailImage))]
	public bool HasThumbnail => Url.IsValid(Thumbnail) && ThumbnailImage != null;

#endregion

	internal SearchResultItem(SearchResult r, bool isRaw = false) : base(null)
	{
		Root         = r;
		Metadata     = null;
		Parent       = null;
		IsRaw        = isRaw;
		ScannedItems = [];
	}

#region

	public override async Task<bool> AllocImageAsync(CancellationToken ct = default)
	{
		if (Url == null) {
			return false;
		}

		if (HasImage) {
			return true;
		}

		bool allocImgOk = false;
		var  allocOk    = await AllocAsync(ct);

		if (allocOk) {
			allocImgOk = await base.AllocImageAsync(ct);
		}

		if (allocImgOk) {
			Width  ??= Image.Width;
			Height ??= Image.Height;

			// Root.Results.Add(this);
		}
		else { }

		return HasImage;
	}


	public async ValueTask<bool> LoadThumbnailAsync(CancellationToken ct = default)
	{
		if (!HasThumbnail) {
			try {
				using var response    = await ImageScanner.GetResponseAsync(Thumbnail, ct);
				var       responseStr = await response.GetStreamAsync();
				ThumbnailImage = await ISImage.LoadAsync(responseStr, ct);
			}
			catch (Exception e) {
				s_logger.LogError(e, "Could not load {Thumb}",Thumbnail);
			}
		}

		return HasThumbnail;
	}

	public async ValueTask<bool> ScanAsync(CancellationToken ct = default)
	{
		if (!(await AllocImageAsync(ct))) {
			// return false;
		}

		if (HasImage || HasScannedItems) {
			return true;
		}

		await using var stream = GetStream();
		using var       sr     = new StreamReader(stream);
		var             str    = await sr.ReadToEndAsync(ct);
		
		var       hp      = new HtmlParser();
		var       urls    = ImageScanner.GetImageUrls(str, Url);
		using var doc     = await hp.ParseDocumentAsync(str);
		var       sriNews = new ConcurrentBag<SearchResultItem>();

		await Parallel.ForEachAsync(urls, ct, async (s, token) =>
		{
			var sriNew = new SearchResultItem(Root, false)
			{
				Parent      = this,
				Url         = s,
				Artist      = Artist,
				Character   = Character,
				Description = Description,
				Title       = Title,
				Site        = Site,
				Source      = Source,
				Time        = Time,
			};

			// var sriNew = CloneToChildWithUrl(s);

			var allocImgOk = await sriNew.AllocImageAsync(token);

			if (allocImgOk) {
				sriNews.Add(sriNew);
			}
			else {
				sriNew?.Dispose();
			}
		});

		// Root.Results.InsertRange(Root.Results.IndexOf(this), sriNews);
		// ScannedItems = sriNews.ToList();

		ScannedItems.AddRange(sriNews);

		return HasScannedItems;
	}

#endregion

	public SearchResultItem CloneToChildWithUrl(Url u)
	{
		var clone = (MemberwiseClone() as SearchResultItem);
		clone.Url    = u;
		clone.Parent = this;
		return clone;
	}

	// public IFlurlResponse Response { get; private set; }

	public override string ToString()
	{
		return
			$"{Url} {Similarity / 100:P} {Artist} {Description} {Site} {Source} {Title} {Character} {Time} {Width}x{Height}";
	}

	public override void Dispose()
	{
		GC.SuppressFinalize(this);
		base.Dispose();
		s_logger.LogDebug("Disposing {Item} of {Name}", Url, Root.Engine.Name);
		ThumbnailImage?.Dispose();

		foreach (var sis in ScannedItems) {
			sis.Dispose();
		}
	}


	#region Relational members

	public virtual bool Equals(SearchResultItem other)
	{
		if (other is null)
			return false;

		if (ReferenceEquals(this, other))
			return true;

		return Root.Equals(other.Root) && Url == other.Url;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Root, Url);
	}

	public int CompareTo(SearchResultItem other)
	{
		if (ReferenceEquals(this, other))
			return 0;

		if (other is null)
			return 1;

		return Nullable.Compare(Similarity, other.Similarity);
	}

	public int CompareTo(object obj)
	{
		if (obj is null)
			return 1;

		if (ReferenceEquals(this, obj))
			return 0;

		return obj is SearchResultItem other
			       ? CompareTo(other)
			       : throw new ArgumentException($"Object must be of type {nameof(SearchResultItem)}");
	}

	public static bool operator <(SearchResultItem left, SearchResultItem right)
	{
		return Comparer<SearchResultItem>.Default.Compare(left, right) < 0;
	}

	public static bool operator >(SearchResultItem left, SearchResultItem right)
	{
		return Comparer<SearchResultItem>.Default.Compare(left, right) > 0;
	}

	public static bool operator <=(SearchResultItem left, SearchResultItem right)
	{
		return Comparer<SearchResultItem>.Default.Compare(left, right) <= 0;
	}

	public static bool operator >=(SearchResultItem left, SearchResultItem right)
	{
		return Comparer<SearchResultItem>.Default.Compare(left, right) >= 0;
	}

#endregion

}