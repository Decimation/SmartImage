#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SimpleCore.Utilities;
using SmartImage.Engines.Imgur;
using SmartImage.Engines.SauceNao;
using SmartImage.Searching;
using SmartImage.Utilities;

// ReSharper disable InconsistentNaming

// ReSharper disable IdentifierTypo

namespace SmartImage
{
	/// <summary>
	///     Search config
	/// </summary>
	/// <remarks>
	///     Config is read from config file (<see cref="ConfigLocation" />) or from specified arguments
	/// </remarks>
	public sealed class SearchConfig
	{
		// todo: create config field type
		// todo: create config field attribute
		// todo: refactor

		private const string CFG_IMGUR_APIKEY = "imgur_client_id";
		private const string CFG_SAUCENAO_APIKEY = "saucenao_key";
		private const string CFG_SEARCH_ENGINES = "search_engines";
		private const string CFG_PRIORITY_ENGINES = "priority_engines";



		public const SearchEngines ENGINES_DEFAULT = SearchEngines.All;

		public const SearchEngines PRIORITY_ENGINES_DEFAULT = SearchEngines.SauceNao;

		public static readonly string IMGUR_APIKEY_DEFAULT = String.Empty;

		public static readonly string SAUCENAO_APIKEY_DEFAULT = String.Empty;


		private SearchConfig()
		{
			bool newCfg = false;

			// create cfg with default options if it doesn't exist
			if (!File.Exists(ConfigLocation)) {
				var f = File.Create(ConfigLocation);
				f.Close();
				newCfg = true;
			}

			var cfgFromFileMap = ExplorerSystem.ReadMap(ConfigLocation);

			SearchEngines = ReadMapKeyValue(CFG_SEARCH_ENGINES, cfgFromFileMap, true, ENGINES_DEFAULT);
			PriorityEngines = ReadMapKeyValue(CFG_PRIORITY_ENGINES, cfgFromFileMap, true, PRIORITY_ENGINES_DEFAULT);
			ImgurAuth = ReadMapKeyValue(CFG_IMGUR_APIKEY, cfgFromFileMap, true, IMGUR_APIKEY_DEFAULT);
			SauceNaoAuth = ReadMapKeyValue(CFG_SAUCENAO_APIKEY, cfgFromFileMap, true, SAUCENAO_APIKEY_DEFAULT);

			if (newCfg) {
				WriteToFile();
			}

			// Should be initialized eventually
			Image = string.Empty;
		}


		/// <summary>
		///     User config and arguments
		/// </summary>
		public static SearchConfig Config { get; } = new SearchConfig();

		/// <summary>
		/// Whether no arguments were passed in via CLI
		/// </summary>
		public bool NoArguments { get; set; }

		/// <summary>
		/// Engines to use for searching
		/// </summary>
		public SearchEngines SearchEngines { get; set; }

		/// <summary>
		/// Engines whose results should be opened in the browser
		/// </summary>
		public SearchEngines PriorityEngines { get; set; }

		/// <summary>
		/// <see cref="Imgur"/> API key
		/// </summary>
		public string ImgurAuth { get; set; }

		/// <summary>
		/// <see cref="SauceNao"/> API key
		/// </summary>
		public string SauceNaoAuth { get; set; }

		/// <summary>
		/// Whether to save passed in arguments (via CLI) to the config file upon exit
		/// </summary>
		public bool UpdateConfig { get; set; }


		/// <summary>
		/// The image we are searching for
		/// </summary>
		public string Image { get; set; }

		/// <summary>
		///     Location of config file
		/// </summary>
		public static string ConfigLocation => Path.Combine(RuntimeInfo.AppFolder, RuntimeInfo.NAME_CFG);


		internal IDictionary<string, string> ToMap()
		{
			var m = new Dictionary<string, string>
			{
				{CFG_SEARCH_ENGINES, SearchEngines.ToString()},
				{CFG_PRIORITY_ENGINES, PriorityEngines.ToString()},
				{CFG_IMGUR_APIKEY, ImgurAuth},
				{CFG_SAUCENAO_APIKEY, SauceNaoAuth}
			};

			return m;
		}

		public void Reset()
		{
			SearchEngines = ENGINES_DEFAULT;
			PriorityEngines = PRIORITY_ENGINES_DEFAULT;
			ImgurAuth = IMGUR_APIKEY_DEFAULT;
			SauceNaoAuth = SAUCENAO_APIKEY_DEFAULT;

		}


