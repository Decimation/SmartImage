// Deci SmartImage.UI UniResultItem.cs
// $File.CreatedYear-$File.CreatedMonth-25 @ 4:3

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Cache;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Flurl;
using Kantan.Net.Utilities;
using Kantan.Utilities;
using Novus.FileTypes;
using Novus.OS;
using Novus.Streams;
using Novus.Win32;

namespace SmartImage.UI.Model;

#pragma warning disable CS8618
public class UniResultItem : ResultItem
{

	#region

	public override bool CanLoadImage => !HasImage && Uni != null;

	public string Description { get; }

	public override long Size
	{
		get
		{
			if (Uni != null) {
				return Uni.Stream.Length;
			}

			return Native.INVALID;
		}
	}

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

				if (String.IsNullOrWhiteSpace(Path.GetExtension(Url))) {
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
		// SizeFormat  = ControlsHelper.FormatSize(Uni);
		Description = ControlsHelper.FormatDescription(Name, Uni, Width, Height);
		Hash        = HashHelper.Sha256.ToString(SHA256.HashData(Uni.Stream));
		Uni.Stream.TrySeek();

	}

	#region 

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

		OnPropertyChanged(nameof(DimensionString));
		Trace.WriteLine($"{this} :: {nameof(OnImageDownloadCompleted)} {args}");
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

	#endregion

	public override async Task<string?> DownloadAsync(string? dir = null, bool exp = true)
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