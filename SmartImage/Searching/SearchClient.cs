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

		private readonly string m_imgUrl;

		private SearchResult[] m_results;

		private readonly Thread[] m_threads;

		public SearchClient(string img)
		{
			string auth = SearchConfig.Config.ImgurAuth;
			bool useImgur = !String.IsNullOrWhiteSpace(auth);

			var engines = SearchConfig.Config.SearchEngines;

			if (engines == SearchEngines.None) {
				engines = SearchConfig.ENGINES_DEFAULT;
			}

			m_engines = engines;
			m_imgUrl = Upload(img, useImgur);
			m_threads = CreateSearchThreads();
		}

		private static BaseSauceNaoClient GetSauceNaoClient()
		{
			// SauceNao API works without API key

			// bool apiConfigured = !string.IsNullOrWhiteSpace(SearchConfig.Config.SauceNaoAuth);
			//
			// if (apiConfigured) {
			// 	return new FullSauceNaoClient();
			// }
			// else {
			// 	return new AltSauceNaoClient();
			// }

			return new FullSauceNaoClient();

		}

		/// <summary>
		/// Search results
		/// </summary>
		public ref SearchResult[] Results => ref m_results;

		public void Dispose()
		{
			// Joining each thread isn't necessary as this object is disposed upon program exit
			// Background threads won't prevent program termination

			// foreach (var thread in m_threads) {
			// 	thread.Join();
			// }
		}

		/// <summary>
		/// Starts search
		/// </summary>
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

			m_results = new SearchResult[availableEngines.Length + 1];
			m_results[i] = new SearchResult(ConsoleColor.Gray, "(Original image)", m_imgUrl);

			i++;


			var threads = new List<Thread>();

			foreach (var currentEngine in availableEngines) {

				var resultsCopy = m_results;
				int iCopy = i;

				void RunSearchThread()
				{
					var result = currentEngine.GetResult(m_imgUrl);
					resultsCopy[iCopy] = result;

					// If the engine is priority, open its result in the browser
					if (SearchConfig.Config.PriorityEngines.HasFlag(currentEngine.Engine)) {
						Network.OpenUrl(result.Url);
					}

					ConsoleIO.Status = ConsoleIO.STATUS_REFRESH;
				}

				var t = new Thread((ThreadStart) RunSearchThread)
				{
					Priority = ThreadPriority.Highest,
					IsBackground = true
				};

				threads.Add(t);

				i++;
			}

			return threads.ToArray();


		}

		private static IEnumerable<ISearchEngine> GetAllEngines()
		{
			var engines = new ISearchEngine[]
			{
				//
				GetSauceNaoClient(),
				new IqdbClient(),
				new YandexClient(),
				new TraceMoeClient(),

				//
				new ImgOpsClient(),
				new GoogleImagesClient(),
				new TinEyeClient(),
				new BingClient(),
				new KarmaDecayClient(),
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