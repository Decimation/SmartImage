using SimpleCore.Cli;
using SimpleCore.Utilities;
using SmartImage.Core;
using SmartImage.Engines;
using SmartImage.Engines.Imgur;
using SmartImage.Engines.SauceNao;
using SmartImage.Searching;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using static SmartImage.Core.Interface;

#pragma warning disable HAA0502, HAA0302, HAA0505, HAA0601, HAA0301, HAA0501, HAA0101, HAA0102, RCS1036

// ReSharper disable InconsistentNaming

// ReSharper disable IdentifierTypo

namespace SmartImage.Configuration
{
	/// <summary>
	///     Search configuration and options
	/// </summary>
	/// <remarks>
	///     Config is read from config file (<see cref="ConfigLocation" />) first, then from specified command line arguments.
	/// </remarks>
	/// <seealso cref="ConfigComponents" />
	public sealed class SearchConfig
	{
		public SearchConfig()
		{
			// Read config from config file
			ReadFromFile();

			// Read config from command line arguments
			ReadFromArguments();

			// Setup
			EnsureConfig();
		}

		/// <summary>
		///     User config and arguments
		/// </summary>
		public static SearchConfig Config { get; } = new();

		/// <summary>
		///     Whether no arguments were passed in via CLI
		/// </summary>
		public bool NoArguments { get; set; }

		/// <summary>
		///     Engines to use for searching
		/// </summary>
		[field: ConfigComponent("search_engines", "--search-engines", SearchEngineOptions.All, true)]
		public SearchEngineOptions SearchEngines { get; set; }

		/// <summary>
		///     Engines whose results should be opened in the browser
		/// </summary>
		[field: ConfigComponent("priority_engines", "--priority-engines", SearchEngineOptions.Auto, true)]
		public SearchEngineOptions PriorityEngines { get; set; }

		/// <summary>
		///     <see cref="ImgurClient" /> API key
		/// </summary>
		[field: ConfigComponent("imgur_client_id", "--saucenao-auth", Strings.Empty)]
		public string ImgurAuth { get; set; }

		/// <summary>
		///     <see cref="SauceNaoEngine" /> API key
		/// </summary>
		[field: ConfigComponent("saucenao_key", "--imgur-auth", Strings.Empty)]
		public string SauceNaoAuth { get; set; }

		/// <summary>
		///     Does not open results from priority engines if the result similarity (if available) is below a certain threshold,
		/// or there are no relevant results.
		/// <see cref="BaseSearchResult.Filter"/> is <c>true</c> if <see cref="BaseSearchEngine.FilterThreshold"/> is less than <see cref="BaseSearchResult.Similarity"/>
		/// </summary>
		[field: ConfigComponent("filter_results", "--filter-results", true, true)]
		public bool FilterResults { get; set; }

		/// <summary>
		///     Whether to save passed in arguments (via CLI) to the config file upon exit
		/// </summary>
		public bool UpdateConfig { get; set; }


		/// <summary>
		///     Image input (image file or URL)
		/// </summary>
		public string ImageInput { get; set; }

		public bool HasImageInput => !string.IsNullOrWhiteSpace(ImageInput);

		public bool UseImgur => !string.IsNullOrWhiteSpace(ImgurAuth);

		/// <summary>
		///     Location of config file
		/// </summary>
		public static string ConfigLocation => Path.Combine(Info.AppFolder, Info.NAME_CFG);

		/// <summary>
		/// Read configuration from file (<see cref="ConfigLocation"/>)
		/// </summary>
		private void ReadFromFile()
		{
			bool newCfg = false;

			// create cfg with default options if it doesn't exist
			if (!File.Exists(ConfigLocation)) {

				var f = File.Create(ConfigLocation);
				f.Close();
				newCfg = true;
			}

			var cfgFromFileMap = Collections.ReadDictionary(ConfigLocation);

			ConfigComponents.UpdateFields(this, cfgFromFileMap);


			if (newCfg) {
				SaveFile();
			}

			// Should be initialized eventually
			ImageInput = String.Empty;
		}

		/// <summary>
		/// Reset configuration to defaults
		/// </summary>
		public void Reset() => ConfigComponents.ResetComponents(this);


		public void SaveFile()
		{
			NConsole.WriteInfo("Updating config");
			ConfigComponents.WriteComponentsToFile(this, SearchConfig.ConfigLocation);
			NConsole.WriteInfo("Wrote to {0}", ConfigLocation);
		}


		/// <summary>
		///     Ensures validity of config options
		/// </summary>
		public void EnsureConfig()
		{
			/*
			 * Check search engine options
			 */

			const SearchEngineOptions Illegal = SearchEngineOptions.Auto;

			if (SearchEngines.HasFlag(Illegal)) {
				SearchEngines &= ~Illegal;
				Debug.WriteLine($"Removed illegal flag -> {SearchEngines}");
			}

			if (SearchEngines == SearchEngineOptions.None) {
				ConfigComponents.ResetComponent(this, nameof(SearchEngines));
			}
		}


		public override string ToString()
		{
			var sb = new StringBuilder();


			if (HasImageInput) {
				// Image may be null if not specified (error) or viewed in other UIs
				// if so, omit it
				var fi = new FileInfo(ImageInput).Name;
				sb.AppendLabelWithColor(ColorMain1, "Image", ColorMainFunction, fi).AppendLine().AppendLine();
			}


			sb.AppendLabelWithColor(ColorConfig, "Search engines", ColorMain2, SearchEngines)
				.AppendLine();

			sb.AppendLabelWithColor(ColorConfig, "Priority engines", ColorMain2, PriorityEngines)
				.AppendLine();


			string snAuth = Config.SauceNaoAuth;
			bool   snNull = String.IsNullOrWhiteSpace(snAuth);

			if (!snNull) {
				sb.AppendLabelWithColor(ColorConfig, "SauceNao authentication", ColorMain2,
					snAuth).AppendLine();
			}

			string imgurAuth = Config.ImgurAuth;
			bool   imgurNull = String.IsNullOrWhiteSpace(imgurAuth);

			if (!imgurNull) {
				sb.AppendLabelWithColor(ColorConfig, "Imgur authentication", ColorMain2,
					imgurAuth).AppendLine();
			}

			sb.AppendLabelWithColor(ColorConfig, "Auto filtering", ColorMain2, FilterResults)
				.AppendLine();

			sb.AppendLabelWithColor(ColorConfig, "Image upload service", ColorMain2,
				imgurNull ? "ImgOps" : "Imgur").AppendLine();

			string cfgDirectoryName = new FileInfo(ConfigLocation).Directory.Name;


			sb.AppendLabelWithColor(ColorConfig, "Config location", ColorMain2, cfgDirectoryName)
				.AppendLine();


			return sb.ToString();
		}


		/// <summary>
		///     Read config from command line arguments
		/// </summary>
		private void ReadFromArguments()
		{
			string[] args = Environment.GetCommandLineArgs().Skip(1).ToArray();

			if (!args.Any()) {
				NoArguments = true;
				return;
			}

			var argQueue = new Queue<string>(args);

			using var argEnumerator = argQueue.GetEnumerator();

			while (argEnumerator.MoveNext()) {
				string parameterName = argEnumerator.Current;

				ConfigComponents.ReadComponentFromArgument(this, argEnumerator);

				// Special cases
				switch (parameterName) {

					case "--update-cfg":
						UpdateConfig = true;
						break;

					default:
						ImageInput = parameterName;
						break;
				}
			}
		}
	}
}