using System.Diagnostics;
using JetBrains.Annotations;
using SmartImage.Lib;
using Debug = System.Diagnostics.Debug;

namespace SmartImage.App;

public partial class MainPage : ContentPage
{
	[CanBeNull]
	private FileResult m_file;

	private readonly SearchConfig m_cfg;

	private readonly SearchClient m_client;

	private SearchQuery m_query;

	private int m_results;

	public MainPage()
	{
		InitializeComponent();

		m_cfg    = new SearchConfig();
		m_client = new SearchClient(m_cfg);

		m_client.OnResult   += OnResult;
		m_client.OnComplete += OnComplete;

	}

	private void OnComplete(object sender, List<SearchResult> e) { }

	private void OnResult(object sender, SearchResult result)
	{
		Pbr_Input.Progress = (double) (m_results) / m_client.Engines.Length;
		
		Tv_Results.Root.Add(new TableSection()
		{
			new Cell[]
			{
				new TextCell()
				{
					Text = $"{result.Engine.Name}"
				},
				new TextCell()
				{
					Text = $"{result.Status}"
				},
			}
		});
	}

	private async void OnRunClicked(object sender, EventArgs e)
	{
		var r = await m_client.RunSearchAsync(m_query);
	}
	
	private async void OnPickFileClicked(object sender, EventArgs e)
	{
		Btn_Run.IsEnabled = false;

		m_file = await FilePicker.PickAsync(PickOptions.Images);

		if (m_file is { }) {
			Lbl_Input.Text = m_file.FullPath;
			SemanticScreenReader.Announce(Lbl_Input.Text);
			Act_Status.IsRunning = true;
			m_query              = await SearchQuery.TryCreateAsync(m_file.FullPath);
			Debug.WriteLine($"{m_query}");
			Act_Status.IsRunning = false;

			await m_query.UploadAsync();
			Act_Status.IsRunning = true;

			Debug.WriteLine($"{m_query}");

			Btn_Run.IsEnabled    = true;
			Img_Input.Source     = ImageSource.FromFile(m_file.FullPath);

			Img_Input.Scale      = 0.5;
			Act_Status.IsRunning = false;

		}
	}

	private void Ent_Input_OnTextChanged(object sender, TextChangedEventArgs e)
	{
		Debug.WriteLine($"{e.OldTextValue} -> {e.NewTextValue}");
		
	}
}