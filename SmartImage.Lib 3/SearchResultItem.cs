using System.Dynamic;
using Flurl;
using Kantan.Model;
using Novus.FileTypes;

namespace SmartImage.Lib;

public record SearchResultItem : IDisposable
{
	[NN]
	public SearchResult Root { get; }

	[MN]
	public Url Url { get; internal set; }

	/// <summary>
	/// Title/caption of this result.
	/// </summary>
	[CBN]
	public string Title { get; internal set; }

	/// <summary>
	/// Media source of this result (e.g., anime, movie, game, etc.).
	/// </summary>
	[CBN]
	public string Source { get; internal set; }

	/// <summary>
	/// Image width.
	/// </summary>
	public double? Width { get; internal set; }

	/// <summary>
	/// Image height.
	/// </summary>
	public double? Height { get; internal set; }

	/// <summary>
	/// Artist or author.
	/// </summary>
	[CBN]
	public string Artist { get; internal set; }

	/// <summary>
	/// Image description.
	/// </summary>
	[CBN]
	public string Description { get; internal set; }

	/// <summary>
	/// Character(s) depicted in the image.
	/// </summary>
	[CBN]
	public string Character { get; internal set; }

	/// <summary>
	/// Site which returned this result.
	/// </summary>
	[CBN]
	public string Site { get; internal set; }

	/// <summary>
	/// Percent similarity to query (<see cref="SearchQuery"/>).
	/// </summary>
	/// <remarks>The algorithm used to determine the similarity
	/// may not be consistent across results.</remarks>
	public double? Similarity { get; internal set; }

	/// <summary>
	/// Timestamp of the image.
	/// </summary>
	public DateTime? Time { get; internal set; }

	/// <summary>
	/// Extraneous/additional metadata.
	/// </summary>
	public dynamic Metadata { get; internal set; }

	internal SearchResultItem(SearchResult r)
	{
		Root     = r;
		Metadata = new ExpandoObject();
	}

	public override string ToString()
	{
		return $"{Url} {Similarity / 100:P} {Artist} {Description} {Site} {Source} {Title} {Character} {Time}";
	}

	public void Dispose()
	{
		
	}

	public static bool Validate([CBN] SearchResultItem r)
	{
		return r switch
		{
			not { } => false,
			_       => true
		};
	}
}