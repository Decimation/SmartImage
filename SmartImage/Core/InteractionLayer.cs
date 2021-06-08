#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleCore.Cli;
using SimpleCore.Net;
using SmartImage.Lib.Searching;

namespace SmartImage.Core
{
	internal static class InteractionLayer
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
						Network.OpenUrl(url.ToString());
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