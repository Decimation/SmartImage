// Author: Deci | Project: SmartImage.Lib | Name: ISimilarity.cs
// Date: 2024/11/13 @ 16:11:26

using CoenM.ImageHash;
using SmartImage.Lib.Images;

namespace SmartImage.Lib.Model;

#pragma warning disable CS0168

public interface ISimilarity
{

	public double? Similarity { get; }


	public static double CalculateHashSimilarity<T>(T a, T b) where T : IHash, ISimilarity
	{
		if (a.HasHash && b.HasHash) {
			return CompareHash.Similarity(a.Hash.Value, b.Hash.Value);
		}

		throw new InvalidOperationException();
	}

}