using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SmartImage.UI2.ViewModels;

namespace SmartImage.UI2
{
	public partial class App : Application
	{
		public override void Initialize()
		{
			AvaloniaXamlLoader.Load(this);
		}

		public override void OnFrameworkInitializationCompleted()
		{
			if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
			{
				desktop.MainWindow = new Views.MainWindow()
				{
					DataContext = new MainWindowViewModel()
				};
			}

			base.OnFrameworkInitializationCompleted();
		}
	}
}