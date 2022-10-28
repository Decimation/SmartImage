﻿using NStack;
using Terminal.Gui;
// ReSharper disable InconsistentNaming

namespace SmartImage.UI;

internal static class Values
{
	internal static readonly ustring Err = ustring.Make('!');
	internal static readonly ustring NA  = ustring.Make(Application.Driver.RightDefaultIndicator);
	internal static readonly ustring OK  = ustring.Make(Application.Driver.Checked);
	internal static readonly ustring PRC = ustring.Make(Application.Driver.Diamond);
}