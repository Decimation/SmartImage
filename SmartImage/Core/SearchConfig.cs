using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;
using Novus.Utilities;
using SimpleCore.Console.CommandLine;
using SimpleCore.Utilities;
using SmartImage.Engines;
using SmartImage.Engines.Imgur;
using SmartImage.Engines.SauceNao;
using SmartImage.Searching;
using SmartImage.Utilities;

#pragma warning disable HAA0502, HAA0302, HAA0505, HAA0601, HAA0301, HAA0501, HAA0101, HAA0102, RCS1036

// ReSharper disable InconsistentNaming

// ReSharper disable IdentifierTypo

namespace SmartImage.Core
{
	/// <summary>
	///     Search config
	/// </summary>
	/// <remarks>
	///     Config is read from config file (<see cref="ConfigLocation" />) or from specified arguments
	/// </remarks>
	public sealed class SearchConfig
	{
		// todo: create config field type; create config field attribute
		// todo: refactor


		public const SearchEngineOptions ENGINES_DEFAULT = SearchEngineOptions.All;

		public const SearchEngineOptions PRIORITY_ENGINES_DEFAULT = SearchEngineOptions.SauceNao;

		public static readonly string IMGUR_APIKEY_DEFAULT = String.Empty;

		public static readonly string SAUCENAO_APIKEY_DEFAULT = String.Empty;


		/// <summary>
		///     User config and arguments
		/// </summary>
		public static SearchConfig Config { get; } = new SearchConfig();

		/// <summary>
		///     Whether no arguments were passed in via CLI
		/// </summary>
		public bool NoArguments { get; set; }

		/// <summary>
		///     Engines to use for searching
		/// </summary>
		[field: ConfigField("search_engines", SearchEngineOptions.All)]
		public SearchEngineOptions SearchEngines { get; set; }

		/// <summary>
		///     Engines whose results should be opened in the browser
		/// </summary>
		[field: ConfigField("priority_engines", SearchEngineOptions.Auto)]
		public SearchEngineOptions PriorityEngines { get; set; }

		/// <summary>
		///     <see cref="ImgurClient" /> API key
		/// </summary>
		[field: ConfigField("imgur_client_id", "")]
		public string ImgurAuth { get; set; }

		/// <summary>
		///     <see cref="SauceNaoEngine" /> API key
		/// </summary>
		[field: ConfigField("saucenao_key", "")]
		public string SauceNaoAuth { get; set; }

		/// <summary>
		///     Whether to save passed in arguments (via CLI) to the config file upon exit
		/// </summary>
		public bool UpdateConfig { get; set; }


		/// <summary>
		///     The image we are searching for
		/// </summary>
		public string Image { get; set; }

		/// <summary>
		///     Location of config file
		/// </summary>
		public static string ConfigLocation => Path.Combine(Info.AppFolder, Info.NAME_CFG);


		private SearchConfig()
		{
			// Read config from config file
			ReadFromFile();

			// Read config from command line arguments
			ReadFromArguments();


			// Setup
			EnsureConfig();
		}

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


			//UpdateConfigFields(this, cfgFromFileMap);

			//SearchEngines   = ReadMapKeyValue<SearchEngineOptions>(nameof(SearchEngines), cfgFromFileMap);
			//PriorityEngines = ReadMapKeyValue<SearchEngineOptions>(nameof(PriorityEngines), cfgFromFileMap);
			//ImgurAuth       = ReadMapKeyValue<string>(nameof(ImgurAuth), cfgFromFileMap);
			//SauceNaoAuth    = ReadMapKeyValue<string>(nameof(SauceNaoAuth), cfgFromFileMap);

			UpdateCfgFields(cfgFromFileMap);
			
			
			if (newCfg) {
				WriteToFile();
			}

			// Should be initialized eventually
			Image = String.Empty;
		}

		
		private void UpdateCfgFields(IDictionary<string, string> cfg)
		{
			var f = this.GetType().GetAnnotated<ConfigFieldAttribute>();

			foreach (var t in f) {
				var s   = t.Member.Name;
				
				var val  = ReadMapKeyValue<object>(s, cfg).ToString();
				

				var fi   = t.Member.GetBackingField();
				var val2 = ReadConfigValue(val, fi.FieldType );
				fi.SetValue(this, val2);
			}
		}

		private IDictionary<string, string> ToMap()
		{
			var a = this.GetType().GetAnnotated<ConfigFieldAttribute>()
				.Select(delegate((ConfigFieldAttribute Attribute, MemberInfo Member) f)
				{
					var fv = f.Member.GetBackingField();
					return new KeyValuePair<string, string>(f.Attribute.Id, fv.GetValue(this).ToString());
				});


			var m = new Dictionary<string, string>(a);
			
			//var m = new Dictionary<string, string>
			//{
			//	{"search_engines", SearchEngines.ToString()},
			//	{"priority_engines", PriorityEngines.ToString()},
			//	{"imgur_client_id", ImgurAuth},
			//	{"saucenao_key", SauceNaoAuth}
			//};


			foreach (var kv in m) {
				Debug.WriteLine(kv);
			}
			
			return m;
		}

		public void Reset()
		{
			
			
			SearchEngines   = ENGINES_DEFAULT;
			PriorityEngines = PRIORITY_ENGINES_DEFAULT;
			ImgurAuth       = IMGUR_APIKEY_DEFAULT;
			SauceNaoAuth    = SAUCENAO_APIKEY_DEFAULT;
		}


		public void WriteToFile()
		{
			NConsole.WriteInfo("Updating config");
			Collections.WriteDictionary(ToMap(), ConfigLocation);
			NConsole.WriteInfo("Wrote to {0}", ConfigLocation);
		}


