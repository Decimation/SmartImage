// Read S SmartImage Notification.cs
// 2022-11-10 @ 11:27 PM

// using Windows.ApplicationModel.Background;
// using Windows.UI.Notifications;

// using Windows.UI.Notifications;
// using CommunityToolkit.WinUI.Notifications;

namespace SmartImage.App;

// ReSharper disable PossibleNullReferenceException
// [SupportedOSPlatform(WIN_VER)]
/*
internal static class AppNotification
{
	// public const string WIN_VER = "windows10.0.10240.0";

	internal static async Task ShowAsync(object sender, UniSource[] args)
	{
		Debug.WriteLine($"Building toast", nameof(ShowAsync));

		var builder = new ToastContentBuilder();
		var button  = new ToastButton();
		var button2 = new ToastButton();

		button2.SetContent("Dismiss")
		       .AddArgument(ARG_KEY_ACTION, ARG_VALUE_DISMISS);

		builder.AddText("Search Complete");
		// builder.AddText($"{sender}");

		if (args.Any()) {

			var result = args.First();

			string url = result.Value;

			button.SetContent("Open")
			      .AddArgument(ARG_KEY_ACTION, $"{url}");

			builder.AddAttributionText($"{url}")
			       .AddText($"Direct Results: {args.Length}");

			await AddImageAsync(builder, result);
			builder.AddButton(button);
		}

		builder.AddButton(button2);

		builder.SetBackgroundActivation();
		builder.Show();
	}
	public static void SendUpdatableToastWithProgress()
	{
		// Define a tag (and optionally a group) to uniquely identify the notification, in order update the notification data later;
		string tag   = "weekly-playlist";
		string group = "downloads";

		// Construct the toast content with data bound fields
		var content = new ToastContentBuilder()
		              .AddText("Downloading your weekly playlist...")
		              .AddVisualChild(new AdaptiveProgressBar()
		              {
			              Title               = "Weekly playlist",
			              Value               = new BindableProgressBarValue("progressValue"),
			              ValueStringOverride = new BindableString("progressValueString"),
			              Status              = new BindableString("progressStatus")
		              })
		              .GetToastContent();

		// Generate the toast notification
		var toast = new ToastNotification(content.GetXml());

		// Assign the tag and group
		toast.Tag   = tag;
		toast.Group = group;

		// Assign initial NotificationData values
		// Values must be of type string
		toast.Data                               = new NotificationData();
		toast.Data.Values["progressValue"]       = "0.6";
		toast.Data.Values["progressValueString"] = "15/26 songs";
		toast.Data.Values["progressStatus"]      = "Downloading...";

		// Provide sequence number to prevent out-of-order updates, or assign 0 to indicate "always update"
		toast.Data.SequenceNumber = 1;
		
		// Show the toast notification to the user
		ToastNotificationManager.CreateToastNotifier().Show(toast);
	}

	private static async Task AddImageAsync(ToastContentBuilder builder, UniSource uf)
	{
		var file = await uf.DownloadAsync();
		File.Move(file, file+".jpg");
		file += file + ".jpg";
		Debug.WriteLine($"{uf.Value} {file} {uf.Stream.Length}");

		builder.AddHeroImage(new Uri(uf.Value));

		AppDomain.CurrentDomain.ProcessExit += (_, _) =>
		{
			File.Delete(file);
		};

	}

	internal static void OnActivated(ToastNotificationActivatedEventArgsCompat compat)
	{
		// NOTE: Does not return if invoked from background

		// Obtain the arguments from the notification

		var arguments = ToastArguments.Parse(compat.Argument);

		foreach (var argument in arguments) {
			Debug.WriteLine($"Toast argument: {argument}", nameof(OnActivated));

			if (argument.Key == ARG_KEY_ACTION) {

				if (argument.Value == ARG_VALUE_DISMISS) {
					break;
				}

				HttpUtilities.OpenUrl(argument.Value);
			}
		}

		if (ToastNotificationManagerCompat.WasCurrentProcessToastActivated()) {

			// ToastNotificationManagerCompat.History.Clear();
			// Environment.Exit(0);

			// Closes toast ...

			return;
		}
	}
	
	private static async void RegisterBackgroundAsync()
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
		builder.SetTrigger(new ToastNotificationActionTrigger() { });

		// And register the task
		BackgroundTaskRegistration registration = builder.Register();

		// todo
	}

	private const string ARG_KEY_ACTION    = "action";
	private const string ARG_VALUE_DISMISS = "dismiss";
}*/