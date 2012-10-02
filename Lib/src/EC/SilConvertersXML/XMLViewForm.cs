using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;
using ECInterfaces;
using SilEncConverters31;
using System.IO;

namespace SilConvertersXML
{
	public partial class XMLViewForm : Form
	{
		public const string cstrCaption = "SILConverters for XML";
		protected const string cstrDefaultNameSpaceAbrev = "n";
		protected const string cstrAttributeLabel = "Attribute: ";
		protected const string cstrElementFormat = "Element: [{0}]";
		protected const string cstrExtraWhitespace = "#significant-whitespace";
		protected const string cstrOutputFileAddn = " (Convert'd)";

		protected DateTime m_dtStarted = DateTime.Now;
		TimeSpan m_timeMinStartup = new TimeSpan(0, 0, 1);

		protected XmlDocument m_doc = null;
		protected Dictionary<string, string> m_mapPrefix2NamespaceURI = new Dictionary<string, string>();
		protected string m_strFileSpec = null;

		public XMLViewForm()
		{
			InitializeComponent();

			radioButtonDefaultFont.Text = radioButtonDefaultFont.Font.FontFamily.Name;
			radioButtonDefaultFont.Tag = radioButtonDefaultFont.Font;
			radioButtonDefaultFont.Click += new EventHandler(radioButtonFont_Click);
			radioButtonDefaultFont.Hide();
			tableLayoutPanelSampleData.SetRowSpan(listBoxViewData, 2);

			this.helpProvider.SetHelpString(treeViewXmlDoc, Properties.Resources.treeViewXmlDocHelp);
			this.helpProvider.SetHelpString(dataGridViewConverterMapping, Properties.Resources.dataGridViewConverterMappingHelp);
			this.helpProvider.SetHelpString(listBoxViewData, Properties.Resources.listBoxViewDataHelp);
		}

		private void populateTreeControl(XmlElement rootElement)
		{
			TreeNode rootNode = treeViewXmlDoc.Nodes.Add(rootElement.Name, rootElement.Name);
			string strPrefix = String.IsNullOrEmpty(rootElement.Prefix) ? cstrDefaultNameSpaceAbrev : rootElement.Prefix;
			rootNode.Tag = strPrefix;
			if (!m_mapPrefix2NamespaceURI.ContainsKey(strPrefix) && !String.IsNullOrEmpty(rootElement.NamespaceURI))
				m_mapPrefix2NamespaceURI.Add(strPrefix, rootElement.NamespaceURI);
			populateTreeControl(rootElement, rootNode.Nodes);
		}

		private void populateTreeControl(XmlNode document, TreeNodeCollection nodes)
		{
			// Dictionary<string, TreeNode> aNodeNames = new Dictionary<string, TreeNode>();
			foreach (System.Xml.XmlNode node in document.ChildNodes)
			{
				string text = node.Name;
				if (text == cstrExtraWhitespace)
					continue;

				text = String.Format(cstrElementFormat, text);

				if (node.Value != null)
					text += String.Format(" = [{0}]", node.Value);

				TreeNode thisBranch = null;
				string strPrefix = String.IsNullOrEmpty(node.Prefix) ? cstrDefaultNameSpaceAbrev : node.Prefix;
				if (!nodes.ContainsKey(node.Name))
				{
					thisBranch = nodes.Add(node.Name, text);	// do it once
					thisBranch.Tag = strPrefix;
					if (!m_mapPrefix2NamespaceURI.ContainsKey(strPrefix) && !String.IsNullOrEmpty(node.NamespaceURI))
						m_mapPrefix2NamespaceURI.Add(strPrefix, node.NamespaceURI);
				}
				else
				{
					thisBranch = nodes[node.Name];
					System.Diagnostics.Debug.Assert(strPrefix == (string)thisBranch.Tag);
				}

				// if there are any attributes for this branch/node, then put them in as children
				if ((node.Attributes != null) && (node.Attributes.Count > 0))
				{
					foreach (XmlAttribute attr in node.Attributes)
					{
						TreeNodeCollection tc = thisBranch.Nodes;
						if (!tc.ContainsKey(attr.Name))
						{
							int nNumAttrs = GetAttrCount(tc);
							string strAttrKeyValue = String.Format("{0}[{1}] = [{2}] ", cstrAttributeLabel, attr.Name, attr.Value);
							TreeNode newAttr = thisBranch.Nodes.Insert(nNumAttrs, attr.Name, strAttrKeyValue);
						}
					}
				}

				// iterate to the children of thisBranch
				populateTreeControl(node, thisBranch.Nodes);
			}
		}