		/// <summary>
		/// Illegal <see cref="SearchEngineOptions"/> values for <see cref="SearchEngines"/>
		/// </summary>
		private const SearchEngineOptions IllegalSearchEngineOptions =
			SearchEngineOptions.None | SearchEngineOptions.Auto;


		/// <summary>
		/// Ensures validity of config options
		/// </summary>
		public void EnsureConfig()
		{
			/*
			 * Check search engine options
			 */

			var illegalOptions = SearchEngines & IllegalSearchEngineOptions;

			if (illegalOptions != 0) {
				NConsole.WriteError($"Search engine option {illegalOptions} cannot be used for search engine options");

				NConsoleIO.WaitForSecond();

				// Clear illegal options
				SearchEngines &= ~illegalOptions;
			}

			// Special case
			if (SearchEngines == SearchEngineOptions.None) {
				NConsole.WriteInfo("Reverting search engine options to default");
				NConsoleIO.WaitForSecond();
				SearchEngines = ENGINES_DEFAULT;
			}

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

	

		private  T ReadMapKeyValue<T>(string fname, IDictionary<string, string> cfg)
		{
			var t     = this.GetType();
			var field = t.GetFieldAuto(fname);

			var attr = field.GetCustomAttribute<ConfigFieldAttribute>();

			var defaultValue     = (T) attr.DefaultValue;
			var setDefaultIfNull = attr.SetDefaultIfNull;
			var name             = attr.Id;


			var v = ReadMapKeyValueOld<T>(name, cfg, setDefaultIfNull, defaultValue);
			Debug.WriteLine($"{v} -> {name} {field.Name}");
			return v;
		}

		private static T ReadMapKeyValueOld<T>(string name, IDictionary<string, string> cfg,
			bool setDefaultIfNull = false, T defaultValue = default)
		{
			if (!cfg.ContainsKey(name)) {
				cfg.Add(name, String.Empty);
			}
			//Update();

			string rawValue = cfg[name];

			if (setDefaultIfNull && String.IsNullOrWhiteSpace(rawValue)) {
				WriteMapKeyValue(name, defaultValue.ToString(), cfg);
				rawValue = ReadMapKeyValueOld<string>(name, cfg);
			}

			var parse = ReadConfigValue<T>(rawValue);
			Debug.WriteLine($"{parse} -> {name}");
			return parse;
		}

		public override string ToString()
		{
			var sb = new StringBuilder();


			if (!String.IsNullOrWhiteSpace(Image)) {
				// Image may be null if not specified (error) or viewed in other UIs
				// if so, omit it

				sb.AppendFormat("Image: {0}\n\n", Image);
			}


			sb.AppendFormat("Search engines: {0}\n", SearchEngines);
			sb.AppendFormat("Priority engines: {0}\n", PriorityEngines);


			string snAuth = Config.SauceNaoAuth;
			bool   snNull = String.IsNullOrWhiteSpace(snAuth);

			if (!snNull) {
				sb.AppendFormat("SauceNao authentication: {0}\n", snAuth);
			}


			string imgurAuth = Config.ImgurAuth;
			bool   imgurNull = String.IsNullOrWhiteSpace(imgurAuth);

			if (!imgurNull) {
				sb.AppendFormat("Imgur authentication: {0}\n", imgurAuth);
			}


			sb.AppendFormat("Image upload service: {0}\n",
				imgurNull ? "ImgOps" : "Imgur");


			sb.AppendFormat("Config location: {0}\n", ConfigLocation);

			return sb.ToString();
		}


		public void UpdateFile()
		{
			if (UpdateConfig) {
				WriteToFile();
			}
		}

		/// <summary>
		///     Parse config arguments and options
		/// </summary>
		private void ReadFromArguments()
		{
			var args = Environment.GetCommandLineArgs().Skip(1).ToArray();


			bool noArgs = args.Length == 0;

			if (noArgs) {
				NoArguments = true;
				return;
			}

			var       argQueue      = new Queue<string>(args);
			using var argEnumerator = argQueue.GetEnumerator();

			while (argEnumerator.MoveNext()) {
				string argValue = argEnumerator.Current;

				// todo: structure

				switch (argValue) {
					case "--search-engines":
						argEnumerator.MoveNext();
						string sestr = argEnumerator.Current;
						SearchEngines = ReadConfigValue<SearchEngineOptions>(sestr);
						break;
					case "--priority-engines":
						argEnumerator.MoveNext();
						string pestr = argEnumerator.Current;
						PriorityEngines = ReadConfigValue<SearchEngineOptions>(pestr);
						break;
					case "--saucenao-auth":
						argEnumerator.MoveNext();
						string snastr = argEnumerator.Current;
						SauceNaoAuth = snastr;
						break;
					case "--imgur-auth":
						argEnumerator.MoveNext();
						string imastr = argEnumerator.Current;
						ImgurAuth = imastr;
						break;
					case "--update-cfg":
						UpdateConfig = true;
						break;


					default:
						Image = argValue;
						break;
				}
			}
		}

		private static object ReadConfigValue(string rawValue, Type t)
		{
			if (t.IsEnum)
			{
				Enum.TryParse(t, rawValue, out var e);
				return e;
			}

			if (t == typeof(bool))
			{
				Boolean.TryParse(rawValue, out bool b);
				return (object)b;
			}

			return (object)rawValue;
		}

		private static T ReadConfigValue<T>(string rawValue)
		{
			if (typeof(T).IsEnum) {
				Enum.TryParse(typeof(T), rawValue, out var e);
				return (T) e;
			}

			if (typeof(T) == typeof(bool)) {
				Boolean.TryParse(rawValue, out bool b);
				return (T) (object) b;
			}

			return (T) (object) rawValue;
		}
	}
}