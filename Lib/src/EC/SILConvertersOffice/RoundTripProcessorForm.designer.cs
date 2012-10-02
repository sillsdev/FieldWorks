namespace SILConvertersOffice
{
	partial class RoundTripProcessorForm
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
			this.labelRoundTrip = new System.Windows.Forms.Label();
			this.textBoxRoundTrip = new SilEncConverters31.EcTextBox();
			this.labelRoundTripCodePoints = new System.Windows.Forms.Label();
			this.SuspendLayout();
			//
			// labelRoundTrip
			//
			this.labelRoundTrip.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.labelRoundTrip.AutoSize = true;
			this.labelRoundTrip.Location = new System.Drawing.Point(19, 99);
			this.labelRoundTrip.Name = "labelRoundTrip";
			this.labelRoundTrip.Size = new System.Drawing.Size(59, 13);
			this.labelRoundTrip.TabIndex = 2;
			this.labelRoundTrip.Text = "&Round-trip:";
			//
			// textBoxRoundTrip
			//
			this.textBoxRoundTrip.ContextMenuStrip = this.contextMenuStrip;
			this.textBoxRoundTrip.Font = new System.Drawing.Font("Arial Unicode MS", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.textBoxRoundTrip.Location = new System.Drawing.Point(97, 102);
			this.textBoxRoundTrip.Multiline = true;
			this.textBoxRoundTrip.Name = "textBoxRoundTrip";
			this.textBoxRoundTrip.Size = new System.Drawing.Size(178, 20);
			this.textBoxRoundTrip.TabIndex = 6;
			this.toolTip.SetToolTip(this.textBoxRoundTrip, "This box shows the result of the round-trip conversion");
			this.textBoxRoundTrip.TextChanged += new System.EventHandler(this.textBoxRoundTrip_TextChanged);
			//
			// labelRoundTripCodePoints
			//
			this.labelRoundTripCodePoints.AutoSize = true;
			this.labelRoundTripCodePoints.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.labelRoundTripCodePoints.Location = new System.Drawing.Point(281, 102);
			this.labelRoundTripCodePoints.Margin = new System.Windows.Forms.Padding(3);
			this.labelRoundTripCodePoints.Name = "labelRoundTripCodePoints";
			this.labelRoundTripCodePoints.Padding = new System.Windows.Forms.Padding(3);
			this.labelRoundTripCodePoints.Size = new System.Drawing.Size(8, 21);
			this.labelRoundTripCodePoints.TabIndex = 9;
			//
			// RoundTripProcessorForm
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.ClientSize = new System.Drawing.Size(648, 182);
			this.Controls.Add(this.labelRoundTripCodePoints);
			this.Controls.Add(this.labelRoundTrip);
			this.Controls.Add(this.textBoxRoundTrip);
			this.Name = "RoundTripProcessorForm";
			this.Text = "Round-trip Checking";
			this.Controls.SetChildIndex(this.textBoxRoundTrip, 0);
			this.Controls.SetChildIndex(this.labelRoundTrip, 0);
			this.Controls.SetChildIndex(this.labelRoundTripCodePoints, 0);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label labelRoundTrip;
		private SilEncConverters31.EcTextBox textBoxRoundTrip;
		private System.Windows.Forms.Label labelRoundTripCodePoints;
	}
}
