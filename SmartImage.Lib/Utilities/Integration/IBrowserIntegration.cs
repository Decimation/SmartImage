// Author: Deci | Project: SmartImage.Lib | Name: IBrowserIntegration.cs
// Date: 2025/02/13 @ 03:02:45

using Kantan.Net.Web;

namespace SmartImage.Lib.Utilities.Integration;

public interface IBrowserIntegration
{

	string ChromePath { get; }

	string FirefoxPath { get; }

	bool IsChromeInstalled { get; }

	bool IsFirefoxInstalled { get; }

	BaseCookieReader GetReader();

}