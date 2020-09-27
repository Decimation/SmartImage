using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using SimpleCore.Win32.Cli;
using SmartImage.Searching.Engines.Imgur;
using SmartImage.Searching.Engines.SauceNao;
using SmartImage.Searching.Engines.Simple;
using SmartImage.Searching.Engines.TraceMoe;
using SmartImage.Searching.Model;
using SmartImage.Shell;
using SmartImage.Utilities;

namespace SmartImage.Searching
{
	/// <summary>
	/// Searching client
	/// </summary>
	public class SearchClient : IDisposable
	{
		/// <summary>
		///     Common image extensions
		/// </summary>
		private static readonly string[] ImageExtensions =
		{
			".jpg", ".jpeg", ".png", ".gif", ".tga", ".jfif", ".bmp"
		};

		private readonly SearchEngines m_engines;

		private readonly string m_img;

		private readonly string m_imgUrl;

		private ConsoleOption[] m_results;

		private readonly Thread[] m_threads;

		public SearchClient(string img)
		{


			string auth = SearchConfig.Config.ImgurAuth;
			bool useImgur = !String.IsNullOrWhiteSpace(auth);

			var engines = SearchConfig.Config.SearchEngines;
			//var priority = SearchConfig.Config.PriorityEngines;

			if (engines == SearchEngines.None) {
				//todo
				//CliOutput.WriteError("Please configure search engine preferences!");
				engines = SearchConfig.ENGINES_DEFAULT;
			}

			m_engines = engines;


			m_imgUrl = Upload(img, useImgur);


			m_threads = CreateSearchThreads();
		}

		public ref ConsoleOption[] Results => ref m_results;

		public void Dispose()
		{
			foreach (var thread in m_threads) {
				thread.Join();
			}
		}

		public void Start()
		{
			// Display config
			CliOutput.WriteInfo(SearchConfig.Config);

			CliOutput.WriteInfo("Temporary image url: {0}", m_imgUrl);

			Console.WriteLine();

			foreach (var thread in m_threads) {
				thread.Start();
			}
		}


		private Thread[] CreateSearchThreads()
		{
			// todo: improve


			var availableEngines = GetAllEngines()
				.Where(e => m_engines.HasFlag(e.Engine))
				.ToArray();

			int i = 0;

			m_results = new ConsoleOption[availableEngines.Length + 1];
			m_results[i] = new SearchResult(ConsoleColor.Gray, "(Original image)", m_imgUrl);

			i++;


			var threads = new List<Thread>();

			foreach (var currentEngine in availableEngines) {

				var resultsCopy = m_results;
				int iCopy = i;

				ThreadStart threadFunction = () =>
				{
					var result = currentEngine.GetResult(m_imgUrl);
					resultsCopy[iCopy] = result;

					// If the engine is priority, open its result in the browser
					if (SearchConfig.Config.PriorityEngines.HasFlag(currentEngine.Engine)) {
						Network.OpenUrl(result.Url);
					}

					ConsoleIO.Status = ConsoleIO.STATUS_REFRESH;
				};

				var t = new Thread(threadFunction)
				{
					Priority = ThreadPriority.Highest
				};

				threads.Add(t);

				i++;
			}

			return threads.ToArray();


		}

		private static ISearchEngine[] GetAllEngines()
		{
			var engines = new ISearchEngine[]
			{
				new SauceNaoClient(),
				new ImgOpsClient(),
				new GoogleImagesClient(),
				new TinEyeClient(),
				new IqdbClient(),
				new BingClient(),
				new YandexClient(),
				new KarmaDecayClient(),
				new TraceMoeClient()
			};

			return engines;
		}

		internal static bool IsFileValid(string img)
		{
			if (String.IsNullOrWhiteSpace(img)) {
				return false;
			}

			if (!File.Exists(img)) {
				CliOutput.WriteError("File does not exist: {0}", img);
				return false;
			}

			bool extOkay = ImageExtensions.Any(img.ToLower().EndsWith);

			if (!extOkay) {
				return CliOutput.ReadConfirm("File extension is not recognized as a common image format. Continue?");
			}


			return true;
		}

		private static string Upload(string img, bool useImgur)
		{
			string imgUrl;

			if (useImgur) {
				CliOutput.WriteInfo("Using Imgur for image upload");
				var imgur = new ImgurClient();
				imgUrl = imgur.Upload(img);
			}
			else {
				CliOutput.WriteInfo("Using ImgOps for image upload (2 hour cache)");
				var imgOps = new ImgOpsClient();
				imgUrl = imgOps.UploadTempImage(img, out _);
			}


			return imgUrl;
		}
	}
}