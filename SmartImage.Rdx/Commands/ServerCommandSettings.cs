// Author: Deci | Project: SmartImage.Rdx | Name: ServerCommandSettings.cs
// Date: 2024/12/03 @ 10:12:10

using System.ComponentModel;
using SmartImage.Lib.Utilities.Diagnostics;
using SmartImage.Lib.Utilities.Integration;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SmartImage.Rdx.Commands;

public sealed class ServerCommandSettings : CommandSettings
{

	[CommandOption("--port")]
	[DefaultValue(25565)]
	public int Port { get; set; }

	public override ValidationResult Validate()
	{
		/*if (!BaseOSIntegration.Integration.IsRoot) {
			throw new SmartImageException("Must be admin");
		}*/

		return base.Validate();
	}

}