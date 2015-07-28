// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIL.Utils.FileDialog;

namespace FDOBrowser
{
	/// <summary>
	/// see other half of class
	/// </summary>
	public partial class FileInOutChooser : Form
	{
		// expose these to hold returns
		private OpenFileDialogAdapter Db4oFile;
		// expose these to hold returns
		private SaveFileDialogAdapter XmlFile;

		/// <summary>
		/// Class to choose input, output files for Db4o to XML conversion
		/// </summary>
		public FileInOutChooser()
		{
			InitializeComponent();

			Db4oFile = new OpenFileDialogAdapter();
			Db4oFile.Filter = "Db4o Files|*.fwdb|All Files|*.*";

			XmlFile = new SaveFileDialogAdapter();
			XmlFile.DefaultExt = "fwxml";
		}

		private bool Done { get; set; }

		private void chooseDb4o_Click(object sender, EventArgs e)
		{
			Db4oFile.ShowDialog();
			db4o.Text = Db4oFile.FileName;
			if (Db4oFile.FileName != "")
			{
				string path = System.IO.Path.ChangeExtension(Db4oFile.FileName, ".fwdata");

				XmlFile.FileName = path;
				xml.Text = path;
			}
		}

		private void chooseXML_Click(object sender, EventArgs e)
		{
			XmlFile.ShowDialog();
			XmlFile.FileName = xml.Text;
		}

		private void done_Click(object sender, EventArgs e)
		{
			if (Db4oFile.FileName != "" && XmlFile.FileName != "")
			{
				statusLabel.Text = "Processing...";
				this.ResumeLayout();

				// Here's the real work
				var converter = new Db4oToXmlConverter();
				converter.db4o2xml(Db4oFile.FileName, xml.Text,compressed.Checked);

				Done = true;
				statusLabel.Text = "Done";
				this.Hide();
			}
			else
				MessageBox.Show("Must choose Db4o source and Xml target");
		}

		private void cancel_Click(object sender, EventArgs e)
		{
			this.Hide();
		}
	}
}
