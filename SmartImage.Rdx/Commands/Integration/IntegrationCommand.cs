// Author: Deci | Project: SmartImage.Rdx | Name: IntegrationCommand.cs
// Date: 2024/05/22 @ 16:05:51

using System.Diagnostics;
using SmartImage.Lib.Utilities.Integration;
using SmartImage.Rdx.Shell;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SmartImage.Rdx.Commands.Integration;

internal class IntegrationCommand : Command<IntegrationCommandSettings>
{

	protected override int Execute(CommandContext context, IntegrationCommandSettings settings, CancellationToken cancellationToken)
	{
		try {
			// AnsiConsole.WriteLine($"{AppSupport.IsContextMenuAdded}");

			if (settings.ContextMenu.HasValue) {
				var rv = BaseOSIntegration.Integration.HandleContextMenu(settings.ContextMenu.Value, settings.ContextMenuArguments);
				AnsiConsole.WriteLine($"Context menu change: {rv}");
			}

			AnsiConsole.WriteLine($"Context menu enabled: {BaseOSIntegration.Integration.IsContextMenuAdded}");
		}
		catch (Exception e) {
			AnsiConsole.WriteException(e);
		}

		return Lib.Common.EC_OK;
	}

}