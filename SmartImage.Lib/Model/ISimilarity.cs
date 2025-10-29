// Author: Deci | Project: SmartImage.Lib | Name: ISimilarity.cs
// Date: 2024/11/13 @ 16:11:26

using System.Diagnostics.CodeAnalysis;
using CoenM.ImageHash;
using SmartImage.Lib.Images;

namespace SmartImage.Lib.Model;

#pragma warning disable CS0168

public interface ISimilarity
{

	public double? Similarity { get; }

	[MNN(nameof(IHashable.Hash.Value))]
	public static double CalculateHashSimilarity<T>(T a, T b) where T : IHashable
	{
		if (a.HasHash && b.HasHash) {
			return CompareHash.Similarity(a.Hash.Value, b.Hash.Value);
		}

		throw new InvalidOperationException();
	}

}