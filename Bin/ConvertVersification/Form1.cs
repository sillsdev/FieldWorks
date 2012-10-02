// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: Form1.cs
// Responsibility: MichaelL
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.IO;
using System.Xml;

namespace VrsToXml
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class Form1 : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox textVrsFile;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.TextBox textXmlFile;
		private System.Windows.Forms.Button buttonBrowseVrs;
		private System.Windows.Forms.Button buttonBrowseXml;
		private System.Windows.Forms.Button buttonConvert;
		private System.Windows.Forms.Button buttonExit;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.TextBox textVersificationCode;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.TextBox textDescription;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="Form1"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// ------------------------------------------------------------------------------------
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
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.textVrsFile = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.textXmlFile = new System.Windows.Forms.TextBox();
			this.buttonBrowseVrs = new System.Windows.Forms.Button();
			this.buttonBrowseXml = new System.Windows.Forms.Button();
			this.buttonConvert = new System.Windows.Forms.Button();
			this.buttonExit = new System.Windows.Forms.Button();
			this.label4 = new System.Windows.Forms.Label();
			this.textVersificationCode = new System.Windows.Forms.TextBox();
			this.label5 = new System.Windows.Forms.Label();
			this.textDescription = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			//
			// label1
			//
			this.label1.Location = new System.Drawing.Point(16, 16);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(416, 32);
			this.label1.TabIndex = 0;
			this.label1.Text = "Convert a .vrs Paratext-style versification file to a Translation Editor .xml ver" +
				"sification file.";
			//
			// label2
			//
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(16, 56);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(82, 16);
			this.label2.TabIndex = 1;
			this.label2.Text = "Source .vrs file:";
			//
			// textVrsFile
			//
			this.textVrsFile.Location = new System.Drawing.Point(16, 72);
			this.textVrsFile.Name = "textVrsFile";
			this.textVrsFile.Size = new System.Drawing.Size(336, 20);
			this.textVrsFile.TabIndex = 2;
			this.textVrsFile.Text = "";
			//
			// label3
			//
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(16, 104);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(106, 16);
			this.label3.TabIndex = 4;
			this.label3.Text = "Destination .xml file:";
			//
			// textXmlFile
			//
			this.textXmlFile.Location = new System.Drawing.Point(16, 120);
			this.textXmlFile.Name = "textXmlFile";
			this.textXmlFile.Size = new System.Drawing.Size(336, 20);
			this.textXmlFile.TabIndex = 5;
			this.textXmlFile.Text = "";
			//
			// buttonBrowseVrs
			//
			this.buttonBrowseVrs.Location = new System.Drawing.Point(368, 72);
			this.buttonBrowseVrs.Name = "buttonBrowseVrs";
			this.buttonBrowseVrs.TabIndex = 3;
			this.buttonBrowseVrs.Text = "Browse...";
			this.buttonBrowseVrs.Click += new System.EventHandler(this.buttonBrowseVrs_Click);
			//
			// buttonBrowseXml
			//
			this.buttonBrowseXml.Location = new System.Drawing.Point(368, 120);
			this.buttonBrowseXml.Name = "buttonBrowseXml";
			this.buttonBrowseXml.TabIndex = 6;
			this.buttonBrowseXml.Text = "Browse...";
			this.buttonBrowseXml.Click += new System.EventHandler(this.buttonBrowseXml_Click);
			//
			// buttonConvert
			//
			this.buttonConvert.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.buttonConvert.Location = new System.Drawing.Point(141, 232);
			this.buttonConvert.Name = "buttonConvert";
			this.buttonConvert.TabIndex = 11;
			this.buttonConvert.Text = "Convert";
			this.buttonConvert.Click += new System.EventHandler(this.buttonConvert_Click);
			//
			// buttonExit
			//
			this.buttonExit.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.buttonExit.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonExit.Location = new System.Drawing.Point(229, 232);
			this.buttonExit.Name = "buttonExit";
			this.buttonExit.TabIndex = 12;
			this.buttonExit.Text = "Exit";
			this.buttonExit.Click += new System.EventHandler(this.buttonExit_Click);
			//
			// label4
			//
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(16, 152);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(148, 16);
			this.label4.TabIndex = 7;
			this.label4.Text = "Versification code (3 letters):";
			//
			// textVersificationCode
			//
			this.textVersificationCode.Location = new System.Drawing.Point(160, 152);
			this.textVersificationCode.MaxLength = 3;
			this.textVersificationCode.Name = "textVersificationCode";
			this.textVersificationCode.Size = new System.Drawing.Size(64, 20);
			this.textVersificationCode.TabIndex = 8;
			this.textVersificationCode.Text = "";
			//
			// label5
			//
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(16, 184);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(127, 16);
			this.label5.TabIndex = 9;
			this.label5.Text = "Versification description:";
			//
			// textDescription
			//
			this.textDescription.Location = new System.Drawing.Point(160, 184);
			this.textDescription.MaxLength = 1000;
			this.textDescription.Name = "textDescription";
			this.textDescription.Size = new System.Drawing.Size(192, 20);
			this.textDescription.TabIndex = 10;
			this.textDescription.Text = "";
			//
			// Form1
			//
			this.AcceptButton = this.buttonConvert;
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.CancelButton = this.buttonExit;
			this.ClientSize = new System.Drawing.Size(458, 264);
			this.Controls.Add(this.textDescription);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.textVersificationCode);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.buttonExit);
			this.Controls.Add(this.buttonConvert);
			this.Controls.Add(this.buttonBrowseXml);
			this.Controls.Add(this.buttonBrowseVrs);
			this.Controls.Add(this.textXmlFile);
			this.Controls.Add(this.textVrsFile);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "Form1";
			this.Text = "VrsToXml Converter";
			this.ResumeLayout(false);

		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[STAThread]
		static void Main()
		{
			Application.Run(new Form1());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a click on the Exit button
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void buttonExit_Click(object sender, System.EventArgs e)
		{
			Application.Exit();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a click on the browse button for .vrs files
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void buttonBrowseVrs_Click(object sender, System.EventArgs e)
		{
			OpenFileDialog dlg = new OpenFileDialog();

			dlg.AddExtension = true;
			dlg.CheckFileExists = true;
			dlg.DefaultExt = ".vrs";
			dlg.Filter = "Paratext versification files (*.vrs)|*.vrs";

			if (dlg.ShowDialog(this) == DialogResult.OK)
				textVrsFile.Text = dlg.FileName;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a click on the browse button for .xml files
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void buttonBrowseXml_Click(object sender, System.EventArgs e)
		{
			SaveFileDialog dlg = new SaveFileDialog();

			dlg.AddExtension = true;
			dlg.CheckPathExists = true;
			dlg.DefaultExt = ".xml";
			dlg.Filter = "TE versification files (*.xml)|*.xml";

			if (dlg.ShowDialog(this) == DialogResult.OK)
				textXmlFile.Text = dlg.FileName;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a versification node to hold all of the versification data
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private XmlNode CreateVersificationNode(XmlDocument dest)
		{
			string versificationCode = textVersificationCode.Text;
			string versificationDesc = textDescription.Text;

			XmlNode versificationNode = dest.CreateNode(XmlNodeType.Element, "versification", null);
			XmlAttribute idAttribute = dest.CreateAttribute("id");
			idAttribute.Value = versificationCode;
			XmlAttribute descAttribute = dest.CreateAttribute("description");
			descAttribute.Value = versificationDesc;
			versificationNode.Attributes.Append(idAttribute);
			versificationNode.Attributes.Append(descAttribute);
			dest.AppendChild(versificationNode);

			return versificationNode;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert one line. This line will start with a 3 letter book code followed by
		/// chapter:verse pairs to indicate the number of verses available in each chapter.
		///       1JN 1:10 2:29 3:24 4:21 5:21
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ConvertOneLine(string line, XmlNode rootNode, XmlDocument dest)
		{
			string bookCode = null;
			XmlNode bookNode = dest.CreateNode(XmlNodeType.Element, "book", null);
			rootNode.AppendChild(bookNode);
			foreach (string token in line.Split(' '))
			{
				// The first token needs to be the book code
				if (bookCode == null)
				{
					// Store the book code. If the book code is not 3 characters long,
					// then there is a problem with this line.
					bookCode = token;
					if (bookCode.Length != 3)
						return;
					XmlAttribute bookId = dest.CreateAttribute("id");
					bookId.Value = bookCode;
					bookNode.Attributes.Append(bookId);
				}
				else
				{
					// Split the chapter:verse pair. If there are not two items
					// then there is a problem with this token.
					string[] pair = token.Split(':');
					if (pair.Length != 2)
						continue;
					XmlNode chapterNode = dest.CreateNode(XmlNodeType.Element, "chapter", null);
					bookNode.AppendChild(chapterNode);

					XmlAttribute chapterId = dest.CreateAttribute("number");
					chapterId.Value = pair[0];
					chapterNode.Attributes.Append(chapterId);

					XmlAttribute minVerse = dest.CreateAttribute("minimum_verse");
					minVerse.Value = "1";
					chapterNode.Attributes.Append(minVerse);

					XmlAttribute maxVerse = dest.CreateAttribute("maximum_verse");
					maxVerse.Value = pair[1];
					chapterNode.Attributes.Append(maxVerse);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Run the conversion
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void RunConvert(StreamReader source, XmlDocument dest)
		{
			// Create a versification node
			XmlNode rootNode = CreateVersificationNode(dest);

			string line;
			while ((line = source.ReadLine()) != null)
			{
				// skip blank lines and comment lines
				if (line.Length == 0 || line[0] == '#')
					continue;

				// If a line contains a '=' character then we have reached the mapping section
				// which we are going to ignore for now.
				if (line.IndexOf('=') >= 0)
					break;

				// Process the line
				ConvertOneLine(line, rootNode, dest);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a click on the Convert button
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void buttonConvert_Click(object sender, System.EventArgs e)
		{
			buttonConvert.Enabled = false;
			buttonExit.Enabled = false;

			string fileNameVrs = textVrsFile.Text;
			string fileNameXml = textXmlFile.Text;

			// Make sure that the source file exists
			if (!File.Exists(fileNameVrs))
			{
				MessageBox.Show(this, "The source file " + fileNameVrs + " does not exist.", this.Text, MessageBoxButtons.OK);
				return;
			}

			// Create an XML document to write the destination data to
			XmlDocument doc = new XmlDocument();
			XmlDeclaration decl = doc.CreateXmlDeclaration("1.0", string.Empty, "yes");
			doc.AppendChild(decl);

			using (StreamReader source = new StreamReader(fileNameVrs))
			{
				RunConvert(source, doc);

				doc.Save(fileNameXml);
			}

			buttonConvert.Enabled = true;
			buttonExit.Enabled = true;
		}
	}
}
