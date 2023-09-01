// Read S SmartImage.UI MainWindow.State.cs
// 2023-08-04 @ 1:24 PM

global using MN = System.Diagnostics.CodeAnalysis.MaybeNullAttribute;
global using ICBN = JetBrains.Annotations.ItemCanBeNullAttribute;
global using NN = System.Diagnostics.CodeAnalysis.NotNullAttribute;
using System.Windows;
using SmartImage.UI.Model;
using Kantan.Monad;
using System.Linq;
using System;

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

	private void OpenResultWindow(ResultItem ri)
	{
		var sw = new ResultWindow(ri)
			{ };

		if (ri is UniResultItem uri) {
			sw.Img_Preview.Source = uri.Image;
		}
		else if (ri.Image != null) {
			sw.Img_Preview.Source = ri.Image;
		}
		else {
			sw.Img_Preview.Source = m_image;
		}

		sw.Show();
	}

	private ResultItem? Find(Predicate<ResultItem> f)
	{
		return Results.FirstOrDefault(t => f(t));

	}

	private int FindIndex(Predicate<ResultItem> f)
	{
		var r = Find(f);

		if (r == null) {
			return -1;
		}

		return Results.IndexOf(r);
	}
}