// Author: Deci | Project: SmartImage.UI2 | Name: ReactiveEnumOption.cs
// Date: 2026/08/22 @ 03:08:03

using System;
using System.ComponentModel;
using System.Reflection;
using Kantan.Utilities;
using ReactiveUI;
using SmartImage.Lib;
using SmartImage.Lib.Engines.Search.Base;

namespace SmartImage.UI2.ViewModels;

public class ReactiveEnumOption<TEnum> : ReactiveObject where TEnum : struct, Enum
{

	private readonly INotifyPropertyChanged m_instance;

	public TEnum Option { get; }

	public string Name => Option.ToString();

	public PropertyInfo ValueProperty { get; }

	public ReactiveEnumOption(INotifyPropertyChanged instance, TEnum option, PropertyInfo property)
	{
		m_instance    = instance;
		Option        = option;
		ValueProperty = property;
		ArgumentNullException.ThrowIfNull(property);

		m_instance.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == property.Name) {
				this.RaisePropertyChanged(nameof(IsChecked));
			}
		};
	}

	public ReactiveEnumOption(INotifyPropertyChanged instance, TEnum option, string propertyName)
		: this(instance, option, instance.GetType().GetProperty(propertyName)) { }

	protected TEnum? GetValue()
	{
		var gv = (TEnum?) ValueProperty.GetMethod?.Invoke(m_instance, null);
		return gv;
	}

	protected object? SetValue(TEnum value)
	{
		var sv = ValueProperty.SetMethod?.Invoke(m_instance, [value]);
		return sv;
	}

	public bool IsChecked
	{
		get
		{
			var gm = GetValue();

			if (gm is { } gmv) {
				return gmv.HasFlag(Option);
			}

			return false;
		}
		set
		{
			if (value == IsChecked) {
				return;
			}

			var currentVal = GetValue();

			if (currentVal is { } val) {
				var newVal = value ? val.Or(Option) : val.And(Option.Not());
				SetValue(newVal);

				this.RaisePropertyChanged();

			}
		}
	}

}