
namespace SmartImage.UI
{
	partial class SmartImageForm
	{
		/// <summary>
		///  Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		///  Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		///  Required method for Designer support - do not modify
		///  the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
			this.selectButton = new System.Windows.Forms.Button();
			this.resultsListView = new System.Windows.Forms.ListView();
			this.columnHeaderResult = new System.Windows.Forms.ColumnHeader();
			this.columnHeaderUrl = new System.Windows.Forms.ColumnHeader();
			this.columnHeaderSim = new System.Windows.Forms.ColumnHeader();
			this.columnHeaderArtist = new System.Windows.Forms.ColumnHeader();
			this.columnHeaderSite = new System.Windows.Forms.ColumnHeader();
			this.columnHeaderSource = new System.Windows.Forms.ColumnHeader();
			this.columnHeaderDetailScore = new System.Windows.Forms.ColumnHeader();
			this.columnHeaderResolution = new System.Windows.Forms.ColumnHeader();
			this.columnHeaderStatus = new System.Windows.Forms.ColumnHeader();
			this.searchProgressBar = new System.Windows.Forms.ProgressBar();
			this.inputTextBox = new System.Windows.Forms.TextBox();
			this.runButton = new System.Windows.Forms.Button();
			this.inputLabel = new System.Windows.Forms.Label();
			this.refineButton = new System.Windows.Forms.Button();
			this.uploadTextBox = new System.Windows.Forms.TextBox();
			this.tabControl1 = new System.Windows.Forms.TabControl();
			this.searchTabPage = new System.Windows.Forms.TabPage();
			this.filterCheckBox = new System.Windows.Forms.CheckBox();
			this.resetButton = new System.Windows.Forms.Button();
			this.configTabPage = new System.Windows.Forms.TabPage();
			this.checkedListBox2 = new System.Windows.Forms.CheckedListBox();
			this.checkedListBox1 = new System.Windows.Forms.CheckedListBox();
			this.button1 = new System.Windows.Forms.Button();
			this.tabControl2 = new System.Windows.Forms.TabControl();
			this.tabPage3 = new System.Windows.Forms.TabPage();
			this.label1 = new System.Windows.Forms.Label();
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.button2 = new System.Windows.Forms.Button();
			this.listView1 = new System.Windows.Forms.ListView();
			this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
			this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
			this.columnHeader3 = new System.Windows.Forms.ColumnHeader();
			this.columnHeader4 = new System.Windows.Forms.ColumnHeader();
			this.columnHeader5 = new System.Windows.Forms.ColumnHeader();
			this.columnHeader6 = new System.Windows.Forms.ColumnHeader();
			this.columnHeader7 = new System.Windows.Forms.ColumnHeader();
			this.button3 = new System.Windows.Forms.Button();
			this.textBox2 = new System.Windows.Forms.TextBox();
			this.tabPage4 = new System.Windows.Forms.TabPage();
			this.tabControl1.SuspendLayout();
			this.searchTabPage.SuspendLayout();
			this.configTabPage.SuspendLayout();
			this.tabControl2.SuspendLayout();
			this.tabPage3.SuspendLayout();
			this.SuspendLayout();
			// 
			// openFileDialog1
			// 
			this.openFileDialog1.FileName = "openFileDialog1";
			this.openFileDialog1.FileOk += new System.ComponentModel.CancelEventHandler(this.openFileDialog1_FileOk);
			// 
			// selectButton
			// 
			this.selectButton.Location = new System.Drawing.Point(3, 573);
			this.selectButton.Name = "selectButton";
			this.selectButton.Size = new System.Drawing.Size(75, 23);
			this.selectButton.TabIndex = 0;
			this.selectButton.Text = "Open file";
			this.selectButton.UseVisualStyleBackColor = true;
			this.selectButton.Click += new System.EventHandler(this.selectButton_Click);
			// 
			// resultsListView
			// 
			this.resultsListView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.resultsListView.BackColor = System.Drawing.SystemColors.Control;
			this.resultsListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderResult,
            this.columnHeaderUrl,
            this.columnHeaderSim,
            this.columnHeaderArtist,
            this.columnHeaderSite,
            this.columnHeaderSource,
            this.columnHeaderResolution,
            this.columnHeaderDetailScore,
            this.columnHeaderStatus});
			this.resultsListView.HideSelection = false;
			this.resultsListView.Location = new System.Drawing.Point(3, 3);
			this.resultsListView.Name = "resultsListView";
			this.resultsListView.Size = new System.Drawing.Size(1086, 477);
			this.resultsListView.TabIndex = 2;
			this.resultsListView.UseCompatibleStateImageBehavior = false;
			this.resultsListView.View = System.Windows.Forms.View.Details;
			this.resultsListView.SelectedIndexChanged += new System.EventHandler(this.listView1_SelectedIndexChanged);
			this.resultsListView.Click += new System.EventHandler(this.resultsListView_Click);
			// 
			// columnHeaderResult
			// 
			this.columnHeaderResult.Text = "Result";
			this.columnHeaderResult.Width = 80;
			// 
			// columnHeaderUrl
			// 
			this.columnHeaderUrl.Text = "Url";
			this.columnHeaderUrl.Width = 300;
			// 
			// columnHeaderSim
			// 
			this.columnHeaderSim.Text = "Similarity";
			this.columnHeaderSim.Width = 70;
			// 
			// columnHeaderArtist
			// 
			this.columnHeaderArtist.Text = "Artist";
			this.columnHeaderArtist.Width = 80;
			// 
			// columnHeaderSite
			// 
			this.columnHeaderSite.Text = "Site";
			this.columnHeaderSite.Width = 80;
			// 
			// columnHeaderSource
			// 
			this.columnHeaderSource.Text = "Source";
			this.columnHeaderSource.Width = 80;
			// 
			// columnHeaderDetailScore
			// 
			this.columnHeaderDetailScore.Text = "Detail Score";
			this.columnHeaderDetailScore.Width = 80;
			// 
			// columnHeaderResolution
			// 
			this.columnHeaderResolution.Text = "Resolution";
			this.columnHeaderResolution.Width = 80;
			// 
			// columnHeaderStatus
			// 
			this.columnHeaderStatus.Text = "Status";
			this.columnHeaderStatus.Width = 80;
			// 
			// searchProgressBar
			// 
			this.searchProgressBar.Location = new System.Drawing.Point(3, 486);
			this.searchProgressBar.Name = "searchProgressBar";
			this.searchProgressBar.Size = new System.Drawing.Size(1086, 23);
			this.searchProgressBar.TabIndex = 3;
			// 
			// inputTextBox
			// 
			this.inputTextBox.BackColor = System.Drawing.SystemColors.Control;
			this.inputTextBox.Location = new System.Drawing.Point(3, 515);
			this.inputTextBox.Name = "inputTextBox";
			this.inputTextBox.Size = new System.Drawing.Size(517, 23);
			this.inputTextBox.TabIndex = 4;
			this.inputTextBox.TextChanged += new System.EventHandler(this.inputTextBox_TextChanged);
			// 
			// runButton
			// 
			this.runButton.Location = new System.Drawing.Point(1014, 514);
			this.runButton.Name = "runButton";
			this.runButton.Size = new System.Drawing.Size(75, 53);
			this.runButton.TabIndex = 5;
			this.runButton.Text = "Search";
			this.runButton.UseVisualStyleBackColor = true;
			this.runButton.Click += new System.EventHandler(this.runButton_Click);
			// 
			// inputLabel
			// 
			this.inputLabel.AutoSize = true;
			this.inputLabel.ForeColor = System.Drawing.SystemColors.Control;
			this.inputLabel.Location = new System.Drawing.Point(526, 518);
			this.inputLabel.Name = "inputLabel";
			this.inputLabel.Size = new System.Drawing.Size(12, 15);
			this.inputLabel.TabIndex = 6;
			this.inputLabel.Text = "-";
			// 
			// refineButton
			// 
			this.refineButton.Location = new System.Drawing.Point(1014, 573);
			this.refineButton.Name = "refineButton";
			this.refineButton.Size = new System.Drawing.Size(75, 23);
			this.refineButton.TabIndex = 7;
			this.refineButton.Text = "Refine";
			this.refineButton.UseVisualStyleBackColor = true;
			this.refineButton.Click += new System.EventHandler(this.refineButton_Click);
			// 
			// uploadTextBox
			// 
			this.uploadTextBox.Location = new System.Drawing.Point(3, 544);
			this.uploadTextBox.Name = "uploadTextBox";
			this.uploadTextBox.ReadOnly = true;
			this.uploadTextBox.Size = new System.Drawing.Size(517, 23);
			this.uploadTextBox.TabIndex = 8;
			// 
			// tabControl1
			// 
			this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tabControl1.Controls.Add(this.searchTabPage);
			this.tabControl1.Controls.Add(this.configTabPage);
			this.tabControl1.Location = new System.Drawing.Point(12, 12);
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.SelectedIndex = 0;
			this.tabControl1.Size = new System.Drawing.Size(1100, 627);
			this.tabControl1.TabIndex = 9;
			// 
			// searchTabPage
			// 
			this.searchTabPage.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
			this.searchTabPage.Controls.Add(this.filterCheckBox);
			this.searchTabPage.Controls.Add(this.resetButton);
			this.searchTabPage.Controls.Add(this.inputLabel);
			this.searchTabPage.Controls.Add(this.selectButton);
			this.searchTabPage.Controls.Add(this.refineButton);
			this.searchTabPage.Controls.Add(this.uploadTextBox);
			this.searchTabPage.Controls.Add(this.resultsListView);
			this.searchTabPage.Controls.Add(this.searchProgressBar);
			this.searchTabPage.Controls.Add(this.runButton);
			this.searchTabPage.Controls.Add(this.inputTextBox);
			this.searchTabPage.Location = new System.Drawing.Point(4, 24);
			this.searchTabPage.Name = "searchTabPage";
			this.searchTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.searchTabPage.Size = new System.Drawing.Size(1092, 599);
			this.searchTabPage.TabIndex = 0;
			this.searchTabPage.Text = "Search";
			// 
			// filterCheckBox
			// 
			this.filterCheckBox.AutoSize = true;
			this.filterCheckBox.ForeColor = System.Drawing.SystemColors.Control;
			this.filterCheckBox.Location = new System.Drawing.Point(956, 546);
			this.filterCheckBox.Name = "filterCheckBox";
			this.filterCheckBox.Size = new System.Drawing.Size(52, 19);
			this.filterCheckBox.TabIndex = 10;
			this.filterCheckBox.Text = "Filter";
			this.filterCheckBox.UseVisualStyleBackColor = true;
			// 
			// resetButton
			// 
			this.resetButton.Location = new System.Drawing.Point(933, 518);
			this.resetButton.Name = "resetButton";
			this.resetButton.Size = new System.Drawing.Size(75, 23);
			this.resetButton.TabIndex = 9;
			this.resetButton.Text = "Reset";
			this.resetButton.UseVisualStyleBackColor = true;
			this.resetButton.Click += new System.EventHandler(this.resetButton_Click);
			// 
			// configTabPage
			// 
			this.configTabPage.Controls.Add(this.checkedListBox2);
			this.configTabPage.Controls.Add(this.checkedListBox1);
			this.configTabPage.Location = new System.Drawing.Point(4, 24);
			this.configTabPage.Name = "configTabPage";
			this.configTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.configTabPage.Size = new System.Drawing.Size(1092, 599);
			this.configTabPage.TabIndex = 1;
			this.configTabPage.Text = "Config";
			this.configTabPage.UseVisualStyleBackColor = true;
			// 
			// checkedListBox2
			// 
			this.checkedListBox2.CheckOnClick = true;
			this.checkedListBox2.FormattingEnabled = true;
			this.checkedListBox2.Location = new System.Drawing.Point(156, 24);
			this.checkedListBox2.Name = "checkedListBox2";
			this.checkedListBox2.Size = new System.Drawing.Size(147, 274);
			this.checkedListBox2.TabIndex = 1;
			// 
			// checkedListBox1
			// 
			this.checkedListBox1.CheckOnClick = true;
			this.checkedListBox1.FormattingEnabled = true;
			this.checkedListBox1.Location = new System.Drawing.Point(3, 24);
			this.checkedListBox1.Name = "checkedListBox1";
			this.checkedListBox1.Size = new System.Drawing.Size(147, 274);
			this.checkedListBox1.TabIndex = 0;
			this.checkedListBox1.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.checkedListBox1_ItemCheck);
			this.checkedListBox1.Click += new System.EventHandler(this.checkedListBox1_Click);
			// 
			// button1
			// 
			this.button1.Location = new System.Drawing.Point(695, 485);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(75, 23);
			this.button1.TabIndex = 7;
			this.button1.Text = "Refine";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.refineButton_Click);
			// 
			// tabControl2
			// 
			this.tabControl2.Controls.Add(this.tabPage3);
			this.tabControl2.Controls.Add(this.tabPage4);
			this.tabControl2.Location = new System.Drawing.Point(-6, 17);
			this.tabControl2.Name = "tabControl2";
			this.tabControl2.SelectedIndex = 0;
			this.tabControl2.Size = new System.Drawing.Size(776, 462);
			this.tabControl2.TabIndex = 9;
			// 
			// tabPage3
			// 
			this.tabPage3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
			this.tabPage3.Controls.Add(this.label1);
			this.tabPage3.Controls.Add(this.textBox1);
			this.tabPage3.Controls.Add(this.button2);
			this.tabPage3.Controls.Add(this.listView1);
			this.tabPage3.Controls.Add(this.button3);
			this.tabPage3.Controls.Add(this.textBox2);
			this.tabPage3.Location = new System.Drawing.Point(4, 24);
			this.tabPage3.Name = "tabPage3";
			this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage3.Size = new System.Drawing.Size(768, 434);
			this.tabPage3.TabIndex = 0;
			this.tabPage3.Text = "tabPage1";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.ForeColor = System.Drawing.SystemColors.Control;
			this.label1.Location = new System.Drawing.Point(526, 349);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(12, 15);
			this.label1.TabIndex = 6;
			this.label1.Text = "-";
			// 
			// textBox1
			// 
			this.textBox1.Location = new System.Drawing.Point(3, 375);
			this.textBox1.Name = "textBox1";
			this.textBox1.ReadOnly = true;
			this.textBox1.Size = new System.Drawing.Size(517, 23);
			this.textBox1.TabIndex = 8;
			// 
			// button2
			// 
			this.button2.Location = new System.Drawing.Point(3, 404);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(75, 23);
			this.button2.TabIndex = 0;
			this.button2.Text = "Open file";
			this.button2.UseVisualStyleBackColor = true;
			this.button2.Click += new System.EventHandler(this.selectButton_Click);
			// 
			// listView1
			// 
			this.listView1.BackColor = System.Drawing.SystemColors.Control;
			this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3,
            this.columnHeader4,
            this.columnHeader5,
            this.columnHeader6,
            this.columnHeader7});
			this.listView1.HideSelection = false;
			this.listView1.Location = new System.Drawing.Point(3, 3);
			this.listView1.Name = "listView1";
			this.listView1.Size = new System.Drawing.Size(762, 308);
			this.listView1.TabIndex = 2;
			this.listView1.UseCompatibleStateImageBehavior = false;
			this.listView1.View = System.Windows.Forms.View.Details;
			this.listView1.SelectedIndexChanged += new System.EventHandler(this.listView1_SelectedIndexChanged);
			// 
			// columnHeader1
			// 
			this.columnHeader1.Text = "Engine";
			this.columnHeader1.Width = 90;
			// 
			// columnHeader2
			// 
			this.columnHeader2.Text = "Url";
			this.columnHeader2.Width = 250;
			// 
			// columnHeader3
			// 
			this.columnHeader3.Text = "Similarity";
			this.columnHeader3.Width = 70;
			// 
			// columnHeader4
			// 
			this.columnHeader4.Text = "Artist";
			this.columnHeader4.Width = 80;
			// 
			// columnHeader5
			// 
			this.columnHeader5.Text = "Other";
			// 
			// columnHeader6
			// 
			this.columnHeader6.Text = "Detail Score";
			this.columnHeader6.Width = 80;
			// 
			// columnHeader7
			// 
			this.columnHeader7.Text = "Status";
			this.columnHeader7.Width = 80;
			// 
			// button3
			// 
			this.button3.Location = new System.Drawing.Point(690, 346);
			this.button3.Name = "button3";
			this.button3.Size = new System.Drawing.Size(75, 53);
			this.button3.TabIndex = 5;
			this.button3.Text = "Search";
			this.button3.UseVisualStyleBackColor = true;
			this.button3.Click += new System.EventHandler(this.runButton_Click);
			// 
			// textBox2
			// 
			this.textBox2.BackColor = System.Drawing.SystemColors.Control;
			this.textBox2.Location = new System.Drawing.Point(3, 346);
			this.textBox2.Name = "textBox2";
			this.textBox2.Size = new System.Drawing.Size(517, 23);
			this.textBox2.TabIndex = 4;
			this.textBox2.TextChanged += new System.EventHandler(this.inputTextBox_TextChanged);
			// 
			// tabPage4
			// 
			this.tabPage4.Location = new System.Drawing.Point(4, 24);
			this.tabPage4.Name = "tabPage4";
			this.tabPage4.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage4.Size = new System.Drawing.Size(768, 434);
			this.tabPage4.TabIndex = 1;
			this.tabPage4.Text = "tabPage2";
			this.tabPage4.UseVisualStyleBackColor = true;
			// 
			// SmartImageForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
			this.ClientSize = new System.Drawing.Size(1124, 651);
			this.Controls.Add(this.tabControl1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "SmartImageForm";
			this.Text = "SmartImage";
			this.Load += new System.EventHandler(this.Form1_Load);
			this.tabControl1.ResumeLayout(false);
			this.searchTabPage.ResumeLayout(false);
			this.searchTabPage.PerformLayout();
			this.configTabPage.ResumeLayout(false);
			this.tabControl2.ResumeLayout(false);
			this.tabPage3.ResumeLayout(false);
			this.tabPage3.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.OpenFileDialog openFileDialog1;
		private System.Windows.Forms.Button selectButton;
		private System.Windows.Forms.ListView resultsListView;
		private System.Windows.Forms.ColumnHeader columnHeaderUrl;
		private System.Windows.Forms.ColumnHeader columnHeaderSim;
		private System.Windows.Forms.ColumnHeader columnHeaderStatus;
		private System.Windows.Forms.ProgressBar searchProgressBar;
		private System.Windows.Forms.ColumnHeader columnHeaderArtist;
		private System.Windows.Forms.TextBox inputTextBox;
		private System.Windows.Forms.Button runButton;
		private System.Windows.Forms.Label inputLabel;
		private System.Windows.Forms.Button refineButton;
		private System.Windows.Forms.ColumnHeader columnHeaderDetailScore;
		private System.Windows.Forms.TextBox uploadTextBox;
		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.TabPage searchTabPage;
		private System.Windows.Forms.TabPage configTabPage;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.TabControl tabControl2;
		private System.Windows.Forms.TabPage tabPage3;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox textBox1;
		private System.Windows.Forms.Button button2;
		private System.Windows.Forms.ListView listView1;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ColumnHeader columnHeader2;
		private System.Windows.Forms.ColumnHeader columnHeader3;
		private System.Windows.Forms.ColumnHeader columnHeader4;
		private System.Windows.Forms.ColumnHeader columnHeader5;
		private System.Windows.Forms.ColumnHeader columnHeader6;
		private System.Windows.Forms.ColumnHeader columnHeader7;
		private System.Windows.Forms.Button button3;
		private System.Windows.Forms.TextBox textBox2;
		private System.Windows.Forms.TabPage tabPage4;
		private System.Windows.Forms.CheckedListBox checkedListBox1;
		private System.Windows.Forms.CheckedListBox checkedListBox2;
		private System.Windows.Forms.Button resetButton;
		private System.Windows.Forms.ColumnHeader columnHeaderResult;
		private System.Windows.Forms.ColumnHeader columnHeaderSite;
		private System.Windows.Forms.ColumnHeader columnHeaderSource;
		private System.Windows.Forms.CheckBox filterCheckBox;
		private System.Windows.Forms.ColumnHeader columnHeaderResolution;
	}
}

