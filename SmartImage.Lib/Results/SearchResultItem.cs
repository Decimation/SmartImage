using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Drawing;
using System.Dynamic;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using CoenM.ImageHash.HashAlgorithms;
using Flurl.Http;
using JetBrains.Annotations;
using Kantan.Diagnostics;
using Kantan.Net.Utilities;
using Novus.FileTypes;
using Novus.FileTypes.Uni;
using SmartImage.Lib.Images;
using SmartImage.Lib.Images.Uni;
using CoenM.ImageHash;
using Novus.Streams;
using SmartImage.Lib.Results.Data;
using SmartImage.Lib.Utilities;

#nullable disable
namespace SmartImage.Lib.Results;

public record SearchResultItem : IDisposable, IComparable<SearchResultItem>, IComparable, ISimilarity, IEquatable<SearchResultItem>
{

	/// <summary>
	///     Result containing this result item
	/// </summary>
	[NN]
	[JI]
	public SearchResult Root { get; }

	[CBN]
	[JI]
	public SearchResultItem Parent { get; internal set; }

	[MN]
	[JsonPropertyName("url")]
	public Url Url { get; internal set; }

	/// <summary>
	///     Title/caption of this result
	/// </summary>
	[CBN]
	[JsonPropertyName("title")]
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
	[JsonPropertyName("description")]
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
	///     Percent similarity to query (<see cref="SearchQuery" />).
	/// </summary>
	/// <remarks>
	///     The algorithm used to determine the similarity
	///     may not be consistent across results.
	/// </remarks>
	public double? Similarity { get; internal set; }

	/// <summary>
	///     Timestamp of the image.
	/// </summary>
	public DateTime? Time { get; internal set; }

	/// <summary>
	///     Additional metadata.
	/// </summary>
	[JI]
	public object Metadata { get; internal set; }

	[CBN]
	public Url Thumbnail { get; internal set; }

	[CBN]
	public string ThumbnailTitle { get; internal set; }

	// [MN]
	[JI]
	public List<UniImage> Uni { get; }

	[JI]
	[MNNW(true, nameof(Uni))]
	public bool HasUni => Uni is { Count: > 0 };

	/// <summary>
	/// <see cref="SearchResult.RawResultItem"/>
	/// </summary>
	public bool IsRaw { get; }

	internal SearchResultItem(SearchResult r, bool isRaw = false)
	{
		Root     = r;
		Metadata = null;
		Parent   = null;
		IsRaw    = isRaw;
		Uni      = new List<UniImage>();

		// EmbeddedUrls = null;

		// Children   = [];
	}

	public SearchResultItem[] CreateChildren(string[] rg)
	{
		var rg2 = new SearchResultItem[rg.Length];

		for (int i = 0; i < rg.Length; i++) {

			rg2[i] = new SearchResultItem(this)
			{
				Url = rg[i],

				// Children = [],
				Parent = this,
			};

			// Children.Add(sri);
		}

		return rg2;
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

	public async ValueTask<bool> LoadThumbnail(CancellationToken ct = default)
	{
		if (Url.IsValid(Thumbnail) && !(HasUni && Uni.Any(u => u.ValueString == Thumbnail))) {

			var uni = await UniImage.TryCreateAsync(Thumbnail, ct: ct);

			if (uni == null) {
				return false;
			}

			Uni.Add(uni);
		}

		return true;
	}

	public async Task<bool> ScanAsync(CancellationToken ct = default)
	{
		// TODO: USE CHANNELS
		// TODO: REFACTOR TO USE THIS FUNCTION

		if (HasUni) {
			return true;
		}

		if (Url == null) {
			return false;
		}

		// Uni = await UniSource.TryGetAsync(Url, ct: ct, whitelist: FileType.Image);
		var buf = new ConcurrentBag<UniImage>();

		var ch = Channel.CreateUnbounded<UniImage>(new UnboundedChannelOptions() { SingleReader = true });

		var tasks =  ImageScanner.ScanImagesAsync(Url, ch.Writer, ct: ct);

		while (await ch.Reader.WaitToReadAsync(ct)) {
			var v = await ch.Reader.ReadAsync(ct);

			if (v != UniImage.Null && v.HasImageFormat) {
				buf.Add(v);
			}

			if (ct.IsCancellationRequested) {
				break;
			}
		}

		await tasks;

		Uni.AddRange(buf);
		buf.Clear();

		return HasUni;
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

		return Root.Equals(other.Root) && Url == other.Url && Uni == other.Uni;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Root, Url, Uni);
	}

	public void Dispose()
	{
		Debug.WriteLine($"Disposing {Url} of {Root.Engine.Name}", LogCategories.C_VERBOSE);

		if (Uni != null && Uni.Any()) {
			foreach (var us in Uni) {

				us?.Dispose();
			}

		}

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