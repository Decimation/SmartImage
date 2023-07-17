using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AngleSharp.Dom;
using Kantan.Net.Utilities;
using Novus.FileTypes;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Results;
using Url = Flurl.Url;

namespace SmartImage.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, IDisposable
{
	public MainWindow()
	{
		InitializeComponent();
		this.DataContext = this;
		Client           = new SearchClient(new SearchConfig());
		// m_results = new ConcurrentBag<SearchResult>();
		Results = new();

		Query                  =  SearchQuery.Null;
		Queue                  =  new();
		Lv_Results.ItemsSource =  Results;
		Lv_Queue.ItemsSource   =  Queue;
		Client.OnResult        += OnResult;
		Lb_Engines.ItemsSource =  Engines;
		Lb_Engines.SelectAll();
		m_queuePos = 0;
		BindingOperations.EnableCollectionSynchronization(Results, m_lock);
	}

	private readonly object                m_lock = new();
	public static    SearchEngineOptions[] Engines { get; } = Enum.GetValues<SearchEngineOptions>();

	#region

	public SearchClient Client { get; }

	public SearchConfig Config => Client.Config;

	// public readonly ConcurrentBag<SearchResult>        m_results;

	public SearchQuery Query { get; internal set; }

	public ObservableCollection<Result1> Results { get; }

	public  ObservableCollection<string> Queue { get; }
	private int                          m_queuePos;

	#endregion

	#region

	private async void Btn_Run_Click(object sender, RoutedEventArgs e)
	{
		await SetQueryAsync(Tb_Input.Text);
		Btn_Run.IsEnabled = false;

		var r = await Client.RunSearchAsync(Query);

	}

	private async void Btn_Clear_Click(object sender, RoutedEventArgs e)
	{
		Clear();
	}

	private async void Tb_Input_TextChanged(object sender, TextChangedEventArgs e)
	{
		var txt = Tb_Input.Text;
		var ok  = SearchQuery.IsValidSourceType(txt);

		if (ok) {
			await SetQueryAsync(txt);
		}
		Btn_Run.IsEnabled = ok;
	}

	private void Tb_Input_TextInput(object sender, TextCompositionEventArgs e) { }

	private void Tb_Input2_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		HttpUtilities.TryOpenUrl(Query.Upload);
	}

	private void Tb_Input_Drop(object sender, DragEventArgs e)
	{
		var f  = Enqueue(e);
		var f1 = f.FirstOrDefault();
		Tb_Input.Text = f1;
		e.Handled     = true;

	}

	private void Lv_Queue_Drop(object sender, DragEventArgs e)
	{
		Enqueue(e);
		e.Handled = true;
	}

	private void Tb_Input_DragOver(object sender, DragEventArgs e)
	{
		e.Handled = true;
	}

	private void Lv_Queue_DragOver(object sender, DragEventArgs e)
	{
		e.Handled = true;
	}

	private void Tb_Input_PreviewDragOver(object sender, DragEventArgs e)
	{
		e.Handled = true;
	}

	private void Lv_Queue_PreviewDragOver(object sender, DragEventArgs e)
	{
		e.Handled = true;
	}

	private void Lb_Engines_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		var ai = e.AddedItems.OfType<SearchEngineOptions>().Aggregate(default(SearchEngineOptions), (n, l) => n | l);

		var ri = e.RemovedItems.OfType<SearchEngineOptions>().Aggregate(default(SearchEngineOptions), (n, l) => n | l);

		if (ai.HasFlag(SearchEngineOptions.All)) {
			Lb_Engines.SelectAll();
		}

		if (ri.HasFlag(SearchEngineOptions.All)) {
			Lb_Engines.UnselectAll();
		}

		Config.SearchEngines |= (ai);
		Config.SearchEngines &= ~ri;
	}

	private void Lb_Engines2_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

	private void Btn_Restart_Click(object sender, RoutedEventArgs e)
	{
		Clear();
		Dispose();
	}

	private async void Btn_Next_Click(object sender, RoutedEventArgs e)
	{
		Clear();
		if (m_queuePos < Queue.Count && m_queuePos >= 0) {
			var next = Queue[m_queuePos++];
			Tb_Input.Text = next;
			await SetQueryAsync(next);
		}
	}

	#endregion

	#region

	private async Task SetQueryAsync(string q)
	{
		Query                     = await SearchQuery.TryCreateAsync(q);
		Pb_Status.IsIndeterminate = true;
		var u = await Query.UploadAsync();
		Tb_Input2.Text            = u;
		Pb_Status.IsIndeterminate = false;
	}

	private void OnResult(object o, SearchResult result)
	{
		Pb_Status.Value += (Results.Count / (double) Client.Engines.Length) * 10;

		lock (m_lock) {
			int i = 0;

			var allResults = result.AllResults;

			var sri1 = new SearchResultItem(result)
			{
				Url = result.RawUrl,
			};

			Results.Add(new Result1(sri1, $"{sri1.Root.Engine.Name} (Raw)"));

			foreach (SearchResultItem sri in allResults) {
				Results.Add(new Result1(sri, $"{sri.Root.Engine.Name} #{++i}"));

			}
		}
	}

	private string[] Enqueue(DragEventArgs e)
	{
		var files = GetFilesFromDrop(e);

		foreach (var s in files) {

			if (!Queue.Contains(s)) {
				Queue.Add(s);
			}
		}

		return files;
	}

	private static string[] GetFilesFromDrop(DragEventArgs e)
	{
		if (e.Data.GetDataPresent(DataFormats.FileDrop)) {

			if (e.Data.GetData(DataFormats.FileDrop, true) is string[] files && files.Any()) {

				return files;

			}
		}

		return Array.Empty<string>();
	}

	private void Clear()
	{
		Results.Clear();
		Btn_Run.IsEnabled = true;
		Tb_Input.Text     = string.Empty;
		Query.Dispose();
		Pb_Status.Value = 0;
	}

	public void Dispose()
	{
		Client.Dispose();
		Query.Dispose();
		
		Queue.Clear();
		m_queuePos = 0;

		foreach (var r1 in Results) {
			r1.Dispose();
		}

		Results.Clear();
	}

	#endregion
}

public class Query1 : IDisposable
{
	public SearchQuery Query { get; private set; }

	public string Value { get; }

	public Query1(string value)
	{
		Query = SearchQuery.Null;
		Value = value;
	}

	public async Task Get()
	{
		Query = await SearchQuery.TryCreateAsync(Value);
	}

	public void Dispose()
	{
		Query.Dispose();
	}
}

public class Result1 : IDisposable
{
	public string URL { get; }

	public string Name { get; }

	public double? Similarity { get; }

	public Result1(SearchResultItem result, string name)
	{
		Result     = result;
		Name       = name;
		URL        = Result.Url;
		Similarity = result.Similarity;
	}

	public SearchResultItem Result { get; }

	public void Dispose()
	{
		Result.Dispose();
	}
}