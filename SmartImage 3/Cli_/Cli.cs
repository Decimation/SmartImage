using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartImage.Lib;
using Spectre.Console;

namespace SmartImage.Cli_;

public static class Cli
{
	private static readonly Style PromptStyle = Style.Parse("underline");

	public static readonly TextPrompt<string> Prompt = new("Input:")
	{
		AllowEmpty = false,
		Validator = static s =>
		{
			try {
				var task  = SearchQuery.TryCreateAsync(s);
				var query = task.Result;
				Program.Query = query;
				return ValidationResult.Success();
			}
			catch (Exception e) {
				return ValidationResult.Error($"Error: {e.Message}");
			}
		},
		PromptStyle = PromptStyle,
	};

	public static readonly TextPrompt<string> Prompt2  = new("Engines:")
	{
		AllowEmpty       = true,
		ShowDefaultValue = true,
		PromptStyle      = PromptStyle,
	};

	public static readonly TextPrompt<bool> Prompt3 = new("Stay on top")
	{
		AllowEmpty  = true,
		ShowDefaultValue = true,
		PromptStyle = PromptStyle,
	};

	public static readonly Table ResultsTable = new()
	{
		
		Border = TableBorder.Heavy,
		BorderStyle = Style.Plain
	};

	static Cli()
	{
		Prompt2 = Prompt2.DefaultValue(SearchConfig.SE_DEFAULT.ToString());
		Prompt3 = Prompt3.DefaultValue(SearchConfig.ON_TOP_DEFAULT);
	}
}