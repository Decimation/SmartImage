// Author: Deci | Project: SmartImage.Rdx | Name: CustomHelpProvider.cs
// Date: 2024/04/10 @ 18:04:50

using Spectre.Console.Cli;
using Spectre.Console.Cli.Help;
using Spectre.Console.Rendering;

namespace SmartImage.Rdx;

internal class CustomHelpProvider : HelpProvider
{
	public CustomHelpProvider(ICommandAppSettings settings)
		: base(settings)
	{
	}

	public override IEnumerable<IRenderable> GetUsage(ICommandModel model, ICommandInfo? command)
	{
		return base.GetUsage(model, command);
	}

	/*public override IEnumerable<IRenderable> GetExamples(ICommandModel model, ICommandInfo? command)
	{
		return
		[
			new Text(
				"smartimage \"C:\\Users\\Deci\\Pictures\\Epic anime\\Kallen_FINAL_1-3.png\" --search-engines All --output-format \"Delimited\" --output-file \"output.csv\" --read-cookies")
		];

		return base.GetExamples(model, command);
	}*/

	public override IEnumerable<IRenderable> GetDescription(ICommandModel model, ICommandInfo? command)
	{
		return base.GetDescription(model, command);
	}

	public override IEnumerable<IRenderable> GetFooter(ICommandModel model, ICommandInfo? command)
	{
		return base.GetFooter(model, command);
	}

	/*public override IEnumerable<IRenderable> GetHeader(ICommandModel model, ICommandInfo? command)
	{

		switch (command) {
			case null:

				break;
		}

		return [Text.Empty];
	}*/
}