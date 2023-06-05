// Read S SmartImage ShellMode.Dialog.cs
// 2023-02-14 @ 12:13 AM

#region

using System.Collections;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Flurl.Http;
using Kantan.Console;
using Kantan.Text;
using Novus.OS;
using NStack;
using SmartImage.App;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Mode.Shell.Assets;
using SmartImage.Utilities;
using Terminal.Gui;

#endregion

namespace SmartImage.Mode.Shell;

public sealed partial class ShellMode
{
	private const int WIDTH1  = 15;
	private const int HEIGHT1 = 17;

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

		var b1 = d.CreateLinkButton("Repo", R2.Repo_Url);
		var b2 = d.CreateLinkButton("Wiki", R2.Wiki_Url);
		var b3 = d.CreateLinkButton("Ok", null, () => Application.RequestStop());

		d.AddButton(b1);
		d.AddButton(b2);
		d.AddButton(b3);

		Application.Run(d);
	}

	private void InfoDialog()
	{
		var d = new Dialog()
		{
			Text = $"{Environment.GetCommandLineArgs().QuickJoin("\n")}",

			Title    = R2.Name,
			AutoSize = true,
			Width    = UI.Dim_30_Pct,
			Height   = UI.Dim_30_Pct,
		};

		var b3 = d.CreateLinkButton("Ok", null, () => Application.RequestStop());

		d.AddButton(b3);

		Application.Run(d);
	}

	/// <summary>
	///     <see cref="Btn_Config" />
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
			Width                   = WIDTH1,
			Height                  = HEIGHT1,

			Y           = Pos.Bottom(lbSearchEngines),
			ColorScheme = UI.Cs_ListView2
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
			Width                   = WIDTH1,
			Height                  = HEIGHT1,

			Y = Pos.Bottom(lbPriorityEngines),
			X = Pos.Right(lvSearchEngines) + 1,

			ColorScheme = UI.Cs_ListView2

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
			Width    = Dim.Fill(WIDTH1),
			Height   = 11,

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
			// UI.OnEngineSelected(args1, ref e, lvSearchEngines);
			UI.OnEngineSelected(lvSearchEngines, args1, ref e);
			// Debug.WriteLine($"Setting {e}");
			Config.SearchEngines = e;
			ReloadDialog();
		};

		lvPriorityEngines.OpenSelectedItem += args1 =>
		{
			SearchEngineOptions e = Config.PriorityEngines;
			// UI.OnEngineSelected(args1, ref e, lvPriorityEngines);
			UI.OnEngineSelected(lvPriorityEngines, args1, ref e);
			// Debug.WriteLine($"Setting {e}");
			Config.PriorityEngines = e;
			ReloadDialog();
		};

		// Debug.WriteLine($"{GetItems<SearchEngineOptions>(lvSearchEngines.Source).QuickJoin()}");
		lvSearchEngines.FromEnum(Config.SearchEngines);
		lvPriorityEngines.FromEnum(Config.PriorityEngines);

		// var e=lvSearchEngines.Source.GetEnum2(default(SearchEngineOptions));

		/*============================================================================*\
			Checkboxes
		\*============================================================================*/

		CheckBox cbContextMenu = new(R1.S_ContextMenu)
		{
			X           = Pos.X(tvConfig),
			Y           = Pos.Bottom(tvConfig) + 1,
			Width       = WIDTH1,
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
			Y = Pos.Bottom(lvSearchEngines) + 2
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
			ColorScheme = UI.Cs_Btn3,
			Checked     = Config.AutoSearch
		};

		cbAutoSearch.Toggled += b =>
		{
			Config.AutoSearch = !b;
			ReloadDialog();
		};

		cbContextMenu.Checked = Integration.IsContextMenuAdded;
		cbOnTop.Checked       = Config.OnTop;

		CheckBox cbOpenRaw = new(R1.S_OpenRaw)
		{
			X = Pos.Right(cbAutoSearch) + 1,
			Y = Pos.Y(cbAutoSearch),
			// Width  = WIDTH,
			Height      = 1,
			AutoSize    = true,
			ColorScheme = UI.Cs_Btn3

		};

		cbOpenRaw.Toggled += b =>
		{
			Config.OpenRaw = !b;
			ReloadDialog();
		};

		cbOpenRaw.Checked = Config.OpenRaw;

		CheckBox cbSilent = new(R1.S_Silent)
		{
			X = Pos.Right(cbOpenRaw) + 1,
			Y = Pos.Y(cbOpenRaw),
			// Width  = WIDTH,
			Height      = 1,
			AutoSize    = true,
			ColorScheme = UI.Cs_Btn3

		};

		cbSilent.Toggled += b =>
		{
			Config.Silent = !b;
			ReloadDialog();
		};

		cbSilent.Checked = Config.Silent;

		CheckBox cbCb = new(R1.S_Clipboard)
		{
			X = Pos.X(cbContextMenu),
			Y = Pos.Bottom(cbContextMenu) + 1,
			// Width  = WIDTH,
			Height      = 1,
			AutoSize    = true,
			ColorScheme = UI.Cs_Btn3,
			Checked     = UseClipboard,
		};

		cbCb.Toggled += b =>
		{
			UseClipboard = !b;
			ReloadDialog();
		};

		CheckBox cbSendTo = new(R1.S_SendTo)
		{
			X = Pos.Right(cbCb) + 1,
			Y = Pos.Y(cbCb),
			// Width  = WIDTH,
			Height      = 1,
			AutoSize    = true,
			ColorScheme = UI.Cs_Btn3,
			Checked     = Integration.IsSendToAdded,
		};

		cbSendTo.Toggled += b =>
		{
			Integration.HandleSendToMenu(!b);
			ReloadDialog();
		};

		/*============================================================================*\
			Eh username/password
		\*============================================================================*/

		Label lbEhUsername = new(R1.S_EhUsername)
		{
			X           = Pos.X(cbCb),
			Y           = Pos.Bottom(cbCb) + 1,
			CanFocus    = false,
			ColorScheme = UI.Cs_Lbl1
		};

		TextField tfEhUsername = new()
		{
			X      = Pos.Right(lbEhUsername) + 1,
			Y      = Pos.Y(lbEhUsername),
			Width  = WIDTH1 * 2,
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
			Width  = WIDTH1 * 2,
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

		#region

		var btnClear = new Button("Clear")
		{
			Y = Pos.Bottom(lvSearchEngines),
		};

		btnClear.Clicked += () => OnAction(lvSearchEngines, (e) => Config.SearchEngines = e);

		var btnClear2 = new Button("Clear")
		{
			Y = Pos.Bottom(lvPriorityEngines),
			X = Pos.Bottom(lvPriorityEngines)
		};

		btnClear2.Clicked += () => OnAction(lvPriorityEngines, (e) => Config.PriorityEngines = e);

		void OnAction(ListView lv, Action<SearchEngineOptions> f)
		{
			lv.ClearBy<SearchEngineOptions>(_ => false);
			lv.FromEnum(default(SearchEngineOptions));
			f(default);
			ReloadDialog();

			lv.SetNeedsDisplay();
		}

		#endregion

		dlCfg.Add(tvConfig, lvSearchEngines, lvPriorityEngines,
		          cbContextMenu, cbOnTop, lbConfig, lbSearchEngines, lbPriorityEngines,
		          lbHelp, cbAutoSearch, lbEhUsername, tfEhUsername, lbEhPassword, tfEhPassword,
		          cbOpenRaw, cbSilent, btnClear, btnClear2, cbCb, cbSendTo);

		var btnHelp = dlCfg.CreateLinkButton("?", R2.Wiki_Url);

		dlCfg.AddButton(btnRefresh);
		dlCfg.AddButton(btnOk);
		dlCfg.AddButton(btnSave);
		dlCfg.AddButton(btnHelp);

		Application.Run(dlCfg);

		Tf_Input.SetFocus();
		Tf_Input.EnsureFocus();
	}

	private void Queue_Dialog()
	{
		var d = new Dialog()
		{
			Title    = $"Queue ({Queue.Count} items)",
			AutoSize = false,
			Width    = Dim.Percent(60),
			Height   = Dim.Percent(55),
			// Height   = UI.Dim_80_Pct,
		};

		var cpy = Queue.ToList();

		var tf = new TextField()
		{
			Width  = Dim.Fill(),
			Height = 2,
		};

		var lv = new ListView(cpy)
		{
			Width  = Dim.Fill(),
			Height = Dim.Fill(),
			Y      = Pos.Bottom(tf),
			Border = new Border()
			{
				BorderStyle     = BorderStyle.Rounded,
				BorderThickness = new Thickness(2)
			}
		};

		/*var btnAdd = new Button("Add")
		{
			X = Pos.Right(tf),
			Y = Pos.Y(tf)
		};
		btnAdd.Clicked += () =>
		{
			var s = tf.Text.ToString();
			Queue.Enqueue(s);
			lv.Source = new ListWrapper(Queue.ToList());

		};*/

		var btnRm = new Button("Remove")
		{
			ColorScheme = UI.Cs_Btn3
		};

		btnRm.Clicked += () =>
		{
			var cpy2 = lv.Source.ToList();

			if (lv.SelectedItem < cpy2.Count && lv.SelectedItem >= 0) {
				var i = (string) cpy2[lv.SelectedItem];
				// Debug.WriteLine($"{i}");
				cpy.Remove(i);
				// Queue.Clear();
				Queue = new ConcurrentQueue<string>(cpy);
				lv.SetFocus();

			}
		};

		var btnRmAll = new Button("Clear")
		{
			ColorScheme = UI.Cs_Btn3
		};

		btnRmAll.Clicked += () =>
		{
			lv.Source = new ListWrapper(Array.Empty<string>());
			Queue.Clear();
			lv.SetFocus();
		};

		tf.TextChanged += delegate(ustring ustring)
		{
			// Debug.WriteLine($"{ustring}");
		};

		tf.TextChanging += a =>
		{
			//todo

			var s = a.NewText.ToString().CleanString().Trim('\"');

			// Application.MainLoop.Invoke(() => Task.Delay(TimeSpan.FromSeconds(1)));
			if (SearchQuery.IsValidSourceType(s)) {
				Queue.Enqueue(s);
				lv.Source = new ListWrapper(Queue.ToList());

				tf.DeleteAll();
				Debug.WriteLine($"{tf.Text} {s}");

				// Application.MainLoop.Invoke(() => Action(tf));
				tf.Text = ustring.Empty;
				Debug.WriteLine($"{tf.Text} {a.NewText}");
			}
		};

		static void Action(TextField tf)
		{
			Debug.WriteLine($"clearing");
			// Task.Delay(TimeSpan.FromSeconds(3));
			// tf.Text           = ustring.Empty;
			// tf.CursorPosition = 0;
			tf.DeleteAll();
			tf.ClearHistoryChanges();
			tf.ClearAllSelection();
			tf.SetNeedsDisplay();
			Debug.WriteLine($"cleared");
		}

		var btnOk = new Button("Ok")
		{
			ColorScheme = UI.Cs_Btn3
		};
		btnOk.Clicked += () => { Application.RequestStop(); };

		d.Add(tf, lv);
		d.AddButton(btnRm);
		d.AddButton(btnRmAll);
		d.AddButton(btnOk);

		Application.Run(d);
	}
}