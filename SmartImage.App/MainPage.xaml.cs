using JetBrains.Annotations;
using SmartImage.Lib;
using Debug = System.Diagnostics.Debug;

namespace SmartImage.UI;

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

	private void OnComplete(object sender, SearchResult[] e) { }

	private static Cell[] ToCell(SearchResultItem sri)
	{
		return new Cell[]
		{
			new TextCell()
			{
				Text = sri.Url
			},
			new TextCell()
			{
				Text = sri.Description
			},
			new TextCell()
			{
				Text = sri.Similarity.ToString()
			},
			new TextCell()
			{
				Text = sri.Artist
			},
			new TextCell()
			{
				Text = sri.Character
			},
			new TextCell()
			{
				Text = sri.Site
			},
			new TextCell()
			{
				Text = sri.Source
			},
		};
	}

	private void OnResult(object sender, SearchResult result)
	{
		Pbr_Input.Progress = (double) (m_results) / m_client.Engines.Length;

		var many = result.Results.SelectMany(ToCell).ToArray();

		var view = new TableView()
			{ };

		view.Root.Add(new TableSection()
		{
			many
		});

		var viewCell = new ViewCell()
		{
			View = view
		};

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
				viewCell
			},
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

			Btn_Run.IsEnabled = true;
			Img_Input.Source  = ImageSource.FromFile(m_file.FullPath);

			Img_Input.Scale      = 0.5;
			Act_Status.IsRunning = false;

		}
	}

	private void Ent_Input_OnTextChanged(object sender, TextChangedEventArgs e)
	{
		Debug.WriteLine($"{e.OldTextValue} -> {e.NewTextValue}");

	}
}