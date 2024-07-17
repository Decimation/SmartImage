// Author: Deci | Project: SmartImage.Lib | Name: GenericImageFilter.cs
// Date: 2024/05/02 @ 11:05:10

using System.Diagnostics;
using SmartImage.Lib.Images.Uni;

namespace SmartImage.Lib.Images;

public class GenericImageFilter : IImageFilter
{

	// TODO

	public string[] Blacklist
		=>
		[
			"thumbnail", "avatar", "error", "logo"
		];

	public bool Predicate(UniImage us)
	{
		try {
			if (us.Stream.Length <= 25_000 || us.ImageFormat.DefaultMimeType == null) {
				return false;
			}

			return true;
		}
		catch (Exception e) {
			Debug.WriteLine($"{e.Message}", nameof(Predicate));
			return true;
		}
	}

	public static readonly IImageFilter Instance = new GenericImageFilter();

	public bool Refine(string b)
	{
		if (!Url.IsValid(b)) {
			return false;
		}

		var u  = Url.Parse(b);
		var ps = u.PathSegments;

		if (ps.Any()) {
			return !Blacklist.Any(i => ps.Any(p => p.Contains(i, StringComparison.InvariantCultureIgnoreCase)));
		}

		return true;
	}

}