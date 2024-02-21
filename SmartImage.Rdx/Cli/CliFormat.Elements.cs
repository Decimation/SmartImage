// Deci SmartImage.Rdx CliFormat.Elements.cs
// $File.CreatedYear-$File.CreatedMonth-26 @ 1:25

using System.Collections.ObjectModel;
using SmartImage.Lib.Engines;
using Spectre.Console;

namespace SmartImage.Rdx.Cli;

internal static partial class CliFormat
{

	// Ideally a dictionary would be used here...

	internal static readonly Style s_styleName = new(decoration: Decoration.Italic);

	internal static readonly Style s_styleSim = new(Color.Wheat1, decoration: Decoration.None);

	internal static readonly Style s_styleUrl = new(Color.Cyan1, decoration: Decoration.None);

	public static readonly Color Color1 = new(0x80, 0xFF, 0x80);

	internal static readonly Text EmptyText = new(string.Empty);

	internal const string STR_DEFAULT = "-";

	static CliFormat() { }

	internal static readonly IReadOnlyDictionary<SearchEngineOptions, Color> EngineColors =
		new Dictionary<SearchEngineOptions, Color>()
		{
			{ SearchEngineOptions.SauceNao, Color.Green },
			{ SearchEngineOptions.EHentai, Color.Purple },
			{ SearchEngineOptions.Iqdb, Color.LightGreen },
			{ SearchEngineOptions.Ascii2D, Color.Cyan1 },
			{ SearchEngineOptions.TraceMoe, Color.DodgerBlue1 },
			{ SearchEngineOptions.RepostSleuth, Color.RosyBrown },
			{ SearchEngineOptions.ArchiveMoe, Color.Wheat1 },
			{ SearchEngineOptions.Yandex, Color.Orange1 },
			{ SearchEngineOptions.Iqdb3D, Color.SeaGreen1 },

		};

	internal static readonly Capabilities ProfileCapabilities = AConsole.Profile.Capabilities;

}