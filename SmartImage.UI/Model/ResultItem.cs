// Deci SmartImage.UI ResultItem.cs
// $File.CreatedYear-$File.CreatedMonth-11 @ 12:26

global using CBN = JetBrains.Annotations.CanBeNullAttribute;
global using USI = JetBrains.Annotations.UsedImplicitlyAttribute;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Cache;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using AngleSharp.Css;
using Flurl;
using Flurl.Http;
using JetBrains.Annotations;
using Kantan.Net.Utilities;
using Novus.OS;
using Novus.Win32;
using SmartImage.Lib.Clients;
using SmartImage.Lib.Model;
using SmartImage.Lib.Results;
using SmartImage.Lib.Utilities;

namespace SmartImage.UI.Model;
#pragma warning disable CS8618
public class ResultItem : IDisposable, INotifyPropertyChanged, ITransientImageProvider, INamed, IDownloadable, IItemSize
{

	#region

	private string m_label;

	private double m_previewProgress;

	private BitmapImage m_statusImage;

	public string Name { get; set; }

	public SearchResultItem Result { get; }

	public SearchResultStatus Status => Result.Root.Status;

	public BitmapImage StatusImage
	{
		get => m_statusImage;
		internal set
		{
			if (Equals(value, m_statusImage)) return;
			m_statusImage = value;
			OnPropertyChanged();
		}
	}

	// public Url? Url => Uni != null ? Uni.Value.ToString() : Result.Url;

	/// <summary>
	///     <see cref="SearchResultItem.Url" /> of <see cref="Result" />
	/// </summary>
	public Url? Url { get; protected set; }

	public bool CanScan { get; internal set; }

	public bool CanOpen { get; internal set; }

	public bool CanDownload { get; set; }

	public string StatusMessage { get; internal set; }

	public bool IsLowQuality => !Url.IsValid(Url) || Status.IsError() || Result.IsRaw;

	public string Label
	{
		get => m_label;
		set
		{
			if (value == m_label) return;
			m_label = value;
			OnPropertyChanged();
		}
	}

	public TransientImage TransientImage
	{
		get;
		set;
	}

	public  bool CanLoadImage => !TransientImage.HasImage && Url.IsValid(Result.Thumbnail);

	public string? Download { get; set; }

	public bool IsDownloaded
	{
		get => Download != null;
		set { }
	}

	public bool IsSister { get; internal init; }

	public double PreviewProgress
	{
		get => m_previewProgress;
		set
		{
			if (value.Equals(m_previewProgress)) return;
			m_previewProgress = value;
			OnPropertyChanged();
		}
	}

	public virtual long Size => Native.INVALID;

	private static readonly object _lock = new();

	#endregion

	public ResultItem(SearchResultItem result, string name)
	{
		Result = result;
		Name   = !result.IsRaw ? name : $"{name} (Raw)";

		Url     = result.Url;
		CanOpen = Url.IsValid(Url);
		CanScan = CanOpen;
		
		// (Width, Height) = (Result.Width, Result.Height);

		if (Status.IsSuccessful()) {
			StatusImage = AppComponents.accept;
		}
		else if (Status.IsUnknown()) {
			StatusImage = AppComponents.help;
		}
		else if (Status.IsError()) {
			StatusImage = AppComponents.exclamation;
		}
		else {
			StatusImage = AppComponents.asterisk_yellow;
		}

		StatusMessage = $"[{Status}]";

		if (!String.IsNullOrWhiteSpace(result.Root.ErrorMessage)) {
			StatusMessage += $" :: {result.Root.ErrorMessage}";
		}

	}

	public bool Open()
	{
		return FileSystem.Open(Url);

	}

	public Task<IFlurlResponse> GetResponseAsync(CancellationToken token = default)
	{
		return Url.AllowAnyHttpStatus()
			.WithAutoRedirect(true)
			.WithTimeout(TimeSpan.FromSeconds(3))
			.OnError(x =>
			{
				if (x.Exception is FlurlHttpException fx) {
					Debug.WriteLine($"{fx}");
				}

				x.ExceptionHandled = true;
			})
			.GetAsync(cancellationToken: token);
	}

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

