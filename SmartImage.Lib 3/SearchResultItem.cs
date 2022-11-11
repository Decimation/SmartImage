using System.Collections;
using System.Dynamic;
using Flurl;
using Kantan.Model;
using Novus.FileTypes;

namespace SmartImage.Lib;

public sealed record SearchResultItem : IDisposable, IComparable<SearchResultItem>, IComparable
{
	/// <summary>
	/// Result containing this result item
	/// </summary>
	[NN]
	public SearchResult Root { get; }

	[MN]
	public Url Url { get; internal set; }

	/// <summary>
	/// Title/caption of this result
	/// </summary>
	[CBN]
	public string Title { get; internal set; }

	/// <summary>
	/// Media source of this result (e.g., anime, movie, game, etc.)
	/// </summary>
	[CBN]
	public string Source { get; internal set; }

	/// <summary>
	/// Image width
	/// </summary>
	public double? Width { get; internal set; }

	/// <summary>
	/// Image height
	/// </summary>
	public double? Height { get; internal set; }

	/// <summary>
	/// Artist or author
	/// </summary>
	[CBN]
	public string Artist { get; internal set; }

	/// <summary>
	/// Image description
	/// </summary>
	[CBN]
	public string Description { get; internal set; }

	/// <summary>
	/// Character(s) depicted in the image
	/// </summary>
	[CBN]
	public string Character { get; internal set; }

	/// <summary>
	/// Site which returned this result
	/// </summary>
	[CBN]
	public string Site { get; internal set; }

	/// <summary>
	/// Percent similarity to query (<see cref="SearchQuery"/>).
	/// </summary>
	/// <remarks>
	/// The algorithm used to determine the similarity
	/// may not be consistent across results.
	/// </remarks>
	public double? Similarity { get; internal set; }

	/// <summary>
	/// Timestamp of the image.
	/// </summary>
	public DateTime? Time { get; internal set; }

	/// <summary>
	/// Additional metadata.
	/// </summary>
	public dynamic Metadata { get; internal set; }

	public int Score { get; private set; }

	private bool m_isScored;

	internal SearchResultItem(SearchResult r)
	{
		Root       = r;
		Metadata   = new ExpandoObject();
		m_isScored = false;
	}

	public static bool Validate([CBN] SearchResultItem r)
	{
		return r switch
		{
			not { } => false,
			_       => true
		};
	}

	public void UpdateScore()
	{
		if (m_isScored) {
			return;
		}

		if (Url.IsValid(Url)) {
			Score++;
		}

		var a = new[] { Source, Artist, Character, Description, Title, Site };
		Score += a.Count(s => !String.IsNullOrWhiteSpace(s));

		var b = new[] { Similarity, Width, Height, };
		Score += b.Count(d => d.HasValue);

		if (Time.HasValue) {
			Score++;
		}
		
		Score += Metadata switch
		{
			ICollection c => c.Count,
			string s      => String.IsNullOrWhiteSpace(s) ? 0 : 1,
			_ => 0
		};

		m_isScored = true;
	}

	public const int MAX_SCORE = 12;

	public const int SCORE_THRESHOLD = 5;
	public void Dispose()
	{
	}

	public async Task<UniFile> GetUniAsync()
	{
		var uni = await UniFile.TryGetAsync(Url, whitelist: SearchQuery.ImageTypes);
		return uni;
	}

	public override string ToString()
	{
		return $"{Url} {Similarity / 100:P} {Artist} {Description} {Site} {Source} {Title} {Character} {Time}";
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