// Read S SmartImage.UI ResultItem.cs
// 2023-07-17 @ 5:54 PM

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Net.Cache;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using Flurl;
using Kantan.Net.Utilities;
using Kantan.Utilities;
using Novus.FileTypes;
using Novus.OS;
using SmartImage.Lib.Results;
using SmartImage.Lib.Utilities;

namespace SmartImage.UI;

public class ResultItem : IDisposable
{
	public string Name { get; protected set; }

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

				if (Path.GetExtension(Url) == null) {
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
			Image.Freeze();
		}
		else {
			Image = null;
		}

		StatusImage = AppComponents.picture;
	}

	public string Description
	{
		get
		{
			string bytes;

			if (!Uni.Stream.CanRead) {
				bytes = "???";
			}
			else bytes = FormatHelper.FormatBytes(Uni.Stream.Length);

			string img;

			if (Image != null)  {
				img = $"({Image.Width:F}×{Image.Height:F})";
			}else{
				img = "";
			}
			return $"{Name} ⇉ [{Uni.FileTypes[0]}] " +
			       $"[{bytes}] • {img}";
		}
	}

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