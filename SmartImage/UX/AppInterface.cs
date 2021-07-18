using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Web;
using JetBrains.Annotations;
using Kantan.Cli;
using Kantan.Diagnostics;
using Kantan.Net;
using Kantan.Utilities;
using Microsoft.Toolkit.Uwp.Notifications;
using Novus.Utilities;
using Novus.Win32;
using SmartImage.Core;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Searching;
using SmartImage.Lib.Utilities;
using SmartImage.Utilities;
using static Novus.Utilities.ReflectionOperatorHelpers;
using static Kantan.Diagnostics.LogCategories;
using static SmartImage.Program;

// ReSharper disable UnusedMember.Global
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

// ReSharper disable PossibleNullReferenceException


namespace SmartImage.UX
{
	/// <summary>
	/// Handles the main menu interface
	/// </summary>
	internal static class AppInterface
	{
		/*
		 * TODO
		 *
		 */

		private static readonly NConsoleOption[] MainMenuOptions =
		{
			new()
			{
				Name  = ">>> Run <<<",
				Color = InterfaceElements.ColorMain,
				Function = () =>
				{
					ImageQuery query = NConsole.ReadInput("Image file or direct URL", x =>
					{
						x = x.CleanString();

						(bool url, bool file) = ImageQuery.IsUriOrFile(x);
						return !(url || file);
					}, "Input must be file or direct image link");

					Config.Query = query;
					return true;
				}
			},


			CreateConfigOption(memberof(() => Config.SearchEngines), "Engines"),
			CreateConfigOption(memberof(() => Config.PriorityEngines), "Priority engines"),

			CreateConfigOption(propertyof(() => Config.Filtering), "Filter", 3),
			CreateConfigOption(propertyof(() => Config.Notification), "Notification", 4),
			CreateConfigOption(propertyof(() => Config.NotificationImage), "Notification image", 5),

			CreateConfigOption(propertyof(() => AppIntegration.IsContextMenuAdded), "Context menu", 6,
			                   (added) => AppIntegration.HandleContextMenu(
				                   added ? IntegrationOption.Remove : IntegrationOption.Add)),

			new()
			{
				Name = "Config",
				Function = () =>
				{
					//Console.Clear();

					Console.WriteLine(Config);
					NConsole.WaitForInput();

					return null;
				}
			},
			new()
			{
				Name = "Info",
				Function = () =>
				{
					//Console.Clear();

					Console.WriteLine($"Author: {AppInfo.AUTHOR}");

					Console.WriteLine($"Current version: {AppInfo.Version} ({UpdateInfo.GetUpdateInfo().Status})");
					Console.WriteLine($"Latest version: {ReleaseInfo.GetLatestRelease()}");

					Console.WriteLine();

					var di = new DirectoryInfo(AppInfo.ExeLocation);

					Console.WriteLine($"Executable location: {di.Parent.Name}");
					Console.WriteLine($"In path: {AppInfo.IsAppFolderInPath}");

					Console.WriteLine();
					Console.WriteLine(Strings.Separator);

					foreach (var utility in ImageHelper.UtilitiesMap) {
						Console.WriteLine(utility);
					}

					Console.WriteLine();
					Console.WriteLine(Strings.Separator);

					var dependencies = ReflectionHelper.DumpDependencies();

					foreach (var name in dependencies) {
						Console.WriteLine($"{name.Name} ({name.Version})");
					}

					NConsole.WaitForInput();


					return null;
				}
			},
			new()
			{
				Name = "Update",
				Function = () =>
				{
					UpdateInfo.AutoUpdate();

					return null;
				}
			},
			new()
			{
				Name = "Help",
				Function = () =>
				{
					WebUtilities.OpenUrl(AppInfo.URL_WIKI);

					return null;
				}
			},
#if DEBUG


			new()
			{
				Name = "Debug",
				Function = () =>
				{
					Config.Query = @"C:\Users\Deci\Pictures\Test Images\Test1.jpg";
					return true;
				}
			},
#endif

		};

		internal static readonly NConsoleDialog MainMenuDialog = new()
		{
			Options = MainMenuOptions,
			Header  = AppInfo.NAME_BANNER
		};


		#region Config option

		private static NConsoleOption CreateConfigOption(MemberInfo m, string name)
		{
			return new()
			{
				Name  = name,
				Color = InterfaceElements.ColorOther,
				Function = () =>
				{
					var enumOptions = NConsoleOption.FromEnum<SearchEngineOptions>();

					var selected = NConsole.ReadOptions(new NConsoleDialog
					{
						Options        = enumOptions,
						SelectMultiple = true
					});

					var enumValue = Enums.ReadFromSet<SearchEngineOptions>(selected);
					var field     = Config.GetType().GetResolvedField((m).Name);
					field.SetValue(Config, enumValue);

					Console.WriteLine(enumValue);

					NConsole.WaitForSecond();

					Debug.Assert((SearchEngineOptions) field.GetValue(Config) == enumValue);

					AppConfig.UpdateConfig();

					return null;
				}
			};
		}

		private static NConsoleOption CreateConfigOption(PropertyInfo member, string name, int i, Action<bool> fn)
		{
			bool initVal = (bool) member.GetValue(Config);

			return new NConsoleOption()
			{
				Name = InterfaceElements.GetName(name, initVal),
				Function = () =>
				{
					var pi = member.DeclaringType.GetProperty(member.Name);

					bool curVal = (bool) pi.GetValue(null);

					fn(curVal);

					bool newVal = (bool) pi.GetValue(null);

					MainMenuOptions[i].Name = InterfaceElements.GetName(name, newVal);

					Debug.Assert((bool) pi.GetValue(null) == newVal);

					return null;
				}
			};
		}

		private static NConsoleOption CreateConfigOption(PropertyInfo member, string name, int i)
		{
			bool initVal = (bool) member.GetValue(Config);

			return new NConsoleOption()
			{
				Name = InterfaceElements.GetName(name, initVal),
				Function = () =>
				{
					var    fi     = Config.GetType().GetResolvedField(member.Name);
					object curVal = fi.GetValue(Config);
					bool   newVal = !(bool) curVal;
					fi.SetValue(Config, newVal);

					MainMenuOptions[i].Name = InterfaceElements.GetName(name, newVal);

					Debug.Assert((bool) fi.GetValue(Config) == newVal);

					AppConfig.UpdateConfig();

					return null;
				}
			};
		}

		#endregion


		/*
		/// <summary>Returns true if the current application has focus, false otherwise</summary>
		internal static bool ApplicationIsActivated()
		{
			//https://stackoverflow.com/questions/7162834/determine-if-current-application-is-activated-has-focus
			var activatedHandle = Native.GetForegroundWindow();

			if (activatedHandle == IntPtr.Zero) {
				return false; // No window is currently activated
			}

			var p1     = Process.GetCurrentProcess();
			var procId = p1.Id;
			int activeProcId;
			Native.GetWindowThreadProcessId(activatedHandle, out activeProcId);
			var p2 = Process.GetProcessById(activeProcId);


			return activeProcId == procId;
		}*/

		#region Toast

		private const string ARG_KEY_ACTION    = "action";
		private const string ARG_VALUE_DISMISS = "dismiss";

		public static void ShowToast(object sender, ExtraResultEventArgs args)
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

		public static void OnToastActivated(ToastNotificationActivatedEventArgsCompat compat)
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

		#endregion
	}

	
}