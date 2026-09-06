// Author: Deci | Project: SmartImage.Lib | Name: SearchResultItem.cs
// Date: 2026/02/28 @ 19:02:49

using CoenM.ImageHash;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using SmartImage.Lib.Images;
using SmartImage.Lib.Images.Alloc;
using SmartImage.Lib.Model;
using SmartImage.Lib.Utilities;
using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using System.Threading.Channels;
using System.Xml.Linq;
using SixLabors.ImageSharp.Formats;

namespace SmartImage.Lib.Engines.Results;

public record SearchResultItem : IResultItem, IComparable<SearchResultItem>, IComparable, IComparisonOperators<SearchResultItem, SearchResultItem, bool>,
                                 IMetadataScore, IScannableItem
{

	internal SearchResultItem(SearchResult r, bool isRaw = false)
	{
		Root         = r;
		ExtraData     = null;
		IsRaw        = isRaw;
		ScannedItems = [];
	}

	private static readonly ILogger s_logger = AppSupport.Factory.CreateLogger(nameof(SearchResultItem));


	/// <summary>
	///     Whether this is <see cref="SearchResult.RawResultItem" />
	/// </summary>
	[JI]
	public bool IsRaw { get; }

	[MN]
	[JPN("url")]
	public Url Url { get; protected internal set; }

	/// <summary>
	///     Title/caption of this result
	/// </summary>
	public string Title { get; internal set; }

	/// <summary>
	///     Media source of this result (e.g., anime, movie, game, etc.)
	/// </summary>
	public string Source { get; internal set; }

	/// <summary>
	///     Image width
	/// </summary>
	public int? Width { get; set; }

	/// <summary>
	///     Image height
	/// </summary>
	public int? Height { get; set; }

	[MNNW(true, nameof(Width), nameof(Height))]
	public bool HasDimensions => Width is not null && Height is not null;

	/// <summary>
	///     Artist or author
	/// </summary>
	public string Artist { get; internal set; }

	/// <summary>
	///     Image description
	/// </summary>
	[JPN("description")]
	public string Description { get; internal set; }

	/// <summary>
	///     Character(s) depicted in the image
	/// </summary>
	public string Character { get; internal set; }

	/// <summary>
	///     Site which returned this result
	/// </summary>
	public string Site { get; internal set; }


	/// <summary>
	///     Timestamp of the image.
	/// </summary>
	public DateTime? Time { get; internal set; }


	public double? Similarity { get; set; }

	[MNNW(true, nameof(Similarity))]
	public bool HasSimilarity => Similarity is not null;

	public ulong? Hash { get; internal set; }

	[MNNW(true, nameof(Hash))]
	public bool HasHash => Hash is not null;

	// public int Index => Root.Results.IndexOf(this);

	/// <summary>
	///     Result containing this result item
	/// </summary>
	[NN]
	[JI]
	public SearchResult Root { get; }

	public virtual double Score
	{
		get
		{
			// todo: revise

			if (IsRaw)
				return -1;

			double s = 0;

			/*if (this is IImage {HasImage: true}) {
				s++;
			}*/

			if (HasHash)
				s++;

			if (HasSimilarity)
				s += (double) Similarity * ISimilarity.SIMILARITY_SCORE_SCALAR;

			if (Url.IsValid(Url))
				s++;

			if (HasThumbnail)
				s += 2;

			if (HasDimensions)
				s += 2;

			string[] p = [Title, Source, Artist, Description, Character, Site, ThumbnailTitle];
			s += p.Count(static c => !String.IsNullOrWhiteSpace(c));

			if (Time is not null)
				s++;

			if (ExtraData is not null)
				s++;

			s += ScannedItems.Count;

			return s;
		}
	}

	/// <summary>
	///     Additional metadata.
	/// </summary>
	[JI]
	public object ExtraData { get; internal set; }

#region

	[CBN]
	[field: CBN]
	public Url Thumbnail
	{
		get;
		internal set
		{
			if (SetField(ref field, value)) {
				OnPropertyChanged(nameof(HasThumbnail));
			}
		}
	}

	[CBN]
	[field: CBN]
	public ImImage ThumbnailImage
	{
		get;
		internal set
		{
			if (SetField(ref field, value)) {
				OnPropertyChanged(nameof(HasThumbnail));
			}
		}
	}

	[CBN]
	public string ThumbnailTitle { get; internal set; }

	[MNNW(true, nameof(Thumbnail), nameof(ThumbnailImage))]
	public bool HasThumbnail => Url.IsValid(Thumbnail) && ThumbnailImage != null;

	[MNNW(true, nameof(Thumbnail))]
	public async ValueTask<bool> LoadThumbnailAsync(CancellationToken ct = default)
	{
		if (!HasThumbnail) {
			try {
				using var response    = await ImageScanner.GetResponseAsync(Thumbnail, ct);
				var       responseStr = await response.GetStreamAsync();
				ThumbnailImage = await ImImage.LoadAsync(responseStr, ct);
			}
			catch (Exception e) {
				s_logger.LogError(e, "Could not load {Thumb}", Thumbnail);
			}
		}

		return HasThumbnail;
	}

#endregion

#region Scanning

	public List<ScannedResultItem> ScannedItems { get; }

	[MNNW(true, nameof(ScannedItems))]
	public bool HasScannedItems => ScannedItems is { Count: > 0 };

	public virtual async ValueTask<bool> ScanAsync(CancellationToken ct = default)
	{
		if (HasScannedItems) {
			return true;
		}

		var ch = Channel.CreateUnbounded<ScannedResultItem>(new UnboundedChannelOptions()
		{
			SingleWriter = true,
		});

		var task =  ImageScanner.ScanAsync(this, ch.Writer, ct);

		while (await ch.Reader.WaitToReadAsync(ct)) {
			if (ch.Reader.TryRead(out var scnItem)) {
				ScannedItems.Add(scnItem);
			}
		}

		var b = await task;

		return b;
	}

#endregion


	public virtual bool TryCalculateSimilarity(IHashable hashable)
	{
		Similarity = CompareHash.Calculate(this, hashable);

		return HasSimilarity;
	}

	protected virtual bool PrintMembers(StringBuilder builder)
	{
		return false;
	}

	public override string ToString()
	{
		return
			$"{Url} {Similarity / 100:P} {Artist} {Description} {Site} {Source} {Title} {Character} {Time} {Width}x{Height}";
	}

	public void Dispose()
	{
		GC.SuppressFinalize(this);

		s_logger.LogDebug("Disposing {Item} of {Name}", Url, Root.Engine.Name);
		ThumbnailImage?.Dispose();

		foreach (var scnItem in ScannedItems) {
			scnItem.Dispose();
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

#region

	public event PropertyChangedEventHandler PropertyChanged;

	private void OnPropertyChanged([CMN] string propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	private bool SetField<T>(ref T field, T value, [CMN] string propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, value))
			return false;

		field = value;
		OnPropertyChanged(propertyName);
		return true;
	}

#endregion

}