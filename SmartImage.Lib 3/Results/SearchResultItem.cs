using System.Runtime.CompilerServices;

using System.Collections;
using System.Drawing;
using System.Dynamic;
using Flurl.Http;
using JetBrains.Annotations;
using Kantan.Net.Utilities;
using Novus.FileTypes;
using Novus.FileTypes.Uni;
using SmartImage.Lib.Images;

[assembly: InternalsVisibleTo("SmartImage.Lib Unit Test")]
namespace SmartImage.Lib.Results;

public sealed record SearchResultItem : IDisposable, IComparable<SearchResultItem>, IComparable
{

	private bool m_isScored;

	public const int MAX_SCORE = 13;

	public const int SCORE_THRESHOLD = MAX_SCORE / 2;

	/// <summary>
	///     Result containing this result item
	/// </summary>
	[NN]
	public SearchResult Root { get; }

	[MN]
	public Url Url { get; internal set; }

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

	/// <summary>
	///     Artist or author
	/// </summary>
	[CBN]
	public string Artist { get; internal set; }

	/// <summary>
	///     Image description
	/// </summary>
	[CBN]
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
	public object Metadata { get; internal set; }

	public int Score { get; private set; }

	[CBN]
	public Url Thumbnail { get; internal set; }

	[CBN]
	public string ThumbnailTitle { get; internal set; }

	public BinaryImageFile[] Uni { get; internal set; }

	public bool HasUni => Uni != null && Uni.Any();

	[CBN]
	public SearchResultItem Parent { get; internal set; }

	// public List<SearchResultItem> Children { get; internal set; }

	// public bool IsUniType { get; internal set; }

	public bool IsRaw { get; internal set; }

	public bool Equals(SearchResultItem other)
	{
		if (ReferenceEquals(null, other)) return false;
		if (ReferenceEquals(this, other)) return true;

		return Root.Equals(other.Root) && Uni == other.Uni;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Root, Uni);
	}

	internal SearchResultItem(SearchResult r)
	{
		Root       = r;
		Metadata   = null;
		m_isScored = false;
		Uni        = null;
		Parent     = null;
		IsRaw      = false;

		// Children   = [];
	}

	/*
	internal SearchResultItem[] FromUni()
	{
		if (!HasUni) {
			return null;
		}

		var u = new SearchResultItem[Uni.Length];

		for (int i = 0; i < u.Length; i++) {
			u[i] = new SearchResultItem(this)
			{
				Uni = null,
				IsUniType = true
			};
		}

		return u;
	}
	*/

	public void UpdateScore()
	{
		if (m_isScored) {
			return;
		}

		if (Url.IsValid(Url)) {
			Score++;
		}

		var a = new string[]
			{ Source, Artist, Character, Description, Title, Site, (string) Thumbnail, ThumbnailTitle };
		Score += a.Count(s => !string.IsNullOrWhiteSpace(s));

		var b = new[] { Similarity, Width, Height, };
		Score += b.Count(d => d.HasValue);

		if (Time.HasValue) {
			Score++;
		}

		Score += Metadata switch
		{
			ICollection c => c.Count,
			string s      => string.IsNullOrWhiteSpace(s) ? 0 : 1,
			_             => 0
		};

		if (Uni is { }) {
			Score++;
		}

		m_isScored = true;
	}

	/*
	public void AddChildren(string[] rg)
	{
		for (int i = 0; i < rg.Length; i++) {

			var sri = new SearchResultItem(this)
			{
				Url      = rg[i],
				Children = [],
				Parent   = this
			};

			Children.Add(sri);
		}

	}
	*/
	public SearchResultItem[] AddChildren(string[] rg)
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

	// [MustUseReturnValue]
	public async Task<bool> ScanAsync(CancellationToken ct = default)
	{

		if (HasUni) {
			return true;
		}

		if (Url == null) {
			return false;
		}

		// Uni = await UniSource.TryGetAsync(Url, ct: ct, whitelist: FileType.Image);
		Uni = await ImageScanner.ScanImagesAsync(Url, ct: ct);
		return HasUni;
	}

	// public IFlurlResponse Response { get; private set; }

	public override string ToString()
	{
		return
			$"{Url} {Similarity / 100:P} {Artist} {Description} {Site} {Source} {Title} {Character} {Time} {Width}x{Height}";
	}

	public void Dispose()
	{
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
		if (ReferenceEquals(this, other)) return 0;
		if (ReferenceEquals(null, other)) return 1;

		return Nullable.Compare(Similarity, other.Similarity);
	}

	public int CompareTo(object obj)
	{
		if (ReferenceEquals(null, obj)) return 1;
		if (ReferenceEquals(this, obj)) return 0;

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