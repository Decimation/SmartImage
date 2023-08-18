// Read S SmartImage.UI ResultItem.cs
// 2023-07-17 @ 5:54 PM

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Cache;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Flurl;
using Flurl.Http;
using Kantan.Net.Utilities;
using Kantan.Utilities;
using Novus.FileTypes;
using Novus.OS;
using Novus.Streams;
using SmartImage.Lib;
using SmartImage.Lib.Results;
using SmartImage.Lib.Utilities;

namespace SmartImage.UI.Model;

public class ResultItem : IDisposable
{
	public string Name { get; protected set; }

	public SearchResultItem Result { get; }

	public SearchResultStatus Status { get; }

	public BitmapImage StatusImage { get; internal set; }

	// public Url? Url => Uni != null ? Uni.Value.ToString() : Result.Url;

	/// <summary>
	/// <see cref="SearchResultItem.Url"/> of <see cref="Result"/>
	/// </summary>
	public Url? Url { get; protected set; }

	public bool CanScan     { get; internal set; }
	public bool CanOpen     { get; internal set; }
	public bool CanDownload { get; internal set; }

	public int? Width  { get; internal set; }
	public int? Height { get; internal set; }

	public ResultItem(SearchResultItem result, string name)
	{
		Result          = result;
		Name            = name;
		Status          = result.Root.Status;
		Url             = result.Url;
		CanOpen         = Url.IsValid(Url);
		CanScan         = true;
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

	}

	public bool Open()
	{
		return FileSystem.Open(Url);

	}

	public virtual void Dispose()
	{
		// Result.Dispose();
	}
}

public class UniResultItem : ResultItem
{
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
					Url = Path.ChangeExtension(Url, Uni.FileTypes[0].Name);
				}
			}
			else {
				Url = Uni.Value.ToString();

			}

			Image = new BitmapImage()
				{ };
			Image.BeginInit();
			Image.StreamSource = Uni.Stream;
			// m_image.StreamSource   = Query.Uni.Stream;
			Image.CacheOption    = BitmapCacheOption.OnDemand;
			Image.CreateOptions  = BitmapCreateOptions.DelayCreation;
			Image.UriCachePolicy = new RequestCachePolicy(RequestCacheLevel.Default);
			Image.EndInit();
			if (Image.CanFreeze) {
				Image.Freeze();

			}
			Width  = Image.PixelWidth;
			Height = Image.PixelHeight;
			// StatusImage = Image;
		}
		else {
			Image = null;
		}

		StatusImage = AppComponents.picture;
	}

	public string Download { get; private set; }

	public async Task<string> DownloadAsync(string? dir = null, bool exp = true)
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

		return path2;
	}

	public string Description => ControlsHelper.FormatDescription(Name, Uni, Width, Height);

	public BitmapImage? Image { get; private set; }

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

	public int? UniIndex { get; }

	public override void Dispose()
	{
		base.Dispose();
		Image = null;
		Uni?.Dispose();
	}
}