		internal void WriteToFile()
		{
			CliOutput.WriteInfo("Updating config");
			ExplorerSystem.WriteMap(ToMap(), ConfigLocation);
			CliOutput.WriteInfo("Wrote to {0}", ConfigLocation);

		}


		internal string Dump()
		{
			var sb = new StringBuilder();


			sb.AppendFormat("Search engines: {0}\n", SearchEngines);
			sb.AppendFormat("Priority engines: {0}\n", PriorityEngines);


			string sn = SearchConfig.Config.SauceNaoAuth;
			bool snNull = String.IsNullOrWhiteSpace(sn);

			sb.AppendFormat("SauceNao authentication: {0} ({1})\n",
				snNull ? CliOutput.MUL_SIGN.ToString() : sn, snNull ? "Basic" : "Advanced");

			string imgur = SearchConfig.Config.ImgurAuth;
			bool imgurNull = String.IsNullOrWhiteSpace(imgur);

			sb.AppendFormat("Imgur authentication: {0}\n",
				imgurNull ? CliOutput.MUL_SIGN.ToString() : imgur);

			sb.AppendFormat("Image upload service: {0}\n",
				imgurNull ? "ImgOps" : "Imgur");


			sb.AppendFormat("Config location: {0}\n", SearchConfig.ConfigLocation);

			return sb.ToString();
		}


		private static void WriteMapKeyValue<T>(string name, T value, IDictionary<string, string> cfg)
		{
			string? valStr = value.ToString();

			if (!cfg.ContainsKey(name)) {
				cfg.Add(name, valStr);
			}
			else {
				cfg[name] = valStr;
			}

			//Update();
		}


		private static T ReadMapKeyValue<T>(string name, IDictionary<string, string> cfg,
			bool setDefaultIfNull = false, T defaultValue = default)
		{
			if (!cfg.ContainsKey(name)) {
				cfg.Add(name, String.Empty);
			}
			//Update();

			string rawValue = cfg[name];

			if (setDefaultIfNull && String.IsNullOrWhiteSpace(rawValue)) {
				WriteMapKeyValue(name, defaultValue.ToString(), cfg);
				rawValue = ReadMapKeyValue<string>(name, cfg);
			}

			var parse = CommonUtilities.Read<T>(rawValue);
			return parse;
		}

		public override string ToString()
		{
			var sb = new StringBuilder();

			sb.AppendFormat("Search engines: {0}\n", SearchEngines);
			sb.AppendFormat("Priority engines: {0}\n", PriorityEngines);
			sb.AppendFormat("Imgur auth: {0}\n", ImgurAuth);
			sb.AppendFormat("SauceNao auth: {0}\n", SauceNaoAuth);
			sb.AppendFormat("Image: {0}\n", Image);

			return sb.ToString();
		}


		public static void Cleanup()
		{
			if (Config.UpdateConfig) {
				Config.WriteToFile();
			}
		}

		/// <summary>
		///     Parse config arguments and options
		/// </summary>
		/// <param name="args">Command line arguments</param>
		public static void ReadSearchConfigArguments(string[] args)
		{
			bool noArgs = args == null || args.Length == 0;

			if (noArgs) {
				Config.NoArguments = true;
				return;
			}

			var argQueue = new Queue<string>(args);
			using var argEnumerator = argQueue.GetEnumerator();

			while (argEnumerator.MoveNext()) {
				string argValue = argEnumerator.Current;

				// todo: structure

				switch (argValue) {
					case "--search-engines":
						argEnumerator.MoveNext();
						string sestr = argEnumerator.Current;
						Config.SearchEngines = CommonUtilities.Read<SearchEngines>(sestr);
						break;
					case "--priority-engines":
						argEnumerator.MoveNext();
						string pestr = argEnumerator.Current;
						Config.PriorityEngines = CommonUtilities.Read<SearchEngines>(pestr);
						break;
					case "--saucenao-auth":
						argEnumerator.MoveNext();
						string snastr = argEnumerator.Current;
						Config.SauceNaoAuth = snastr;
						break;
					case "--imgur-auth":
						argEnumerator.MoveNext();
						string imastr = argEnumerator.Current;
						Config.ImgurAuth = imastr;
						break;
					case "--update-cfg":
						Config.UpdateConfig = true;
						break;


					default:
						Config.Image = argValue;
						break;
				}
			}
		}
	}
}