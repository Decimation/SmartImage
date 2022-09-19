﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace SmartImage.Lib;

public sealed class SearchConfig
{
	/// <summary>
	/// Default value for <see cref="SearchEngines"/>
	/// </summary>
	public const SearchEngineOptions SE_DEFAULT = SearchEngineOptions.All;

	/// <summary>
	/// Default value for <see cref="PriorityEngines"/>
	/// </summary>
	public const SearchEngineOptions PE_DEFAULT = SearchEngineOptions.Auto;

	/// <summary>
	/// Engines used to search.
	/// </summary>
	public SearchEngineOptions SearchEngines { get; set; } = SE_DEFAULT;

	/// <summary>
	/// Engines whose results are opened in the default browser.
	/// </summary>
	public SearchEngineOptions PriorityEngines { get; set; } = PE_DEFAULT;

	/// <summary>
	/// Keeps console window on-top.
	/// </summary>
	public bool OnTop { get; set; }

	public SearchConfig() { }

	public override string ToString()
	{
		return $"{nameof(SearchEngines)}: {SearchEngines}, \n" +
		       $"{nameof(PriorityEngines)}: {PriorityEngines}";
	}
}