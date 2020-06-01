using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CommandLine;
using JetBrains.Annotations;
using SimpleCore.Utilities;
using SmartImage.Engines.SauceNao;
using SmartImage.Searching;

namespace SmartImage
{
	// Arguments contains user config
	// created from arguments if specified
	// read from config file otherwise


	// todo

	[Verb("search", true)]
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public sealed class SearchConfig
	{
		private const string CFG_IMGUR_APIKEY     = "imgur_client_id";
		private const string CFG_SAUCENAO_APIKEY  = "saucenao_key";
		private const string CFG_SEARCH_ENGINES   = "search_engines";
		private const string CFG_PRIORITY_ENGINES = "priority_engines";
		private const string CFG_AUTOEXIT         = "auto_exit";

		[Option("engines")]
		public string EnginesStr { get; set; }

		[Option("priority-engines")]
		public string PriorityEnginesStr { get; set; }

		[Option("imgur-auth")]
		public string ImgurAuth { get; set; }

		[Option("saucenao-auth")]
		public string SauceNaoAuth { get; set; }

		[Option("auto-exit", Default = false)]
		public bool AutoExit { get; set; }

		[Option("update-cfg")]
		public bool UpdateConfig { get; set; }

		[Value(0, Required = true)]
		public string Image { get; set; }


		public SearchEngines Engines {
			get => Enums.SafeParse<SearchEngines>(EnginesStr);
			set => EnginesStr = value.ToString();
		}

		public SearchEngines PriorityEngines {
			get => Enums.SafeParse<SearchEngines>(PriorityEnginesStr);
			set => PriorityEnginesStr = value.ToString();
		}

		public bool IsEmpty {
			get {
				bool hasEngines         = Engines != default;
				bool hasPriorityEngines = PriorityEngines != default;
				bool hasImgurAuth       = !String.IsNullOrWhiteSpace(ImgurAuth);
				bool hasSauceNaoAuth    = !String.IsNullOrWhiteSpace(SauceNaoAuth);


				return !hasEngines && !hasPriorityEngines && !hasImgurAuth && !hasSauceNaoAuth;
			}
		}

		public bool IsFromFile { get; private set; }

		public IDictionary<string, string> ToMap()
		{
			var m = new Dictionary<string, string>
			{
				{CFG_SEARCH_ENGINES, Engines.ToString()},
				{CFG_PRIORITY_ENGINES, PriorityEngines.ToString()},
				{CFG_IMGUR_APIKEY, ImgurAuth},
				{CFG_SAUCENAO_APIKEY, SauceNaoAuth},
				{CFG_AUTOEXIT, AutoExit.ToString()},
			};

			return m;
		}

		public static SearchConfig GetDefault()
		{
			var uc = new SearchConfig();
			uc.Reset();
			return uc;
		}

		public void Reset()
		{
			Engines         = SearchEngines.All;
			PriorityEngines = SearchEngines.SauceNao;
			ImgurAuth       = null;
			SauceNaoAuth    = null;
			AutoExit        = false;
		}

		public static SearchConfig ReadFromFile(string location, bool isNew)
		{
			var cfgFromFileMap = ExplorerSystem.ReadMap(location);

			var cfgFromFile = new SearchConfig
			{
				EnginesStr         = Read<string>(CFG_SEARCH_ENGINES, cfgFromFileMap),
				PriorityEnginesStr = Read<string>(CFG_PRIORITY_ENGINES, cfgFromFileMap),
				ImgurAuth          = Read<string>(CFG_IMGUR_APIKEY, cfgFromFileMap),
				SauceNaoAuth       = Read<string>(CFG_SAUCENAO_APIKEY, cfgFromFileMap),
				AutoExit           = Read<bool>(CFG_AUTOEXIT, cfgFromFileMap),
				IsFromFile         = true
			};

			if (isNew) {
				cfgFromFile.Reset();
				cfgFromFile.WriteToFile();
			}

			return cfgFromFile;
		}


		public static SearchConfig Update(SearchConfig cfgFromCli, SearchConfig cfgFromFile)
		{
			// todo: find a more sustainable way of doing this
			// todo: use reflection

			if (cfgFromCli.Engines == default) {
				cfgFromCli.Engines = cfgFromFile.Engines;
			}

			if (cfgFromCli.PriorityEngines == default) {
				cfgFromCli.PriorityEngines = cfgFromFile.PriorityEngines;
			}

			if (String.IsNullOrWhiteSpace(cfgFromCli.ImgurAuth)) {
				cfgFromCli.ImgurAuth = cfgFromFile.ImgurAuth;
			}

			if (String.IsNullOrWhiteSpace(cfgFromCli.SauceNaoAuth)) {
				cfgFromCli.SauceNaoAuth = cfgFromFile.SauceNaoAuth;
			}

			if (cfgFromCli.AutoExit == default) {
				cfgFromCli.AutoExit = cfgFromFile.AutoExit;
			}

			return cfgFromCli;
		}

		// todo: update cfg file

		internal void WriteToFile()
		{
			ExplorerSystem.WriteMap(ToMap(), RuntimeInfo.ConfigLocation);
			CliOutput.WriteInfo("wrote to {0}", RuntimeInfo.ConfigLocation);
		}

		public static void Write<T>(string name, T value, IDictionary<string, string> cfg)
		{
			var valStr = value.ToString();
			if (!cfg.ContainsKey(name)) {
				cfg.Add(name, valStr);
			}
			else {
				cfg[name] = valStr;
			}

			//Update();
		}


		public static T Read<T>(string                      name,
		                        IDictionary<string, string> cfg,
		                        bool                        setDefaultIfNull = false,
		                        T                           defaultValue     = default)
		{
			if (!cfg.ContainsKey(name)) {
				cfg.Add(name, String.Empty);
				//Update();
			}

			var rawValue = cfg[name];

			if (setDefaultIfNull && String.IsNullOrWhiteSpace((string) rawValue)) {
				Write(name, defaultValue.ToString(), cfg);
				rawValue = Read<string>(name, cfg);
			}

			if (typeof(T).IsEnum) {
				Enum.TryParse(typeof(T), (string) rawValue, out var e);
				return (T) e;
			}

			if (typeof(T) == typeof(bool)) {
				Boolean.TryParse(rawValue, out var b);
				return (T) (object) b;
			}

			return (T) (object) rawValue;
		}

		public override string ToString()
		{
			var sb = new StringBuilder();

			sb.AppendFormat("Search engines: {0}\n", Engines);
			sb.AppendFormat("Priority engines: {0}\n", PriorityEngines);
			sb.AppendFormat("Imgur auth: {0}\n", ImgurAuth);
			sb.AppendFormat("SauceNao auth: {0}\n", SauceNaoAuth);
			sb.AppendFormat("Auto exit: {0}\n", AutoExit);
			sb.AppendFormat("Image: {0}\n", Image);
			sb.AppendFormat("Config fallback: {0}\n", IsFromFile);
			sb.AppendFormat("Empty: {0}", IsEmpty);

			return sb.ToString();
		}

		

		public static void Cleanup()
		{
			if (RuntimeInfo.Config.UpdateConfig) {
				CliOutput.WriteInfo("Updating cfg");
				RuntimeInfo.Config.WriteToFile();
			}
		}
	}
}