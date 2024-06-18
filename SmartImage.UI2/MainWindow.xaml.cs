using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using ReactiveUI;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SmartImage.UI2;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, IViewFor<ViewModel>
{

	public static readonly DependencyProperty ViewModelProperty = DependencyProperty
		.Register(nameof(ViewModel), typeof(ViewModel), typeof(MainWindow));

	public MainWindow()
	{
		InitializeComponent();
		ViewModel = new ViewModel();

		// Setup the bindings
		// Note: We have to use WhenActivated here, since we need to dispose the
		// bindings on XAML-based platforms, or else the bindings leak memory.
		this.WhenActivated(disposable => { });
	}

	public ViewModel ViewModel
	{
		get => (ViewModel) GetValue(ViewModelProperty);
		set => SetValue(ViewModelProperty, value);
	}

	object IViewFor.ViewModel
	{
		get => ViewModel;
		set => ViewModel = (ViewModel) value;
	}

}

public class ViewModel : ReactiveObject
{

	public ObservableCollection<string> Items { get; set; }

	public ReactiveCommand<string, Unit> ItemClickedCommand { get; }

	public ViewModel()
	{
		ItemClickedCommand = ReactiveCommand.Create<string>(ExecuteItemClicked);
	}

	private void ExecuteItemClicked(string item)
	{
		// Handle the item click logic here
		MessageBox.Show($"Item clicked: {item}");
	}

}