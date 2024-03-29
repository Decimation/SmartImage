﻿// Read S SmartImage UI.Values.cs
// 2023-02-14 @ 12:12 AM

#region

using NStack;
using SmartImage.Lib.Engines;
using Terminal.Gui;

#endregion

// ReSharper disable InconsistentNaming

namespace SmartImage.Mode.Shell.Assets;

internal static partial class UI
{
	internal static readonly ustring Err = ustring.Make('x');
	internal static readonly ustring Clp = ustring.Make('c');
	internal static readonly ustring NA  = ustring.Make(Application.Driver.RightDefaultIndicator);
	internal static readonly ustring OK  = ustring.Make(Application.Driver.Checked);
	internal static readonly ustring PRC = ustring.Make(Application.Driver.Diamond);

	internal static readonly Rune Line = Application.Driver.HLine;

	internal static SearchEngineOptions[] EngineOptions = Enum.GetValues<SearchEngineOptions>();
}