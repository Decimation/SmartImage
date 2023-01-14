using System.Collections.ObjectModel;
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

	private readonly ObservableCollection<SearchResult> m_searchResults;

	public MainPage()
	{
		InitializeComponent();

		m_cfg    = new SearchConfig();
		m_client = new SearchClient(m_cfg);
		m_searchResults = new ();

		m_client.OnResult   += OnResult;
		m_client.OnComplete += OnComplete;
		m_results           =  0;
		/*Lv_Results.ItemsSource = m_searchResults;

		Lv_Results.ItemTemplate = new DataTemplate(() =>
		{
			// Create views with bindings for displaying each property.
			Label nameLabel = new();
			nameLabel.SetBinding(Label.TextProperty, "Engine.Name");

			ListView birthdayLabel = new();

			birthdayLabel.SetBinding(ListView.ItemsSourceProperty,
			                         new Binding(nameof(SearchResult.Results), BindingMode.OneWay,
			                                     null, null));
			
			// Return an assembled ViewCell.
			return new ViewCell
			{
				View = new StackLayout
				{
					Padding     = new Thickness(0, 5),
					Orientation = StackOrientation.Horizontal,
					Children =
					{
						new StackLayout
						{
							VerticalOptions = LayoutOptions.Center,
							Spacing         = 0,
							Children =
							{
								nameLabel,
								birthdayLabel
							}
						}
					}
				}
			};
			;
		});*/
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

		m_searchResults.Add(result);
		var c = result.Results.SelectMany(ToCell);

		Tv_Results.Root.Add(new TableSection(result.Engine.Name)
		{
			c
		});

		/*var s = result.Results.Select(ToCell).Select(r => new TableSection() { r });
		var root = new TableRoot()
		{
			s
		};

		Tv_Results.Root.Add(new TableSection()
		{
			new TextCell()
			{
				Text = $"{result.Engine.Name}"
			},
			new ViewCell()
			{
				View = new TableView()
				{
					Root = root
				}
			}
		});*/
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

		m_searchResults.Clear();
	}

	private void Ent_Input_OnTextChanged(object sender, TextChangedEventArgs e)
	{
		Debug.WriteLine($"{e.OldTextValue} -> {e.NewTextValue}");

	}
}