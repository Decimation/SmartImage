// Author: Deci | Project: SmartImage.Rdx | Name: IntegrationCommandSettings.cs
// Date: 2024/05/22 @ 16:05:47

using Spectre.Console.Cli;

namespace SmartImage.Rdx;

public class IntegrationCommandSettings : CommandSettings
{

	[CommandOption("--ctx-menu")]
	public bool? ContextMenu { get; internal set; }

}