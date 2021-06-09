#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Novus.Win32;
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

				ComboFunction = () =>
				{
					var direct = result.PrimaryResult.Direct;

					var ok = direct != null;

					if (ok) {
						var p = WebUtilities.Download(direct!.ToString());
						FileSystem.ExploreFile(p);
					}

					return null;
				},
				//Name = result.Engine.Name,
				Data = result.ToString()
			};

			option.CtrlFunction = () =>
			{
				result.OtherResults.AsParallel().ForAll(x => x.FindDirectImages());

				result.PrimaryResult.UpdateFrom(result.OtherResults.First());

				option.Data = result.ToString();

				return null;
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