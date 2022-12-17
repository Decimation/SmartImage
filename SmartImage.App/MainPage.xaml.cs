using System.Diagnostics;
using JetBrains.Annotations;
using SmartImage.Lib;

namespace SmartImage.App;

public partial class MainPage : ContentPage
{
	[CanBeNull]
	private FileResult m_file;

	private SearchConfig m_cfg;

	private SearchClient m_client;

	private SearchQuery m_query;

	public MainPage()
	{
		InitializeComponent();

		m_cfg    = new SearchConfig();
		m_client = new SearchClient(m_cfg);
	}

	private async void OnRunClicked(object sender, EventArgs e)
	{
		var r = await m_client.RunSearchAsync(m_query);
	}

	private async void OnPickFolderClicked(object sender, EventArgs e)
	{
		Btn_Run.IsEnabled = false;

		m_file = await FilePicker.PickAsync(PickOptions.Images);

		if (m_file is { }) {
			FolderLabel.Text = m_file.FullPath;
			SemanticScreenReader.Announce(FolderLabel.Text);
			m_query = await SearchQuery.TryCreateAsync(m_file.FullPath);
			Debug.WriteLine($"{m_query}");
			await m_query.UploadAsync();
			Debug.WriteLine($"{m_query}");
			Btn_Run.IsEnabled = true;
		}
	}
}