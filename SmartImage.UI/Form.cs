using SimpleCore.Net;
using SimpleCore.Utilities;
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
using System.Runtime.InteropServices;
using System.Security;
using System.Threading.Tasks;
using System.Windows.Forms;
using SmartImage.Lib.Utilities;
using Exception = System.Exception;

// ReSharper disable LocalizableElement
// ReSharper disable IdentifierTypo
#pragma warning disable IDE1006

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

			Init();
			searchProgressBar.Step = GetStep();

			var i = Enum.GetValues<SearchEngineOptions>()
				.Cast<object>()
				.ToArray();

			checkedListBox1.Items.AddRange(i);
			checkedListBox2.Items.AddRange(i);

			var allIdx  = checkedListBox1.Items.IndexOf((object) (SearchEngineOptions.All));
			var noneIdx = checkedListBox1.Items.IndexOf((object) (SearchEngineOptions.None));

			checkedListBox1.SetItemCheckState(allIdx, CheckState.Checked);
			checkedListBox1.SetItemCheckState(noneIdx, CheckState.Indeterminate);

			AllowDrop =  true;
			DragEnter += Form1_DragEnter;
			DragDrop  += Form1_DragDrop;
		}


		private SearchClient m_cl;

		private static readonly Color[] SimilarityGradient =
			ColorHelper.GetGradients(Color.Red, Color.ForestGreen, 100).ToArray();

		private int GetStep() => (int) Math.Ceiling(((double) 1 / m_cl.Engines.Length) * 100);

		private void Init()
		{
			m_cl                 =  new SearchClient(new SearchConfig());
			m_cl.ResultCompleted += HandleResult;
		}

		private void HandleResult(object o, SearchResultEventArgs args)
		{
			var searchResult = args.Result;

			if (!searchResult.IsNonPrimitive && filterCheckBox.Checked) {
				searchProgressBar.PerformStep();
				return;
			}

			ListViewGroup listViewGroup = null;

			bool hasg = false;

			for (int i = 0; i < resultsListView.Groups.Count; i++) {
				if (resultsListView.Groups[i].Header == searchResult.Engine.Name) {
					listViewGroup = resultsListView.Groups[i];
					hasg          = true;
				}
			}

			if (!hasg) {
				listViewGroup = new ListViewGroup(searchResult.Engine.Name);
				resultsListView.Groups.Add(listViewGroup);
			}

			AddSearchResult(searchResult, listViewGroup);

			for (int i = 0; i < searchResult.OtherResults.Count; i++) {
				var oItem = searchResult.OtherResults[i];

				var listViewItem2 = new ListViewItem($"Other #{i + 1}")
				{
					UseItemStyleForSubItems = false,
					Group                   = listViewGroup
				};

				listViewItem2.SubItems.Add(oItem.Url?.ToString());
				AddImageResult(oItem, listViewItem2);
				resultsListView.Items.Add(listViewItem2);
			}


			searchProgressBar.PerformStep();
		}

		/*public static Icon Extract(string file, int number, bool largeIcon)
		{
			IntPtr large;
			IntPtr small;
			ExtractIconEx(file, number, out large, out small, 1);

			try {
				return Icon.FromHandle(largeIcon ? large : small);
			}
			catch {
				return null;
			}

		}

		[DllImport("Shell32.dll", EntryPoint = "ExtractIconExW", CharSet = CharSet.Unicode, ExactSpelling = true,
			CallingConvention                = CallingConvention.StdCall)]
		private static extern int ExtractIconEx(string sFile, int iIndex, out IntPtr piLargeVersion,
			out IntPtr piSmallVersion, int amountIcons);*/

		private void AddSearchResult(SearchResult searchResult, ListViewGroup listViewGroup)
		{
			var listViewItem = new ListViewItem($"Primary")
			{
				UseItemStyleForSubItems = false
			};

			var imageResult = searchResult.PrimaryResult;

			if (searchResult.IsSuccessful) {
				listViewItem.SubItems.Add(!searchResult.IsNonPrimitive
					? searchResult.RawUri.ToString()
					: imageResult.Url?.ToString());
			}
			else {
				listViewItem.SubItems.Add(String.Empty);
			}

			//
			AddImageResult(imageResult, listViewItem);

			if (searchResult.Status == ResultStatus.Success) {
				listViewItem.SubItems.Add(searchResult.Status.ToString(), Color.Empty, Color.Green, Font);

			}
			else listViewItem.SubItems.Add(searchResult.Status.ToString());

			listViewItem.Group = listViewGroup;

			resultsListView.SmallImageList = new ImageList();

			var img = searchResult.OtherResults.FirstOrDefault(f => ImageHelper.IsDirect(f?.Url?.ToString()));

			if (img is not null) {
				var s = WebUtilities.GetStream(img.Url.ToString());
				resultsListView.SmallImageList.Images.Add(Image.FromStream(s));
			}

			listViewItem.ImageIndex = 0;
			resultsListView.Items.Add(listViewItem);
		}

		private void AddImageResult(ImageResult imageResult, ListViewItem listViewItem)
		{
			if (imageResult.Similarity.HasValue) {

				listViewItem.SubItems.Add($"{imageResult.Similarity / 100:P}",
					SimilarityGradient[(int) Math.Ceiling(imageResult.Similarity.Value)], Color.Empty, null);

			}
			else {
				listViewItem.SubItems.Add(String.Empty);
			}

			listViewItem.SubItems.Add(imageResult.Artist      ?? String.Empty);
			listViewItem.SubItems.Add(imageResult.Site        ?? String.Empty);
			listViewItem.SubItems.Add(imageResult.Source      ?? String.Empty);
			listViewItem.SubItems.Add(imageResult.Description ?? String.Empty);

			listViewItem.SubItems.Add(imageResult.HasImageDimensions
				? $"{imageResult.Height}x{imageResult.Width}"
				: String.Empty);

			listViewItem.SubItems.Add(imageResult.DetailScore > 0 ? imageResult.DetailScore.ToString() : String.Empty);
		}

		private static void Alert() => SystemSounds.Asterisk.Play();

		private async void RunSearch()
		{
			if (m_cl.Config.Query == null) {
				MessageBox.Show("Specify an image", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

				return;
			}

			// if (m_cl.IsComplete) {
			// 	clear_fields();
			// 	reset1();
			// }

			var r = m_cl.RunSearchAsync();
			await r;
			Alert();
			Debug.WriteLine($"Finding best");

			var sw   = Stopwatch.StartNew();
			var best = await Task.Run((() => m_cl.FindBestResult()));
			sw.Stop();
			Debug.WriteLine($"{sw.Elapsed.TotalSeconds}");

			if (best is not null) {
				previewPictureBox.Image = Image.FromStream(WebUtilities.GetStream(best.Url?.ToString()));
			}
		}

		private void Update(ImageQuery query)
		{
			m_cl.Config.Query     = query;
			inputTextBox.Text     = query.Value;
			uploadTextBox.Text    = m_cl.Config.Query.Image.ToString();
			inputPictureBox.Image = Image.FromStream(query.Stream);
		}


		private void Reset()
		{
			ClearResults();
			//m_cl.Reset();
			//Update(null);
			Init();

			ClearFields();
		}

		private void ClearResults()
		{
			resultsListView.Items.Clear();
			searchProgressBar.Value = 0;
		}

		private void ClearFields()
		{
			uploadTextBox.Text      = null;
			inputTextBox.Text       = null;
			inputPictureBox.Image   = null;
			previewPictureBox.Image = null;
		}

		#region Controls

		private void Form1_Load(object sender, EventArgs e) { }

		private void openFileDialog1_FileOk(object sender, CancelEventArgs e) { }

		private void Form1_DragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
				e.Effect = DragDropEffects.Copy;
			}
		}

		private void Form1_DragDrop(object sender, DragEventArgs e)
		{
			string[] files = (string[]) e.Data.GetData(DataFormats.FileDrop);

			foreach (string file in files) {
				Debug.WriteLine(file);
			}

			Update(files.First());

			if (autoSearchCheckBox.Checked) {
				RunSearch();
			}
		}

		private void runButton_Click(object sender, EventArgs e)
		{
			RunSearch();
		}

		private void selectButton_Click(object sender, EventArgs e)
		{
			if (openFileDialog1.ShowDialog() == DialogResult.OK) {
				try {
					var filePath = openFileDialog1.FileName;
					//using Stream str      = openFileDialog1.OpenFile();

					Update(filePath);

				}
				catch (SecurityException ex) {
					MessageBox.Show($"Security error.\n\nError message: {ex.Message}\n\n" +
					                $"Details:\n\n{ex.StackTrace}");
				}
			}
		}

		private void inputTextBox_TextChanged(object sender, EventArgs e)
		{
			try {
				Update(inputTextBox.Text);
				inputLabel.Text      = "✓";
				inputLabel.ForeColor = Color.LawnGreen;
			}
			catch (Exception ex) {
				inputLabel.Text      = "X";
				inputLabel.ForeColor = Color.Red;
				Debug.WriteLine($"{ex.Message}");
			}
		}

		private async void refineButton_Click(object sender, EventArgs e)
		{
			ClearResults();

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

		/*private void handleflags(SearchEngineOptions enumv)
		{
			var enumv2 = Enums.GetSetFlags(enumv);

			foreach (var v in enumv2) {
				var idx = checkedListBox1.Items.IndexOf(v);

				checkedListBox1.SetItemCheckState(idx,
					checkedListBox1.GetItemCheckState(idx) == CheckState.Checked
						? CheckState.Unchecked
						: CheckState.Checked);
			}
		}*/

		private void resetButton_Click(object sender, EventArgs e)
		{
			Reset();
		}

		private void resultsListView_Click(object sender, EventArgs e)
		{
			foreach (int i in resultsListView.SelectedIndices) {
				var s = resultsListView.Items[i].SubItems;

				if (s.Count >= 2) {
					// url column
					var text = s[1].Text;

					if (!string.IsNullOrWhiteSpace(text)) {
						var url = text;

						WebUtilities.OpenUrl(url);
					}
				}
			}
		}

		private void checkedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			checkedListBox1.ItemCheck -= checkedListBox1_ItemCheck;

			var txt   = checkedListBox1.Items[e.Index].ToString();
			var enumv = Enum.Parse<SearchEngineOptions>(txt);


			if (e.NewValue == CheckState.Checked) {
				m_cl.Config.SearchEngines |= enumv;
			}
			else if (e.NewValue == CheckState.Unchecked) {
				m_cl.Config.SearchEngines &= ~enumv;
			}


			var enumv2 = Enums.GetSetFlags(m_cl.Config.SearchEngines);


			//for (int index = 0; index < checkedListBox1.Items.Count; index++) {
			//	var item = Enum.Parse<SearchEngineOptions>(checkedListBox1.Items[index].ToString());

			//	var state = checkedListBox1.GetItemCheckState(index);

			//	if (enumv2.Contains(item)) {
			//		checkedListBox1.SetItemCheckState(index, state ==CheckState.Checked?CheckState.Unchecked :  CheckState.Checked);
			//	}
			//	else if (enumv == 0) {

			//	}

			//}

			Debug.WriteLine($"{txt} | {m_cl.Config.SearchEngines} | {enumv2.QuickJoin()}");
			//Debug.WriteLine($"{txt} | {enumv} | {enumv2.QuickJoin()}");
			checkedListBox1.ItemCheck += checkedListBox1_ItemCheck;

			searchProgressBar.Step = GetStep();
		}

		private void checkedListBox1_Click(object sender, EventArgs e) { }

		#endregion
	}
}