		private int GetAttrCount(TreeNodeCollection tc)
		{
			int nNumAttrs = 0;
			foreach (TreeNode node in tc)
				if (IsAttribute(node))
					nNumAttrs++;
			return nNumAttrs;
		}

		private bool IsAttribute(TreeNode node)
		{
			return (node.Text.IndexOf(cstrAttributeLabel) != -1);
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void openXMLDocumentToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (!CheckForModified())
				return;

			// in case it was already open and the user clicks the "Open XML documents" again.
			Reset();

			DialogResult res = this.openFileDialog.ShowDialog();
			if (res == DialogResult.OK)
			{
				OpenDocument(openFileDialog.FileName);
			}
		}

		public void OpenDocument(string strFileSpec)
		{
			try
			{
				Cursor = Cursors.WaitCursor;
				m_strFileSpec = strFileSpec;
				m_doc = new XmlDocument();
				m_doc.Load(strFileSpec);
				Program.Modified = true; // probably changed, so set modified to allow initial output
				Program.AddFilenameToTitle(strFileSpec);

				// deal with potential namespace issues
				if (!String.IsNullOrEmpty(m_doc.DocumentElement.NamespaceURI))
				{
					string strPrefix = m_doc.DocumentElement.Prefix;
					if (String.IsNullOrEmpty(strPrefix))
						strPrefix = cstrDefaultNameSpaceAbrev;
					m_mapPrefix2NamespaceURI.Add(strPrefix, m_doc.DocumentElement.NamespaceURI);
				}

				// populate the tree
				treeViewXmlDoc.SuspendLayout();
				populateTreeControl(m_doc.DocumentElement);
				treeViewXmlDoc.ExpandAll();
				treeViewXmlDoc.ResumeLayout(true);

				treeViewXmlDoc.Nodes[0].EnsureVisible();
				converterMappingsToolStripMenuItem.Enabled = true;
				enterXPathExpressionToolStripMenuItem.Enabled = true;
			}
			catch (ApplicationException ex)
			{
				// we throw this to cancel
				if (ex.Message != cstrCaption)
					MessageBox.Show(ex.Message, cstrCaption);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, cstrCaption);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		protected void Reset()
		{
			m_doc = null;
			treeViewXmlDoc.Nodes.Clear();
			listBoxViewData.Items.Clear();
			m_mapEncConverters.Clear();
			m_mapPrefix2NamespaceURI.Clear();
			dataGridViewConverterMapping.Rows.Clear();
			m_aEcDefault = null;
			Program.Modified = false;
		}

		protected bool IsNamespaceRequired
		{
			get { return (m_mapPrefix2NamespaceURI.Count > 0); }
		}

		protected bool HasElementChildren(TreeNode node)
		{
			foreach (TreeNode nodeChild in node.Nodes)
				if (!IsAttribute(nodeChild))
					return true;
			return false;
		}

		protected int m_nBotheration = 0;

		private void treeViewXmlDoc_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
		{
			// prevent the false click that occurs when the user chooses a menu item
			if ((DateTime.Now - m_dtStarted) < m_timeMinStartup)
				return;

			System.Diagnostics.Trace.WriteLine("treeViewXmlDoc_NodeMouseClick");
			try
			{
				// either it has to be an attribute or a leaf element (i.e. no sub-nodes)
				if (!IsAttribute(e.Node) && HasElementChildren(e.Node))
				{
					if (++m_nBotheration > 2)
						return;
					else
						throw new ApplicationException("Can't convert elements that contain sub-elements (there's no data!)");
				}

				if (e.Button == MouseButtons.Right)
				{
					if (treeViewXmlDoc.SelectedNode != e.Node)
						treeViewXmlDoc.SelectedNode = e.Node;

					// wait for the BackgroundWorker to finish the work.
					while (this.backgroundWorker.IsBusy)
					{
						System.Diagnostics.Trace.WriteLine("BackgroundWorker IsBusy: true!");

						// tell it to stop
						this.backgroundWorker.CancelAsync();

						// Keep UI messages moving, so the form remains
						// responsive during the asynchronous operation.
						Application.DoEvents();
					}

					listBoxViewData.Items.Clear();
					backgroundWorker.RunWorkerAsync(e.Node);
				}
				else
				{
					string strXPath;
					XPathNodeIterator xpIterator = GetIterator(e.Node, out strXPath);

					buttonProcessAndSave.Enabled = true;
					AddRow(strXPath, xpIterator);

					// in case something was checked.
					UncheckAllNodes(treeViewXmlDoc.Nodes);
				}
			}
			catch (ApplicationException ex)
			{
				// we throw this to cancel
				if (ex.Message != cstrCaption)
					MessageBox.Show(ex.Message, cstrCaption);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, cstrCaption);
			}
		}

