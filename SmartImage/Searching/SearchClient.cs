﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SimpleCore.CommandLine;
using SimpleCore.Win32;
using SmartImage.Searching.Engines.Imgur;
using SmartImage.Searching.Engines.Other;
using SmartImage.Searching.Engines.SauceNao;
using SmartImage.Searching.Engines.TraceMoe;
using SmartImage.Searching.Model;
using SmartImage.Utilities;

// ReSharper disable ConvertIfStatementToReturnStatement

#pragma warning disable HAA0502, HAA0302, HAA0601, HAA0101, HAA0301, HAA0603

namespace SmartImage.Searching
{
	// todo: replace threads with tasks and async


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

		private readonly SearchEngineOptions m_engines;

		private readonly string m_imgUrl;

		private readonly FileInfo m_img;

		private SearchResult[] m_results;

		private readonly Task[] m_tasks;

		private readonly Thread m_monitor;

		public SearchClient(string img)
		{
			string auth = SearchConfig.Config.ImgurAuth;
			bool useImgur = !String.IsNullOrWhiteSpace(auth);

			var engines = SearchConfig.Config.SearchEngines;

			if (engines == SearchEngineOptions.None) {
				engines = SearchConfig.ENGINES_DEFAULT;
			}

			m_engines = engines;
			m_img = new FileInfo(img);
			m_imgUrl = Upload(img, useImgur);
			m_tasks = CreateSearchTasks();

			m_monitor = new Thread(Monitor)
			{
				Priority = ThreadPriority.Highest,
				IsBackground = true
			};
		}

		private void Monitor()
		{
			Task.WaitAll(m_tasks);

			Console.Beep(1000, 100);

			//Array.Sort(m_results, Comparison);

			// todo: wtf

			NConsole.IO.Refresh();
			NConsole.IO.Refresh();
		}

		private static int CompareResults(SearchResult x, SearchResult y)
		{
			// Keep original image at first index
			if (x?.Name == ORIGINAL_IMAGE_NAME || y?.Name == ORIGINAL_IMAGE_NAME) {
				return 1;
			}

			var xSim = x?.Similarity ?? 0;
			var ySim = y?.Similarity ?? 0;

			if (xSim > ySim) {
				return -1;
			}

			if (xSim < ySim) {
				return 1;
			}

			return 0;
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

			// m_monitor.Join();

		}

		/// <summary>
		/// Starts search
		/// </summary>
		public void Start()
		{
			// Display config
			NConsole.WriteInfo(SearchConfig.Config);

			NConsole.WriteInfo("Temporary image url: {0}", m_imgUrl);

			m_monitor.Start();

			foreach (var thread in m_tasks) {
				thread.Start();
			}
		}

		private static (int Width, int Height) GetImageDimensions(string img)
		{
			var bmp = new Bitmap(img);

			return (bmp.Width, bmp.Height);
		}

		private const string ORIGINAL_IMAGE_NAME = "(Original image)";

		private SearchResult GetOriginalImageResult()
		{
			var result = new SearchResult(Color.White, ORIGINAL_IMAGE_NAME, m_imgUrl);

			result.ExtendedInfo.Add(string.Format("Location: {0}", m_img));

			var fileFormat = FileOperations.ResolveFileType(m_img.FullName);

			const float magnitude = 1024f;
			var fileSizeMegabytes = Math.Round(FileOperations.GetFileSize(m_img.FullName) / magnitude / magnitude, 2);

			var dim = GetImageDimensions(m_img.FullName);

			result.Width = dim.Width;
			result.Height = dim.Height;

			var infoStr = string.Format("Info: {0} ({1} MB) ({2})",
				m_img.Name, fileSizeMegabytes, fileFormat);

			result.ExtendedInfo.Add(infoStr);

			return result;
		}


		private Task[] CreateSearchTasks()
		{
			// todo: improve
			// todo: hacky :(

			var availableEngines = GetAllEngines()
				.Where(e => m_engines.HasFlag(e.Engine))
				.ToArray();

			int i = 0;

			m_results = new SearchResult[availableEngines.Length + 1];
			m_results[i] = GetOriginalImageResult();

			i++;


			var threads = new List<Task>();

			foreach (var currentEngine in availableEngines) {

				var resultsCopy = m_results;
				int iCopy = i;


				var task = new Task(RunSearchThread);
				
				void RunSearchThread()
				{
					var result = currentEngine.GetResult(m_imgUrl);

					result.CtrlFunction = () =>
					{
						// todo
						return null;
					};

					resultsCopy[iCopy] = result;

					// If the engine is priority, open its result in the browser
					if (SearchConfig.Config.PriorityEngines.HasFlag(currentEngine.Engine)) {
						Network.OpenUrl(result.Url);
					}


					// todo: UI won't update after sorting sometimes, wtf

					// Sort results
					Array.Sort(resultsCopy, CompareResults);

					// Reload console UI
					NConsole.IO.Refresh();
				}

				
				threads.Add(task);

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
				NConsole.WriteError("File does not exist: {0}", img);
				return false;
			}

			bool extOkay = ImageExtensions.Any(img.ToLower().EndsWith);

			if (!extOkay) {
				return NConsole.IO.ReadConfirm("File extension is not recognized as a common image format. Continue?");
			}


			return true;
		}

		private static string Upload(string img, bool useImgur)
		{
			string imgUrl;

			if (useImgur) {
				try {
					UploadImgur();
				}
				catch (Exception e) {
					NConsole.WriteError("Error uploading with Imgur: {0}", e.Message);
					NConsole.WriteInfo("Using ImgOps instead");
					UploadImgOps();
				}
			}
			else {
				UploadImgOps();
			}


			void UploadImgur()
			{
				NConsole.WriteInfo("Using Imgur for image upload");
				var imgur = new ImgurClient();
				imgUrl = imgur.Upload(img);
			}

			void UploadImgOps()
			{
				NConsole.WriteInfo("Using ImgOps for image upload (2 hour cache)");
				var imgOps = new ImgOpsClient();
				imgUrl = imgOps.UploadTempImage(img, out _);
			}


			return imgUrl;
		}
	}
}