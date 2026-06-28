// Author: Deci | Project: SmartImage.Lib | Name: ISimilarity.cs
// Date: 2024/11/13 @ 16:11:26

using System.Diagnostics.CodeAnalysis;
using CoenM.ImageHash;
using SmartImage.Lib.Engines.Results;

namespace SmartImage.Lib.Model;

public interface ISimilarity
{

	public double? Similarity { get; set; }

	[MNNW(true, nameof(Similarity))]
	public bool HasSimilarity => Similarity is not null;

	public bool TryCalculateSimilarity(IHashable hashable)
	{
		if (this is IHashable h) {
			Similarity = CompareHash.Calculate(h, hashable);
		}

		return HasSimilarity;
	}

	/// <summary>
	/// Scalar for <see cref="ISimilarity.Similarity"/> when implementing <see cref="IMetadataScore"/>
	/// </summary>
	public const double SIMILARITY_SCORE_SCALAR = 0.66D;

}


public static class SimilarityExtensions
{

	extension(CompareHash)
	{


		public static double? Calculate(IHashable a, IHashable b)
		{
			if (a.Hash is { } ah && b.Hash is { } bh) {

				return CompareHash.Similarity(ah, bh);
			}

			return null;
		}
	}
	

}