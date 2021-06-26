#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Novus.Win32;
using SimpleCore.Cli;
using SimpleCore.Net;
using SimpleCore.Utilities;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Searching;
using SmartImage.Lib.Utilities;

namespace SmartImage.Core
{
	/// <summary>
	/// Creates <see cref="NConsoleOption"/> for objects
	/// </summary>
	public static class NConsoleFactory
	{
		public static NConsoleOption Create(SearchResult result)
		{
			var color = EngineColorMap[result.Engine.EngineOption];

			var option = new NConsoleOption
			{
				Function = CreateOpenFunction(result.PrimaryResult is {Url: { }}
					? result.PrimaryResult.Url
					: result.RawUri),

				AltFunction = () =>
				{
					if (result.OtherResults.Any()) {

						int i = 0;

						var options = result.OtherResults
						                    .Select(r => Create(r, i++, color)).ToArray();


						NConsole.ReadOptions(new NConsoleDialog
						{
							Options = options
						});
					}

					return null;
				},

				ComboFunction = CreateComboFunction(result.PrimaryResult),
				ShiftFunction = CreateOpenFunction(result.RawUri),

				Color = color,

				Name = result.Engine.Name,
				Data = result,
			};

			option.CtrlFunction = () =>
			{
				var cts = new CancellationTokenSource();

				NConsoleProgress.Queue(cts);

				result.OtherResults.AsParallel().ForAll(f => f.FindDirectImagesAsync());

				result.PrimaryResult = result.OtherResults.First();
				

				cts.Cancel();
				cts.Dispose();

				option.Data = result;

				return null;
			};


			return option;
		}

		private static NConsoleOption Create(ImageResult result, int i, Color c)
		{

			const float correctionFactor = -.3f;

			var option = new NConsoleOption
			{
				Function      = CreateOpenFunction(result.Url),
				ComboFunction = CreateComboFunction(result),
				Color         = c.ChangeBrightness(correctionFactor),
				Name          = $"Other result #{i}",
				Data          = result
			};

			return option;
		}

		private static NConsoleFunction CreateOpenFunction(Uri? url)
		{
			return () =>
			{
				if (url != null) {
					WebUtilities.OpenUrl(url.ToString());
				}

				return null;
			};
		}


		private static NConsoleFunction CreateComboFunction(ImageResult result)
		{
			return () =>
			{
				var direct = result.Direct;

				if (direct != null) {
					string download = WebUtilities.Download(direct.ToString());
					FileSystem.ExploreFile(download);
				}

				return null;
			};
		}

		private static readonly Dictionary<SearchEngineOptions, Color> EngineColorMap = new()
		{
			{SearchEngineOptions.Iqdb, Color.Pink},
			{SearchEngineOptions.SauceNao, Color.SpringGreen},
			{SearchEngineOptions.Ascii2D, Color.NavajoWhite},
			{SearchEngineOptions.Bing, Color.DeepSkyBlue},
			{SearchEngineOptions.GoogleImages, Color.FloralWhite},
			{SearchEngineOptions.ImgOps, Color.Gray},
			{SearchEngineOptions.KarmaDecay, Color.IndianRed},
			{SearchEngineOptions.Tidder, Color.Orange},
			{SearchEngineOptions.TraceMoe, Color.MediumSlateBlue},
			{SearchEngineOptions.Yandex, Color.OrangeRed},
			{SearchEngineOptions.TinEye, Color.CornflowerBlue},
		};
	}
}