using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Toolkit.Uwp.Notifications;
using SimpleCore.Net;

namespace SmartImage.UX
{
	public static class AppToast
	{
		private const string ARG_KEY_ACTION = "action";

		private const string ARG_VALUE_DISMISS = "dismiss";

		public static void Show()
		{
			var bestResult = Program.Client.FindBestResult();


			var builder = new ToastContentBuilder();

			var button = new ToastButton();


			var button2 = new ToastButton();


			button2.SetContent("Dismiss")
			       .AddArgument(ARG_KEY_ACTION, ARG_VALUE_DISMISS);


			button.SetContent("Open")
			      .AddArgument(ARG_KEY_ACTION, $"{bestResult.Url}");


			builder.AddButton(button)
			       .AddButton(button2)
			       .AddText("Search complete")
			       .AddText($"{bestResult}")
			       .AddText($"Results: {Program.Client.Results.Count}");

			if (Program.Config.NotificationImage) {

				var direct = Program.Client.FindDirectResult();

				Debug.WriteLine(direct);

				Debug.WriteLine(direct.Direct.ToString());


				string filename = Path.GetFileName(direct.Direct.AbsolutePath);

				var file = Path.Combine(Path.GetTempPath(), filename);

				using var wc = new WebClient();

				wc.DownloadFile(direct.Direct, file);

				Debug.WriteLine($"Downloaded {file}");

				builder.AddHeroImage(new Uri(file));

				AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
				{
					File.Delete(file);
				};
			}

			builder.SetBackgroundActivation();

			//...


			builder.Show();

			//ToastNotificationManager.CreateToastNotifier();
		}

		
		[DoesNotReturn]
		public static void OnActivated(ToastNotificationActivatedEventArgsCompat compat)
		{
			// Obtain the arguments from the notification

			var arguments = ToastArguments.Parse(compat.Argument);

			foreach (var argument in arguments) {
				Debug.WriteLine($">>> {argument}");

				if (argument.Key == ARG_KEY_ACTION) {

					if (argument.Value == ARG_VALUE_DISMISS) {
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
	}
}