using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;

namespace GenGuiModel
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class Form1 : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.OpenFileDialog openFileDialog1;
		private System.Windows.Forms.OpenFileDialog openFileDialog2;
		private System.Windows.Forms.TextBox textBox1;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.TextBox textBox2;
		private System.Windows.Forms.Button button2;
		private System.Windows.Forms.Button button3;
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
			label1.AccessibleName = "Title";
			textBox1.AccessibleName = "SourceTbx";
			textBox2.AccessibleName = "TargetTbx";
			button1.AccessibleName = "BrowseSource";
			button2.AccessibleName = "BrowseTarget";
			button3.AccessibleName = "Convert";
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
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(Form1));
			this.label1 = new System.Windows.Forms.Label();
			this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.button1 = new System.Windows.Forms.Button();
			this.button2 = new System.Windows.Forms.Button();
			this.textBox2 = new System.Windows.Forms.TextBox();
			this.button3 = new System.Windows.Forms.Button();
			this.openFileDialog2 = new System.Windows.Forms.OpenFileDialog();
			this.SuspendLayout();
			//
			// label1
			//
			this.label1.BackColor = System.Drawing.Color.Transparent;
			this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label1.ForeColor = System.Drawing.Color.LightSalmon;
			this.label1.Location = new System.Drawing.Point(16, 16);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(264, 32);
			this.label1.TabIndex = 0;
			this.label1.Text = "GUI Model Generator";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			//
			// openFileDialog1
			//
			this.openFileDialog1.DefaultExt = "txt";
			this.openFileDialog1.Title = "Select AccExplorer Text File";
			//
			// textBox1
			//
			this.textBox1.BackColor = System.Drawing.Color.LightSeaGreen;
			this.textBox1.ForeColor = System.Drawing.Color.Bisque;
			this.textBox1.Location = new System.Drawing.Point(48, 88);
			this.textBox1.Name = "textBox1";
			this.textBox1.Size = new System.Drawing.Size(176, 20);
			this.textBox1.TabIndex = 1;
			this.textBox1.Text = "AccExploere Text File";
			//
			// button1
			//
			this.button1.BackColor = System.Drawing.Color.Transparent;
			this.button1.ForeColor = System.Drawing.Color.LightSalmon;
			this.button1.Location = new System.Drawing.Point(232, 88);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(56, 24);
			this.button1.TabIndex = 2;
			this.button1.Text = "Browse";
			this.button1.Click += new System.EventHandler(this.button1_Click);
			//
			// button2
			//
			this.button2.BackColor = System.Drawing.Color.Transparent;
			this.button2.ForeColor = System.Drawing.Color.LightSalmon;
			this.button2.Location = new System.Drawing.Point(232, 152);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(56, 24);
			this.button2.TabIndex = 4;
			this.button2.Text = "Browse";
			this.button2.Click += new System.EventHandler(this.button2_Click);
			//
			// textBox2
			//
			this.textBox2.BackColor = System.Drawing.Color.LightSeaGreen;
			this.textBox2.ForeColor = System.Drawing.Color.Bisque;
			this.textBox2.Location = new System.Drawing.Point(48, 152);
			this.textBox2.Name = "textBox2";
			this.textBox2.Size = new System.Drawing.Size(176, 20);
			this.textBox2.TabIndex = 3;
			this.textBox2.Text = "Model XML file";
			//
			// button3
			//
			this.button3.BackColor = System.Drawing.Color.Transparent;
			this.button3.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.button3.ForeColor = System.Drawing.Color.GreenYellow;
			this.button3.Location = new System.Drawing.Point(168, 208);
			this.button3.Name = "button3";
			this.button3.Size = new System.Drawing.Size(104, 24);
			this.button3.TabIndex = 5;
			this.button3.Text = "Convert";
			this.button3.Click += new System.EventHandler(this.button3_Click);
			//
			// Form1
			//
			this.AccessibleDescription = "Gui model generator window";
			this.AccessibleName = "gmgMainWindow";
			this.AccessibleRole = System.Windows.Forms.AccessibleRole.Window;
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
			this.ClientSize = new System.Drawing.Size(292, 266);
			this.Controls.Add(this.button3);
			this.Controls.Add(this.button2);
			this.Controls.Add(this.textBox2);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.textBox1);
			this.Controls.Add(this.label1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "Form1";
			this.Text = "GUI Model Generator";
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

		private void button1_Click(object sender, System.EventArgs e)
		{
			openFileDialog1.DefaultExt = "txt";
			openFileDialog1.AddExtension = true;
			openFileDialog1.ShowDialog(this);
			textBox1.Text = openFileDialog1.FileName;
		}

		private void button2_Click(object sender, System.EventArgs e)
		{
			openFileDialog2.DefaultExt = "xml";
			openFileDialog2.AddExtension = true;
			openFileDialog2.ShowDialog(this);
			textBox2.Text = openFileDialog2.FileName;
		}

		private void button3_Click(object sender, System.EventArgs e)
		{
			string source = textBox1.Text;
			string target = textBox2.Text;
			Converter conv = new Converter(source, target);
			if (conv.go()) System.Windows.Forms.MessageBox.Show(this,"Done.");
			else           System.Windows.Forms.MessageBox.Show(this,"Failed.");
		}

		private void Form1_Load(object sender, System.EventArgs e)
		{

		}
	}
}
