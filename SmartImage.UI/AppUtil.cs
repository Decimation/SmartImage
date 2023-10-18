global using R2 = SmartImage.UI.Resources;
global using R1 = SmartImage.Lib.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using Novus.OS;
using Flurl.Http;
using JetBrains.Annotations;
using Kantan.Net.Utilities;
using Novus.Win32;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Windows.Media.Imaging;
using Novus.Win32.Structures.User32;

// ReSharper disable InconsistentNaming

#nullable disable
namespace SmartImage.UI;

internal static class AppUtil
{
	#region

	public static readonly Assembly Assembly = Assembly.GetExecutingAssembly();

	public static string ExeLocation
	{
		get
		{
			var module = Process.GetCurrentProcess().MainModule;

			// Require.NotNull(module);
			Trace.Assert(module != null);
			return module.FileName;
		}
	}

	static AppUtil() { }

	public static readonly Version Version = Assembly.GetName().Version;

	public static string CurrentAppFolder => Path.GetDirectoryName(ExeLocation);

	public static bool IsAppFolderInPath => FileSystem.IsFolderInPath(CurrentAppFolder);

	public static bool IsOnTop { get; private set; }

	#endregion

	internal static void FlashTaskbar(IntPtr hndw)
	{
		var pwfi = new FLASHWINFO()
		{
			cbSize    = (uint) Marshal.SizeOf<FLASHWINFO>(),
			hwnd      = hndw,
			dwFlags   = FlashWindowType.FLASHW_TRAY,
			uCount    = 8,
			dwTimeout = 75
		};

		Native.FlashWindowEx(ref pwfi);
	}

	public static void AddToPath(bool b)
	{

		if (b) {
			var p = FileSystem.GetEnvironmentPath();
			FileSystem.SetEnvironmentPath(p + $";{CurrentAppFolder}");

		}
		else {
			FileSystem.RemoveFromPath(CurrentAppFolder);

		}
	}

	public static bool IsContextMenuAdded
	{
		get
		{
			var reg = Registry.CurrentUser.OpenSubKey(R1.Reg_Shell_Cmd);
			return reg != null;
		}
	}

	/*
	 * HKEY_CLASSES_ROOT is an alias, a merging, of two other locations:
	 *		HKEY_CURRENT_USER\Software\Classes
	 *		HKEY_LOCAL_MACHINE\Software\Classes
	 */

	/// <returns><c>true</c> if operation succeeded; <c>false</c> otherwise</returns>
	public static bool HandleContextMenu(bool option)
	{
		/*
		 * New context menu
		 */
		switch (option) {
			case true:

				RegistryKey regMenu = null;
				RegistryKey regCmd  = null;

				string fullPath = ExeLocation;

				try {
					regMenu = Registry.CurrentUser.CreateSubKey(R1.Reg_Shell);
					regMenu?.SetValue(String.Empty, R1.Name);
					regMenu?.SetValue("Icon", $"\"{fullPath}\"");

					regCmd = Registry.CurrentUser.CreateSubKey(R1.Reg_Shell_Cmd);

					regCmd?.SetValue(String.Empty,
					                 $"\"{fullPath}\" -i \"%1\" -as");
				}
				catch (Exception ex) {
					Trace.WriteLine($"{ex.Message}");
					return false;
				}
				finally {
					regMenu?.Close();
					regCmd?.Close();
				}

				break;
			case false:

				try {
					var reg = Registry.CurrentUser.OpenSubKey(R1.Reg_Shell_Cmd);

					if (reg != null) {
						reg.Close();
						Registry.CurrentUser.DeleteSubKey(R1.Reg_Shell_Cmd);
					}

					reg = Registry.CurrentUser.OpenSubKey(R1.Reg_Shell);

					if (reg != null) {
						reg.Close();
						Registry.CurrentUser.DeleteSubKey(R1.Reg_Shell);
					}
				}
				catch (Exception ex) {
					Trace.WriteLine($"{ex.Message}");

					return false;
				}

				break;

		}

		return false;

	}