		protected void CheckForLimitations(ref string strXPath, TreeNodeCollection nodes, XPathNavigator navigator,
			XmlNamespaceManager manager)
		{
			int i = nodes.Count;
			while (i-- > 0)
			{
				TreeNode node = nodes[i];

				// depth first (or multiple constraints won't work)
				CheckForLimitations(ref strXPath, node.Nodes, navigator, manager);

				if (node.Checked)
					CheckForLimitation(node, ref strXPath, navigator, manager);
			}
		}

		protected int IndexOfFirstDifference(string str1, string str2)
		{
			int nMinLength = Math.Min(str1.Length, str2.Length);
			int nIndex = -1;
			while (++nIndex < nMinLength)
				if (str1[nIndex] != str2[nIndex])
					break;
			return nIndex;
		}

		protected string m_strLastConstraint = null;

		protected void CheckForLimitation(TreeNode node, ref string strXPath, XPathNavigator navigator,
			XmlNamespaceManager manager)
		{
			// make sure it has something in common with the path we're going to work on (otherwise
			//  such a filter makes no sense)
			string strLimitXPath = GetXPath(node);
			int nIndexOfLastSlash = IndexOfFirstDifference(strXPath, strLimitXPath);

			// under certain conditions, we want to skip to the last element:
			//  1) if the given strXPath contains a constraint already (we want that included; not consider a diff
			//  2) if the two XPath expressions are identical (in which case, the user is filtering on the item they
			//      want...)
			if ((strXPath.Length > nIndexOfLastSlash) && (strXPath[nIndexOfLastSlash] != '[')
				|| (strXPath == strLimitXPath))
				nIndexOfLastSlash = strLimitXPath.LastIndexOf('/', nIndexOfLastSlash - 1);
			if (nIndexOfLastSlash > 0)
			{
				XPathNodeIterator xpIterator = null;
				if (manager != null)
					xpIterator = navigator.Select(strLimitXPath, manager);
				else
					xpIterator = navigator.Select(strLimitXPath);

				// ask the user what they want to do
				string strXPathRoot = strLimitXPath.Substring(0, nIndexOfLastSlash);
				string strName = (strLimitXPath.Length > nIndexOfLastSlash) ? strLimitXPath.Substring(nIndexOfLastSlash + 1)
					: null;
				CreateLimitationForm dlg = new CreateLimitationForm(strXPathRoot, strName, xpIterator,
					IsAttribute(node), Properties.Settings.Default.RecentFilters);
				DialogResult res = dlg.ShowDialog();
				if (res == DialogResult.OK)
				{
					string strFilter = dlg.FilterXPath;
					string strLastConstraint = strFilter + strXPath.Substring(nIndexOfLastSlash);
					Program.AddRecentXPathExpression(strLastConstraint);
					strXPath = strLastConstraint;
				}
				else if (res == DialogResult.Cancel)
					throw new ApplicationException(cstrCaption);
			}
		}

