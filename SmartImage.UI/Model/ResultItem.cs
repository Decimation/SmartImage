// $User.Name $File.ProjectName $File.FileName
// $File.CreatedYear-$File.CreatedMonth-$File.CreatedDay @ $File.CreatedHour:$File.CreatedMinute

global using CBN = JetBrains.Annotations.CanBeNullAttribute;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Cache;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Flurl;
using Flurl.Http;
using JetBrains.Annotations;
using Kantan.Net.Utilities;
using Kantan.Utilities;
using Novus.FileTypes;
using Novus.OS;
using Novus.Streams;
using SmartImage.Lib.Clients;
using SmartImage.Lib.Model;
using SmartImage.Lib.Results;
using SmartImage.Lib.Utilities;

namespace SmartImage.UI.Model;

public class ResultItem : IDisposable, INotifyPropertyChanged, IGuiImageSource, INamed, IDownloadable
{

	#region

	public string DimensionString
	{
		get => ControlsHelper.FormatDimensions(Width, Height);
	}

	private string m_label;

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

	public bool? IsThumbnail { get; protected set; }

	public int? Width { get; internal set; }

	public int? Height { get; internal set; }

	public BitmapImage? Image { get; /*protected*/ set; }

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

	public bool HasImage => Image != null;

	public virtual bool CanLoadImage => !HasImage && Url.IsValid(Result.Thumbnail);

	public string? Download { get; set; }

	public bool IsDownloaded
	{
		get => Download != null;
		set { }
	}

	public bool IsSister { get; internal init; }

	private double m_previewProgress;

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

	private static readonly object _lock = new();

	#endregion

	public ResultItem(SearchResultItem result, string name)
	{
		Result = result;
		Name   = !result.IsRaw ? name : $"{name} (Raw)";

		Url     = result.Url;
		CanOpen = Url.IsValid(Url);
		CanScan = CanOpen;

		(Width, Height) = (Result.Width, Result.Height);

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

		if (!string.IsNullOrWhiteSpace(result.Root.ErrorMessage)) {
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
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, value)) return false;
		field = value;
		OnPropertyChanged(propertyName);
		return true;
	}

	protected virtual void OnImageDownloadCompleted(object? sender, EventArgs args)
	{
		Label = $"Preview cache complete";

		if (Image is { CanFreeze: true }) {
			Image.Freeze();
		}

		CanDownload = HasImage;
		// Width       = Image.PixelWidth;
		// Height      = Image.PixelHeight;
		// OnPropertyChanged(nameof(Width));
		// OnPropertyChanged(nameof(Height));
		IsThumbnail = HasImage;
		UpdateProperties();
	}

	public void UpdateProperties()
	{
		OnPropertyChanged(nameof(CanOpen));
		OnPropertyChanged(nameof(IsDownloaded));
		OnPropertyChanged(nameof(IsSister));
		OnPropertyChanged(nameof(Label));
		OnPropertyChanged(nameof(Image));
	}

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

	public virtual bool LoadImage()
	{
		lock (_lock) {
			if (CanLoadImage) {
				// Label = $"Loading {Name}";

				/*
				 * NOTE:
				 * BitmapCreateOptions.DelayCreation does not seem to work properly so this is a workaround.
				 *
				 */

				Image = new BitmapImage()
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

			UpdateProperties();
			return HasImage;
		}
	}

	#region

	public virtual void Dispose()
	{
		GC.SuppressFinalize(this);
		Debug.WriteLine($"Disposing {Name}");
		Result.Dispose();
		Image = null;
	}

	public virtual async Task<string> DownloadAsync(string? dir = null, bool exp = true)
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

public class UniResultItem : ResultItem
{

	#region

	public override bool CanLoadImage => !HasImage && Uni != null;

	public string SizeFormat { get; }

	public string Description { get; }

	public UniSource? Uni
	{
		get
		{
			if (UniIndex.HasValue && Result.Uni != null) {
				return Result.Uni[UniIndex.Value];

			}

			return null;
		}
	}

	public string Hash { get; }

	public int? UniIndex { get; }

	#endregion

	public UniResultItem(ResultItem ri, int? idx)
		: base(ri.Result, $"{ri.Name} ({idx})")
	{
		UniIndex = idx;

		if (Uni == null) {
			Debugger.Break();
		}

		if (Uni != null) {
			if (Uni.IsStream) {
				// todo: update GetFileName
				Url = ri.Url.GetFileName().Split(':')[0];

				if (string.IsNullOrWhiteSpace(Path.GetExtension(Url))) {
					Url = Path.ChangeExtension(Url, Uni.FileType.Subtype);
				}
			}
			else {
				Url = Uni.Value.ToString();

			}

			// StatusImage = Image;
		}
		else {
			Image = null;
		}

		StatusImage = AppComponents.picture;
		SizeFormat  = ControlsHelper.FormatSize(Uni);
		Description = ControlsHelper.FormatDescription(Name, Uni, Width, Height);
		Hash        = HashHelper.Sha256.ToString(SHA256.HashData(Uni.Stream));
		Uni.Stream.TrySeek();

	}

	protected override void OnImageDownloadProgress(object? sender, DownloadProgressEventArgs args)
	{
		PreviewProgress = (args.Progress * 100.0f);
		Label           = "Download progress...";
	}

	protected override void OnImageDownloadFailed(object? sender, ExceptionEventArgs args)
	{
		PreviewProgress = 0;
		Label           = $"Download failed: {args.ErrorException.Message}";
	}

	protected override void OnImageDownloadCompleted(object? sender, EventArgs args)
	{
		Label       = $"Download complete";
		IsThumbnail = false;

		if (Image is { CanFreeze: true }) {
			Image.Freeze();
		}
	}

	public override bool LoadImage()
	{
		if (CanLoadImage) {
			Image = new BitmapImage()
				{ };
			Image.BeginInit();
			Trace.Assert(Uni != null);

			Image.StreamSource = Uni.Stream;
			// Image.StreamSource = Uni.Stream;
			// m_image.StreamSource   = Query.Uni.Stream;
			// Image.CacheOption    = BitmapCacheOption.OnLoad;
			Image.CacheOption = BitmapCacheOption.OnDemand;
			// Image.CreateOptions  = BitmapCreateOptions.DelayCreation;
			Image.UriCachePolicy = new RequestCachePolicy(RequestCacheLevel.Default);
			Image.EndInit();

			Image.DownloadFailed    += OnImageDownloadFailed;
			Image.DownloadProgress  += OnImageDownloadProgress;
			Image.DownloadCompleted += OnImageDownloadCompleted;

		}

		UpdateProperties();
		return HasImage;
	}

	public override async Task<string> DownloadAsync(string? dir = null, bool exp = true)
	{
		string path;
		Trace.Assert(Uni != null);

		if (Uni.IsStream) {
			path = Url;
		}
		else /*if (uni.IsUri)*/ {
			var url = (Url) Uni.Value.ToString();
			path = url.GetFileName();

		}

		dir ??= AppUtil.MyPicturesFolder;
		var path2 = Path.Combine(dir, path);

		var fs = File.OpenWrite(path2);
		Uni.Stream.TrySeek();

		StatusImage = AppComponents.picture_save;
		await Uni.Stream.CopyToAsync(fs);

		if (exp) {
			FileSystem.ExploreFile(path2);
		}

		await fs.DisposeAsync();
		CanDownload = false;
		Download    = path2;

		// u.Dispose();
		UpdateProperties();

		return path2;
	}

	public override void Dispose()
	{
		GC.SuppressFinalize(this);
		base.Dispose();

		Uni?.Dispose();
	}

}