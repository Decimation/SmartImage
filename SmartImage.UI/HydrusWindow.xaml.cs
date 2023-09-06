using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Kantan.Utilities;
using Novus.Streams;
using SmartImage.UI.Model;

namespace SmartImage.UI;

/// <summary>
/// Interaction logic for HydrusWindow.xaml
/// </summary>
public partial class HydrusWindow : Window
{
	public HydrusWindow(SharedInfo s)
	{
		DataContext = this;
		InitializeComponent();
		Shared = s;
	}

	public SharedInfo Shared { get; set; }

	private void Btn_1_OnClick(object sender, RoutedEventArgs e)
	{
		/*Tb_Info.Dispatcher.InvokeAsync(async () =>
		{
			Shared.Query.Uni.Stream.TrySeek();
			var    data        = SHA256.HashData(Shared.Query.Uni.Stream);
			var hash        = HashHelper.Sha256.ToString(data);
			var    t           = await Shared.m_hydrus.GetFileAsync(hash);
			var    d           = await t.GetStreamAsync();
			var    imageSource = new BitmapImage();
			imageSource.BeginInit();
			imageSource.StreamSource = d;
			imageSource.EndInit();
			imageSource.Freeze();
			Img_Preview.Source       = imageSource;
		});*/
		e.Handled = true;
	}
}