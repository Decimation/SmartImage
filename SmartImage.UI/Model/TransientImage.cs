// Deci SmartImage.UI TransientImage.cs
// $File.CreatedYear-$File.CreatedMonth-25 @ 11:28

using System;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SmartImage.UI.Model;

public class TransientImage
{

	public bool HasImage => Image != null;

	public bool? IsThumbnail { get; set; }

	public bool CanDownload { get; set; }

	public int? Width => Image?.PixelWidth;

	public int? Height => Image?.PixelHeight;

	public string DimensionString => ControlsHelper.FormatDimensions(Width, Height);

	public BitmapImage? Image { get; set; }

	public double PreviewProgress { get; set; }

	public string Label { get; set; }

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

		IsThumbnail = HasImage;

		// UpdateProperties();

		/*if (Image is { }) {
			Width  = System.Drawing.Image.PixelWidth;
			Height = System.Drawing.Image.PixelHeight;
			OnPropertyChanged(nameof(DimensionString));
			OnPropertyChanged(nameof(Size));
		}*/

		Trace.WriteLine($"{this} :: {nameof(OnImageDownloadCompleted)} {args}");
	}

}

public interface ITransientImageProvider
{

	public bool TryLoadImage();

	public TransientImage TransientImage { get; set; }

	public bool CanLoadImage { get; }

}