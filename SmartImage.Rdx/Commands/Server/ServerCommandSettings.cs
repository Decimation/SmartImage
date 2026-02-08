// Author: Deci | Project: SmartImage.Rdx | Name: ServerCommandSettings.cs
// Date: 2024/12/03 @ 10:12:10

using System.ComponentModel;
using SmartImage.Rdx.Commands.Common;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SmartImage.Rdx.Commands.Server;
#if SERVER

public sealed class ServerCommandSettings : CommonCommandSettings
{

	[CommandOption("--port")]
	[DefaultValue(8080)]
	public int Port { get; set; }

	public override ValidationResult Validate()
	{
		/*if (!BaseOSIntegration.Integration.IsRoot) {
			throw new SmartImageException("Must be admin");
		}*/

		return base.Validate();
	}

}
#endif
