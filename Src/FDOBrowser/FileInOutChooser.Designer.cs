namespace FDOBrowser
{
	partial class FileInOutChooser
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
			if (disposing && !IsDisposed)
			{
				if (components != null)
					components.Dispose();
				Db4oFile.Dispose();
				XmlFile.Dispose();
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
			this.chooseDb4o = new System.Windows.Forms.Button();
			this.chooseXML = new System.Windows.Forms.Button();
			this.db4o = new System.Windows.Forms.TextBox();
			this.xml = new System.Windows.Forms.TextBox();
			this.done = new System.Windows.Forms.Button();
			this.cancel = new System.Windows.Forms.Button();
			this.Db4oFile = new System.Windows.Forms.OpenFileDialog();
			this.XmlFile = new System.Windows.Forms.SaveFileDialog();
			this.status = new System.Windows.Forms.StatusStrip();
			this.statusLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.compressed = new System.Windows.Forms.CheckBox();
			this.status.SuspendLayout();
			this.SuspendLayout();
			//
			// chooseDb4o
			//
			this.chooseDb4o.Location = new System.Drawing.Point(2, 7);
			this.chooseDb4o.Name = "chooseDb4o";
			this.chooseDb4o.Size = new System.Drawing.Size(122, 23);
			this.chooseDb4o.TabIndex = 0;
			this.chooseDb4o.Text = "Choose Db4o File";
			this.chooseDb4o.UseVisualStyleBackColor = true;
			this.chooseDb4o.Click += new System.EventHandler(this.chooseDb4o_Click);
			//
			// chooseXML
			//
			this.chooseXML.Location = new System.Drawing.Point(2, 45);
			this.chooseXML.Name = "chooseXML";
			this.chooseXML.Size = new System.Drawing.Size(122, 23);
			this.chooseXML.TabIndex = 1;
			this.chooseXML.Text = "Choose XML File";
			this.chooseXML.UseVisualStyleBackColor = true;
			this.chooseXML.Click += new System.EventHandler(this.chooseXML_Click);
			//
			// db4o
			//
			this.db4o.Location = new System.Drawing.Point(139, 11);
			this.db4o.Name = "db4o";
			this.db4o.Size = new System.Drawing.Size(399, 20);
			this.db4o.TabIndex = 3;
			//
			// xml
			//
			this.xml.Location = new System.Drawing.Point(139, 45);
			this.xml.Name = "xml";
			this.xml.Size = new System.Drawing.Size(399, 20);
			this.xml.TabIndex = 4;
			//
			// done
			//
			this.done.Location = new System.Drawing.Point(304, 71);
			this.done.Name = "done";
			this.done.Size = new System.Drawing.Size(98, 26);
			this.done.TabIndex = 5;
			this.done.Text = "Extract";
			this.done.UseVisualStyleBackColor = true;
			this.done.Click += new System.EventHandler(this.done_Click);
			//
			// cancel
			//
			this.cancel.Location = new System.Drawing.Point(417, 71);
			this.cancel.Name = "cancel";
			this.cancel.Size = new System.Drawing.Size(113, 26);
			this.cancel.TabIndex = 6;
			this.cancel.Text = "Cancel";
			this.cancel.UseVisualStyleBackColor = true;
			this.cancel.Click += new System.EventHandler(this.cancel_Click);
			//
			// Db4oFile
			//
			this.Db4oFile.Filter = "Db4o Files|*.fwdb|All Files|*.*";
			//
			// XmlFile
			//
			this.XmlFile.DefaultExt = "fwxml";
			//
			// status
			//
			this.status.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.statusLabel});
			this.status.Location = new System.Drawing.Point(0, 100);
			this.status.Name = "status";
			this.status.Size = new System.Drawing.Size(542, 22);
			this.status.TabIndex = 7;
			this.status.Text = "Choose input and output file names";
			//
			// statusLabel
			//
			this.statusLabel.Name = "statusLabel";
			this.statusLabel.Size = new System.Drawing.Size(197, 17);
			this.statusLabel.Text = "Choose input and output file names";
			//
			// compressed
			//
			this.compressed.AutoSize = true;
			this.compressed.Location = new System.Drawing.Point(171, 77);
			this.compressed.Name = "compressed";
			this.compressed.Size = new System.Drawing.Size(90, 17);
			this.compressed.TabIndex = 8;
			this.compressed.Text = "Compressed?";
			this.compressed.UseVisualStyleBackColor = true;
			//
			// FileInOutChooser
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(542, 122);
			this.Controls.Add(this.compressed);
			this.Controls.Add(this.status);
			this.Controls.Add(this.cancel);
			this.Controls.Add(this.done);
			this.Controls.Add(this.xml);
			this.Controls.Add(this.db4o);
			this.Controls.Add(this.chooseXML);
			this.Controls.Add(this.chooseDb4o);
			this.Name = "FileInOutChooser";
			this.Text = "Extract db4o to xml";
			this.status.ResumeLayout(false);
			this.status.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button chooseDb4o;
		private System.Windows.Forms.Button chooseXML;
		private System.Windows.Forms.TextBox db4o;
		private System.Windows.Forms.TextBox xml;
		private System.Windows.Forms.Button done;
		private System.Windows.Forms.Button cancel;

		// expose these to hold returns
		private System.Windows.Forms.OpenFileDialog Db4oFile;
		// expose these to hold returns
		private System.Windows.Forms.SaveFileDialog XmlFile;
		private System.Windows.Forms.StatusStrip status;
		private System.Windows.Forms.ToolStripStatusLabel statusLabel;
		private System.Windows.Forms.CheckBox compressed;

	}
}