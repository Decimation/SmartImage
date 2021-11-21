using System.Diagnostics;
using System.Drawing;
using Kantan.Diagnostics;
using Kantan.Net;
using Kantan.Numeric;
using Microsoft.Toolkit.Uwp.Notifications;
using SmartImage.Lib;
using SmartImage.Lib.Utilities;

namespace SmartImage.UI;

internal static class AppToast
{
	internal static void ShowToast(object sender, SearchCompletedEventArgs args)
	{
		Debug.WriteLine($"Building toast", LogCategories.C_DEBUG);
		var bestResult = args.Detailed;

		var builder = new ToastContentBuilder();
		var button  = new ToastButton();
		var button2 = new ToastButton();

		button2.SetContent("Dismiss")
		       .AddArgument(AppInterface.Elements.ARG_KEY_ACTION, AppInterface.Elements.ARG_VALUE_DISMISS);

		button.SetContent("Open")
		      .AddArgument(AppInterface.Elements.ARG_KEY_ACTION, $"{bestResult.Value.Url}");

		builder.AddButton(button)
		       .AddButton(button2)
		       .AddText("Search complete")
		       .AddText($"{bestResult}")
		       .AddText($"Results: {Program.Client.Results.Count}");

		if (Program.Config.Notification && Program.Config.NotificationImage) {

			var imageResult = args.FirstDirect.Value;

			if (imageResult != null) {
				var path = Path.GetTempPath();

				string file = ImageHelper.Download(imageResult.Direct, path);

				if (file == null) {
					int i = 0;

					var imageResults = args.Direct.Value;

					do {
						file = ImageHelper.Download(imageResults[i++].Direct, path);

					} while (String.IsNullOrWhiteSpace(file) && i < imageResults.Length);

				}

				if (file != null) {

					file = GetHeroImage(path, file);

					Debug.WriteLine($"{nameof(AppInterface)}: Downloaded {file}", LogCategories.C_INFO);

					builder.AddHeroImage(new Uri(file));

					AppDomain.CurrentDomain.ProcessExit += (sender2, args2) =>
					{
						File.Delete(file);
					};
				}

			}


		}

		builder.SetBackgroundActivation();

		//...

		builder.Show();

		// ToastNotificationManager.CreateToastNotifier();
	}

	private static string GetHeroImage(string path, string file)
	{
		var  bytes     = File.ReadAllBytes(file).Length;
		var  kiloBytes = MathHelper.ConvertToUnit(bytes, MetricPrefix.Kilo);
		bool tooBig    = kiloBytes >= MAX_IMG_SIZE_KB;

		if (tooBig) {
			var    bitmap  = new Bitmap(file);
			var    newSize = new Size(Convert.ToInt32(bitmap.Width / 2), Convert.ToInt32(bitmap.Height / 2));
			Bitmap bitmap2 = ImageHelper.ResizeImage(bitmap, newSize);

			if (bitmap2 != null) {
				string s = Path.Combine(path, Path.GetTempFileName());
				bitmap2.Save(s, System.Drawing.Imaging.ImageFormat.Jpeg);
				bytes     = File.ReadAllBytes(file).Length;
				kiloBytes = MathHelper.ConvertToUnit(bytes, MetricPrefix.Kilo);

				Debug.WriteLine($"-> {bytes} {kiloBytes} | {s}");
				file = s;
			}
				
		}

		return file;
	}

	internal static void OnToastActivated(ToastNotificationActivatedEventArgsCompat compat)
	{
		// NOTE: Does not return if invoked from background

		// Obtain the arguments from the notification

		var arguments = ToastArguments.Parse(compat.Argument);

		foreach (var argument in arguments) {
			Debug.WriteLine($"Toast argument: {argument}", LogCategories.C_DEBUG);

			if (argument.Key == AppInterface.Elements.ARG_KEY_ACTION) {

				if (argument.Value == AppInterface.Elements.ARG_VALUE_DISMISS) {
					break;
				}

				WebUtilities.OpenUrl(argument.Value);
			}
		}

		if (ToastNotificationManagerCompat.WasCurrentProcessToastActivated()) {
			//
			Environment.Exit(0);
		}
	}

	private const int MAX_IMG_SIZE_KB = 200;
}