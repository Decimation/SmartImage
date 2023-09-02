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
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SmartImage.UI;

/// <summary>
/// Interaction logic for PopupWindow.xaml
/// </summary>
public partial class PopupWindow : Window
{
	public PopupWindow()
	{
		DataContext = this;
		InitializeComponent();
		RenderOptions.SetBitmapScalingMode(Img_Preview, BitmapScalingMode.HighQuality);
		RenderOptions.SetBitmapScalingMode(Img_Compare, BitmapScalingMode.HighQuality);

	}

	private void Img_Preview_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
	}

	private void Img_Compare_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
	}
}