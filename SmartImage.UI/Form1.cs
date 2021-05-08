using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Searching;

namespace SmartImage.UI
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();

			openFileDialog1 = new OpenFileDialog()
			{
				FileName = "Select a text file",
				Filter   = "Image Files(*.BMP;*.JPG;*.GIF)|*.BMP;*.JPG;*.GIF|All files (*.*)|*.*",
				Title    = "Open text file"
			};


		}

		private void Form1_Load(object sender, EventArgs e) { }

		private void openFileDialog1_FileOk(object sender, CancelEventArgs e) { }

		private ImageQuery   q;
		private SearchClient cl;
		SearchConfig         cfg;

		private async void runButton_Click(object sender, EventArgs e)
		{
			//var r  = cl.Maximize(r => r.PrimaryResult.Similarity);
			//var r2 = await r;

			//listBox1.DisplayMember = "Engine";
			//listBox1.ValueMember   = "PrimaryResult";
			

			cl.ResultCompleted += (o, args) =>
			{
				var searchResult = args.Result;


				var a = new ListViewItem(searchResult.Engine.Name);
				a.UseItemStyleForSubItems = false;

				var imageResult = searchResult.PrimaryResult;

				if (searchResult.IsSuccessful) {
					a.SubItems.Add(searchResult.IsPrimitive
						? searchResult.RawUri.ToString()
						: imageResult.Url.ToString());
				}
				else {
					a.SubItems.Add(String.Empty);
				}


				a.SubItems.Add(imageResult.Similarity.HasValue
					? imageResult.Similarity.ToString()
					: string.Empty);

				a.SubItems.Add(imageResult.Artist ?? string.Empty);

				a.SubItems.Add(searchResult.OtherResults.Count.ToString());

				if (searchResult.Status == ResultStatus.Success) {
					a.SubItems.Add(searchResult.Status.ToString(), Color.Empty, Color.Green, Font);

				}
				else a.SubItems.Add(searchResult.Status.ToString());

				listView1.Items.Add(a);

				progressBar1.Value = (int) Math.Ceiling(((double) cl.Results.Count / cl.Engines.Length) * 100);
			};
			var r = cl.RunSearchAsync();
			await r;
		}

		void setq(ImageQuery newq)
		{
			cfg = new SearchConfig
				{ Query = newq, SearchEngines = SearchEngineOptions.All };
			cl = new SearchClient(cfg);
			q = newq;
		}
		private async void selectButton_Click(object sender, EventArgs e)
		{
			if (openFileDialog1.ShowDialog() == DialogResult.OK) {
				try {
					var filePath = openFileDialog1.FileName;
					//using Stream str      = openFileDialog1.OpenFile();


					setq(q);
					textBox1.Text = q.ToString();


				}
				catch (SecurityException ex) {
					MessageBox.Show($"Security error.\n\nError message: {ex.Message}\n\n" +
					                $"Details:\n\n{ex.StackTrace}");
				}
			}
		}

		private void listView1_SelectedIndexChanged(object sender, EventArgs e) { }

		private void textBox1_TextChanged(object sender, EventArgs e)
		{
			setq(textBox1.Text);
		}
	}
}