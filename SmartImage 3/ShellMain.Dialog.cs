// Read Stanton SmartImage ShellMain.Dialog.cs
// 2023-01-13 @ 11:28 PM

using System.Data;
using Kantan.Console;
using Novus.OS;
using NStack;
using SmartImage.App;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Shell;
using Terminal.Gui;

namespace SmartImage;

public sealed partial class ShellMain
{
	private void AboutDialog()
	{
		var d = new Dialog()
		{
			Text = $"{R2.Name} {Integration.Version} by {R2.Author}\n" +
			       $"Current directory: {Integration.CurrentAppFolder}",

			Title    = R2.Name,
			AutoSize = true,
			Width    = UI.Dim_30_Pct,
			Height   = UI.Dim_30_Pct,
		};

		var b1 = UI.CreateLinkButton(d, "Repo", R2.Repo_Url);
		var b2 = UI.CreateLinkButton(d, "Wiki", R2.Wiki_Url);
		var b3 = UI.CreateLinkButton(d, "Ok", null, () => Application.RequestStop());

		d.AddButton(b1);
		d.AddButton(b2);
		d.AddButton(b3);

		Application.Run(d);
	}

	/// <summary>
	/// <see cref="Btn_Config"/>
	/// </summary>
	private void ConfigDialog()
	{
		var dlCfg = new Dialog("Configuration")
		{
			Text     = ustring.Empty,
			AutoSize = false,
			Width    = UI.Dim_80_Pct,
			Height   = UI.Dim_80_Pct,
		};

		var btnRefresh = new Button("Refresh") { };
		var btnSave    = new Button("Save") { };
		var btnOk      = new Button("Ok") { };

		const int WIDTH  = 15;
		const int HEIGHT = 17;

		/*============================================================================*\
			Engines
		\*============================================================================*/

		Label lbSearchEngines = new(R1.S_SearchEngines)
		{
			X           = 0,
			Y           = 0,
			AutoSize    = true,
			ColorScheme = UI.Cs_Lbl1
		};

		ListView lvSearchEngines = new(UI.EngineOptions)
		{
			AllowsMultipleSelection = true,
			AllowsMarking           = true,
			AutoSize                = true,
			Width                   = WIDTH,
			Height                  = HEIGHT,

			Y = Pos.Bottom(lbSearchEngines)
		};

		Label lbPriorityEngines = new(R1.S_PriorityEngines)
		{
			X           = Pos.Right(lbSearchEngines) + 1,
			Y           = 0,
			AutoSize    = true,
			ColorScheme = UI.Cs_Lbl1
		};

		ListView lvPriorityEngines = new(UI.EngineOptions)
		{
			AllowsMultipleSelection = true,
			AllowsMarking           = true,
			AutoSize                = true,
			Width                   = WIDTH,
			Height                  = HEIGHT,

			Y = Pos.Bottom(lbPriorityEngines),
			X = Pos.Right(lvSearchEngines) + 1
		};

		var cfgInfo = new FileInfo(SearchConfig.Configuration.FilePath);

		Label lbConfig = new($"Config")
		{
			X           = Pos.Right(lbPriorityEngines) + 1,
			Y           = 0,
			AutoSize    = true,
			ColorScheme = UI.Cs_Lbl1
			// Height = 10,
		};

		lbConfig.Clicked += () =>
		{
			FileSystem.ExploreFile(cfgInfo.FullName);
		};

		DataTable dtConfig = Config.ToTable();

		var tvConfig = new TableView(dtConfig)
		{
			AutoSize = true,
			Y        = Pos.Bottom(lbConfig),
			X        = Pos.Right(lvPriorityEngines) + 1,
			Width    = Dim.Fill(WIDTH),
			Height   = 9,
		};

		void ReloadDialog()
		{
			tvConfig.Table = Config.ToTable();
			tvConfig.SetNeedsDisplay();
			dlCfg.SetNeedsDisplay();
		}

		lvSearchEngines.OpenSelectedItem += args1 =>
		{
			SearchEngineOptions e = Config.SearchEngines;
			UI.OnEngineSelected(args1, ref e, lvSearchEngines);
			Config.SearchEngines = e;
			ReloadDialog();
		};

		lvPriorityEngines.OpenSelectedItem += args1 =>
		{
			SearchEngineOptions e = Config.PriorityEngines;
			UI.OnEngineSelected(args1, ref e, lvPriorityEngines);
			Config.PriorityEngines = e;
			ReloadDialog();
		};

		lvSearchEngines.FromEnum(Config.SearchEngines);
		lvPriorityEngines.FromEnum(Config.PriorityEngines);

		/*============================================================================*\
			Checkboxes
		\*============================================================================*/

		CheckBox cbContextMenu = new(R2.Int_ContextMenu)
		{
			X           = Pos.X(tvConfig),
			Y           = Pos.Bottom(tvConfig) + 1,
			Width       = WIDTH,
			Height      = 1,
			ColorScheme = UI.Cs_Btn3
		};

		cbContextMenu.Toggled += b =>
		{
			Integration.HandleContextMenu(!b);
		};

		Label lbHelp = new($"{UI.Line} Arrow keys or mouse :: select option\n" +
		                   $"{UI.Line} Space bar or click :: toggle mark option\n" +
		                   $"{UI.Line} Enter :: save option")
		{
			AutoSize = true,

			X = 0,
			Y = Pos.Bottom(lvSearchEngines) + 1
		};

		CheckBox cbOnTop = new(R1.S_OnTop)
		{
			X           = Pos.Right(cbContextMenu) + 1,
			Y           = Pos.Y(cbContextMenu),
			AutoSize    = true,
			ColorScheme = UI.Cs_Btn3,
			Height      = 1,
		};

		cbOnTop.Toggled += b =>
		{
			Integration.KeepOnTop(!b);
			Config.OnTop = Integration.IsOnTop;
			ReloadDialog();
		};

		CheckBox cbAutoSearch = new(R1.S_AutoSearch)
		{
			X = Pos.Right(cbOnTop) + 1,
			Y = Pos.Y(cbOnTop),
			// Width  = WIDTH,
			Height      = 1,
			AutoSize    = true,
			ColorScheme = UI.Cs_Btn3

		};

		cbAutoSearch.Toggled += b =>
		{
			m_autoSearch = b;
			ReloadDialog();
		};

		cbContextMenu.Checked = Integration.IsContextMenuAdded;
		cbOnTop.Checked       = Config.OnTop;

		/*============================================================================*\
			Eh username/password
		\*============================================================================*/

		Label lbEhUsername = new(R1.S_EhUsername)
		{
			X           = Pos.X(cbContextMenu),
			Y           = Pos.Bottom(cbContextMenu) + 1,
			CanFocus    = false,
			ColorScheme = UI.Cs_Lbl1
		};

		TextField tfEhUsername = new()
		{
			X      = Pos.Right(lbEhUsername) + 1,
			Y      = Pos.Y(lbEhUsername),
			Width  = WIDTH * 2,
			Height = 1,
		};

		tfEhUsername.TextChanging += args =>
		{
			Config.EhUsername = args.NewText.ToString();
			ReloadDialog();
		};

		Label lbEhPassword = new(R1.S_EhPassword)
		{
			X           = Pos.X(lbEhUsername),
			Y           = Pos.Bottom(lbEhUsername),
			CanFocus    = false,
			ColorScheme = UI.Cs_Lbl1
		};

		TextField tfEhPassword = new()
		{
			X      = Pos.Right(lbEhPassword) + 1,
			Y      = Pos.Y(lbEhPassword),
			Width  = WIDTH * 2,
			Height = 1,
		};

		tfEhPassword.TextChanging += args =>
		{
			Config.EhPassword = args.NewText.ToString();
			ReloadDialog();
		};

		/*============================================================================*\
		    Register
		\*============================================================================*/

		btnRefresh.Clicked += ReloadDialog;

		btnOk.Clicked += () =>
		{
			Application.RequestStop();
		};

		btnSave.Clicked += () =>
		{
			Config.Save();
			ReloadDialog();
		};

		dlCfg.Add(tvConfig, lvSearchEngines, lvPriorityEngines,
		          cbContextMenu, cbOnTop, lbConfig, lbSearchEngines, lbPriorityEngines,
		          lbHelp, cbAutoSearch, lbEhUsername, tfEhUsername, lbEhPassword, tfEhPassword);

		dlCfg.AddButton(btnRefresh);
		dlCfg.AddButton(btnOk);
		dlCfg.AddButton(btnSave);

		Application.Run(dlCfg);

		Tf_Input.SetFocus();
		Tf_Input.EnsureFocus();
	}
}