using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
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
public partial class MainWindow : Window
{

	public MainWindow()
	{
		InitializeComponent();
		DataContext = new ViewModel();

		// Setup the bindings
		// Note: We have to use WhenActivated here, since we need to dispose the
		// bindings on XAML-based platforms, or else the bindings leak memory.
		// this.WhenActivated(disposable => { });
	}

}

public abstract class ModelBase : INotifyPropertyChanged
{

	public event PropertyChangedEventHandler? PropertyChanged;

	protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, value)) return false;

		field = value;
		OnPropertyChanged(propertyName);
		return true;
	}

}

public class QueryModel : ModelBase
{

	private BitmapImage m_image;

	public BitmapImage Image
	{
		get => m_image;
		set => SetField(ref m_image, value);
	}

}

public class ViewModel : ModelBase
{

	public ObservableCollection<QueryModel> Items { get; set; }

	public ViewModel() { }

}