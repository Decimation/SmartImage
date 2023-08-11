// Read S SmartImage.UI MainWindow.State.cs
// 2023-08-04 @ 1:24 PM

using System.Windows;
using SmartImage.UI.Model;

namespace SmartImage.UI;

public partial class MainWindow
{

	#region

	private ResultItem m_currentResultItem;

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