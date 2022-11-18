using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.Versioning;
using Windows.ApplicationModel.Background;
using Flurl.Http;
using Kantan.Net;
using Kantan.Net.Utilities;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.Notifications;
using Novus;
using Novus.FileTypes;
using SmartImage.Lib;

namespace SmartImage.App;

// ReSharper disable PossibleNullReferenceException

[SupportedOSPlatform(Global.OS_WIN)]
internal static class AppToast
{
	internal static async Task ShowAsync(object sender, UniFile[] args)
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
		}

		builder.AddButton(button)
		       .AddButton(button2);

		builder.SetBackgroundActivation();
		builder.Show();

	}

	private static async Task AddImageAsync(ToastContentBuilder builder, UniFile uf)
	{
		var file = await uf.DownloadAsync();

		builder.AddHeroImage(new Uri(file));

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

	[method: SupportedOSPlatform("windows10.0.10240.0")]
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
}