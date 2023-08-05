// Read S SmartImage.UI MainWindow.State.cs
// 2023-08-04 @ 1:24 PM

namespace SmartImage.UI;

public partial class MainWindow
{
	#region

	private ResultItem m_selectedResult;

	public ResultItem SelectedResult
	{
		get => m_selectedResult;
		set
		{
			if (Equals(value, m_selectedResult)) return;
			m_selectedResult = value;
			OnPropertyChanged();
		}
	}

	#endregion
}