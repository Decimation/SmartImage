using System.Diagnostics;
using Windows.ApplicationModel.Background;
using Kantan.Net;
using Microsoft.Toolkit.Uwp.Notifications;
using SmartImage.Lib;
using SmartImage.Lib.Utilities;

// ReSharper disable PossibleNullReferenceException

namespace SmartImage.App;

internal static class AppToast
{
	internal static void ShowToast(object sender, SearchCompletedEventArgs args)
	{
		Debug.WriteLine($"Building toast", C_DEBUG);

		var builder = new ToastContentBuilder();
		var button  = new ToastButton();
		var button2 = new ToastButton();

		button2.SetContent("Dismiss")
		       .AddArgument(ARG_KEY_ACTION, ARG_VALUE_DISMISS);


		builder.AddText("Search Complete");

		string url = null;


		if (Program.Client.DetailedResults.Any()) {
			var detailed = Program.Client.DetailedResults.First();
			url = detailed.Url.ToString();
		}
		else if (Program.Client.Results.Any()) {
			var result = Program.Client.Results.First();
			url = result.PrimaryResult.Url.ToString();

			builder.AddText($"Engine: {result.Engine}");
		}

		button.SetContent("Open")
		      .AddArgument(ARG_KEY_ACTION, $"{url}");

		builder.AddButton(button)
		       .AddButton(button2)
		       .AddAttributionText($"{url}")
		       .AddText($"Results: {Program.Client.Results.Count}");

		if (Program.Config.NotificationImage) {
			AddNotificationImage(builder);
		}

		show_toast:
		builder.SetBackgroundActivation();
		builder.Show();
	}

	private static void AddNotificationImage(ToastContentBuilder builder)
	{

		
		Task.WaitAny(Program.Client.ContinueTasks.ToArray());

		// var w = Program.Client.DirectResultsWaitHandle;
		// w.WaitOne();
		// w.Dispose();

		var directResults = Program.Client.DirectResults;

		if (!directResults.Any()) {
			return;
		}

		var directImage = directResults.OrderByDescending(x => x.PixelResolution)
		                               .First();

		var path = Path.GetTempPath();

		string file = ImageHelper.Download(directImage.DirectImage.Url, path);

		if (file == null) {
			int i = 0;

			do {
				file = ImageHelper.Download(directResults[i++].DirectImage.Url, path);

			} while (String.IsNullOrWhiteSpace(file) && i < directResults.Count);

		}

		/**/

		if (file != null) {
			// NOTE: The file size limit doesn't seem to actually matter...

			Debug.WriteLine($"{nameof(AppToast)}: Downloaded {file}", C_INFO);

			builder.AddHeroImage(new Uri(file));

			AppDomain.CurrentDomain.ProcessExit += (_, _) =>
			{
				File.Delete(file);
			};
		}
	}

	internal static void OnToastActivated(ToastNotificationActivatedEventArgsCompat compat)
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
		var builder = new BackgroundTaskBuilder()
		{
			Name = taskName
		};

		// Assign the toast action trigger
		builder.SetTrigger(new ToastNotificationActionTrigger());

		// And register the task
		BackgroundTaskRegistration registration = builder.Register();

		// todo
	}


	private const string ARG_KEY_ACTION    = "action";
	private const string ARG_VALUE_DISMISS = "dismiss";
}