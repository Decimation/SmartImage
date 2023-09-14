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

	#region 

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

	private ResultItem? FindResult(Predicate<ResultItem> f)
	{
		return Results.FirstOrDefault(t => f(t));

	}

	private int FindResultIndex(Predicate<ResultItem> f)
	{
		var r = FindResult(f);

		if (r == null) {
			return -1;
		}

		return Results.IndexOf(r);
	}

	#endregion

	private bool m_isSelected;

	public bool IsSelected
	{
		get { return m_isSelected; }
		set
		{
			m_isSelected = value;
			OnPropertyChanged();
		}
	}

	#region 

	public ResultModel? FindQueue(string s)
	{
		var x = Queue.FirstOrDefault(x => x.Value == s);

		return x;
	}

	public void ClearQueue()
	{
		lock (Queue) {
			foreach (var kv in Queue) {
				kv.Dispose();
			}
			Lb_Queue.Dispatcher.Invoke(() =>
			{
				Lb_Queue.SelectedIndex = -1;
				Queue.Clear();
				Queue.Insert(0, new ResultModel());
				Lb_Queue.SelectedIndex = 0;
			});
			
			// CurrentQueueItem       = new ResultModel();

			/*var item = new ResultModel();
			Queue.Add(item);
			CurrentQueueItem = item;*/
		}
	}

	public bool SetQueue(string s)
	{
		var x = FindQueue(s);

		var b = x == null;

		if (b) {
			x = new ResultModel(s);
			Queue.Add(x);
		}

		CurrentQueueItem = x;

		return b;
	}

	#endregion

	#region 

	private bool m_showMedia;

	public bool ShowMedia
	{
		get => m_showMedia;
		set
		{
			if (value == m_showMedia) return;
			m_showMedia = value;
			OnPropertyChanged();
		}
	}

	private void CheckMedia()
	{
		if (ShowMedia)
		{
			Me_Preview.Stop();
			Me_Preview.Close();
			Me_Preview.Source = null;
			ShowMedia         = false;
		}
		else { }
	}

	#endregion
}