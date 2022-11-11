using Kantan.Net.Utilities;
using NStack;
using Terminal.Gui;

// ReSharper disable InconsistentNaming

namespace SmartImage.Shell;

internal static partial class UI
{
	internal static readonly ustring Err = ustring.Make('x');
	internal static readonly ustring Clp = ustring.Make('c');
	internal static readonly ustring NA  = ustring.Make(Application.Driver.RightDefaultIndicator);
	internal static readonly ustring OK  = ustring.Make(Application.Driver.Checked);
	internal static readonly ustring PRC = ustring.Make(Application.Driver.Diamond);

	internal static readonly Rune Line = Application.Driver.HLine;

	internal static Button BtnGenerator(Dialog d, string s, string? u = null, Action? f = null)
	{
		var b = new Button()
		{
			Text     = s,
			AutoSize = true,
		};

		f ??= () => HttpUtilities.TryOpenUrl(u);

		b.Clicked += () =>
		{
			f();
			d.SetNeedsDisplay();
		};
		
		return b;
	}
}