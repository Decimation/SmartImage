// Author: Deci | Project: SmartImage.Rdx | Name: IntegrationCommandSettings.cs
// Date: 2024/05/22 @ 16:05:47

using SmartImage.Lib.Utilities.Integration;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SmartImage.Rdx.Commands;

internal class IntegrationCommandSettings : CommandSettings
{

	[CommandOption("--ctx-menu")]
	public bool? ContextMenu { get; internal set; }

	[CommandOption("--ctx-menu-args")]
	public string? ContextMenuArguments { get; internal set; }

	public override ValidationResult Validate()
	{
		ContextMenuArguments ??= BaseOSIntegration.Integration.LaunchArgs;

		return base.Validate();
	}

}