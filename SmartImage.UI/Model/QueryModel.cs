// Read S SmartImage.UI ResultModel.cs
// 2023-09-13 @ 5:28 PM

global using ISImage = SixLabors.ImageSharp.Image;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Net.Cache;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using Flurl;
using Kantan.Text;
using Novus.Win32;
using SmartImage.Lib;
using SmartImage.Lib.Model;
using SmartImage.UI.Controls;
using Application = System.Windows.Application;

namespace SmartImage.UI.Model;

#pragma warning disable CS8618
public class QueryModel : INotifyPropertyChanged, IDisposable, IBitmapImageSource, INamed, IItemSize
{

	//todo
	private string? m_value;

	public string? Value
	{
		get => m_value;
		set
		{
			value = value?.CleanString();
			if (value == m_value) return;

			m_value = value;
			OnPropertyChanged();

			// OnPropertyChanged(nameof(HasValue));
		}
	}

	private SearchQuery? m_query;

	public SearchQuery? Query
	{
		get => m_query;
		set
		{
			if (Equals(value, m_query)) return;

			m_query = value;
			OnPropertyChanged();
			UpdateProperties();
		}
	}

	public long Size
	{
		get
		{
			if (HasQuery) {
				return Query.Image.Size;
			}

			return Native.INVALID;
		}
	}

	public void UpdateProperties()
	{
		OnPropertyChanged(nameof(HasQuery));
		OnPropertyChanged(nameof(IsComplete));
		OnPropertyChanged(nameof(CanSearch));
		OnPropertyChanged(nameof(IsPrimitive));
		OnPropertyChanged(nameof(Results));
		OnPropertyChanged(nameof(CanDelete));
		OnPropertyChanged(nameof(Query));

	}

	public Lazy<BitmapImage?> Image { get; set; }

	private ObservableCollection<ResultItem> m_results;

	public ObservableCollection<ResultItem> Results
	{
		get => m_results;
		set
		{
			if (Equals(value, m_results)) return;

			m_results = value;

			OnPropertyChanged();
			OnPropertyChanged(nameof(IsPrimitive));
			OnPropertyChanged(nameof(IsComplete));
			OnPropertyChanged(nameof(CanSearch));
		}
	}

	[MNNW(true, nameof(Query))]
	public bool HasQuery => (Query != SearchQuery.Null) && Query != null;

	[MNNW(true, nameof(Query))]
	public bool HasInitQuery => HasQuery && Query.IsUploaded;

	public bool CanLoadImage => !HasImage && HasQuery;

	public bool IsThumbnail => false;

	public int? Width => HasImage ? Image.Value.PixelWidth : null;

	public int? Height => HasImage ? Image.Value.PixelHeight : null;

	public ImageSourceProperties Properties
	{
		get => throw new NotImplementedException();
		set => throw new NotImplementedException();
	}

	[MNNW(true, nameof(Image))]
	public bool HasImage => Image != null;

	[MNNW(true, nameof(Value))]
	public bool HasValue => !String.IsNullOrWhiteSpace(Value);

	public bool IsPrimitive => !Results.Any() && !HasQuery;

	public bool IsComplete => Results.Any() && HasQuery && Query.IsUploaded;

	public bool CanDelete => HasQuery && Query is { Image.IsFile: true };

	public bool CanSearch => !Results.Any() && HasInitQuery;

	public string Name
	{
		get
		{
			/*if (HasQuery && Query.HasUni) {
				return Query.Uni.Name;
			}*/

			return "(query)";
		}
		set { throw new InvalidOperationException(); }
	}

	public QueryModel() : this(String.Empty) { }

	public QueryModel(string value)
	{
		Value   = value;
		Results = [];
		Query   = SearchQuery.Null;
		Status  = null;
		Status2 = null;

		// Dim          = null;
		Image = new Lazy<BitmapImage?>(LoadImage, LazyThreadSafetyMode.ExecutionAndPublication);
	}

	#region

	private string? m_status2;

	public string? Status2
	{
		get => m_status2;
		set
		{
			if (value == m_status2) return;

			m_status2 = value;
			OnPropertyChanged();
		}
	}

	private string? m_status;

	public string? Status
	{
		get => m_status;
		set
		{
			if (value == m_status) return;

			m_status = value;
			OnPropertyChanged();
		}
	}

	#endregion

	private bool m_invalid;

	public bool Invalid
	{
		get => m_invalid;
		internal set
		{
			if (value == m_invalid) return;

			m_invalid = value;
			OnPropertyChanged();
		}
	}