	public void UpdateProperties()
	{
		OnPropertyChanged(nameof(CanOpen));
		OnPropertyChanged(nameof(IsDownloaded));
		OnPropertyChanged(nameof(IsSister));
		OnPropertyChanged(nameof(Label));
		OnPropertyChanged(nameof(Image));
	}

	#region 

	protected virtual void OnImageDownloadProgress(object? sender, DownloadProgressEventArgs args)
	{
		PreviewProgress = ((float) args.Progress * 100.0f);
		Label           = $"Preview cache...";
	}

	protected virtual void OnImageDownloadFailed(object? sender, ExceptionEventArgs args)
	{
		PreviewProgress = 0;
		Label           = $"Preview fetch failed: {args.ErrorException.Message}";

	}

	protected virtual void OnImageDownloadCompleted(object? sender, EventArgs args)
	{
		Label = $"Preview cache complete";

		if (Image is { CanFreeze: true }) {
			Image.Freeze();
		}

		CanDownload = HasImage;

		// OnPropertyChanged(nameof(Width));
		// OnPropertyChanged(nameof(Height));
		IsThumbnail = HasImage;

		UpdateProperties();

		if (Image is { }) {
			Width  = Image.PixelWidth;
			Height = Image.PixelHeight;
			OnPropertyChanged(nameof(DimensionString));
			OnPropertyChanged(nameof(Size));
		}

		Trace.WriteLine($"{this} :: {nameof(OnImageDownloadCompleted)} {args}");
	}

	public bool TryLoadImage()
	{
		lock (_lock) {
			if (CanLoadImage) {
				// Label = $"Loading {Name}";

				/*
				 * NOTE:
				 * BitmapCreateOptions.DelayCreation does not seem to work properly so this is a workaround.
				 *
				 */
				var Image = new BitmapImage()
					{ };

				TransientImage= new TransientImage()
					{ };

				Image.BeginInit();
				Image.UriSource = new Uri(Result.Thumbnail);
				// Image.StreamSource  = await Result.Thumbnail.GetStreamAsync();
				Image.CacheOption = BitmapCacheOption.OnDemand;
				// Image.CreateOptions = BitmapCreateOptions.DelayCreation;
				// Image.CreateOptions = BitmapCreateOptions.None;

				Image.UriCachePolicy = new RequestCachePolicy(RequestCacheLevel.Default);

				Image.EndInit();

				Image.DownloadFailed    += OnImageDownloadFailed;
				Image.DownloadProgress  += OnImageDownloadProgress;
				Image.DownloadCompleted += OnImageDownloadCompleted;
			}
			else {
				/*if (HasImage) {
					Label= $"{Result.Thumbnail}";

				}*/
			}
			Image = new BitmapImage()
				{ };
			OnPropertyChanged(nameof(DimensionString));
			UpdateProperties();
			return HasImage;
		}
	}

	#endregion

	public override string ToString()
	{
		return $"{Name} / {Result}";
	}

	#region

	public virtual void Dispose()
	{
		GC.SuppressFinalize(this);
		Debug.WriteLine($"Disposing {Name}");
		Result.Dispose();
		Image = null;
	}

	public virtual async Task<string?> DownloadAsync(string? dir = null, bool exp = true)
	{
		if (!Url.IsValid(Url) || !HasImage) {
			return null;
		}

		string path;

		path = Url.GetFileName();

		dir ??= AppUtil.MyPicturesFolder;
		var path2 = Path.Combine(dir, path);

		var encoder = new PngBitmapEncoder();
		encoder.Frames.Add(BitmapFrame.Create(Image));

		await using (var fs = new FileStream(path2, FileMode.Create)) {
			encoder.Save(fs);
		}

		StatusImage = AppComponents.picture_save;

		if (exp) {
			FileSystem.ExploreFile(path2);
		}

		CanDownload = false;
		Download    = path2;

		// u.Dispose();
		UpdateProperties();

		return path2;
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	#endregion

}