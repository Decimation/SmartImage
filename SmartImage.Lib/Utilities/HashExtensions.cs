// Author: Deci | Project: SmartImage.Lib | Name: HashExtensions.cs
// Date: 2026/08/01 @ 00:08:31

using CoenM.ImageHash;
using SmartImage.Lib.Model;

namespace SmartImage.Lib.Utilities;

public static class HashExtensions
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