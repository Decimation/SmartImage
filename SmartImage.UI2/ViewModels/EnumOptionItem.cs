// Author: Deci | Project: SmartImage.UI2 | Name: EnumOptionItem.cs
// Date: 2026/08/22 @ 03:08:03

using System;
using System.Reflection;
using Kantan.Utilities;
using ReactiveUI;
using SmartImage.Lib;
using SmartImage.Lib.Engines.Search.Base;

namespace SmartImage.UI2.ViewModels;


public class EnumOptionItem<TEnum> : ReactiveObject where TEnum : struct, Enum
{

	private readonly SearchConfig m_config;

	public TEnum Option { get; }

	public string Name => Option.ToString();

	protected PropertyInfo Prop { get; }

	public EnumOptionItem(SearchConfig config, TEnum option, PropertyInfo prop)
	{
		m_config = config;
		Option   = option;
		Prop     = prop;

		m_config.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == prop.Name) {
				this.RaisePropertyChanged(nameof(IsChecked));
			}
		};
	}

	public EnumOptionItem(SearchConfig config, TEnum option, string name) 
		: this(config, option, config.GetType().GetProperty(name)) { }

	protected TEnum GetValue()
	{
		var gm = (TEnum) Prop.GetMethod.Invoke(m_config, null);
		return gm;
	}

	protected object SetValue(TEnum t)
	{
		var gm = Prop.SetMethod.Invoke(m_config, [t]);
		return gm;
	}

	public bool IsChecked
	{
		get
		{
			var gm = GetValue();
			return gm.HasFlag(Option);
		}
		set
		{
			if (value == IsChecked) {
				return;
			}

			var val    = GetValue();
			var newVal = value ? val.Or(Option) : val.And(Option.Not());
			SetValue(newVal);

			this.RaisePropertyChanged();
		}
	}

}