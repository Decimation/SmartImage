using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using JetBrains.Annotations;
using Kantan.Cli;
using Kantan.Diagnostics;
using Kantan.Net;
using Kantan.Utilities;
using Novus.Utilities;
using Novus.Win32;
using SmartImage.Core;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Searching;
using SmartImage.Lib.Utilities;

// ReSharper disable PossibleNullReferenceException

namespace SmartImage.UI
{
	internal static class NConsoleFactory
	{

		/*
		 * todo: this is all glue code :(
		 */

		internal static NConsoleOption CreateConfigOption(string f, string name)
		{
			return new()
			{
				Name  = name,
				Color = AppInterface.Elements.ColorOther,
				Function = () =>
				{
					var enumOptions = NConsoleOption.FromEnum<SearchEngineOptions>();

					var selected = NConsole.ReadOptions(new NConsoleDialog
					{
						Options        = enumOptions,
						SelectMultiple = true
					});

					var enumValue = Enums.ReadFromSet<SearchEngineOptions>(selected);
					var field     = Program.Config.GetType().GetAnyResolvedField(f);
					field.SetValue(Program.Config, enumValue);

					Console.WriteLine(enumValue);

					NConsole.WaitForSecond();

					Debug.Assert((SearchEngineOptions) field.GetValue(Program.Config) == enumValue);

					AppConfig.UpdateConfig();

					return null;
				}
			};
		}

		internal static NConsoleOption CreateConfigOption(PropertyInfo member, string name, int i, Action<bool> fn)
		{
			bool initVal = (bool) member.GetValue(Program.Config);

			return new NConsoleOption()
			{
				Name = AppInterface.Elements.GetName(name, initVal),
				Function = () =>
				{
					var pi = member.DeclaringType.GetProperty(member.Name);

					bool curVal = (bool) pi.GetValue(null);

					fn(curVal);

					bool newVal = (bool) pi.GetValue(null);

					AppInterface.MainMenuOptions[i].Name = AppInterface.Elements.GetName(name, newVal);

					Debug.Assert((bool) pi.GetValue(null) == newVal);

					return null;
				}
			};
		}

		internal static NConsoleOption CreateConfigOption(string m, string name, int i)
		{
			bool initVal = (bool) (Program.Config).GetType().GetAnyResolvedField(m).GetValue(Program.Config);

			return new NConsoleOption()
			{
				Name = AppInterface.Elements.GetName(name, initVal),
				Function = () =>
				{
					var    fi     = Program.Config.GetType().GetAnyResolvedField(m);
					object curVal = fi.GetValue(Program.Config);
					bool   newVal = !(bool) curVal;
					fi.SetValue(Program.Config, newVal);

					AppInterface.MainMenuOptions[i].Name = AppInterface.Elements.GetName(name, newVal);

					Debug.Assert((bool) fi.GetValue(Program.Config) == newVal);

					AppConfig.UpdateConfig();

					return null;
				}
			};
		}

		internal static NConsoleOption CreateResultOption(SearchResult result)
		{
			var color = AppInterface.Elements.EngineColorMap[result.Engine.EngineOption];

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
				c = AppInterface.Elements.ColorOther;
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
					var path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
					
					var file = ImageHelper.Download(direct, path);

					FileSystem.ExploreFile(file);

					Debug.WriteLine($"Download: {file}", LogCategories.C_INFO);
				}

				return null;
			};
		}
	}
}
