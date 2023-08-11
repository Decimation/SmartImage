// Read S SmartImage.UI MainWindow.State.cs
// 2023-08-04 @ 1:24 PM

global using MN = System.Diagnostics.CodeAnalysis.MaybeNullAttribute;
global using ICBN = JetBrains.Annotations.ItemCanBeNullAttribute;
global using NN = System.Diagnostics.CodeAnalysis.NotNullAttribute;
using System.Windows;
using SmartImage.UI.Model;

namespace SmartImage.UI;

public partial class MainWindow
{
	#region

	private ResultItem m_currentResultItem;

	// [MN]
	public ResultItem CurrentResultItem
	{
		get => m_currentResultItem;
		set
		{
			if (Equals(value, m_currentResultItem)) return;
			m_currentResultItem = value;
			OnPropertyChanged();
		}
	}

	#endregion
}