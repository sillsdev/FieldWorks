using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;

using Sfm2Xml;		// for converter
using System.Xml;
using System.Xml.Xsl;
using System.Xml.XPath;

using SIL.Utils;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Summary description for ImportLexicon.
	/// </summary>
	public class ImportLexiconDlg : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.TextBox tbMapFileName;
		private System.Windows.Forms.Label MapFileNameLabel;
		private System.Windows.Forms.Button btnMapFileNameChooser;
		private System.Windows.Forms.OpenFileDialog openFileDialog;
		private System.Windows.Forms.Button btnDictFileNameChooser;
		private System.Windows.Forms.Label DictFileNameLabel;
		private System.Windows.Forms.TextBox tbDictFileName;
		private System.Windows.Forms.Button btnCheck;
		private System.Windows.Forms.Button btnImport;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		// local variables for maintaining the state of info
		private string m_CheckedMapFileName = "";
		private string m_CheckedDictFileName = "";

		private string m_Phase2XSLT;
		private string m_Phase3XSLT;
		private string m_Phase4XSLT;
		private string m_BuildPhase2XSLT;

		private string m_Phase1Output;
		private string m_Phase2Output;
		private string m_Phase3Output;
		private string m_Phase4Output;

		public ImportLexiconDlg(string mapFileName, string dictFileName)
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			tbMapFileName.Text = mapFileName;
			tbDictFileName.Text = dictFileName;

			string tempDir = @"C:\fw\Src\Utilities\SfmToXml\TestData\";
			string workingDir = @"C:\fw\Src\Utilities\SfmToXml\TestData\";

			// Output files
			m_Phase1Output = tempDir + @"Phase1Output.xml";
			m_Phase2Output = tempDir + @"Phase2Output.xml";
			m_Phase3Output = tempDir + @"Phase3Output.xml";
			m_Phase4Output = tempDir + @"Phase4Output.xml";

			// XSLT files
			m_Phase2XSLT = workingDir + "Phase2.xsl";
			m_Phase3XSLT = workingDir + "Phase3.xsl";
			m_Phase4XSLT = workingDir + "Phase4.xsl";
			m_BuildPhase2XSLT = workingDir + "BuildPhase2XSLT.xsl";

			// can't import with out checking the file first
			btnImport.Enabled = false;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if(components != null)
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
			this.tbMapFileName = new System.Windows.Forms.TextBox();
			this.MapFileNameLabel = new System.Windows.Forms.Label();
			this.btnMapFileNameChooser = new System.Windows.Forms.Button();
			this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnDictFileNameChooser = new System.Windows.Forms.Button();
			this.DictFileNameLabel = new System.Windows.Forms.Label();
			this.tbDictFileName = new System.Windows.Forms.TextBox();
			this.btnCheck = new System.Windows.Forms.Button();
			this.btnImport = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// tbMapFileName
			//
			this.tbMapFileName.Location = new System.Drawing.Point(72, 48);
			this.tbMapFileName.Name = "tbMapFileName";
			this.tbMapFileName.Size = new System.Drawing.Size(384, 20);
			this.tbMapFileName.TabIndex = 0;
			this.tbMapFileName.Text = "";
			this.tbMapFileName.TextChanged += new System.EventHandler(this.tbMapFileName_TextChanged);
			//
			// MapFileNameLabel
			//
			this.MapFileNameLabel.Location = new System.Drawing.Point(16, 48);
			this.MapFileNameLabel.Name = "MapFileNameLabel";
			this.MapFileNameLabel.Size = new System.Drawing.Size(56, 16);
			this.MapFileNameLabel.TabIndex = 1;
			this.MapFileNameLabel.Text = "Map file:";
			//
			// btnMapFileNameChooser
			//
			this.btnMapFileNameChooser.Location = new System.Drawing.Point(464, 48);
			this.btnMapFileNameChooser.Name = "btnMapFileNameChooser";
			this.btnMapFileNameChooser.Size = new System.Drawing.Size(26, 23);
			this.btnMapFileNameChooser.TabIndex = 2;
			this.btnMapFileNameChooser.Text = "...";
			this.btnMapFileNameChooser.Click += new System.EventHandler(this.btnMapFileNameChooser_Click);
			//
			// btnCancel
			//
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(392, 104);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(96, 23);
			this.btnCancel.TabIndex = 4;
			this.btnCancel.Text = "Cancel";
			//
			// btnDictFileNameChooser
			//
			this.btnDictFileNameChooser.Location = new System.Drawing.Point(464, 16);
			this.btnDictFileNameChooser.Name = "btnDictFileNameChooser";
			this.btnDictFileNameChooser.Size = new System.Drawing.Size(26, 23);
			this.btnDictFileNameChooser.TabIndex = 7;
			this.btnDictFileNameChooser.Text = "...";
			this.btnDictFileNameChooser.Click += new System.EventHandler(this.btnDictFileNameChooser_Click);
			//
			// DictFileNameLabel
			//
			this.DictFileNameLabel.Location = new System.Drawing.Point(16, 16);
			this.DictFileNameLabel.Name = "DictFileNameLabel";
			this.DictFileNameLabel.Size = new System.Drawing.Size(56, 16);
			this.DictFileNameLabel.TabIndex = 6;
			this.DictFileNameLabel.Text = "Dict file:";
			//
			// tbDictFileName
			//
			this.tbDictFileName.Location = new System.Drawing.Point(72, 16);
			this.tbDictFileName.Name = "tbDictFileName";
			this.tbDictFileName.Size = new System.Drawing.Size(384, 20);
			this.tbDictFileName.TabIndex = 5;
			this.tbDictFileName.Text = "";
			this.tbDictFileName.TextChanged += new System.EventHandler(this.tbDictFileName_TextChanged);
			//
			// btnCheck
			//
			this.btnCheck.Location = new System.Drawing.Point(72, 104);
			this.btnCheck.Name = "btnCheck";
			this.btnCheck.Size = new System.Drawing.Size(136, 23);
			this.btnCheck.TabIndex = 8;
			this.btnCheck.Text = "&Check Dictionary File";
			this.btnCheck.Click += new System.EventHandler(this.btnCheck_Click);
			//
			// btnImport
			//
			this.btnImport.Location = new System.Drawing.Point(216, 104);
			this.btnImport.Name = "btnImport";
			this.btnImport.Size = new System.Drawing.Size(96, 23);
			this.btnImport.TabIndex = 9;
			this.btnImport.Text = "&Import";
			this.btnImport.Click += new System.EventHandler(this.btnImport_Click);
			//
			// ImportLexiconDlg
			//
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(504, 149);
			this.Controls.Add(this.btnImport);
			this.Controls.Add(this.btnCheck);
			this.Controls.Add(this.btnDictFileNameChooser);
			this.Controls.Add(this.DictFileNameLabel);
			this.Controls.Add(this.tbDictFileName);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnMapFileNameChooser);
			this.Controls.Add(this.MapFileNameLabel);
			this.Controls.Add(this.tbMapFileName);
			this.Name = "ImportLexiconDlg";
			this.Text = "(Beta) Import Lexicon";
			this.ResumeLayout(false);

		}
		#endregion

		private void UpdateBtns()
		{
			if (System.IO.File.Exists(tbDictFileName.Text) &&
				System.IO.File.Exists(tbMapFileName.Text))
			{
				btnCheck.Enabled = true;
				if (tbDictFileName.Text == m_CheckedDictFileName &&
					tbMapFileName.Text == m_CheckedMapFileName)
				{
					btnImport.Enabled = true;
				}
			}
			else
			{
				btnImport.Enabled = false;
				btnCheck.Enabled = false;
			}

		}

		private void btnMapFileNameChooser_Click(object sender, System.EventArgs e)
		{
			openFileDialog.Filter = FileUtils.FileDialogFilterCaseInsensitiveCombinations("Mapping file|*.xml");
			openFileDialog.Multiselect = false;
			DialogResult res = openFileDialog.ShowDialog();
			if (res == DialogResult.OK)
				tbMapFileName.Text = openFileDialog.FileName;
		}

		private void btnDictFileNameChooser_Click(object sender, System.EventArgs e)
		{
			openFileDialog.Filter = FileUtils.FileDialogFilterCaseInsensitiveCombinations("Dictionary file|*.mdf");
			openFileDialog.Multiselect = false;
			DialogResult res = openFileDialog.ShowDialog();
			if (res == DialogResult.OK)
				tbDictFileName.Text = openFileDialog.FileName;
		}

		private void tbMapFileName_TextChanged(object sender, System.EventArgs e)
		{
			UpdateBtns();
		}

		private void tbDictFileName_TextChanged(object sender, System.EventArgs e)
		{
			UpdateBtns();
		}

		private void btnCheck_Click(object sender, System.EventArgs e)
		{
			m_CheckedDictFileName = tbDictFileName.Text;
			m_CheckedMapFileName = tbMapFileName.Text;

			try
			{
				Converter conv = new Converter();
				conv.Convert(m_CheckedDictFileName, m_CheckedMapFileName, m_Phase1Output);
				ProcessPhase1Errors(true);
				btnImport.Enabled = true;
			}
			catch
			{
			}
		}

		private void ProcessPhase1Errors(bool show)
		{
			System.Xml.XmlDocument xmlMap = new System.Xml.XmlDocument();
			try
			{
				xmlMap.Load(m_Phase1Output);
			}
			catch //(System.Xml.XmlException e)
			{
				MessageBox.Show("Unable to load the Phase 1 output file.", "Phase 1 Error");
			}

			int errorCount = 0;
			String errMsg = "";

			System.Xml.XmlNode errorsNode = xmlMap.SelectSingleNode("database/ErrorLog/Errors");
			if (errorsNode != null)
			{
				foreach(System.Xml.XmlAttribute Attribute in errorsNode.Attributes)
				{
					switch (Attribute.Name)
					{
						case "count":
							errorCount = Convert.ToInt32(Attribute.Value);
							break;
					}
				}
				if (errorCount > 0)
				{
					errMsg += errorCount.ToString() + " Error(s):" + System.Environment.NewLine;
					System.Xml.XmlNodeList errorList = errorsNode.SelectNodes("Error");
					foreach (System.Xml.XmlNode errorNode in errorList)
					{
						errMsg += "   - " + errorNode.InnerText + System.Environment.NewLine;
					}
				}
			}

			int warningCount = 0;
			System.Xml.XmlNode warningsNode = xmlMap.SelectSingleNode("database/ErrorLog/Warnings");
			if (warningsNode != null)
			{
				foreach(System.Xml.XmlAttribute Attribute in warningsNode.Attributes)
				{
					switch (Attribute.Name)
					{
						case "count":
							warningCount = Convert.ToInt32(Attribute.Value);
							break;
					}
				}
				if (warningCount > 0)
				{
					errMsg += warningCount.ToString() + " Warning";
					if (warningCount > 1)
						errMsg += "s";
					errMsg += ":" + System.Environment.NewLine;
					System.Xml.XmlNodeList warningList = warningsNode.SelectNodes("Warning");
					foreach (System.Xml.XmlNode warningNode in warningList)
					{
						errMsg += "   - " + warningNode.InnerText + System.Environment.NewLine;
					}
				}
			}

			// set the result button color
			if (errorCount > 0)
				btnCheck.ForeColor = System.Drawing.Color.Red;
			else if (warningCount > 0)
				btnCheck.ForeColor = System.Drawing.Color.Yellow;
			else
				btnCheck.ForeColor = System.Drawing.Color.Green;

			// show the error and warning message if present
			if (show && (warningCount + errorCount > 0))
				MessageBox.Show(errMsg, "Phase 1 ErrorLog");
		}


		private void btnImport_Click(object sender, System.EventArgs e)
		{
			DoTransform(m_BuildPhase2XSLT, m_Phase1Output, m_Phase2XSLT);
			DoTransform(m_Phase2XSLT, m_Phase1Output, m_Phase2Output);
			DoTransform(m_Phase3XSLT, m_Phase2Output, m_Phase3Output);
			DoTransform(m_Phase4XSLT, m_Phase3Output, m_Phase4Output);
		}

		private void DoTransform(string xsl, string xml, string output)
		{
			//Create the XslTransform and load the stylesheet.
			XslTransform xslt = new XslTransform();
			xslt.Load(xsl);


			//Load the XML data file.
			XPathDocument doc = new XPathDocument(xml);

			//Create an XmlTextWriter to output to the console.
			XmlTextWriter writer = new XmlTextWriter(output, System.Text.Encoding.UTF8);
			writer.Formatting = Formatting.Indented;

			//Transform the file.
			xslt.Transform(doc, null, writer, null);
			writer.Close();
		}

	}
}