		protected void GetNamespaceManager(XPathNavigator navigator, out XmlNamespaceManager manager)
		{
			manager = new XmlNamespaceManager(navigator.NameTable);
			foreach (KeyValuePair<string, string> kvp in m_mapPrefix2NamespaceURI)
				manager.AddNamespace(String.IsNullOrEmpty(kvp.Key) ? String.Empty : kvp.Key, kvp.Value);
		}

		protected string GetXPath(TreeNode node)
		{
			string strXPath = null;
			if (node.Name[0] == '#')
				node = node.Parent;

			do
			{
				string strName = node.Name;
				if (IsAttribute(node))
					strXPath = "/@" + strName;
				else
				{
					string strPrefix = (string)node.Tag;
					if (!IsNamespaceRequired || (strName.IndexOf(String.Format("{0}:", strPrefix)) != -1))
						strPrefix = null;
					strXPath = String.Format("/{0}{1}{2}{3}",
						strPrefix, String.IsNullOrEmpty(strPrefix) ? "" : ":", strName, strXPath);
				}
			}
			while ((node = node.Parent) != null);

			return strXPath;
		}

		protected bool XPathHasNameSpace(string strXPath)
		{
			return (strXPath.IndexOf(':') != -1);
		}

		protected void UncheckAllNodes(TreeNodeCollection nodes)
		{
			foreach (TreeNode node in nodes)
			{
				node.Checked = false;
				UncheckAllNodes(node.Nodes);
			}
		}

		private void processAndSaveDocuments(object sender, EventArgs e)
		{
			ProcessAndSave(true, null);
		}

