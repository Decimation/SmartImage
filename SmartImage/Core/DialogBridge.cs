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
	/// Bridges library objects to interactive interface objects
	/// </summary>
	internal static class DialogBridge
	{
		internal static NConsoleOption CreateOption(SearchResult result)
		{
			var option = new NConsoleOption
			{
				Function = CreateMainFunction(result.PrimaryResult),
				AltFunction = () =>
				{
					if (result.OtherResults.Any()) {
						//var x=NConsoleOption.FromArray(result.OtherResults.ToArray());

						var options = result.OtherResults.Select(CreateOption).ToArray();

						NConsole.ReadOptions(new NConsoleDialog
						{
							Options = options
						});
					}

					return null;
				},
				ComboFunction = CreateComboFunction(result.PrimaryResult),

				Name = result.Engine.Name.AddColor(EngineNameColorMap[result.Engine.EngineOption]),
				Data = result.ToString(false)
			};

			option.CtrlFunction = () =>
			{
				var cts = new CancellationTokenSource();

				NConsoleProgress.Queue(cts);

				result.OtherResults.AsParallel().ForAll(x => x.FindDirectImages());


				result.PrimaryResult.UpdateFrom(result.OtherResults.First());

				cts.Cancel();
				cts.Dispose();

				option.Data = result.ToString();

				return null;
			};

			return option;
		}

		private static NConsoleOption CreateOption(ImageResult r)
		{
			var option = new NConsoleOption
			{
				Function      = CreateMainFunction(r),
				ComboFunction = CreateComboFunction(r),
				Name          = $"Other result\n\b",
				//Data     = r.ToString().Replace("\n", "\n\t"),
				Data = r.ToString(true)
			};

			return option;
		}

		private static NConsoleFunction CreateMainFunction(ImageResult? primaryResult)
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

		private static NConsoleFunction CreateComboFunction(ImageResult result)
		{
			return () =>
			{
				var direct = result.Direct;

				var ok = direct != null;

				if (ok) {
					var p = WebUtilities.Download(direct!.ToString());
					FileSystem.ExploreFile(p);
				}

				return null;
			};
		}

		private static readonly Dictionary<SearchEngineOptions, Color> EngineNameColorMap = new()
		{
			{SearchEngineOptions.Iqdb, Color.SandyBrown},
			{SearchEngineOptions.SauceNao, Color.SpringGreen},
			{SearchEngineOptions.Ascii2D, Color.NavajoWhite},
			{SearchEngineOptions.Bing, Color.DeepSkyBlue},
			{SearchEngineOptions.GoogleImages, Color.Violet},
			{SearchEngineOptions.ImgOps, Color.Gray},
			{SearchEngineOptions.KarmaDecay, Color.Orange},
			{SearchEngineOptions.Tidder, Color.OrangeRed},
			{SearchEngineOptions.TraceMoe, Color.MediumSlateBlue},
			{SearchEngineOptions.Yandex, Color.IndianRed},
			{SearchEngineOptions.TinEye, Color.CornflowerBlue},
		};
	}
}