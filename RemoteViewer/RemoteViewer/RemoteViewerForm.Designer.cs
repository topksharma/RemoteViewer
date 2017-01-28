namespace RemoteViewer
{
	partial class RemoteViewerForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
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
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.toolStripTextBox1 = new System.Windows.Forms.ToolStripTextBox();
			this.toolStripButtonConnect = new System.Windows.Forms.ToolStripButton();
			this.toolStrip1 = new System.Windows.Forms.ToolStrip();
			this.imgViewer1 = new RemoteViewer.ImgViewer();
			this.toolStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// toolStripTextBox1
			// 
			this.toolStripTextBox1.Name = "toolStripTextBox1";
			this.toolStripTextBox1.Size = new System.Drawing.Size(150, 25);
			this.toolStripTextBox1.ToolTipText = "Enter IP Address here";
			// 
			// toolStripButtonConnect
			// 
			this.toolStripButtonConnect.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.toolStripButtonConnect.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButtonConnect.Name = "toolStripButtonConnect";
			this.toolStripButtonConnect.Size = new System.Drawing.Size(56, 22);
			this.toolStripButtonConnect.Text = "Connect";
			this.toolStripButtonConnect.TextImageRelation = System.Windows.Forms.TextImageRelation.TextBeforeImage;
			this.toolStripButtonConnect.Click += new System.EventHandler(this.toolStripButtonConnect_Click);
			// 
			// toolStrip1
			// 
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripTextBox1,
            this.toolStripButtonConnect});
			this.toolStrip1.Location = new System.Drawing.Point(0, 0);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.Size = new System.Drawing.Size(573, 25);
			this.toolStrip1.TabIndex = 0;
			this.toolStrip1.Text = "toolStrip1";
			// 
			// imgViewer1
			// 
			this.imgViewer1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.imgViewer1.ImageBitmap = null;
			this.imgViewer1.Location = new System.Drawing.Point(0, 25);
			this.imgViewer1.Name = "imgViewer1";
			this.imgViewer1.Size = new System.Drawing.Size(573, 486);
			this.imgViewer1.TabIndex = 1;
			// 
			// RemoteViewerForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(573, 511);
			this.Controls.Add(this.imgViewer1);
			this.Controls.Add(this.toolStrip1);
			this.MinimizeBox = false;
			this.Name = "RemoteViewerForm";
			this.Text = "Remote viewer";
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ToolStripTextBox toolStripTextBox1;
		private System.Windows.Forms.ToolStripButton toolStripButtonConnect;
		private System.Windows.Forms.ToolStrip toolStrip1;
		private ImgViewer imgViewer1;
	}
}