		public void ProcessAndSave(bool bShowUI, string strOutputFileSpec)
		{
			try
			{
				Cursor = Cursors.WaitCursor;
				bool bModified = Program.Modified;
				int nIndex = 0;
				while (nIndex < dataGridViewConverterMapping.Rows.Count)
				{
					DataGridViewRow aRow = dataGridViewConverterMapping.Rows[nIndex];
					string strXmlPath = (string)aRow.Cells[cnXmlPathsColumn].Value;
					DirectableEncConverter aEC = (DirectableEncConverter)m_mapEncConverters[strXmlPath];
					if (aEC == null)
					{
						nIndex++;
						continue;
					}

					XPathNavigator navigator = m_doc.CreateNavigator();
					XPathNodeIterator xpIterator = null;
					if (IsNamespaceRequired)
					{
						XmlNamespaceManager manager;
						GetNamespaceManager(navigator, out manager);
						xpIterator = navigator.Select(strXmlPath, manager);
					}
					else
					{
						xpIterator = navigator.Select(strXmlPath);
					}

					System.Diagnostics.Trace.WriteLine(String.Format("Selected: {0} records", xpIterator.Count));
					while (xpIterator.MoveNext())
					{
						System.Diagnostics.Trace.WriteLine(String.Format("Position: {0} Count: {1}", xpIterator.CurrentPosition, xpIterator.Count));
						string strInput = xpIterator.Current.Value;
						string strOutput = CallSafeConvert(aEC, strInput);
						if (bShowUI)
						{
							textBoxInput.Text = strInput;
							textBoxOutput.Text = strOutput;
							Application.DoEvents();
						}
						bModified |= (strInput != strOutput);
						xpIterator.Current.SetValue(strOutput);
					}

					dataGridViewConverterMapping.Rows.RemoveAt(nIndex);
				}

				// update in case Save is cancelled
				Program.Modified = bModified;

				// reset this so we can do new versions
				m_mapEncConverters.Clear();

				bool bHaveOutputFilename = false;
				do
				{
					if (String.IsNullOrEmpty(strOutputFileSpec))
					{
						System.Diagnostics.Debug.Assert(!String.IsNullOrEmpty(m_strFileSpec));
						string strOutputFilenameOrig = String.Format(@"{0}{1}{2}{3}",
							 GetDirEnsureFinalSlash(m_strFileSpec),
							 Path.GetFileNameWithoutExtension(m_strFileSpec),
							 cstrOutputFileAddn,
							 Path.GetExtension(m_strFileSpec));

						saveFileDialog.FileName = strOutputFilenameOrig;
						DialogResult res = saveFileDialog.ShowDialog();
						if (res == DialogResult.OK)
						{
							strOutputFileSpec = saveFileDialog.FileName;
							bHaveOutputFilename = true;
						}
						else
						{
							DialogResult dres = MessageBox.Show("The document has already been converted. If you cancel saving it now, that will leave your document in an unuseable state (unless you are doing 'spell fixing' or something which doesn't change the encoding). Click 'Yes' to confirm the cancellation or 'No' to continue with the conversion.", cstrCaption, MessageBoxButtons.YesNo);
							if (dres == DialogResult.Yes)
								throw new ApplicationException("User cancelled");
						}
					}
				} while (!bHaveOutputFilename);

				if (!Directory.Exists(Path.GetDirectoryName(strOutputFileSpec)))
					Directory.CreateDirectory(Path.GetDirectoryName(strOutputFileSpec));

				// if the user is insisting on saving it with the same name, then make a backup.
				if (m_strFileSpec == strOutputFileSpec)
					File.Copy(m_strFileSpec, m_strFileSpec + ".bak", true);

				// Save the document to a file and auto-indent the output.
				XmlTextWriter writer = new XmlTextWriter(strOutputFileSpec, Encoding.UTF8);
				writer.Formatting = Formatting.Indented;
				m_doc.Save(writer);
				m_strFileSpec = strOutputFileSpec;
				Program.Modified = false;
				Program.AddFilenameToTitle(strOutputFileSpec);
			}
			catch (ApplicationException ex)
			{
				// we throw this to cancel
				if (ex.Message != cstrCaption)
					MessageBox.Show(ex.Message, cstrCaption);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, cstrCaption);
			}
			finally
			{
				textBoxInput.Text = null;
				textBoxOutput.Text = null;
				Cursor = Cursors.Default;
			}
		}

		// the GetDirectoryName returns a final slash, but only for files in the root folder
		//  so make sure we get exactly one.
		private string GetDirEnsureFinalSlash(string strFilename)
		{
			string strFolder = Path.GetDirectoryName(strFilename);
			if (strFolder[strFolder.Length - 1] != '\\')
				strFolder += '\\';
			return strFolder;
		}

		protected bool EnableSave
		{
			get { return Program.Modified || (dataGridViewConverterMapping.Rows.Count > 0); }
		}

		private void ToolStripMenuItemFile_DropDownOpening(object sender, EventArgs e)
		{
			recentFilesToolStripMenuItem.DropDownItems.Clear();
			foreach (string strRecentFile in Properties.Settings.Default.RecentFiles)
				recentFilesToolStripMenuItem.DropDownItems.Add(strRecentFile, null, recentFilesToolStripMenuItem_Click);

			processAndSaveDocumentsToolStripMenuItem.Enabled = EnableSave;
			recentFilesToolStripMenuItem.Enabled = (recentFilesToolStripMenuItem.DropDownItems.Count > 0);
		}

