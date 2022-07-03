using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Text;
using Kantan.Collections;
using Kantan.Text;
using SmartImage.Lib.Properties;
using SmartImage.Lib.Searching;

namespace SmartImage.Lib;

/// <summary>
/// Contains configuration for <see cref="SearchClient"/>
/// </summary>
/// <remarks>Search config is only applicable when used in <see cref="SearchClient"/></remarks>
public sealed class SearchConfig /*: ConfigurationSection*/
{
	/// <summary>
	/// Search query
	/// </summary>
	public ImageQuery Query { get; set; } //todo: remove as field

	public FileInfo FullName
	{
		get
		{
			string file = Path.Combine(Folder, Name);

			if (!File.Exists(file)) {
				var f = File.Create(file);
				f.Close();
			}


			return new FileInfo(file);
		}
	}

	public string Folder { get; init; }

	public string Name => Resources.F_Config;

	public void Save()
	{
		var map = ToMap();

		EnumerableHelper.WriteDictionary(map, FullName.ToString());

		Debug.WriteLine($"Saved to {FullName.Name}", C_INFO);
	}

	public void Update()
	{
		var map = EnumerableHelper.ReadDictionary(FullName.ToString());

		foreach (var (key, value) in ToMap()) {
			if (!map.ContainsKey(key)) {
				map.Add(key, value);
			}

		}

		SearchEngines     = Enum.Parse<SearchEngineOptions>(map[Resources.K_Engines]);
		PriorityEngines   = Enum.Parse<SearchEngineOptions>(map[Resources.K_PriorityEngines]);
		Filtering         = Boolean.Parse(map[Resources.K_Filter]);
		Notification      = Boolean.Parse(map[Resources.K_Notification]);
		NotificationImage = Boolean.Parse(map[Resources.K_NotificationImage]);
		OutputOnly        = Boolean.Parse(map[Resources.K_OutputOnly]);
		RestartAfterExit        = Boolean.Parse(map[Resources.K_RestartAfterExit]);

		Save();

		Debug.WriteLine($"Updated config from {FullName.Name}", C_INFO);
	}

	public Dictionary<string, string> ToMap()
	{
		var map = new Dictionary<string, string>
		{
			{ Resources.K_Engines, SearchEngines.ToString() },
			{ Resources.K_PriorityEngines, PriorityEngines.ToString() },
			{ Resources.K_Filter, Filtering.ToString() },
			{ Resources.K_Notification, Notification.ToString() },
			{ Resources.K_NotificationImage, NotificationImage.ToString() },
			{ Resources.K_OutputOnly, OutputOnly.ToString() },
			{ Resources.K_RestartAfterExit, RestartAfterExit.ToString() },

		};
		return map;
	}

	#region Settings

	/// <summary>
	/// Search engines to use
	/// </summary>
	public SearchEngineOptions SearchEngines { get; set; }

	/// <summary>
	/// Priority engines
	/// </summary>
	public SearchEngineOptions PriorityEngines { get; set; }

	/// <summary>
	/// Filters any non-primitive results.
	/// Filtered results are determined by <see cref="SearchResult.IsNonPrimitive"/>.
	/// </summary>
	public bool Filtering { get; set; } = true;

	public bool Notification { get; set; } = true;

	public bool NotificationImage { get; set; }

	public bool OutputOnly { get; set; }

	public bool RestartAfterExit { get; set; } = true;

	#endregion

	public override string ToString()
	{
		return Strings.GetMapString(ToMap());
	}

	public static string AppFolder
		=> Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
		                Resources.Name); //todo
}