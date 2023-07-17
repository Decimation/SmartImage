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
using SmartImage.Lib;
using SmartImage.Lib.Results;

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
		m_results = new();

		Query                  = SearchQuery.Null;
		Lv_Results.ItemsSource = m_results;

		Client.OnResult += (o, result) =>
		{
			Pb_Status.Value += (m_results.Count / (double) Client.Engines.Length)*10;

			lock (m_lock) {
				int i = 0;

				var allResults = result.AllResults;

				var sri1 = new SearchResultItem(result)
				{
					Url = result.RawUrl,
				};

				m_results.Add(new Result1(sri1, $"{sri1.Root.Engine.Name} (Raw)"));

				foreach (SearchResultItem sri in allResults) {
					m_results.Add(new Result1(sri, $"{sri.Root.Engine.Name} #{++i}"));

				}
			}
		};

		BindingOperations.EnableCollectionSynchronization(m_results, m_lock);
	}

	private readonly object m_lock = new();

	public SearchClient Client { get; }

	// public readonly ConcurrentBag<SearchResult>        m_results;

	public SearchQuery Query { get; internal set; }

	public ObservableCollection<Result1> m_results { get; }

	private async void Btn_Run_Click(object sender, RoutedEventArgs e)
	{
		Query                     = await SearchQuery.TryCreateAsync(Tb_Input.Text);
		Pb_Status.IsIndeterminate = true;
		var u = await Query.UploadAsync();
		Tb_Input2.Text            = u;
		Pb_Status.IsIndeterminate = false;
		Btn_Run.IsEnabled         = false;

		var r = await Client.RunSearchAsync(Query);
	}

	public void Dispose()
	{
		Client.Dispose();
		Query.Dispose();
	}

	private async void Tb_Input_TextChanged(object sender, TextChangedEventArgs e) { }

	private void Tb_Input_TextInput(object sender, TextCompositionEventArgs e) { }

	private async void Btn_Clear_Click(object sender, RoutedEventArgs e)
	{
		m_results.Clear();
		Btn_Run.IsEnabled = true;
		Tb_Input.Text = string.Empty;
		Query.Dispose();
		Pb_Status.Value = 0;
	}

	private void Tb_Input2_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		
	}

	private void Tb_Input_Drop(object sender, DragEventArgs e)
	{
		if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
			var files = e.Data.GetData(DataFormats.FileDrop, true) as string[];
		}
	}
}

public class Result1
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
}