		void recentFilesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToolStripDropDownItem aRecentFile = (ToolStripDropDownItem)sender;
			try
			{
				if (!CheckForModified())
					return;

				// in case it was already open and the user clicks the "Open XML documents" again.
				Reset();

				OpenDocument(aRecentFile.Text);
			}
			catch (Exception ex)
			{
				// probably means the file doesn't exist anymore, so remove it from the recent used list
				Properties.Settings.Default.RecentFiles.Remove(aRecentFile.Text);
				MessageBox.Show(ex.Message, cstrCaption);
			}
		}

		private void dataGridViewConverterMapping_UserDeletedRow(object sender, DataGridViewRowEventArgs e)
		{
			buttonProcessAndSave.Enabled = EnableSave;
		}

		private void treeViewXmlDoc_AfterCollapse(object sender, TreeViewEventArgs e)
		{
			// keep track of the time so we can reject false "NodeMouseClick" events (we get it, but
			//  it's not like the user actually click to do that function with this)
			m_dtStarted = DateTime.Now;
		}

		private void treeViewXmlDoc_AfterExpand(object sender, TreeViewEventArgs e)
		{
			// keep track of the time so we can reject false "NodeMouseClick" events (we get it, but
			//  it's not like the user actually click to do that function with this)
			m_dtStarted = DateTime.Now;
		}

		private void treeViewXmlDoc_AfterCheck(object sender, TreeViewEventArgs e)
		{
			// keep track of the time so we can reject false "NodeMouseClick" events (we get it, but
			//  it's not like the user actually click to do that function with this)
			if (e.Action != TreeViewAction.Unknown)
				m_dtStarted = DateTime.Now;
		}

		protected bool CheckForModified()
		{
			if (Program.Modified && (dataGridViewConverterMapping.Rows.Count > 0))
			{
				DialogResult res = MessageBox.Show("Do you want to save the current file?", cstrCaption, MessageBoxButtons.YesNoCancel);
				if (res == DialogResult.Cancel)
					return false;
				else if (res == DialogResult.Yes)
					processAndSaveDocuments(null, null);
			}
			return true;
		}

		private void XMLViewForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (dataGridViewConverterMapping.Rows.Count > 0)
			{
				// ask the user about converting these?
				DialogResult res = MessageBox.Show("Do you want to execute the requested conversion(s)?", cstrCaption, MessageBoxButtons.YesNoCancel);
				if (res == DialogResult.Yes)
					processAndSaveDocuments(sender, e);
				else if (res == DialogResult.Cancel)
				{
					e.Cancel = true;
					return;
				}
			}

			e.Cancel = !CheckForModified();
		}

		void treeViewXmlDoc_PreviewKeyDown(object sender, System.Windows.Forms.PreviewKeyDownEventArgs e)
		{
			System.Diagnostics.Trace.WriteLine(String.Format("PreviewKeyDown: sender: {3}, KeyValue: {0}, KeyCode: {1}, KeyData: {2}",
				e.KeyValue, e.KeyCode, e.KeyData, sender.ToString()));

			int nIndex = dataGridViewConverterMapping.Rows.Count - 1;
			if ((e.KeyCode == Keys.Delete) && (nIndex > -1))
				dataGridViewConverterMapping.Rows.RemoveAt(nIndex);
		}

		protected XPathNodeIterator GetIterator(TreeNode node, out string strXPath)
		{
			// either it has to be an attribute or a leaf element (i.e. no sub-nodes)
			strXPath = GetXPath(node);
			return GetIterator(ref strXPath, true);
		}

		protected XPathNodeIterator GetIterator(ref string strXPath, bool bCheckForLimits)
		{
			XPathNavigator navigator = m_doc.CreateNavigator();
			XPathNodeIterator xpIterator = null;
			if (IsNamespaceRequired)
			{
				XmlNamespaceManager manager;
				GetNamespaceManager(navigator, out manager);
				if (bCheckForLimits)
					CheckForLimitations(ref strXPath, treeViewXmlDoc.Nodes, navigator, manager);
				xpIterator = navigator.Select(strXPath, manager);
			}
			else
			{
				if (bCheckForLimits)
					CheckForLimitations(ref strXPath, treeViewXmlDoc.Nodes, navigator, null);
				xpIterator = navigator.Select(strXPath);
			}

			return xpIterator;
		}

		private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			BackgroundWorker worker = (BackgroundWorker)sender;

			string strXPath;
			try
			{
				XPathNodeIterator xpIterator = GetIterator((TreeNode)e.Argument, out strXPath);

				List<string> lstUnique = new List<string>();
				while (xpIterator.MoveNext() && !worker.CancellationPending)
				{
					string strValue = xpIterator.Current.Value;
					if (!lstUnique.Contains(strValue))
					{
						lstUnique.Add(strValue);
						worker.ReportProgress((lstUnique.Count * 100) / xpIterator.Count, strValue);
					}
				}
			}
			catch (ApplicationException ex)
			{
				// we throw this to cancel
				if (ex.Message != cstrCaption)
					MessageBox.Show(ex.Message, cstrCaption);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, cstrCaption);
			}
		}

		private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			listBoxViewData.Items.Add(e.UserState);
		}

		private void listBoxViewData_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
				if (fontDialog.ShowDialog() == DialogResult.OK)
				{
					listBoxViewData.Font = fontDialog.Font;
					tableLayoutPanelSampleData.SuspendLayout();
					radioButtonDefaultFont.Show();
					tableLayoutPanelSampleData.SetRowSpan(listBoxViewData, 1);
					RadioButton radioButtonFont = NewFontRadioButton(fontDialog.Font);
					tableLayoutPanelSampleData.RowCount = tableLayoutPanelSampleData.Controls.Count + 1;
					tableLayoutPanelSampleData.RowStyles.Add(new System.Windows.Forms.RowStyle(SizeType.AutoSize, radioButtonFont.Height));
					tableLayoutPanelSampleData.Controls.Add(radioButtonFont, 0, tableLayoutPanelSampleData.Controls.Count);
					tableLayoutPanelSampleData.ResumeLayout(false);
					tableLayoutPanelSampleData.PerformLayout();
					radioButtonFont.Checked = true;
				}
		}

		protected RadioButton NewFontRadioButton(Font font)
		{
			RadioButton radioButtonFont = new RadioButton();
			radioButtonFont.AutoSize = true;
			radioButtonFont.Location = new System.Drawing.Point(3, 3);
			radioButtonFont.Name = "radioButtonFont" + font.FontFamily.Name.Replace(" ", null);
			radioButtonFont.Size = new System.Drawing.Size(tableLayoutPanelSampleData.Width, 20);
			radioButtonFont.TabIndex = tableLayoutPanelSampleData.Controls.Count - 1;
			radioButtonFont.TabStop = true;
			radioButtonFont.Text = font.FontFamily.Name;
			radioButtonFont.UseVisualStyleBackColor = true;
			radioButtonFont.Tag = font;
			radioButtonFont.Click += new EventHandler(radioButtonFont_Click);
			return radioButtonFont;
		}

		void radioButtonFont_Click(object sender, EventArgs e)
		{
			listBoxViewData.Font = (Font)((RadioButton)sender).Tag;
		}

		private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			// in case something was checked.
			UncheckAllNodes(treeViewXmlDoc.Nodes);
		}

		private void enterXPathExpressionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			AddManualXPathExpression(null);
		}

		protected bool AddManualXPathExpression(string strExistingXPath)
		{
			XPathFilterForm dlg = new XPathFilterForm(strExistingXPath);
			if (dlg.ShowDialog() == DialogResult.OK)
			{
				string strXPath = dlg.FilterExpression;
				Program.AddRecentXPathExpression(strXPath);
				try
				{
					XPathNodeIterator xpIterator = GetIterator(ref strXPath, false);
					AddRow(strXPath, xpIterator);
					return true;
				}
				catch (ApplicationException ex)
				{
					// we throw this to cancel
					if (ex.Message != cstrCaption)
						MessageBox.Show(ex.Message, cstrCaption);
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message, cstrCaption);
				}
			}

			return false;
		}

		private void dataGridViewConverterMapping_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == ' ' || e.KeyChar == '\r')
			{
				if (dataGridViewConverterMapping.SelectedCells.Count > 0)
				{
					int nRow = dataGridViewConverterMapping.SelectedCells[0].RowIndex;
					dataGridViewConverterMapping_CellMouseClick(sender, new DataGridViewCellMouseEventArgs(2, nRow, 0, 0, new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0)));
				}
			}
		}
	}
}