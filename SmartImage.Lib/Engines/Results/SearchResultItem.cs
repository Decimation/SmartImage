#nullable disable
using System.Collections.Concurrent;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Flurl.Http;
using Kantan.Diagnostics;
using SixLabors.ImageSharp.Formats;
using SmartImage.Lib.Images;
using SmartImage.Lib.Images.Uni;
using SmartImage.Lib.Model;

namespace SmartImage.Lib.Engines.Results;

public class SearchResultItem : UniImageUri, IComparable<SearchResultItem>, IComparable, ISimilarity, IEquatable<SearchResultItem>, IDisposable,
                                IHashable,
                                IImageSource
{

	/// <summary>
	///     Result containing this result item
	/// </summary>
	[NN]
	[JI]
	public SearchResult Root { get; }

	[JI]
	[CBN]
	public SearchResultItem Parent { get; internal set; }

	// [MN]
	// [JPN("url")]
	// public Url Url { get; protected set; }

	/// <summary>
	///     Title/caption of this result
	/// </summary>
	[CBN]
	[JPN("title")]
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

	public override long? Size => base.Size;

	/// <summary>
	/// <see cref="SearchResult.RawResultItem"/>
	/// </summary>
	public bool IsRaw { get; }

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

			return s;
		}
	}

	internal SearchResultItem(SearchResult r, bool isRaw = false) : base(null)
	{
		Root     = r;
		Metadata = null;
		Parent   = null;
		IsRaw    = isRaw;

		// EmbeddedUrls = null;

		// Children   = [];
	}


	/*public async ValueTask<bool> HashAsync(SearchQuery query, int idx = 0, CancellationToken ct = default)
	{
		if (!HasUni) {
			return false;
		}

		if ((idx < 0 || idx > Uni.Length)) {
			return false;
		}

		var ui = Uni[idx];

		try {
			query.Uni.TryCalculateHash();
			ui.TryCalculateHash();
			Similarity = CompareHash.Similarity(query.Uni.Hash.Value, ui.Hash.Value);
		}
		catch (Exception e) {
			Trace.WriteLine($"{e}");
		}
		return true;
	}*/


	public override async Task<bool> AllocImageAsync(CancellationToken ct = default)
	{
		if (Url == null) {
			return false;
		}

		var ok = await base.AllocImageAsync(ct);

		if (ok) {
			Width  ??= Image.Width;
			Height ??= Image.Height;

		}
		else {
			
		}

		return ok;
	}

	public override bool CalculateSimilarity(IHashable hashable)
	{
		return base.CalculateSimilarity(hashable);
	}


	public async ValueTask<bool> LoadThumbnailAsync(CancellationToken ct = default)
	{
		if (Url.IsValid(Thumbnail) && ThumbnailImage != null) {
			using var response    = await ImageScanner.GetResponseAsync(Thumbnail, ct);
			var       responseStr = await response.GetStreamAsync();
			ThumbnailImage = await ISImage.LoadAsync(responseStr, ct);

		}

		return HasThumbnail;
	}

	// public IFlurlResponse Response { get; private set; }

	public override string ToString()
	{
		return
			$"{Url} {Similarity / 100:P} {Artist} {Description} {Site} {Source} {Title} {Character} {Time} {Width}x{Height}";
	}

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

	public override void Dispose()
	{
		GC.SuppressFinalize(this);
		Debug.WriteLine($"Disposing {Url} of {Root.Engine.Name}", LogCategories.C_VERBOSE);
		base.Dispose();
		ThumbnailImage?.Dispose();

		/*foreach (var sis in Sisters) {

			sis.Dispose();
		}*/
	}

#region Relational members

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