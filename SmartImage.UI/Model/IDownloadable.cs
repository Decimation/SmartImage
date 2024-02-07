// Read S SmartImage.UI IDownloadable.cs
// 2023-10-25 @ 11:32 PM

using System;
using System.Threading.Tasks;

namespace SmartImage.UI.Model;

public interface IDownloadable : IDisposable
{
	public string? Download { get; internal set; }

	public bool CanDownload  { get; internal set; }
	public bool IsDownloaded { get; internal set; }
	public Task<string?> DownloadAsync(string? dir = null, bool exp = true);
}