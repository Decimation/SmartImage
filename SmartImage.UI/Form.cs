using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Searching;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Security;
using System.Windows.Forms;
using SimpleCore.Utilities;

namespace SmartImage.UI
{
	public partial class SmartImageForm : Form
	{
		public SmartImageForm()
		{
			InitializeComponent();

			openFileDialog1 = new OpenFileDialog()
			{
				FileName = "Select a text file",
				Filter   = "Image Files(*.BMP;*.JPG;*.GIF;*.PNG)|*.BMP;*.JPG;*.GIF;*.PNG|All files (*.*)|*.*",
				Title    = "Open text file"
			};


			m_cl                 =  new SearchClient(new SearchConfig());
			m_cl.ResultCompleted += Display;

			var i = Enum.GetValues<SearchEngineOptions>()
				.Cast<object>()
				.ToArray();

			checkedListBox1.Items.AddRange(i);

			checkedListBox2.Items.AddRange(i);

			var allIdx  = checkedListBox1.Items.IndexOf((object) (SearchEngineOptions.All));
			var noneIdx = checkedListBox1.Items.IndexOf((object) (SearchEngineOptions.None));

			checkedListBox1.SetItemCheckState(allIdx, CheckState.Checked);
			checkedListBox1.SetItemCheckState(noneIdx, CheckState.Indeterminate);


			handleflags(SearchEngineOptions.All);
		}

		private void Form1_Load(object sender, EventArgs e) { }

		private void openFileDialog1_FileOk(object sender, CancelEventArgs e) { }

		private SearchClient m_cl;

		private void Display(object o, SearchClient.SearchResultEventArgs args)
		{
			var searchResult = args.Result;

			var listViewItem = new ListViewItem(searchResult.Engine.Name)
			{
				UseItemStyleForSubItems = false
			};

			var imageResult = searchResult.PrimaryResult;

			if (searchResult.IsSuccessful) {
				listViewItem.SubItems.Add(searchResult.IsPrimitive
					? searchResult.RawUri.ToString()
					: imageResult.Url?.ToString());
			}
			else {
				listViewItem.SubItems.Add(String.Empty);
			}

			listViewItem.SubItems.Add(imageResult.Similarity.HasValue
				? imageResult.Similarity.ToString()
				: String.Empty);

			listViewItem.SubItems.Add(imageResult.Artist ?? String.Empty);

			listViewItem.SubItems.Add(searchResult.OtherResults.Any()
				? searchResult.OtherResults.Count.ToString()
				: String.Empty);

			listViewItem.SubItems.Add(imageResult.DetailScore > 0 ? imageResult.DetailScore.ToString() : String.Empty);

			if (searchResult.Status == ResultStatus.Success) {
				listViewItem.SubItems.Add(searchResult.Status.ToString(), Color.Empty, Color.Green, Font);
			}
			else listViewItem.SubItems.Add(searchResult.Status.ToString());

			resultsListView.Items.Add(listViewItem);

			searchProgressBar.Value = (int) Math.Ceiling(((double) m_cl.Results.Count / m_cl.Engines.Length) * 100);

		}

		private static void Alert() => SystemSounds.Asterisk.Play();

		private async void runButton_Click(object sender, EventArgs e)
		{
			//var r  = cl.Maximize(r => r.PrimaryResult.Similarity);
			//var r2 = await r;

			//listBox1.DisplayMember = "Engine";
			//listBox1.ValueMember   = "PrimaryResult";

			var r = m_cl.RunSearchAsync();
			await r;
			Alert();
		}

		private void Update(ImageQuery query)
		{


			m_cl.Config.Query  = query;
			uploadTextBox.Text = m_cl.Config.Query.Uri.ToString();


		}

		private void selectButton_Click(object sender, EventArgs e)
		{
			if (openFileDialog1.ShowDialog() == DialogResult.OK) {
				try {
					var filePath = openFileDialog1.FileName;
					//using Stream str      = openFileDialog1.OpenFile();

					Update(filePath);
					inputTextBox.Text = filePath;
				}
				catch (SecurityException ex) {
					MessageBox.Show($"Security error.\n\nError message: {ex.Message}\n\n" +
					                $"Details:\n\n{ex.StackTrace}");
				}
			}
		}

		private void listView1_SelectedIndexChanged(object sender, EventArgs e) { }

		private void inputTextBox_TextChanged(object sender, EventArgs e)
		{
			if (inputTextBox == null) {
				inputLabel.Text      = "-";
				inputLabel.ForeColor = SystemColors.Control;
				return;
			}

			try {
				Update(inputTextBox.Text);
				inputLabel.Text      = "✓";
				inputLabel.ForeColor = Color.Green;
			}
			catch {
				inputLabel.Text      = "X";
				inputLabel.ForeColor = Color.Red;
			}
		}

		private async void refineButton_Click(object sender, EventArgs e)
		{
			resultsListView.Items.Clear();
			searchProgressBar.Value = 0;

			var r = m_cl.RefineSearchAsync();
			await r;
			Alert();
		}

		/*private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e)
		{

			//var s = string.Join(", ", checkedListBox1.CheckedItems.Cast<string>());

			try {
				SearchEngineOptions options = SearchEngineOptions.None;

				foreach (var item in checkedListBox1.CheckedItems) {
					var v = Enum.Parse<SearchEngineOptions>((string)item);
					options |= v;
				}

				Debug.WriteLine($"{options}");

				m_cfg.SearchEngines = options;
			}
			catch {
				
			}
		}*/

		/*

		 * if (checkedListBox1.GetItemCheckState(checkedListBox1.Items.IndexOf(SearchEngineOptions.Auto)) == CheckState.Checked) {
				foreach (var item in checkedListBox1.Items) {
					checkedListBox1.SetItemCheckState(checkedListBox1.Items.IndexOf(item), CheckState.Unchecked);
				}
			}
		 */
		void handleflags(SearchEngineOptions enumv)
		{

			var enumv2 = Enums.GetSetFlags(enumv);

			foreach (var v in enumv2) {
				var idx = checkedListBox1.Items.IndexOf(v);
				checkedListBox1.SetItemCheckState(idx, CheckState.Checked);
			}
		}


		private void checkedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
		{


			var txt    = checkedListBox1.Items[e.Index].ToString();
			var enumv  = Enum.Parse<SearchEngineOptions>(txt);
			var enumv2 = Enums.GetSetFlags(enumv);


			if (e.NewValue == CheckState.Checked) {
				m_cl.Config.SearchEngines |= enumv;

			}
			else if (e.NewValue == CheckState.Unchecked) {
				m_cl.Config.SearchEngines &= ~enumv;
			}

			Debug.WriteLine($"{m_cl.Config.SearchEngines}");
			//Debug.WriteLine($"{txt} | {enumv} | {enumv2.QuickJoin()}");
		}

		private void resetButton_Click(object sender, EventArgs e)
		{
			resultsListView.Items.Clear();
			m_cl.Reset();
			searchProgressBar.Value = 0;


			uploadTextBox.Text = null;
			inputTextBox.Text  = null;
		}
	}
}