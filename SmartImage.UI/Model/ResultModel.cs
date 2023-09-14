// Read S SmartImage.UI ResultModel.cs
// 2023-09-13 @ 5:28 PM

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Cache;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Flurl;
using Kantan.Text;
using SmartImage.Lib;

namespace SmartImage.UI.Model;

#pragma warning disable CS8618
public class ResultModel : INotifyPropertyChanged, IDisposable, IImageProvider
{
	//todo
	private string m_value;

	public string Value
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

	public SearchQuery? Query { get; set; }

	public BitmapImage? Image { get; set; }

	public ObservableCollection<ResultItem> Results { get; set; }

	public bool HasQuery => (Query != SearchQuery.Null) && Query != null;

	public bool CanLoadImage => !HasImage;

	public bool HasImage => Image != null;

	public bool HasValue => !string.IsNullOrWhiteSpace(Value);

	public ResultModel() : this(string.Empty) { }

	public bool IsPrimitive    => !Results.Any() && !HasQuery;

	public bool IsNonPrimitive => !IsPrimitive;
	
	public bool IsComplete => Results.Any() && HasQuery && Query.IsUploaded;
	public bool CanSearch => !Results.Any() && HasQuery && Query.IsUploaded;
	public ResultModel(string value)
	{
		Value   = value;
		Results = new ObservableCollection<ResultItem>();
		Query   = SearchQuery.Null;
	}

	#region 

	private string m_status2;

	public string Status2
	{
		get => m_status2;
		set
		{
			if (value == m_status2) return;
			m_status2 = value;
			OnPropertyChanged();
		}
	}

	private string m_status;

	public string Status
	{
		get => m_status;
		set
		{
			if (value == m_status) return;
			m_status = value;
			OnPropertyChanged();
		}
	}

	private string m_info;

	public string Info
	{
		get => m_info;
		set
		{
			if (value == m_info) return;
			m_info = value;
			OnPropertyChanged();
		}
	}

	#endregion

	public async Task LoadQueryAsync(CancellationToken ct)
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

		bool b2;

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

		Debug.Assert(Query != null);
		var uriString = Query.ValueString;
		Debug.Assert(uriString != null);

		// Tb_Preview.Text = "Rendering preview...";

		// Dispatcher.InvokeAsync(UpdateImage);
		// UpdateImage();
		Application.Current.Dispatcher.InvokeAsync(LoadImage);

		// await UploadAsync(ct);

		ret:

		Debug.WriteLine($"finished {Value}");
		return;

	}

	public async Task UploadAsync(CancellationToken ct)
	{
		Url upload;
		Status = "Uploading...";
		upload = await Query.UploadAsync(ct: ct);

		if (!Url.IsValid(upload)) {
			// todo: show user specific error message

			// Btn_Delete.IsEnabled      = true;

			Status  = "-";
			Status2 = "Failed to upload: server timed out or input was invalid";
			return;
			// return;
		}
	}

	public void UpdateInfo()
	{
		if (!HasQuery) {
			return;
		}

		Info =
			ControlsHelper.FormatDescription("Query", Query.Uni, Image?.PixelWidth, Image?.PixelHeight);
	}

	public bool LoadImage()
	{
		if (!CanLoadImage) {
			goto ret;
		}

		Image = new BitmapImage()
			{ };
		Image.BeginInit();
		Image.UriSource = new Uri(Query.ValueString);
		// Image.StreamSource   = Query.Uni.Stream;
		Image.CacheOption    = BitmapCacheOption.OnLoad;
		Image.UriCachePolicy = new RequestCachePolicy(RequestCacheLevel.Default);

		Image.EndInit();

		if (Query.Uni.IsUri) {
			Image.DownloadCompleted += (sender, args) =>
			{
				UpdateInfo();
			};

		}
		else {
			UpdateInfo();
		}

		if (Image.CanFreeze) {
			Image.Freeze();

		}

		// Img_Preview.Source = m_image;

		// UpdatePreview();

		ret:
		return HasImage;
	}

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

	public void Dispose()
	{
		PropertyChanged = null;

		foreach (var item in Results) {
			item.Dispose();
		}

		Results.Clear();
		Query?.Dispose();
		Query   = SearchQuery.Null;
		Image   = null;
		Status  = null;
		Status2 = null;
		Info    = null;

	}
}