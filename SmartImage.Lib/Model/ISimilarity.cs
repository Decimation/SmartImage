// Author: Deci | Project: SmartImage.Lib | Name: ISimilarity.cs
// Date: 2024/11/13 @ 16:11:26

using CoenM.ImageHash;

namespace SmartImage.Lib.Model;

#pragma warning disable CS0168

public interface ISimilarity
{

	public double? Similarity { get; }

	[MNNW(true, nameof(Similarity))]
	public bool HasSimilarity => Similarity.HasValue;

	[MNN(nameof(IHashable.Hash.Value))]
	public static double CalculateHashSimilarity<THashable>(THashable a, THashable b) where THashable : IHashable
	{
		if (a.HasHash && b.HasHash) {
			return CompareHash.Similarity(a.Hash.Value, b.Hash.Value);
		}

		throw new InvalidOperationException();
	}

	public bool CalculateSimilarity(IHashable hashable);

}