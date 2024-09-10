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
using SmartImage.Lib.Images.Uni;
using SmartImage.Lib.Model;
using SmartImage.Lib.Utilities;
using SmartImage.UI.Controls;

namespace SmartImage.UI.Model;

#pragma warning disable CS8618
public class UniResultItem : ResultItem
{

	#region

	public override bool CanLoadImage => !HasImage && Uni != null;

	public override long Size
	{
		get
		{
			if (Uni != null) {
				return Uni.Stream.Length;
			}

			return Native.ERROR_SV;
		}
	}

	public UniImage? Uni
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

				if (String.IsNullOrWhiteSpace(Path.GetExtension(Url)) && Uni.HasImageFormat) {

					Url = Path.ChangeExtension(Url, Uni.ImageFormat?.FileExtensions.First());
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
		Hash = HashHelper.Sha256.ToString(SHA256.HashData(Uni.Stream));
		Uni.Stream.TrySeek();

	}

	protected override void OnImageDownloadProgress(object? sender, DownloadProgressEventArgs args)
	{
		PreviewProgress = (args.Progress);
		PreviewText     = $"Download progress...{PreviewProgress}";
	}

	protected override void OnImageDownloadFailed(object? sender, ExceptionEventArgs args)
	{
		PreviewProgress = 0;
		PreviewText     = $"Download failed: {args.ErrorException.Message}";
	}

	protected override void OnImageDownloadCompleted(object? sender, EventArgs args)
	{
		PreviewText = $"Download complete";

		IsThumbnail = false;
		OnPropertyChanged(nameof(IsThumbnail));

		// Properties &= ResultItemProperties.Thumbnail;

		if (Image is { CanFreeze: true }) {
			Image.Freeze();
		}

	}

	public override bool LoadImage()
	{
		if (HasImage) {
			return true;
		}
		else if (!CanLoadImage) {
			return false;
		}

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

		Image = image;
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