	internal static async Task<GHRelease[]> GetRepoReleasesAsync()
	{
		var res = await "https://api.github.com/repos/Decimation/SmartImage/releases"
			          .WithAutoRedirect(true)
			          .AllowAnyHttpStatus()
			          .WithHeaders(new
			          {
				          User_Agent = HttpUtilities.UserAgent
			          })
			          .OnError(e =>
			          {
				          e.ExceptionHandled = true;
			          })
			          .GetJsonAsync<GHRelease[]>();

		return res;
	}

	public static async Task<GHRelease> GetLatestReleaseAsync()
	{
		var r = await GetRepoReleasesAsync();

		if (r == null) {
			return null;
		}

		foreach (var x in r) {
			if (Version.TryParse(x.tag_name[1..], out var xv)) {
				x.Version = xv;
			}

		}

		return r.OrderByDescending(x => x.published_at).First();
	}

	// Root myDeserializedClass = JsonConvert.DeserializeObject<List<Root>>(myJsonResponse);
	internal static readonly string MyPicturesFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

	// internal static uint m_registerWindowMessage = Native.RegisterWindowMessage("WM_SHOWME");
	public static Bitmap BitmapImage2Bitmap(this BitmapImage bitmapImage)
	{
		// BitmapImage bitmapImage = new BitmapImage(new Uri("../Images/test.png", UriKind.Relative));

		using (MemoryStream outStream = new MemoryStream()) {
			BitmapEncoder enc = new BmpBitmapEncoder();
			enc.Frames.Add(BitmapFrame.Create(bitmapImage));
			enc.Save(outStream);
			System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(outStream);

			return new Bitmap(bitmap);
		}
	}

	public static double CompareImages(Bitmap InputImage1, Bitmap InputImage2, int Tollerance)
	{
		Bitmap Image1     = new Bitmap(InputImage1, new Size(512,512));
		Bitmap Image2     = new Bitmap(InputImage2, new Size(512, 512));
		int    Image1Size = Image1.Width * Image1.Height;
		int    Image2Size = Image2.Width * Image2.Height;
		Bitmap Image3;

		if (Image1Size > Image2Size) {
			Image1 = new Bitmap(Image1, Image2.Size);
			Image3 = new Bitmap(Image2.Width, Image2.Height);
		}
		else {
			Image1 = new Bitmap(Image1, Image2.Size);
			Image3 = new Bitmap(Image2.Width, Image2.Height);
		}

		for (int x = 0; x < Image1.Width; x++) {
			for (int y = 0; y < Image1.Height; y++) {
				Color Color1 = Image1.GetPixel(x, y);
				Color Color2 = Image2.GetPixel(x, y);
				int   r      = Color1.R > Color2.R ? Color1.R - Color2.R : Color2.R - Color1.R;
				int   g      = Color1.G > Color2.G ? Color1.G - Color2.G : Color2.G - Color1.G;
				int   b      = Color1.B > Color2.B ? Color1.B - Color2.B : Color2.B - Color1.B;
				Image3.SetPixel(x, y, Color.FromArgb(r, g, b));
			}
		}

		int Difference = 0;

		for (int x = 0; x < Image1.Width; x++) {
			for (int y = 0; y < Image1.Height; y++) {
				Color Color1 = Image3.GetPixel(x, y);
				int   Media  = (Color1.R + Color1.G + Color1.B) / 3;

				if (Media > Tollerance)
					Difference++;
			}
		}

		double UsedSize = Image1Size > Image2Size ? Image2Size : Image1Size;
		double result   = Difference * 100 / UsedSize;
		return Difference * 100 / UsedSize;
	}

