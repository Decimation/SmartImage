// Author: Deci | Project: SmartImage.UI | Name: LazyProperty.cs
// Date: $File.CreatedYear-$File.CreatedMonth-21 @ 13:0:28

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SmartImage.UI.Model;

public class LazyProperty<T> : INotifyPropertyChanged
{

	private CancellationTokenSource m_cancelTokenSource = new();

	private bool m_isLoading;
	private bool m_errorOnLoading;

	private T m_defaultValue;
	private T m_value;

	private Func<CancellationToken, Task<T>> m_retrievalFunc;

	private bool IsLoaded { get; set; }

	public bool IsLoading
	{
		get => m_isLoading;
		set
		{
			if (m_isLoading != value) {
				m_isLoading = value;
				OnPropertyChanged();
			}
		}
	}

	public bool ErrorOnLoading
	{
		get => m_errorOnLoading;
		set
		{
			if (m_errorOnLoading != value) {
				m_errorOnLoading = value;
				OnPropertyChanged();
			}
		}
	}

	public T Value
	{
		get
		{
			if (IsLoaded)
				return m_value;

			if (!m_isLoading) {
				IsLoading = true;

				LoadValueAsync().ContinueWith(t =>
				{
					if (!t.IsCanceled) {
						if (t.IsFaulted) {
							m_value        = m_defaultValue;
							ErrorOnLoading = true;
							IsLoaded       = true;
							IsLoading      = false;
							OnPropertyChanged();
						}
						else {
							Value = t.Result;
						}
					}
				});
			}

			return m_defaultValue;
		}

		// if you want a ReadOnly-property just set this setter to private
		set
		{
			if (m_isLoading)

				// since we set the value now, there is no need
				// to retrieve the "old" value asynchronously
				CancelLoading();

			if (!EqualityComparer<T>.Default.Equals(m_value, value)) {
				m_value        = value;
				IsLoaded       = true;
				IsLoading      = false;
				ErrorOnLoading = false;

				OnPropertyChanged();
			}
		}
	}

	private async Task<T> LoadValueAsync()
	{
		return await m_retrievalFunc(m_cancelTokenSource.Token);
	}

	public void CancelLoading()
	{
		m_cancelTokenSource.Cancel();
	}

	public LazyProperty(Func<CancellationToken, Task<T>> retrievalFunc, T defaultValue)
	{
		m_retrievalFunc = retrievalFunc ?? throw new ArgumentNullException(nameof(retrievalFunc));
		m_defaultValue  = defaultValue;

		m_value = default(T);
	}

	/// <summary>
	/// This allows you to assign the value of this lazy property directly
	/// to a variable of type T
	/// </summary>        
	public static implicit operator T(LazyProperty<T> p)
	{
		return p.Value;
	}

	public event PropertyChangedEventHandler PropertyChanged;

	protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

}