using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using CommandLine;
using JetBrains.Annotations;
using SimpleCore.Utilities;
using SmartImage.Searching;

namespace SmartImage
{
	// Arguments contains user config
	// created from arguments if specified
	// read from config file otherwise


	// todo

	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public class Config
	{
		private const string CFG_IMGUR_CLIENT_ID  = "imgur_client_id";
		private const string CFG_SAUCENAO_APIKEY  = "saucenao_key";
		private const string CFG_SEARCH_ENGINES   = "search_engines";
		private const string CFG_PRIORITY_ENGINES = "priority_engines";

		[Option("engines")]
		public string EnginesStr { get; set; }

		[Option("priority-engines")]
		public string PriorityEnginesStr { get; set; }

		[Option("imgur-auth")]
		public string ImgurAuth { get; set; }

		[Option("saucenao-auth")]
		public string SauceNaoAuth { get; set; }

		[Value(0, Required = true)]
		public string Image { get; set; }


		public SearchEngines Engines {
			get => ParseQ<SearchEngines>(EnginesStr);
			set => EnginesStr = value.ToString();
		}

		public SearchEngines PriorityEngines {
			get => ParseQ<SearchEngines>(PriorityEnginesStr);
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
				{CFG_IMGUR_CLIENT_ID, ImgurAuth},
				{CFG_SAUCENAO_APIKEY, SauceNaoAuth}
			};

			return m;
		}

		public void Reset()
		{
			Engines         = SearchEngines.All;
			PriorityEngines = SearchEngines.SauceNao;
			ImgurAuth       = String.Empty;
			SauceNaoAuth    = String.Empty;
		}

		private static T ParseQ<T>(string s)
		{
			if (string.IsNullOrWhiteSpace(s)) {
				return default;
			}

			Enum.TryParse(typeof(T), s, out var e);
			return (T) e;
		}

		public static Config ReadFromFile(Config arg, string location)
		{
			if (!File.Exists(Core.ConfigLocation)) {
				var f = File.Create(Core.ConfigLocation);
				f.Close();
				arg.Reset();
				ExplorerSystem.WriteMap(arg.ToMap(), Core.ConfigLocation);
			}

			var map = ExplorerSystem.ReadMap(location);

			arg.EnginesStr         = Read<string>(CFG_SEARCH_ENGINES, map);
			arg.PriorityEnginesStr = Read<string>(CFG_PRIORITY_ENGINES, map);
			arg.ImgurAuth          = Read<string>(CFG_IMGUR_CLIENT_ID, map);
			arg.SauceNaoAuth       = Read<string>(CFG_SAUCENAO_APIKEY, map);

			arg.IsFromFile = true;


			return arg;
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

			return (T) (object) rawValue;
		}

		public override string ToString()
		{
			var sb = new StringBuilder();

			sb.AppendFormat("Search engines: {0}\n", Engines);
			sb.AppendFormat("Priority engines: {0}\n", PriorityEngines);
			sb.AppendFormat("Imgur auth: {0}\n", ImgurAuth);
			sb.AppendFormat("SauceNao auth: {0}\n", SauceNaoAuth);
			sb.AppendFormat("Image: {0}\n", Image);
			sb.AppendFormat("Config fallback: {0}\n", IsFromFile);

			return sb.ToString();
		}
	}
}