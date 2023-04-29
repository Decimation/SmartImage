using System;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SmartImage.Lib;
using SmartImage.Lib.Results;

namespace SmartImage.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
	public MainWindow()
	{
		InitializeComponent();
		// Tb_Input.AllowDrop = true;
		m_sc     = new SearchClient(SearchConfig.Default);
		m_query  = SearchQuery.Null;
		m_result = new ResultItem();

		m_sc.OnResult += (sender, result) =>
		{
			Results.Add(result);
		};

		/*Binding binding = new Binding();
		binding.Source = this;
		PropertyPath path = new PropertyPath(nameof(Results));
		binding.Path = path;

		// Setup binding:
		BindingOperations.SetBinding(this.Lb_Res, ListBox.ItemsSourceProperty, binding);*/
	}

	private SearchClient m_sc;
	private SearchQuery  m_query;
	private ResultItem   m_result;

	public static ObservableCollection<SearchResult> Results { get; set; } = new ObservableCollection<SearchResult>();

	private void Tb_Input_TextChanged(object sender, TextChangedEventArgs e) { }

	private void Tb_Input_OnDrop(object sender, DragEventArgs e) { }

	private async void Tb_Input_Drop(object sender, DragEventArgs e)
	{
		if (null != e.Data && e.Data.GetDataPresent(DataFormats.FileDrop)) {
			var data = e.Data.GetData(DataFormats.FileDrop) as string[];
			e.Handled = true;
			// handle the files here!

			await SetInput(data[0]);
		}
	}

	private async Task SetInput(string v)
	{
		Tb_Input.Text     = v;
		Btn_Run.IsEnabled = false;

		m_query = await SearchQuery.TryCreateAsync(v);

		Img_Query.Source = new BitmapImage(new Uri(m_query.Uni.Value.ToString()));

		await m_query.UploadAsync();
		Btn_Run.IsEnabled = true;

	}

	private void Tb_Input_DragOver(object sender, DragEventArgs e)
	{
		if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
			e.Effects = DragDropEffects.Copy;
			e.Handled = true;
		}
		else {
			e.Effects = DragDropEffects.None;
		}
	}

	private void Btn_Clear_Click(object sender, RoutedEventArgs e)
	{
		Tb_Input.Text    = String.Empty;
		Img_Query.Source = null;
		m_query.Dispose();
	}

	private async void Btn_Run_Click(object sender, RoutedEventArgs e)
	{
		// Lb_Res.Items[0] = new Image();
		await m_sc.RunSearchAsync(m_query);
	}
}