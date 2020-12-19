#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Novus;
using Novus.Memory;
using Novus.Utilities;
using Novus.Win32;
using Novus.Win32.FileSystem;
using SimpleCore.Console.CommandLine;
using SimpleCore.Net;
using SimpleCore.Utilities;
using SmartImage.Core;
using SmartImage.Engines;
using SmartImage.Engines.Imgur;
using SmartImage.Engines.Other;
using SmartImage.Engines.SauceNao;
using SmartImage.Engines.TraceMoe;
using SmartImage.Utilities;
using static SimpleCore.Console.CommandLine.NConsoleOption;

// ReSharper disable ConvertIfStatementToReturnStatement


namespace SmartImage.Searching
{
	// todo: replace threads with tasks and async


	/// <summary>
	///     Searching client
	/// </summary>
	public sealed class SearchClient
	{
		private const string ORIGINAL_IMAGE_NAME = "(Original image)";


		private static readonly string InterfacePrompt =
			$"Enter the option number to open or {NConsoleIO.NC_GLOBAL_EXIT_KEY} to exit.\n" +
			$"Hold down {NC_ALT_FUNC_MODIFIER} to show more info.\n"                         +
			$"Hold down {NC_CTRL_FUNC_MODIFIER} to download.\n"                              +
			$"Hold down {NC_COMBO_FUNC_MODIFIER} to open raw result.\n";

		private readonly SearchEngineOptions m_engines;

		private readonly FileInfo m_img;
		private readonly Bitmap   m_bmp;
		private readonly string   m_imgUrl;

		private readonly Thread m_monitor;

		private readonly Task[] m_tasks;

		private List<FullSearchResult> m_results;

		public bool Complete { get; private set; }


		/// <summary>
		///     Searching client
		/// </summary>
		public static SearchClient Client { get; } = new(SearchConfig.Config.Image);

		/// <summary>
		///     Search results
		/// </summary>
		public ref List<FullSearchResult> Results => ref m_results;

		public NConsoleUI Interface { get; }

		private SearchClient(string img)
		{
			if (!IsFileValid(img)) {
				throw new SmartImageException("Invalid image");
			}

			string auth     = SearchConfig.Config.ImgurAuth;
			bool   useImgur = !String.IsNullOrWhiteSpace(auth);

			var engines = SearchConfig.Config.SearchEngines;

			if (engines == SearchEngineOptions.None) {
				engines = SearchConfig.ENGINES_DEFAULT;
			}

			m_results = null!;
			m_engines = engines;
			m_img     = new FileInfo(img);
			m_bmp     = new Bitmap(img);

			//

			string? imgUrl = Upload(img, useImgur);

			m_imgUrl = imgUrl ?? throw new SmartImageException("Image upload failed");


			//

			m_tasks = CreateSearchTasks();

			// Joining each thread isn't necessary as this object is disposed upon program exit
			// Background threads won't prevent program termination

			m_monitor = new Thread(RunSearchMonitor)
			{
				Priority     = ThreadPriority.Highest,
				IsBackground = true
			};

			Complete = false;

			Interface = new NConsoleUI(Results)
			{
				SelectMultiple = false,
				Prompt         = InterfacePrompt
			};
		}


		private void RunSearchMonitor()
		{
			while (m_tasks.Any(t => !t.IsCompleted)) {
				var inProgress = m_tasks.Count(t => t.IsCompleted);
				var len        = m_tasks.Length;

				Interface.Status = $"Searching: {inProgress}/{len}";
			}

			//Task.WaitAll(m_tasks);

			var p = new SoundPlayer(Info.Resources.SndHint);
			p.Play();

			Complete         = true;
			Interface.Status = "Search complete";
			NConsoleIO.Refresh();
		}

		private static int CompareResults(FullSearchResult x, FullSearchResult y)
		{
			float xSim = x?.Similarity ?? 0;
			float ySim = y?.Similarity ?? 0;

			if (xSim > ySim) {
				return -1;
			}

			if (xSim < ySim) {
				return 1;
			}

			if (x?.ExtendedResults.Count > y?.ExtendedResults.Count) {
				return -1;
			}

			if (x?.ExtendedInfo.Count > y?.ExtendedInfo.Count) {
				return -1;
			}

			return 0;
		}

		/// <summary>
		///     Starts search
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


		private FullSearchResult GetOriginalImageResult()
		{
			var result = new FullSearchResult(Color.White, ORIGINAL_IMAGE_NAME, m_imgUrl)
			{
				Similarity = 100.0f,
			};

			result.ExtendedInfo.Add($"Location: {m_img}");

			var fileFormat = Files.ResolveFileType(m_img.FullName);

			double fileSizeMegabytes =
				MathHelper.ConvertToUnit(Files.GetFileSize(m_img.FullName), MetricUnit.Mega);

			(int width, int height) = (m_bmp.Width, m_bmp.Height);

			result.Width  = width;
			result.Height = height;


			var mpx = MathHelper.ConvertToUnit(width * height, MetricUnit.Mega);

			string infoStr = $"Info: {m_img.Name} ({fileSizeMegabytes:F} MB) ({mpx:F} MP) ({fileFormat.Name})";

			result.ExtendedInfo.Add(infoStr);

			return result;
		}


		private Task[] CreateSearchTasks()
		{
			// todo: improve, hacky :(

			var availableEngines = GetAllEngines()
				.Where(e => m_engines.HasFlag(e.Engine))
				.ToArray();

			m_results = new List<FullSearchResult>(availableEngines.Length + 1)
			{
				GetOriginalImageResult()
			};

			return availableEngines.Select(currentEngine => new Task(() => RunSearchTask(currentEngine))).ToArray();
		}

		private void RunSearchTask(ISearchEngine currentEngine)
		{
			var result = currentEngine.GetResult(m_imgUrl);

			m_results.Add(result);


			// If the engine is priority, open its result in the browser
			if (SearchConfig.Config.PriorityEngines.HasFlag(currentEngine.Engine)) {
				Network.OpenUrl(result.Url);
			}

			//Update();

			// Sort results
			m_results.Sort(CompareResults);

			// Reload console UI
			NConsoleIO.Refresh();
		}


		private static IEnumerable<ISearchEngine> GetAllEngines()
		{
			return new ISearchEngine[]
			{
				//
				new SauceNaoEngine(),
				new IqdbEngine(),
				new YandexEngine(),
				new TraceMoeEngine(),

				//
				new ImgOpsEngine(),
				new GoogleImagesEngine(),
				new TinEyeEngine(),
				new BingEngine(),
				new KarmaDecayEngine()
			};
		}

		private static bool IsFileValid(string img)
		{
			if (String.IsNullOrWhiteSpace(img)) {
				return false;
			}

			if (!File.Exists(img)) {
				NConsole.WriteError($"File does not exist: {img}");
				return false;
			}

			bool isImageType = Files.ResolveFileType(img).Type == FileType.Image;

			if (!isImageType) {
				return NConsoleIO.ReadConfirmation("File format is not recognized as a common image format. Continue?");
			}

			return true;
		}

		private static string? Upload(string img, bool useImgur)
		{
			IUploadEngine uploadEngine;

			string imgUrl;

			if (useImgur) {
				try {
					NConsole.WriteInfo("Using Imgur for image upload");
					uploadEngine = new ImgurClient();
					imgUrl       = uploadEngine.Upload(img);
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

			void UploadImgOps()
			{
				NConsole.WriteInfo("Using ImgOps for image upload (2 hour cache)");
				uploadEngine = new ImgOpsEngine();
				imgUrl       = uploadEngine.Upload(img);
			}


			return imgUrl;
		}
	}
}