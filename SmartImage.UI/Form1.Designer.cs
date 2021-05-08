
namespace SmartImage.UI
{
	partial class Form1
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
			this.listView1 = new System.Windows.Forms.ListView();
			this.columnHeaderName = new System.Windows.Forms.ColumnHeader();
			this.columnHeaderUrl = new System.Windows.Forms.ColumnHeader();
			this.columnHeaderSim = new System.Windows.Forms.ColumnHeader();
			this.columnHeaderArtist = new System.Windows.Forms.ColumnHeader();
			this.columnHeaderOther = new System.Windows.Forms.ColumnHeader();
			this.columnHeaderStatus = new System.Windows.Forms.ColumnHeader();
			this.progressBar1 = new System.Windows.Forms.ProgressBar();
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.runButton = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// openFileDialog1
			// 
			this.openFileDialog1.FileName = "openFileDialog1";
			this.openFileDialog1.FileOk += new System.ComponentModel.CancelEventHandler(this.openFileDialog1_FileOk);
			// 
			// selectButton
			// 
			this.selectButton.Location = new System.Drawing.Point(13, 398);
			this.selectButton.Name = "selectButton";
			this.selectButton.Size = new System.Drawing.Size(75, 23);
			this.selectButton.TabIndex = 0;
			this.selectButton.Text = "Open file";
			this.selectButton.UseVisualStyleBackColor = true;
			this.selectButton.Click += new System.EventHandler(this.selectButton_Click);
			// 
			// listView1
			// 
			this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderName,
            this.columnHeaderUrl,
            this.columnHeaderSim,
            this.columnHeaderArtist,
            this.columnHeaderOther,
            this.columnHeaderStatus});
			this.listView1.HideSelection = false;
			this.listView1.Location = new System.Drawing.Point(12, 12);
			this.listView1.Name = "listView1";
			this.listView1.Size = new System.Drawing.Size(776, 321);
			this.listView1.TabIndex = 2;
			this.listView1.UseCompatibleStateImageBehavior = false;
			this.listView1.View = System.Windows.Forms.View.Details;
			this.listView1.SelectedIndexChanged += new System.EventHandler(this.listView1_SelectedIndexChanged);
			// 
			// columnHeaderName
			// 
			this.columnHeaderName.Text = "Engine";
			this.columnHeaderName.Width = 90;
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
			// columnHeaderOther
			// 
			this.columnHeaderOther.Text = "Other";
			// 
			// columnHeaderStatus
			// 
			this.columnHeaderStatus.Text = "Status";
			this.columnHeaderStatus.Width = 80;
			// 
			// progressBar1
			// 
			this.progressBar1.Location = new System.Drawing.Point(12, 339);
			this.progressBar1.Name = "progressBar1";
			this.progressBar1.Size = new System.Drawing.Size(776, 23);
			this.progressBar1.TabIndex = 3;
			// 
			// textBox1
			// 
			this.textBox1.Location = new System.Drawing.Point(13, 369);
			this.textBox1.Name = "textBox1";
			this.textBox1.Size = new System.Drawing.Size(517, 23);
			this.textBox1.TabIndex = 4;
			this.textBox1.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
			// 
			// runButton
			// 
			this.runButton.Location = new System.Drawing.Point(713, 369);
			this.runButton.Name = "runButton";
			this.runButton.Size = new System.Drawing.Size(75, 53);
			this.runButton.TabIndex = 5;
			this.runButton.Text = "Search";
			this.runButton.UseVisualStyleBackColor = true;
			this.runButton.Click += new System.EventHandler(this.runButton_Click);
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(800, 450);
			this.Controls.Add(this.runButton);
			this.Controls.Add(this.textBox1);
			this.Controls.Add(this.progressBar1);
			this.Controls.Add(this.listView1);
			this.Controls.Add(this.selectButton);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "Form1";
			this.Text = "Form1";
			this.Load += new System.EventHandler(this.Form1_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.OpenFileDialog openFileDialog1;
		private System.Windows.Forms.Button selectButton;
		private System.Windows.Forms.ListView listView1;
		private System.Windows.Forms.ColumnHeader columnHeaderName;
		private System.Windows.Forms.ColumnHeader columnHeaderUrl;
		private System.Windows.Forms.ColumnHeader columnHeaderSim;
		private System.Windows.Forms.ColumnHeader columnHeaderOther;
		private System.Windows.Forms.ColumnHeader columnHeaderStatus;
		private System.Windows.Forms.ProgressBar progressBar1;
		private System.Windows.Forms.ColumnHeader columnHeaderArtist;
		private System.Windows.Forms.TextBox textBox1;
		private System.Windows.Forms.Button runButton;
	}
}

