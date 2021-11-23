using System.Diagnostics;
using System.Drawing;
using System.Text;
using Windows.ApplicationModel.Background;
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

		
		var sb = new StringBuilder();

		string url = null;


		if (args.Detailed.Any()) {
			var detailed = args.Detailed.First();
			url = detailed.Url.ToString();
			sb.Append(detailed);
		}
		else if (args.Results.Any()) {
			var result = args.Results.First();
			url = result.PrimaryResult.Url.ToString();
			sb.Append(result);
		}


		button.SetContent("Open")
		      .AddArgument(Elements.ARG_KEY_ACTION, $"{url}");

		builder.AddButton(button)
		       .AddButton(button2)
		       .AddText("Search complete")
		       .AddText($"{sb}")
		       .AddText($"Results: {Program.Client.Results.Count}");

		if (Program.Config.Notification && Program.Config.NotificationImage) {

			var directResults = await Program.Client.WaitForDirectResults();

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
				// NOTE: The file size limit doesn't seem to actually matter...

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
			
			// ToastNotificationManagerCompat.History.Clear();
			// Environment.Exit(0);

			// Closes toast ...

			return;
		}
	}

	private static async void RegisterBackground()
	{
		const string taskName = "ToastBackgroundTask";

		// If background task is already registered, do nothing
		if (BackgroundTaskRegistration.AllTasks.Any(i => i.Value.Name.Equals(taskName)))
			return;

		// Otherwise request access
		BackgroundAccessStatus status = await BackgroundExecutionManager.RequestAccessAsync();

		// Create the background task
		BackgroundTaskBuilder builder = new BackgroundTaskBuilder()
		{
			Name = taskName
		};

		// Assign the toast action trigger
		builder.SetTrigger(new ToastNotificationActionTrigger());

		// And register the task
		BackgroundTaskRegistration registration = builder.Register();

		// todo
	}


}