using System;
using System.Collections.Generic;
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
	}

	private SearchClient _searchClient;
	private SearchQuery  _query;

	private void Tb_Input_TextChanged(object sender, TextChangedEventArgs e) { }

	private void Tb_Input_OnDrop(object sender, DragEventArgs e) { }

	private void UIElement_OnDrop(object sender, DragEventArgs e) { }

	private async void Grid_Drop(object sender, DragEventArgs e)
	{
		if (null != e.Data && e.Data.GetDataPresent(DataFormats.FileDrop)) {
			var data = e.Data.GetData(DataFormats.FileDrop) as string[];
			e.Handled = true;
			// handle the files here!

			var v = data[0];

			Tb_Input.Text = v;

			var sq = await SearchQuery.TryCreateAsync(v);
			Img_Query.Source = new BitmapImage(new Uri(sq.Uni.Value.ToString()));
		}
	}

	private void Grid_DragOver(object sender, DragEventArgs e)
	{
		if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
			e.Effects = DragDropEffects.Copy;
			e.Handled = true;
		}
		else {
			e.Effects = DragDropEffects.None;
		}
	}
}