using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Kantan.Cli;
using Kantan.Diagnostics;
using Kantan.Net;
using Kantan.Utilities;
using Novus.Win32;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Searching;
using SmartImage.Lib.Utilities;

namespace SmartImage.UX
{
	internal static class InterfaceElements
	{
		internal static readonly Color ColorMain  = Color.Yellow;
		internal static readonly Color ColorOther = Color.Aquamarine;
		internal static readonly Color ColorYes   = Color.GreenYellow;
		internal static readonly Color ColorNo    = Color.Red;

		internal static readonly Dictionary<SearchEngineOptions, Color> EngineColorMap = new()
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

		internal const string Description = "Press the result number to open in browser\n" +
		                                    "Ctrl: Load direct | Alt: Show other | Shift: Open raw | Alt+Ctrl: Download";

		private static readonly string Enabled = StringConstants.CHECK_MARK.ToString().AddColor(ColorYes);

		private static readonly string Disabled = StringConstants.MUL_SIGN.ToString().AddColor(ColorNo);

		internal static string ToToggleString(this bool b) => b ? Enabled : Disabled;

		internal static string GetName(string s, bool added) => $"{s} ({(added.ToToggleString())})";

		#region Result options

		internal static NConsoleOption CreateResultOption(SearchResult result)
		{
			var color = InterfaceElements.EngineColorMap[result.Engine.EngineOption];

			var option = new NConsoleOption
			{
				Function = CreateOpenFunction(result.PrimaryResult is {Url: { }}
					? result.PrimaryResult.Url
					: result.RawUri),

				AltFunction = () =>
				{
					if (result.OtherResults.Any()) {

						var options = CreateResultOptions(result.OtherResults, $"Other result");

						NConsole.ReadOptions(new NConsoleDialog
						{
							Options = options
						});
					}

					return null;
				},

				ComboFunction = CreateDownloadFunction(result.PrimaryResult),
				ShiftFunction = CreateOpenFunction(result.RawUri),

				Color = color,

				Name = result.Engine.Name,
				Data = result,
			};

			option.CtrlFunction = () =>
			{
				var cts = new CancellationTokenSource();

				NConsoleProgress.Queue(cts);

				result.OtherResults.AsParallel().ForAll(f => f.FindDirectImages());

				result.PrimaryResult = result.OtherResults.First();


				cts.Cancel();
				cts.Dispose();

				option.Data = result;

				return null;
			};


			return option;
		}

		[StringFormatMethod("n")]
		internal static NConsoleOption[] CreateResultOptions(IEnumerable<ImageResult> result, string n, Color c = default)
		{
			if (c == default) {
				c = InterfaceElements.ColorOther;
			}

			int i = 0;
			return result.Select(r => CreateResultOption(r, $"{n} #{i++}", c)).ToArray();
		}

		internal static NConsoleOption CreateResultOption(ImageResult result, string n, Color c,
		                                                  float correction = -.3f)
		{
			var option = new NConsoleOption
			{
				Function      = CreateOpenFunction(result.Url),
				ComboFunction = CreateDownloadFunction(result),
				Color         = c.ChangeBrightness(correction),
				Name          = n,
				Data          = result
			};

			return option;
		}

		internal static NConsoleFunction CreateOpenFunction(Uri url)
		{
			return () =>
			{
				if (url != null) {
					WebUtilities.OpenUrl(url.ToString());
				}

				return null;
			};
		}

		internal static NConsoleFunction CreateDownloadFunction(ImageResult result)
		{
			return () =>
			{
				var direct = result.Direct;


				if (direct != null) {

					var file = ImageHelper.Download(direct);

					FileSystem.ExploreFile(file);

					Debug.WriteLine($"Download: {file}", LogCategories.C_INFO);
				}

				return null;
			};
		}

		#endregion
	}
}