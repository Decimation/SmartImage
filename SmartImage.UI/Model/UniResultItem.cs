// Deci SmartImage.UI UniResultItem.cs
// $File.CreatedYear-$File.CreatedMonth-25 @ 4:3

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Cache;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Flurl;
using Kantan.Net.Utilities;
using Kantan.Utilities;
using Novus.FileTypes;
using Novus.FileTypes.Uni;
using Novus.OS;
using Novus.Streams;
using Novus.Win32;
using SmartImage.Lib.Images;
using SmartImage.Lib.Model;
using SmartImage.Lib.Utilities;
using SmartImage.UI.Controls;

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

	public BinaryImageFile? Uni
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
					Url = Path.ChangeExtension(Url, Uni.Info.FileExtensions.First());
				}
			}
			else {
				Url = Uni.Value.ToString();

			}

			// StatusImage = Image;
		}
		else {
			Image = new Lazy<BitmapImage?>(default(BitmapImage?));
		}

		StatusImage = AppComponents.picture;

		// SizeFormat  = ControlsHelper.FormatSize(Uni);
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
		Label = $"Download complete";

		IsThumbnail = false;

		// Properties &= ResultItemProperties.Thumbnail;

		if (Image is { IsValueCreated: true, Value.CanFreeze: true }) {
			Image.Value.Freeze();
		}

	}

	public override BitmapImage? LoadImage()
	{

		var image = new BitmapImage()
			{ };
		image.BeginInit();
		Trace.Assert(Uni != null);

		image.StreamSource = Uni.Stream;

		// Image.StreamSource = Uni.Stream;
		// m_image.StreamSource   = Query.Uni.Stream;
		// Image.CacheOption    = BitmapCacheOption.OnLoad;
		image.CacheOption = BitmapCacheOption.OnDemand;

		// Image.CreateOptions  = BitmapCreateOptions.DelayCreation;
		image.UriCachePolicy = new RequestCachePolicy(RequestCacheLevel.Default);
		image.EndInit();

		image.DownloadFailed    += OnImageDownloadFailed;
		image.DownloadProgress  += OnImageDownloadProgress;
		image.DownloadCompleted += OnImageDownloadCompleted;

		UpdateProperties();
		return image;
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

		// CanDownload = false;
		Properties = Properties &= ~ImageSourceProperties.CanDownload;
		Download   = path2;

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