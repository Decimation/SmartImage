using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Kantan.Diagnostics;
using Kantan.Net;
using Microsoft.Toolkit.Uwp.Notifications;
using SmartImage.Lib;
using SmartImage.Lib.Utilities;
using static Kantan.Diagnostics.LogCategories;

namespace SmartImage.UX
{
	public static class AppToast
	{
		private const string ARG_KEY_ACTION = "action";

		private const string ARG_VALUE_DISMISS = "dismiss";

		public static void Show(object sender, ExtraResultEventArgs args)
		{
			var bestResult = args.Best;


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


			var direct = args.Direct?.Direct;

			if (direct != null) {

				var file = ImageHelper.Download(direct);

				Debug.WriteLine($"Downloaded {file}", LogCategories.C_INFO);

				builder.AddHeroImage(new Uri(file));

				AppDomain.CurrentDomain.ProcessExit += (sender2, args2) =>
				{
					File.Delete(file);
				};
			}

			builder.SetBackgroundActivation();

			//...


			builder.Show();

			//ToastNotificationManager.CreateToastNotifier();
		}


		public static void OnActivated(ToastNotificationActivatedEventArgsCompat compat)
		{
			// NOTE: Does not return if invoked from background

			// Obtain the arguments from the notification

			var arguments = ToastArguments.Parse(compat.Argument);

			foreach (var argument in arguments) {
				Debug.WriteLine($"Toast argument: {argument}", C_DEBUG);

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