// Author: Deci | Project: SmartImage.Rdx | Name: ServerCommand.cs
// Date: 2024/11/22 @ 03:11:26

using Spectre.Console;
using Spectre.Console.Cli;

namespace SmartImage.Rdx;

public class ServerCommandSettings : CommandSettings
{

	public override ValidationResult Validate()
	{
		return base.Validate();
	}

}
public class ServerCommand : AsyncCommand<ServerCommandSettings>, IDisposable
{


	public override async Task<int> ExecuteAsync(CommandContext context, ServerCommandSettings settings)
	{

	}

	public void Dispose()
	{
	}

}