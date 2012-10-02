using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml;

using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.ScriptureUtils;
using OxesIO;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.TE
{
	/// <summary>
	/// Simple dialog for obtaining the OXES file to import.
	/// </summary>
	public partial class ImportXmlDialog : Form, IFWDisposable
	{
		#region Member variables
		private string m_sDescriptionFmt;
		private FdoCache m_cache;
		private TeImportExportFileDialog m_openFileDialog;
		#endregion

		#region Constructor
		/// <summary>
		/// Constructor.
		/// </summary>
		public ImportXmlDialog(FdoCache cache)
		{
			m_cache = cache;
			InitializeComponent();
			m_sDescriptionFmt = m_lblDescription.Text;
			m_lblDescription.Text = "";
		}
		#endregion

		#region Private helper methods
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Check the version number of the OXES file, and if it's out of date, migrate
		/// (a temporary copy) to the current version via XSLT scripts.
		/// </summary>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		private bool EnsureProperOxesFile()
		{
			try
			{
				if (Migrator.IsMigrationNeeded(m_tbFilename.Text))
				{
					string migratedFile = Migrator.MigrateToLatestVersion(m_tbFilename.Text);
					string backupFile = m_tbFilename.Text + ".~migbak";

					try
					{
						File.Delete(backupFile);
					}
					catch { }

					File.Move(m_tbFilename.Text, backupFile);
					File.Move(migratedFile, m_tbFilename.Text);
					File.Move(backupFile, migratedFile);
				}
				return true;
			}
			catch (ApplicationException e)
			{
				DisplayImportError(SUE_ErrorCode.OxesMigrationFailed, e.Message);
				return false;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Displays the "Unable to Import" message box.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void DisplayImportError(SUE_ErrorCode errorCode, string msgDetails)
		{
			string msgTemplate;
			string topic;
			bool includeDetails;
			ScriptureUtilsException.GetErrorMsgAndHelpTopic(errorCode,
				out msgTemplate, out topic, out includeDetails);
			MessageBox.Show(this, string.Format(msgTemplate, msgDetails),
				ScriptureUtilsException.GetResourceString("kstidImportErrorCaption"),
				MessageBoxButtons.OK,
				MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, 0, FwApp.App.HelpFile,
				HelpNavigator.Topic, topic);
		}

		private bool VerifyLanguageInOxesFile()
		{
			string sFilename = m_tbFilename.Text;

			try
			{
				XmlReaderSettings readerSettings = new XmlReaderSettings();
				readerSettings.ValidationType = ValidationType.None;
				readerSettings.IgnoreComments = true;
				// The first element should look something like
				// <oxes xmlns="http://www.wycliffe.net/scripture/namespace/version_1.1.0">
				using (XmlReader reader = XmlReader.Create(sFilename, readerSettings))
				{
					if (!reader.IsStartElement("oxes"))
						throw new Exception();
					string xmlns = reader.GetAttribute("xmlns");
					if (String.IsNullOrEmpty(xmlns) ||
						!xmlns.StartsWith("http://www.wycliffe.net/scripture/namespace/version_"))
					{
						throw new Exception();
					}
					// We appear to have an OXES file.  Find the first <revisionDesc> element.
					reader.ReadStartElement("oxes");
					if (!reader.IsStartElement("oxesText"))
						throw new Exception();
					Dictionary<string, string> attrs = TeXmlImporter.ReadXmlAttributes(reader);
					string sValue;
					if (attrs.TryGetValue("xml:lang", out sValue))
					{
						// Verify that the vernacular language matches the OXES file.
						ILgWritingSystem lgws = LgWritingSystem.CreateFromDBObject(m_cache, m_cache.DefaultVernWs);
						if (sValue != lgws.RFC4646bis)
						{
							// "The project's vernacular language ({0}) does not match the OXES file's data language ({1}).  Should import continue?"
							string sFmt = TeResourceHelper.GetResourceString("kstidImportXmlLangMismatch");
							// "Import Language Mismatch"
							string sCaption = TeResourceHelper.GetResourceString("kstidImportXmlLangMismatchCaption");
							string sMsg = String.Format(sFmt, lgws.RFC4646bis, sValue);
							if (MessageBox.Show(sMsg, sCaption, MessageBoxButtons.YesNo) != DialogResult.Yes)
								return false;
						}
					}
				}
			}
			catch
			{
				// "{0} is not a valid OXES file."
				string sFmt = TeResourceHelper.GetResourceString("kstidImportXmlBadFile");
				string sMsg = String.Format(sFmt, sFilename);
				// "Warning"
				string sCaption = TeResourceHelper.GetResourceString("kstidImportXmlWarning");
				MessageBox.Show(sMsg, sCaption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return false;
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the revision date and description from the specified oxes file.
		/// </summary>
		/// <param name="filename"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private string ReadRevisionDesc(string filename)
		{
			if (!File.Exists(filename))
				return null;

			try
			{
				XmlDocument xmlDoc = new XmlDocument();
				xmlDoc.Load(filename);

				XmlNode node = xmlDoc.DocumentElement;
				while (node != null && node.Name != "revisionDesc")
					node = node.FirstChild;

				if (node == null)
					return null;

				string sDate = null;
				string sFirstPara = null;
				foreach (XmlNode xn in node.ChildNodes)
				{
					if (xn.Name == "date")
					{
						sDate = xn.InnerText;
					}
					else if (xn.Name == "para")
					{
						if (String.IsNullOrEmpty(sFirstPara))
							sFirstPara = xn.InnerText;
					}
					if (!String.IsNullOrEmpty(sDate) && !String.IsNullOrEmpty(sFirstPara))
						break;
				}

				if (sDate != null && sFirstPara != null)
					return string.Format(m_sDescriptionFmt, sDate, sFirstPara);
			}
			catch
			{
				// don't need to display any error - that will be done in validation on Ok.
			}

			return null;
		}

		private void EnableImportButton()
		{
			if (!String.IsNullOrEmpty(m_tbFilename.Text) &&
				!String.IsNullOrEmpty(m_tbFilename.Text.Trim()))
			{
				m_btnImport.Enabled = true;
			}
			else
			{
				m_btnImport.Enabled = false;
			}
		}
		#endregion

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the m_btnImport control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_btnImport_Click(object sender, EventArgs e)
		{
			if (!File.Exists(m_tbFilename.Text))
			{
				// "{0} does not exist!"
				string sFmt = TeResourceHelper.GetResourceString("kstidImportXmlNotAFile");
				string sMsg = String.Format(sFmt, m_tbFilename.Text);
				// "Warning"
				string sCaption = TeResourceHelper.GetResourceString("kstidImportXmlWarning");
				MessageBox.Show(sMsg, sCaption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			if (VerifyLanguageInOxesFile() && EnsureProperOxesFile())
			{
				string validationErrors = Validator.GetAnyValidationErrors(m_tbFilename.Text);
				if (validationErrors == null)
				{
					this.DialogResult = DialogResult.OK;
					this.Close();
				}
				else
					DisplayImportError(SUE_ErrorCode.OxesValidationFailed, validationErrors);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the m_btnBrowse control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_btnBrowse_Click(object sender, EventArgs e)
		{
			string fileName = string.Empty;
			if (!String.IsNullOrEmpty(m_tbFilename.Text) &&
				!String.IsNullOrEmpty(m_tbFilename.Text.Trim()))
			{
				fileName = m_tbFilename.Text;
			}

			if (m_openFileDialog == null)
				m_openFileDialog = new TeImportExportFileDialog(m_cache, FileType.OXES);
			DialogResult res = m_openFileDialog.ShowOpenDialog(fileName, Owner);
			if (res == DialogResult.OK)
				m_tbFilename.Text = m_openFileDialog.FileName;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the m_btnCancel control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_btnCancel_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel;
			this.Close();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Enable the OK button when there is text in the file name text box. File validation
		/// happens in the OK button's click event.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_tbFilename_TextChanged(object sender, EventArgs e)
		{
			m_btnImport.Enabled = (!string.IsNullOrEmpty(m_tbFilename.Text));
			m_lblDescription.Text = ReadRevisionDesc(m_tbFilename.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the m_btnHelp control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(FwApp.App, "khtpImportXML");
		}
		#endregion

		#region Public properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get or set the OXES filename.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string FileName
		{
			get { return m_tbFilename.Text; }
			set
			{
				m_tbFilename.Text = value;
				EnableImportButton();
			}
		}
		#endregion

		#region IFWDisposable Members

		/// <summary>
		/// Verify that this object has not yet been disposed.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		#endregion
	}
}