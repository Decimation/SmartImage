// Read S SmartImage.UI ResultItem.cs
// 2023-08-11 @ 12:26 PM

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Cache;
using System.Runtime.CompilerServices;
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
using SmartImage.Lib.Results;
using SmartImage.Lib.Utilities;

namespace SmartImage.UI.Model;

public class ResultItem : IDisposable, INotifyPropertyChanged, IImageProvider, INamed, IDownloadable
{

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

	private static readonly object _lock = new();

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

	public virtual void Dispose()
	{
		Debug.WriteLine($"Disposing {Name}");
		Result.Dispose();
		Image = null;
	}

	#region

	protected virtual void OnImageDownloadCompleted(object? sender, EventArgs args)
	{
		Label = $"Cache complete";

		if (Image.CanFreeze) {
			Image.Freeze();
		}

		CanDownload = HasImage;
		Width       = Image.PixelWidth;
		Height      = Image.PixelHeight;
		OnPropertyChanged(nameof(Width));
		OnPropertyChanged(nameof(Height));
		IsThumbnail = HasImage;
		UpdateProperties();
	}

	public void UpdateProperties()
	{
		OnPropertyChanged(nameof(CanOpen));
		OnPropertyChanged(nameof(IsDownloaded));
		OnPropertyChanged(nameof(IsSister));
	}

	protected virtual void OnImageDownloadProgress(object? sender, DownloadProgressEventArgs args)
	{
		Label = $"{args.Progress}";
	}

	protected virtual void OnImageDownloadFailed(object? sender, ExceptionEventArgs args)
	{
		Label = $"{args.ErrorException.Message}";

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

	public bool IsDownloaded
	{
		get => Download != null;
		set {}
	}

	public virtual bool LoadImage()
	{
		lock (_lock) {
			if (CanLoadImage) {
				Label = $"Loading {Name}";

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
				Label = null;
			}

			UpdateProperties();
			return HasImage;
		}
	}

	#endregion

	public event PropertyChangedEventHandler? PropertyChanged;

	public bool IsSister { get; internal init; }

}

public class UniResultItem : ResultItem
{

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

	#region

	protected override void OnImageDownloadCompleted(object? sender, EventArgs args)
	{
		base.OnImageDownloadCompleted(sender, args);
		IsThumbnail = false;
	}

	public override bool LoadImage()
	{
		if (CanLoadImage) {
			Image = new BitmapImage()
				{ };
			Image.BeginInit();
			Image.StreamSource = Uni.Stream;
			// m_image.StreamSource   = Query.Uni.Stream;
			Image.CacheOption    = BitmapCacheOption.OnLoad;
			Image.CreateOptions  = BitmapCreateOptions.DelayCreation;
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

		fs.Dispose();
		CanDownload = false;
		Download    = path2;

		// u.Dispose();
		UpdateProperties();

		return path2;
	}

	#endregion

	public override void Dispose()
	{
		base.Dispose();

		Uni?.Dispose();
	}

}