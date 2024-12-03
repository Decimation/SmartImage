// Author: Deci | Project: SmartImage.Rdx | Name: IntegrationCommand.cs
// Date: 2024/05/22 @ 16:05:51

using System.Diagnostics;
using SmartImage.Lib.Utilities;
using SmartImage.Rdx.Shell;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SmartImage.Rdx.Commands;

internal class IntegrationCommand : Command<IntegrationCommandSettings>
{

	public override int Execute(CommandContext context, IntegrationCommandSettings settings)
	{
		try {
			// AnsiConsole.WriteLine($"{AppUtil.IsContextMenuAdded}");

			if (settings.ContextMenu.HasValue) {
				var rv = AppUtil.HandleContextMenu(settings.ContextMenu.Value, settings.ContextMenuArguments);
				AnsiConsole.WriteLine($"Context menu change: {rv}");
			}

			AnsiConsole.WriteLine($"Context menu enabled: {AppUtil.IsContextMenuAdded}");
		}
		catch (Exception e) {
			AnsiConsole.WriteException(e);
		}

		return ConsoleFormat.EC_OK;
	}

}