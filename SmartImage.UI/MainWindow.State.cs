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
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;
using SmartImage.Lib;

namespace SmartImage.UI;

public partial class MainWindow
{

	#region

	private ResultItem m_currentResult;

	// [MN]
	public ResultItem CurrentResult
	{
		get => m_currentResult;
		set
		{
			if (Equals(value, m_currentResult)) return;
			m_currentResult = value;
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
			sw.Img_Preview.Source = Image;
		}

		sw.Show();
	}

	private ResultItem? FindResult(Predicate<ResultItem> f)
	{
		if (!HasQuerySelected) {
			return null;
		}

		return CurrentQuery.Results.FirstOrDefault(t => f(t));

	}

	private int FindResultIndex(Predicate<ResultItem> f)
	{
		var r = FindResult(f);

		if (r == null || !HasQuerySelected) {
			return -1;
		}

		return CurrentQuery.Results.IndexOf(r);
	}

	#endregion

	private bool m_hasResultSelected;

	[MNNW(true, nameof(CurrentResult))]
	public bool HasResultSelected
	{
		get { return m_hasResultSelected; }
		set
		{
			m_hasResultSelected = value;
			OnPropertyChanged();
		}
	}

	#region

	public QueryModel? FindQueue(string s)
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
				var rm = new QueryModel();
				Queue.Add(rm);
				// Lb_Queue.SelectedIndex = 0;
				CurrentQuery = rm;
			});

			// CurrentQueueItem       = new ResultModel();

			/*var item = new ResultModel();
			Queue.Add(item);
			CurrentQueueItem = item;*/
		}
	}

	public bool SetQueue(string s, out QueryModel? qm)
	{
		qm = FindQueue(s);

		var b = qm == null;

		if (b) {
			qm           = new QueryModel(s);
			Queue.Add(qm);
			CurrentQuery = qm;
		}

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
		if (ShowMedia) {
			CloseMedia();
			// Me_Preview.Pause();
			// ShowMedia = false;
		}
		else { }
	}

	private void CloseMedia()
	{
		m_ctsm.Cancel();

		Me_Preview.Stop();
		// Me_Preview.Position = TimeSpan.Zero;
		Me_Preview.Close();

		Me_Preview.ClearValue(MediaElement.SourceProperty);
		Me_Preview.Source = null;
		// Me_Preview.Dispose();
		ShowMedia  = false;
		m_isPaused = false;
		m_ctsm     = new CancellationTokenSource();
	}

	private bool m_isPaused;

	private void PlayPauseMedia()
	{
		if (ShowMedia) {

			if (m_isPaused) {
				Me_Preview.Play();
				m_isPaused = false;
			}
			else {

				Me_Preview.Pause();
				m_isPaused = true;
			}
		}

	}

	#endregion

}