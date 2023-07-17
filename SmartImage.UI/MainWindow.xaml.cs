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
		this.DataContext = this;
		InitializeComponent();
		Client           = new SearchClient(new SearchConfig());
		// m_results = new ConcurrentBag<SearchResult>();
		m_results        = new();

		Query                  = SearchQuery.Null;
		Lv_Results.ItemsSource = m_results;
		
		Client.OnResult += (o, result) =>
		{
			lock (m_lock) {
				m_results.Add(new Result1(result));

				// foreach (SearchResultItem sri in result.AllResults) { }
			}
		};

		BindingOperations.EnableCollectionSynchronization(m_results, m_lock);
	}

	private readonly object m_lock = new();

	public SearchClient Client { get; }

	// public readonly ConcurrentBag<SearchResult>        m_results;

	public SearchQuery Query { get; internal set; }

	public readonly ObservableCollection<Result1> m_results;

	private async void Btn_Run_Click(object sender, RoutedEventArgs e)
	{
		Query              = await SearchQuery.TryCreateAsync(Tb_Input.Text);
		Lbl_Status.Content = "🔃";
		var u = await Query.UploadAsync();
		Lbl_Status.Content = "☑";
		Btn_Run.IsEnabled  = false;

		var r = await Client.RunSearchAsync(Query);
	}

	public void Dispose()
	{
		Client.Dispose();
		Query.Dispose();
	}

	private async void Tb_Input_TextChanged(object sender, TextChangedEventArgs e) { }

	private void Tb_Input_TextInput(object sender, TextCompositionEventArgs e) { }

}

public class Result1
{
	public string URL  { get; }
	public string Name { get; }
	public Result1(SearchResult result)
	{
		Result = result;
		Name   = Result.Engine.Name;
		URL    = Result.Best?.Url;
	}
	public SearchResult Result { get; }
}