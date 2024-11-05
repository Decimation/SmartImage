// Author: Deci | Project: SmartImage.Rdx | Name: ConsoleItems.cs
// Date: 2024/11/05 @ 13:11:34

namespace SmartImage.Rdx.Shell;

internal static class ConsoleItems
{

	internal const int EC_ERROR = -1;

	internal const int EC_OK    = 0;

	internal static readonly string[] s_commandChoices = ["open", "scan", "exit"];

	internal const double COMPLETE = 100.0d;

}