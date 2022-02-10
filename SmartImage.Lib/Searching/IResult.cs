global using CPI = Kantan.Cli.ConsoleManager.UI.ProgressIndicator;

using System;
using System.Diagnostics;
using System.Threading;
using Kantan.Cli;
using Kantan.Cli.Controls;
using Kantan.Diagnostics;
using Kantan.Model;
using Kantan.Net;
using Novus.OS;
using Novus.OS.Win32;
using SmartImage.Lib.Utilities;

namespace SmartImage.Lib.Searching;
#pragma warning disable IDE0079
#pragma warning disable CA1416

public interface IResult : IDisposable, IConsoleOption
{
	protected static ConsoleOptionFunction GetDownloadFunction(Func<Uri> f)
	{
		// Because of value type and pointer semantics, a func needs to be used here to ensure the
		// Direct field of ImageResult is updated.

		return () =>
		{
			var direct = f();

			if (direct == null) {
				return null;
			}

			var path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
			var cts  = new CancellationTokenSource();

			CPI.Instance.Start(cts);


			var file = ImageMedia.Download(direct, path);

			// Program.ResultDialog.Refresh();

			cts.Cancel();
			cts.Dispose();

			if (file is null) {
				return null;
			}

			FileSystem.ExploreFile(file);
			Debug.WriteLine($"Download: {file}", LogCategories.C_INFO);

			return null;
		};
	}

	protected static ConsoleOptionFunction GetOpenFunction(Uri url)
	{
		return () =>
		{
			if (url != null) {
				WebUtilities.OpenUrl(url.ToString());
			}

			return null;
		};
	}
}