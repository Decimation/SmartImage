// Read S SmartImage.UI ResultItem.cs
// 2023-07-17 @ 5:54 PM

using System;
using System.Drawing;
using System.IO;
using System.Net.Cache;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using Flurl;
using Kantan.Net.Utilities;
using Novus.FileTypes;
using SmartImage.Lib.Results;
using SmartImage.Lib.Utilities;

namespace SmartImage.UI;

public class ResultItem : IDisposable
{
	public string Name { get; }

	public SearchResultItem Result { get; }

	public SearchResultStatus Status { get; }

	public BitmapImage StatusImage { get; internal set; }

	// public Url? Url => Uni != null ? Uni.Value.ToString() : Result.Url;
	public Url? Url { get; protected set; }

	public bool CanScan     { get; internal set; }
	public bool CanOpen     { get; internal set; }
	public bool CanDownload { get; internal set; }

	public ResultItem(SearchResultItem result, string name)
	{
		Result  = result;
		Name    = name;
		Status  = result.Root.Status;
		Url     = result.Url;
		CanOpen = Url.IsValid(Url);
		CanScan = true;

		if (Status.IsSuccessful()) {
			StatusImage = AppComponents.accept;
		}
		else if (Status.IsUnknown()) {
			StatusImage = AppComponents.help;
		}
		else if (Status.IsError()) {
			StatusImage = AppComponents.exclamation;
		}

	}

	public bool Open()
	{
		return HttpUtilities.TryOpenUrl(Url);

	}

	public virtual void Dispose()
	{
		Result.Dispose();
	}
}

public class UniResultItem : ResultItem
{
	public UniResultItem(ResultItem ri, int? idx)
		: base(ri.Result, $"{ri.Name} ({idx})")
	{
		UniIndex = idx;
		if (Uni is {}) {
			if (Uni.IsStream) {
				Url = ri.Url.GetFileName().Split(':')[0];

				if (Path.GetExtension(Url) == null) {
					Url = Path.ChangeExtension(Url, Uni.FileTypes[0].Name);
				}
			}
			else {
				Url = Uni.Value.ToString();

			}

			Image= new BitmapImage()
				{ };
			Image.BeginInit();
			Image.StreamSource = Uni.Stream;
			// m_image.StreamSource   = Query.Uni.Stream;
			Image.CacheOption    = BitmapCacheOption.OnLoad;
			Image.UriCachePolicy = new RequestCachePolicy(RequestCacheLevel.Default);
			Image.EndInit();
		}
		StatusImage = AppComponents.picture;
	}

	public BitmapImage Image
	{
		get;
		private set;

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

	public          int? UniIndex  { get; }
	public override void Dispose()
	{
		base.Dispose();
		Image = null;
	}
}
