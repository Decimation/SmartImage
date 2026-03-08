// Author: Deci | Project: SmartImage.Rdx | Name: ConsoleAction.cs
// Date: 2026/02/21 @ 14:02:02

#nullable disable
using SmartImage;
using SmartImage.Lib.Engines.Results;
using Spectre.Console;
using Spectre.Console.Rendering;

// ReSharper disable InconsistentNaming

namespace SmartImage.Rdx.Shell;

// todo
public class ConsoleAction<T, T2>
{

	public ConsoleKey Key { get; init; }

	public string Description { get; init; }

	public Func<T, T2, bool> Func { get; init; }

}

public class PreviewConsoleAction : ConsoleAction<CanvasImage, ScannedResultItem> { }