// Author: Deci | Project: SmartImage.Rdx | Name: IntegrationCommandSettings.cs
// Date: 2024/05/22 @ 16:05:47

using SmartImage.Lib.Utilities;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SmartImage.Rdx;

internal class IntegrationCommandSettings : CommandSettings
{

	[CommandOption("--ctx-menu")]
	public bool? ContextMenu { get; internal set; }

	[CommandOption("--ctx-menu-args")]
	public string? ContextMenuArguments { get; internal set; }

	public override ValidationResult Validate()
	{
		if (AppUtil.IsWindows) {
			ContextMenuArguments ??= R1.Reg_Launch_Args;
		}
		else if (AppUtil.IsLinux) {
			ContextMenuArguments ??= R1.Linux_Launch_Args;
		}

		return base.Validate();
	}

}