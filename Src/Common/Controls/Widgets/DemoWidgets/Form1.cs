using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using SIL.FieldWorks.Resources;

namespace DemoWidgets
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class Form1 : System.Windows.Forms.Form
	{
		private System.Windows.Forms.TextBox textBox1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private SIL.FieldWorks.Common.Widgets.FwTextBox fwTextBox1;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.ListBox listBox1;
		private System.Windows.Forms.Label label4;
		private SIL.FieldWorks.Common.Widgets.FwListBox fwListBox2;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.ComboBox comboBox1;
		private System.Windows.Forms.Label label6;
		private SIL.FieldWorks.Common.Widgets.FwComboBox fwComboBox1;
		private Image m_PullDownArrow;
		private SIL.FieldWorks.Common.Widgets.ComboListBox m_comboListBox;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public Form1()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.listBox1 = new System.Windows.Forms.ListBox();
			this.label4 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.comboBox1 = new System.Windows.Forms.ComboBox();
			this.label6 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			//
			// textBox1
			//
			this.textBox1.Location = new System.Drawing.Point(147, 14);
			this.textBox1.Name = "textBox1";
			this.textBox1.Size = new System.Drawing.Size(83, 20);
			this.textBox1.TabIndex = 0;
			this.textBox1.Text = "textBox1";
			//
			// label1
			//
			this.label1.Location = new System.Drawing.Point(7, 14);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(120, 20);
			this.label1.TabIndex = 1;
			this.label1.Text = "Regular TextBox";
			//
			// label2
			//
			this.label2.Location = new System.Drawing.Point(7, 42);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(120, 20);
			this.label2.TabIndex = 2;
			this.label2.Text = "FieldWorks TextBox";
			//
			// label3
			//
			this.label3.Location = new System.Drawing.Point(7, 76);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(83, 20);
			this.label3.TabIndex = 3;
			this.label3.Text = "&Regular listbox";
			//
			// listBox1
			//
			this.listBox1.Items.AddRange(new object[] {
														  "This",
														  "is",
														  "a",
														  "list",
														  "with",
														  "several",
														  "items"});
			this.listBox1.Location = new System.Drawing.Point(140, 76);
			this.listBox1.Name = "listBox1";
			this.listBox1.Size = new System.Drawing.Size(100, 69);
			this.listBox1.TabIndex = 4;
			//
			// label4
			//
			this.label4.Location = new System.Drawing.Point(13, 159);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(107, 20);
			this.label4.TabIndex = 5;
			this.label4.Text = "FieldWorks ListBox";
			//
			// label5
			//
			this.label5.Location = new System.Drawing.Point(13, 277);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(84, 20);
			this.label5.TabIndex = 6;
			this.label5.Text = "Regular combo";
			//
			// comboBox1
			//
			this.comboBox1.Items.AddRange(new object[] {
														   "Here",
														   "are",
														   "some",
														   "combo",
														   "items",
														   "we",
														   "can",
														   "use",
														   "to",
														   "test"});
			this.comboBox1.Location = new System.Drawing.Point(152, 272);
			this.comboBox1.Name = "comboBox1";
			this.comboBox1.Size = new System.Drawing.Size(121, 21);
			this.comboBox1.TabIndex = 7;
			this.comboBox1.Text = "default";
			//
			// label6
			//
			this.label6.Location = new System.Drawing.Point(13, 319);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(100, 20);
			this.label6.TabIndex = 8;
			this.label6.Text = "FieldWorks combo";
			//
			// Form1
			//
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(486, 390);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.comboBox1);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.listBox1);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.textBox1);
			this.Name = "Form1";
			this.Text = "Form1";
			this.Load += new System.EventHandler(this.Form1_Load);
			this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.Run(new Form1());
		}

		private void Form1_Load(object sender, System.EventArgs e)
		{
			fwTextBox1 = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			fwTextBox1.Name = "FwTextBox1";
			fwTextBox1.Location = new System.Drawing.Point(168, 48);
			fwTextBox1.TabStop = true;
			this.Controls.Add(fwTextBox1);
			//
			// fwListBox2
			//
			fwListBox2 = new SIL.FieldWorks.Common.Widgets.FwListBox();
			//this.fwListBox2.ItemHeight = 16;
			this.fwListBox2.Items.AddRange(new object[] {
														  "This",
														  "is",
														  "a",
														  "list",
														  "with",
														  "several",
														  "items"});
			fwListBox2.Location = new System.Drawing.Point(168, 188);
			fwListBox2.Name = "fwListBox2";
			fwListBox2.Size = new System.Drawing.Size(120, 84);
			fwListBox2.TabIndex = 5;
			fwListBox2.TabStop = true;
			this.Controls.Add(fwListBox2);
			//
			// comboBox1
			//
			this.fwComboBox1 = new SIL.FieldWorks.Common.Widgets.FwComboBox();
			this.fwComboBox1.Items.AddRange(new object[] {
														   "Here",
														   "are",
														   "some",
														   "combo",
														   "items",
														   "we",
														   "can",
														   "use",
														   "to",
														   "test"});
			fwComboBox1.Location = new System.Drawing.Point(152, 300);
			fwComboBox1.Name = "fwComboBox1";
			fwComboBox1.Size = new System.Drawing.Size(121, 21);
			fwComboBox1.TabIndex = 8;
			fwComboBox1.Text = "default";
			fwComboBox1.TabStop = true;
			Controls.Add(fwComboBox1);

			// Image with associated list.
			this.m_PullDownArrow = ResourceHelper.InterlinPopupArrow;

			m_comboListBox = new SIL.FieldWorks.Common.Widgets.ComboListBox();
			m_comboListBox.Items.AddRange(new object[] {
														   "Here",
														   "are",
														   "some",
														   "combo",
														   "items",
														   "for",
														   "the",
														   "yellow",
														   "button"});
			m_comboListBox.Size = new System.Drawing.Size(121, 150);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint (e);
			e.Graphics.DrawImage(m_PullDownArrow, 152, 330);
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown (e);
			Rectangle arrowRect = new Rectangle(152, 330, 13,13);
			if (arrowRect.Contains(e.X, e.Y))
			{
				m_comboListBox.Launch(RectangleToScreen(arrowRect), Screen.GetWorkingArea(this));
			}
		}


	}
}
