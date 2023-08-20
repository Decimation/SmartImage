global using MNNW = System.Diagnostics.CodeAnalysis.MemberNotNullWhenAttribute;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
using SmartImage.UI.Model;

namespace SmartImage.UI;

/// <summary>
/// Interaction logic for SubWindow.xaml
/// </summary>
public partial class ResultWindow : Window
{
	public ResultWindow(ResultItem result)
	{
		DataContext = this;
		Result      = result;

		InitializeComponent();
	}

	public ResultItem Result { get; set; }

	[MNNW(true, nameof(UniResult))]
	public bool IsUni => Result is UniResultItem;

	public UniResultItem? UniResult => Result as UniResultItem;

	private void Btn_HyRun_Click(object sender, RoutedEventArgs e)
	{
		
	}
}