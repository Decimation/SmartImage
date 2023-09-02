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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SmartImage.UI;

/// <summary>
/// Interaction logic for SharedImageControl.xaml
/// </summary>
public partial class SharedImageControl : UserControl
{
	public SharedImageControl()
	{
		InitializeComponent();
	}

	public static readonly DependencyProperty ImageSourceProperty =
		DependencyProperty.Register(nameof(ImageSource), typeof(ImageSource), typeof(SharedImageControl),
		                            new PropertyMetadata(null));

	public ImageSource ImageSource
	{
		get { return (ImageSource) GetValue(ImageSourceProperty); }
		set { SetValue(ImageSourceProperty, value); }
	}
}