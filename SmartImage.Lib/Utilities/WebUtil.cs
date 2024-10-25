// Author: Deci | Project: SmartImage.Lib | Name: WebUtil.cs
// Date: 2024/10/25 @ 13:10:52

namespace SmartImage.Lib.Utilities;

internal static class WebUtil
{

	public static readonly string ChromePath =
		Path.Combine(AppUtil.ProgramFilesPath, @"Google\Chrome\Application\chrome.exe");


	/*public static readonly string FirefoxPath = ProgramFilesPath
			                                        + @"\Mozilla Firefox\firefox.exe";*/

	public static readonly string FirefoxPath = Path.Combine(AppUtil.AppDataPath, @"Mozilla");


	public static bool IsChromeInstalled => Path.Exists(ChromePath);

	public static bool IsFirefoxInstalled => Path.Exists(FirefoxPath);

}