	public async Task<bool> LoadQueryAsync(CancellationToken ct)
	{
		/*if (query == Query?.ValueString)
		{
			return;
		}*/

		/*if (await m_us.WaitAsync(TimeSpan.Zero)) {
			Debug.WriteLine($"blocking");
			return;
		}*/

		// Lb_Queue.IsEnabled = false;
		// Btn_Run.IsEnabled = false;

		if (HasQuery) {
			goto ret;
		}

		// LoadAttempts++;

		// bool queryExists = b2 = m_queries.TryGetValue(query, out var existingQuery);

		Status2 = null;

		/*if (queryExists)
		{
			// Require.NotNull(existingQuery);
			// assert(existingQuery!= null);
			Debug.Assert(existingQuery != null);
			Query = existingQuery;
		}

		else
		{

			Query = await SearchQuery.TryCreateAsync(Value, ct);
			// Pb_Status.Foreground      = new SolidColorBrush(Colors.Green);
			// Pb_Status.IsIndeterminate = true;
			// queryExists = Query != SearchQuery.Null;
		}*/

		Query = await SearchQuery.TryCreateAsync(Value, ct);
		Url upload;

		// Debug.Assert(Query != null);

		var uriString = Query.Image.ValueString;

		if (Query == null || String.IsNullOrWhiteSpace(uriString)) {
			Invalid = true;
			return false;
		}

		// Debug.Assert(uriString != null);

		// Tb_Preview.Text = "Rendering preview...";

		// Dispatcher.InvokeAsync(UpdateImage);
		// UpdateImage();
		// Application.Current.Dispatcher.Invoke(LoadImage);

		// await UploadAsync(ct);

	ret:

		Debug.WriteLine($"finished {Value}");
		return true;

	}

	public async Task<bool> UploadAsync(CancellationToken ct)
	{
		Url? upload = null;
		Status = "Uploading...";
		string? emsg = null;

		if (!HasQuery) {
			goto ret;
		}

		try {
			upload = await Query.UploadAsync(ct: ct);
		}
		catch (Exception e) {
			emsg = e.Message;
		}

	ret:

		if (!Url.IsValid(upload)) {
			// todo: show user specific error message

			// Btn_Delete.IsEnabled      = true;

			Status  = ControlsHelper.STR_NA;
			Status2 = R3.Msg_Timeout1;

			var res = MessageBox.Show($"{emsg}\nChoose a different server then click [Reload].",
			                          "Failed to upload",
			                          MessageBoxButton.OK, MessageBoxImage.Error);
			return false;

			// return;
		}

		return true;
	}

	/*public void UpdateInfo()
	{
		if (!HasQuery) {
			return;
		}

		// Dim =
		// 	ControlsHelper.FormatDescription("Query", Query.Uni, Image?.PixelWidth, Image?.PixelHeight);
	}*/

	public BitmapImage? LoadImage()
	{
		Trace.Assert(HasQuery);

		var image = new BitmapImage()
			{ };
		image.BeginInit();
		image.UriSource = new Uri(Query.Image.ValueString);

		// Image.StreamSource   = Query.Uni.Stream;
		image.CacheOption    = BitmapCacheOption.OnLoad;
		image.UriCachePolicy = new RequestCachePolicy(RequestCacheLevel.Default);

		image.EndInit();

		Trace.Assert(Query != null);

		if (Query.Image.IsUri) {
			image.DownloadCompleted += (sender, args) =>
			{
				UpdateProperties();

				// UpdateInfo();
			};

		}
		else {
			UpdateProperties();

			// UpdateInfo();
		}

		if (image.CanFreeze) {
			image.Freeze();

		}

		// Img_Preview.Source = m_image;

		// UpdatePreview();	
		return image;
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		var eventArgs = new PropertyChangedEventArgs(propertyName);
		PropertyChanged?.Invoke(this, eventArgs);

		// Debug.WriteLine($"{this} :: {eventArgs.PropertyName}");
	}

	protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, value)) return false;

		field = value;
		OnPropertyChanged(propertyName);
		return true;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Value);
	}

	public void ClearResults()
	{
		foreach (var r in Results) {
			r.Dispose();
		}

		Results.Clear();
		UpdateProperties();
	}

	public void Dispose()
	{
		GC.SuppressFinalize(this);

		ClearResults();

		if (HasQuery) {
			Query.Upload = null;
			Query.Dispose();

		}

		Query   = SearchQuery.Null;
		Image   = null;
		Status  = null;
		Status2 = null;

		// Dim    = null;

		PropertyChanged = null;
	}

}