// Author: Deci | Project: SmartImage.UI2 | Name: SearchEngineOptionItem.cs
// Date: 2026/08/22 @ 03:08:28

using ReactiveUI;
using SmartImage.Lib;
using SmartImage.Lib.Engines.Search.Base;

namespace SmartImage.UI2.ViewModels;

public sealed class SearchEngineOptionItem : ReactiveObject
{

	private readonly SearchConfig m_config;

	public SearchEngineOptions Option { get; }

	public string Name => Option.ToString();

	public SearchEngineOptionItem(SearchConfig config, SearchEngineOptions option)
	{
		m_config = config;
		Option   = option;

		m_config.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == nameof(SearchConfig.SearchEngines)) {
				this.RaisePropertyChanged(nameof(IsChecked));
			}
		};
	}

	public bool IsChecked
	{
		get => m_config.SearchEngines.HasFlag(Option);
		set
		{
			if (value == IsChecked) {
				return;
			}

			m_config.SearchEngines = value ? m_config.SearchEngines | Option : m_config.SearchEngines & ~Option;
			this.RaisePropertyChanged();
		}
	}

}