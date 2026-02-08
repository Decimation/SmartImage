#nullable disable
using System.Collections.Concurrent;
using System.ComponentModel;
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

// todo: refactor to not inherit from UniImageUrl and instead contain a UniImageUrl

public class SearchResultItem : UniImageUrl, IComparable<SearchResultItem>, IComparable, IEquatable<SearchResultItem>
{

#region

	/// <summary>
	///     Result containing this result item
	/// </summary>
	[NN]
	[JI]
	public SearchResult Root { get; }

	[CBN]
	[JI]
	public SearchResultItem Parent { get; private set; }

	public bool IsCloned { get; private init; }

	[MNNW(true, nameof(Parent))]
	public bool IsChild => Parent != null && Parent != this;

#endregion

	/*
	[MN]
	[JPN("url")]
	public Url Url { get; protected set; }
	*/

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


	/// <summary>
	/// Whether this is <see cref="SearchResult.RawResultItem"/>
	/// </summary>
	[JI]
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

			// s += Root.ScannedResults.Count;

			return s;
		}
	}

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
	public ISImage ThumbnailImage
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

#endregion

	internal SearchResultItem(SearchResult r, bool isRaw = false) : base(null)
	{
		Root     = r;
		Metadata = null;
		Parent   = this;
		IsRaw    = isRaw;

		ScannedItems = [];
	}

#region

	public List<SearchResultItem> ScannedItems { get; }

	[MNNW(true, nameof(ScannedItems))]
	public bool HasScannedItems => ScannedItems is { Count: > 0 };

	[MNNW(true, nameof(Image))]
	public override async ValueTask<bool> AllocImageAsync(CancellationToken ct = default)
	{
		if (Url == null) {
			return false;
		}

		if (HasImage) {
			return true;
		}

		bool allocImgOk = false;
		var  allocOk    = await AllocSourceAsync(ct);

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

	[MNNW(true, nameof(Thumbnail))]
	public async ValueTask<bool> LoadThumbnailAsync(CancellationToken ct = default)
	{
		if (!HasThumbnail) {
			try {
				using var response    = await ImageScanner.GetResponseAsync(Thumbnail, ct);
				var       responseStr = await response.GetStreamAsync();
				ThumbnailImage = await ISImage.LoadAsync(responseStr, ct);
			}
			catch (Exception e) {
				s_logger.LogError(e, "Could not load {Thumb}", Thumbnail);
			}
		}

		return HasThumbnail;
	}

	public async ValueTask<bool> ScanAsync(CancellationToken ct = default)
	{
		if (HasScannedItems) {
			return true;
		}

		var cw = Channel.CreateUnbounded<SearchResultItem>();

		var task = ScanAsync(cw.Writer, s =>
		{
			var clone = PartialCopyCloneWithUrl(s);
			return clone;
		}, ct);

		while (await cw.Reader.WaitToReadAsync(ct)) {
			var val = await cw.Reader.ReadAsync(ct);

			ScannedItems.Add(val);
		}

		var ok = await task;

		return ok;
	}

#endregion

	public SearchResultItem PartialCopyCloneWithUrl(Url s)
	{
		return new SearchResultItem(Root, false)
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
			Metadata    = Metadata,
			IsCloned    = true,
		};
	}

	public SearchResultItem MemberwiseCloneWithUrl(Url u)
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

		foreach (SearchResultItem item in ScannedItems) {
			item.Dispose();
		}

		ScannedItems.Clear();
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

#region Equality members

	/// <inheritdoc />
	public override bool Equals(object obj)
	{
		if (obj is null)
			return false;

		if (ReferenceEquals(this, obj))
			return true;

		if (obj.GetType() != GetType())
			return false;

		return Equals((SearchResultItem) obj);
	}

	public static bool operator ==(SearchResultItem left, SearchResultItem right)
	{
		return Equals(left, right);
	}

	public static bool operator !=(SearchResultItem left, SearchResultItem right)
	{
		return !Equals(left, right);
	}

#endregion

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