#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleCore.Cli;
using SimpleCore.Net;
using SimpleCore.Utilities;
using SmartImage.Lib.Searching;
using SmartImage.Lib.Utilities;

namespace SmartImage.Core
{
	internal static class DialogUtilities
	{
		internal static NConsoleOption Convert(SearchResult result)
		{
			var option = new NConsoleOption
			{
				Function = CreateFunction(result.PrimaryResult),
				AltFunction = () =>
				{
					if (result.OtherResults.Any()) {
						//var x=NConsoleOption.FromArray(result.OtherResults.ToArray());

						var options = result.OtherResults.Select(Convert).ToArray();

						NConsole.ReadOptions(new NConsoleDialog
						{
							Options = options
						});
					}

					return null;
				},
				/*CtrlFunction = () =>
				{
					var flatten = new List<ImageResult>()
					{
						result.PrimaryResult,
					};
					flatten.AddRange(result.OtherResults);

					flatten = flatten.Where(f => f.Url != null).ToList();

					//var direct = flatten.AsParallel().SelectMany(x => ImageHelper.FindDirectImages(x?.Url?.ToString()));



					Parallel.ForEach(flatten, f =>
					{
						f.FindDirectImages();
					});



					foreach (var s in flatten) {
						Debug.WriteLine($"{s.Direct}");
					}

					

					return null;
				},*/
				//Name = result.Engine.Name,
				Data = result.ToString()
			};

			return option;
		}

		private static NConsoleFunction CreateFunction(ImageResult? primaryResult)
		{
			return () =>
			{
				if (primaryResult is { }) {
					var url = primaryResult.Url;

					if (url != null) {
						WebUtilities.OpenUrl(url.ToString());
					}
				}

				return null;
			};
		}

		private static NConsoleOption Convert(ImageResult r)
		{
			var option = new NConsoleOption
			{
				Function = CreateFunction(r),
				Name     = $"Other result\n\b",
				//Data     = r.ToString().Replace("\n", "\n\t"),
				Data = r.ToString(true)
			};

			return option;
		}
	}
}