using System;
using System.Diagnostics;
using System.Threading;
using Kantan.Cli.Controls;
using Kantan.Diagnostics;
using Kantan.Model;
using Kantan.Net;
using Novus.Win32;
using SmartImage.Lib.Utilities;

namespace SmartImage.Lib.Searching;
#pragma warning disable	CA1416
public interface IResult : IDisposable, IConsoleComponent
{
	protected static ConsoleOptionFunction CreateDownloadFunction(Func<Uri> d)
	{
		// Because of value type and pointer semantics, a func needs to be used here to ensure the
		// Direct field of ImageResult is updated.

		return () =>
		{
			var direct = d();

			if (direct == null) {
				return null;
			}

			var path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
			var cts  = new CancellationTokenSource();

			ConsoleProgressIndicator.Start(cts);

			// Console.WriteLine($"\nDownloading...".AddColor(Elements.ColorOther));

			var file = ImageHelper.Download(direct, path);

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

	protected static ConsoleOptionFunction CreateOpenFunction(Uri url)
	{
		return () =>
		{
			if (url != null)
			{
				WebUtilities.OpenUrl(url.ToString());
			}

			return null;
		};
	}
}