	public static double CalculateMSE(Bitmap image1, Bitmap image2)
	{
		int    width  = Math.Min(image1.Width, image2.Width);
		int    height = Math.Min(image1.Height, image2.Height);
		double mse    = 0;

		for (int y = 0; y < height; y++) {
			for (int x = 0; x < width; x++) {
				Color pixel1 = image1.GetPixel(x, y);
				Color pixel2 = image2.GetPixel(x, y);

				double diffR = pixel1.R - pixel2.R;
				double diffG = pixel1.G - pixel2.G;
				double diffB = pixel1.B - pixel2.B;

				mse += (diffR * diffR + diffG * diffG + diffB * diffB) / 3.0;
			}
		}

		mse /= (width * height);
		return mse;
	}
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal class GHReleaseAsset
{
	public string     url                  { get; set; }
	public int        id                   { get; set; }
	public string     node_id              { get; set; }
	public string     name                 { get; set; }
	public object     label                { get; set; }
	public GHUploader uploader             { get; set; }
	public string     content_type         { get; set; }
	public string     state                { get; set; }
	public int        size                 { get; set; }
	public int        download_count       { get; set; }
	public DateTime   created_at           { get; set; }
	public DateTime   updated_at           { get; set; }
	public string     browser_download_url { get; set; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal class GHAuthor
{
	public string login               { get; set; }
	public int    id                  { get; set; }
	public string node_id             { get; set; }
	public string avatar_url          { get; set; }
	public string gravatar_id         { get; set; }
	public string url                 { get; set; }
	public string html_url            { get; set; }
	public string followers_url       { get; set; }
	public string following_url       { get; set; }
	public string gists_url           { get; set; }
	public string starred_url         { get; set; }
	public string subscriptions_url   { get; set; }
	public string organizations_url   { get; set; }
	public string repos_url           { get; set; }
	public string events_url          { get; set; }
	public string received_events_url { get; set; }
	public string type                { get; set; }
	public bool   site_admin          { get; set; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal class GHReactions
{
	public string url         { get; set; }
	public int    total_count { get; set; }

	[JsonPropertyName("+1")]
	public int Plus1 { get; set; }

	[JsonPropertyName("-1")]
	public int Minus1 { get; set; }

	public int laugh    { get; set; }
	public int hooray   { get; set; }
	public int confused { get; set; }
	public int heart    { get; set; }
	public int rocket   { get; set; }
	public int eyes     { get; set; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal class GHUploader
{
	public string login               { get; set; }
	public int    id                  { get; set; }
	public string node_id             { get; set; }
	public string avatar_url          { get; set; }
	public string gravatar_id         { get; set; }
	public string url                 { get; set; }
	public string html_url            { get; set; }
	public string followers_url       { get; set; }
	public string following_url       { get; set; }
	public string gists_url           { get; set; }
	public string starred_url         { get; set; }
	public string subscriptions_url   { get; set; }
	public string organizations_url   { get; set; }
	public string repos_url           { get; set; }
	public string events_url          { get; set; }
	public string received_events_url { get; set; }
	public string type                { get; set; }
	public bool   site_admin          { get; set; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal class GHRelease
{
	public string               url              { get; set; }
	public string               assets_url       { get; set; }
	public string               upload_url       { get; set; }
	public string               html_url         { get; set; }
	public int                  id               { get; set; }
	public GHAuthor             author           { get; set; }
	public string               node_id          { get; set; }
	public string               tag_name         { get; set; }
	public string               target_commitish { get; set; }
	public string               name             { get; set; }
	public bool                 draft            { get; set; }
	public bool                 prerelease       { get; set; }
	public DateTime             created_at       { get; set; }
	public DateTime             published_at     { get; set; }
	public List<GHReleaseAsset> assets           { get; set; }
	public string               tarball_url      { get; set; }
	public string               zipball_url      { get; set; }
	public string               body             { get; set; }
	public string               discussion_url   { get; set; }
	public GHReactions          reactions        { get; set; }

	[JsonIgnore]
	[NonSerialized]
	public Version Version;
}