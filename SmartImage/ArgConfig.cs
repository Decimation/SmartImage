using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CommandLine;
using JetBrains.Annotations;
using Neocmd;
using SmartImage.Searching;

namespace SmartImage
{
	// Arguments contains user config
	// created from arguments if specified
	// read from config file otherwise


	// todo

	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public class ArgConfig
	{
		[Option("search-engines")]
		public string EnginesR { get; set; }

		[Option("priority-engines")]
		public string PriorityEnginesR { get; set; }

		[Option("imgur-auth")]
		public string ImgurAuth { get; set; }

		[Option("saucenao-auth")]
		public string SauceNaoAuth { get; set; }

		[Value(0, Required = true)]
		public string Image { get; set; }

		public SearchEngines Engines {
			get => ParseQ<SearchEngines>(EnginesR);
			set => EnginesR = value.ToString();
		}
		
		public SearchEngines PriorityEngines {
			get => ParseQ<SearchEngines>(PriorityEnginesR);
			set => PriorityEnginesR = value.ToString();
		}
		
		public bool __simple {
			get {
				bool hasEngines         = Engines != default;
				bool hasPriorityEngines = PriorityEngines != default;
				bool hasImgurAuth       = !String.IsNullOrWhiteSpace(ImgurAuth);
				bool hasSauceNaoAuth    = !String.IsNullOrWhiteSpace(SauceNaoAuth);


				return !hasEngines && !hasPriorityEngines && !hasImgurAuth && !hasSauceNaoAuth;
			}
		}

		public bool CfgFallback { get; private set; }

		private static T ParseQ<T>(string s)
		{
			if (string.IsNullOrWhiteSpace(s)) {
				return default;
			}
			
			Enum.TryParse(typeof(T), s, out var e);
			return (T) e;
		}

		public static ArgConfig ReadFromFile(ArgConfig arg,string location)
		{
			
			var map = ExplorerSystem.ReadMap(location);


			arg.EnginesR = (map[CFG_SEARCH_ENGINES]);

			arg.PriorityEnginesR = map[CFG_PRIORITY_ENGINES];


			arg.ImgurAuth    = map[CFG_IMGUR_CLIENT_ID];
			arg.SauceNaoAuth = map[CFG_SAUCENAO_APIKEY];

			arg.CfgFallback = true;


			return arg;
		}

		public void Write<T>(string name, T value, IDictionary<string, string> cfg)
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


		public T Read<T>(string name, IDictionary<string, string> cfg, bool setDefaultIfNull = false,
		                 T      defaultValue = default)
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
			sb.AppendFormat("Config fallback: {0}\n", CfgFallback);

			return sb.ToString();
		}

		private const string CFG_IMGUR_CLIENT_ID  = "imgur_client_id";
		private const string CFG_SAUCENAO_APIKEY  = "saucenao_key";
		private const string CFG_SEARCH_ENGINES   = "search_engines";
		private const string CFG_PRIORITY_ENGINES = "priority_engines";
	}
}