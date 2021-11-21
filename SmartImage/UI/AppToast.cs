using System.Diagnostics;
using System.Drawing;
using System.Text;
using Windows.UI.Notifications;
using Kantan.Net;
using Kantan.Numeric;
using Kantan.Text;
using Microsoft.Toolkit.Uwp.Notifications;
using SmartImage.Lib;
using SmartImage.Lib.Searching;
using SmartImage.Lib.Utilities;
using static Kantan.Diagnostics.LogCategories;
using static SmartImage.UI.AppInterface;

namespace SmartImage.UI;

internal static class AppToast
{
	internal static async void ShowToast(object sender, SearchCompletedEventArgs args)
	{
		Debug.WriteLine($"Building toast", C_DEBUG);


		var builder = new ToastContentBuilder();
		var button  = new ToastButton();
		var button2 = new ToastButton();

		button2.SetContent("Dismiss")
		       .AddArgument(Elements.ARG_KEY_ACTION, Elements.ARG_VALUE_DISMISS);


		var    sb  = new StringBuilder();
		string url = null;


		if (args.Results.Any()) {
			var b = args.Results.First();
			url = b.PrimaryResult.Url.ToString();
		}

		button.SetContent("Open")
		      .AddArgument(Elements.ARG_KEY_ACTION, $"{url}");

		builder.AddButton(button)
		       .AddButton(button2)
		       .AddText("Search complete")
		       .AddText($"{sb}")
		       .AddText($"Results: {Program.Client.Results.Count}");

		if (Program.Config.Notification && Program.Config.NotificationImage) {

			var directResults = await Program.Client.WaitForDirect();

			if (!directResults.Any()) {
				goto ShowToast;
			}

			var directImage = directResults.First();
			var path        = Path.GetTempPath();

			string file = ImageHelper.Download(directImage.Direct, path);

			if (file == null) {
				int i = 0;

				do {
					file = ImageHelper.Download(directResults[i++].Direct, path);

				} while (String.IsNullOrWhiteSpace(file) && i < directResults.Count);
			}


			/**/

			if (file != null) {
				// NOTE: The file size limit doesn't seem to actually matter ...
				//file = GetHeroImage(path, file);

				Debug.WriteLine($"{nameof(AppInterface)}: Downloaded {file}", C_INFO);

				builder.AddHeroImage(new Uri(file));

				AppDomain.CurrentDomain.ProcessExit += (_, _) =>
				{
					File.Delete(file);
				};
			}


		}

		ShowToast:
		builder.SetBackgroundActivation();
		builder.Show();
	}

	private static string GetHeroImage(string folder, string filePath)
	{
		// NOTE: The file size limit doesn't seem to actually matter ...

		/*var bytes     = File.ReadAllBytes(filePath).Length;
		var kiloBytes = MathHelper.ConvertToUnit(bytes, MetricPrefix.Kilo);


		bool tooBig    = kiloBytes >= MAX_IMG_SIZE_KB;

		if (tooBig) {
			var    bitmap    = new Bitmap(filePath);
			var    newSize   = new Size(Convert.ToInt32(bitmap.Width / 2), Convert.ToInt32(bitmap.Height / 2));
			Bitmap newBitmap = ImageHelper.ResizeImage(bitmap, newSize);

			if (newBitmap != null) {
				var fileWithoutExt = Path.GetFileNameWithoutExtension(filePath);
				var ext            = Path.GetExtension(filePath);

				string newFile = Path.Combine(folder, fileWithoutExt + "-1" + ext);

				newBitmap.Save(newFile, System.Drawing.Imaging.ImageFormat.Jpeg);

				bytes     = File.ReadAllBytes(filePath).Length;
				kiloBytes = MathHelper.ConvertToUnit(bytes, MetricPrefix.Kilo);

				Debug.WriteLine($"Compressed {filePath} -> {newFile} ({kiloBytes})");

				filePath = newFile;
			}

		}*/

		return filePath;
	}

	internal static void OnToastActivated(ToastNotificationActivatedEventArgsCompat compat)
	{
		// NOTE: Does not return if invoked from background

		// Obtain the arguments from the notification

		var arguments = ToastArguments.Parse(compat.Argument);

		foreach (var argument in arguments) {
			Debug.WriteLine($"Toast argument: {argument}", C_DEBUG);

			if (argument.Key == Elements.ARG_KEY_ACTION) {

				if (argument.Value == Elements.ARG_VALUE_DISMISS) {
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