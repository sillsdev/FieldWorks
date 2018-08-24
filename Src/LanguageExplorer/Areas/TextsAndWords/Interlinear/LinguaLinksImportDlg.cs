// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Controls.LexText;
using SIL.LCModel.Core.Text;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Controls.FileDialog;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.FieldWorks.Resources;
using SIL.LCModel.Utils;
using WaitCursor = SIL.FieldWorks.Common.FwUtils.WaitCursor;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// Summary description for IFwImportDialog.
	/// </summary>
	public class LinguaLinksImportDlg : Form, IFwExtension
	{
		public const int kLlName = 0;
		public const int kFwName = 1;
		public const int kec = 2;
		public const int kLlCode = 3;
		public const int kFwCode = 4;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		protected LcmCache m_cache;
		private System.Windows.Forms.LinkLabel linkLabel2;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox m_LinguaLinksXmlFileName;
		private System.Windows.Forms.Button btn_LinguaLinksXmlBrowse;
		private OpenFileDialogAdapter openFileDialog;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ColumnHeader columnHeader2;
		private System.Windows.Forms.ColumnHeader columnHeader3;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.ListView listViewMapping;
		private System.Windows.Forms.Button btnModifyMapping;
		private System.Windows.Forms.Button btnImport;
		protected IPropertyTable m_propertyTable;
		protected IPublisher m_publisher;
		private System.Windows.Forms.Button btn_Cancel;
		private string m_sTempDir;
		private string m_sRootDir;
		private string m_sLastXmlFileName;
		private System.Windows.Forms.Label lblFinishWOImport;
		private System.Windows.Forms.Button m_btnHelp;
		private System.Windows.Forms.Label lblMappingLanguages;
		private int m_startPhase = 1;
		private System.Windows.Forms.Label label3;
		private string m_nextInput;

		private const string s_helpTopic = "khtpLinguaLinksImport";
		private System.Windows.Forms.HelpProvider helpProvider;

		public LinguaLinksImportDlg()
		{
			InitializeComponent();
			openFileDialog = new OpenFileDialogAdapter();
			AccessibleName = GetType().Name;

			// Copied from the LexImportWizard dlg Init (LexImportWizard.cs)
			// Ensure that we have the default encoding converter (to/from MS Windows Code Page
			// for Western European languages)
			var encConv = new SilEncConverters40.EncConverters();
			var de = encConv.GetEnumerator();
			var sEncConvName = "Windows1252<>Unicode";	// REVIEW: SHOULD THIS NAME BE LOCALIZED?
			var fMustCreateEncCnv = true;
			while (de.MoveNext())
			{
				if ((string) de.Key == null || (string) de.Key != sEncConvName)
				{
					continue;
				}
				fMustCreateEncCnv = false;
				break;
			}

			if (!fMustCreateEncCnv)
			{
				return;
			}
			try
			{
				encConv.AddConversionMap(sEncConvName, "1252", ECInterfaces.ConvType.Legacy_to_from_Unicode, "cp", string.Empty, string.Empty, ECInterfaces.ProcessTypeFlags.CodePageConversion);
			}
			catch (SilEncConverters40.ECException exception)
			{
				MessageBox.Show(exception.Message, ITextStrings.ksConvMapError, MessageBoxButtons.OK);
			}
		}

		/// <summary>
		/// From IFwExtension
		/// </summary>
		void IFwExtension.Init(LcmCache cache, IPropertyTable propertyTable, IPublisher publisher)
		{
			m_cache = cache;
			m_propertyTable = propertyTable;
			m_publisher = publisher;
			m_sRootDir = FwDirectoryFinder.CodeDirectory;
			if (!m_sRootDir.EndsWith("\\"))
			{
				m_sRootDir += "\\";
			}
			m_sRootDir += "Language Explorer\\Import\\";

			m_sTempDir = Path.Combine(Path.GetTempPath(), "LanguageExplorer\\");
			if (!Directory.Exists(m_sTempDir))
			{
				Directory.CreateDirectory(m_sTempDir);
			}
			m_sLastXmlFileName = string.Empty;

			var helpTopicProvider = m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider");
			if (helpTopicProvider == null)
			{
				return;
			}
			helpProvider = new HelpProvider
			{
				HelpNamespace = helpTopicProvider.HelpFile
			};
			helpProvider.SetHelpKeyword(this, helpTopicProvider.GetHelpString(s_helpTopic));
			helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			if (IsDisposed)
			{
				// No need to do it more than once.
				return;
			}

			if (disposing)
			{
				components?.Dispose();
				openFileDialog.Dispose();
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LinguaLinksImportDlg));
			this.linkLabel2 = new System.Windows.Forms.LinkLabel();
			this.label1 = new System.Windows.Forms.Label();
			this.m_LinguaLinksXmlFileName = new System.Windows.Forms.TextBox();
			this.btn_LinguaLinksXmlBrowse = new System.Windows.Forms.Button();
			this.listViewMapping = new System.Windows.Forms.ListView();
			this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
			this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
			this.columnHeader3 = new System.Windows.Forms.ColumnHeader();
			this.label2 = new System.Windows.Forms.Label();
			this.btnModifyMapping = new System.Windows.Forms.Button();
			this.btnImport = new System.Windows.Forms.Button();
			this.btn_Cancel = new System.Windows.Forms.Button();
			this.lblFinishWOImport = new System.Windows.Forms.Label();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.lblMappingLanguages = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			//
			// linkLabel2
			//
			resources.ApplyResources(this.linkLabel2, "linkLabel2");
			this.linkLabel2.Name = "linkLabel2";
			this.linkLabel2.TabStop = true;
			this.linkLabel2.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel2_LinkClicked);
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			//
			// m_LinguaLinksXmlFileName
			//
			resources.ApplyResources(this.m_LinguaLinksXmlFileName, "m_LinguaLinksXmlFileName");
			this.m_LinguaLinksXmlFileName.Name = "m_LinguaLinksXmlFileName";
			this.m_LinguaLinksXmlFileName.Leave += new System.EventHandler(this.m_LinguaLinksXmlFileName_Leave);
			//
			// btn_LinguaLinksXmlBrowse
			//
			resources.ApplyResources(this.btn_LinguaLinksXmlBrowse, "btn_LinguaLinksXmlBrowse");
			this.btn_LinguaLinksXmlBrowse.Name = "btn_LinguaLinksXmlBrowse";
			this.btn_LinguaLinksXmlBrowse.Click += new System.EventHandler(this.btn_LinguaLinksXmlBrowse_Click);
			//
			// listViewMapping
			//
			this.listViewMapping.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.columnHeader1,
			this.columnHeader2,
			this.columnHeader3});
			this.listViewMapping.FullRowSelect = true;
			this.listViewMapping.HideSelection = false;
			resources.ApplyResources(this.listViewMapping, "listViewMapping");
			this.listViewMapping.MultiSelect = false;
			this.listViewMapping.Name = "listViewMapping";
			this.listViewMapping.Sorting = System.Windows.Forms.SortOrder.Ascending;
			this.listViewMapping.UseCompatibleStateImageBehavior = false;
			this.listViewMapping.View = System.Windows.Forms.View.Details;
			this.listViewMapping.SelectedIndexChanged += new System.EventHandler(this.listViewMapping_SelectedIndexChanged);
			this.listViewMapping.DoubleClick += new System.EventHandler(this.listViewMapping_DoubleClick);
			//
			// columnHeader1
			//
			resources.ApplyResources(this.columnHeader1, "columnHeader1");
			//
			// columnHeader2
			//
			resources.ApplyResources(this.columnHeader2, "columnHeader2");
			//
			// columnHeader3
			//
			resources.ApplyResources(this.columnHeader3, "columnHeader3");
			//
			// label2
			//
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			this.label2.Click += new System.EventHandler(this.label2_Click);
			//
			// btnModifyMapping
			//
			resources.ApplyResources(this.btnModifyMapping, "btnModifyMapping");
			this.btnModifyMapping.Name = "btnModifyMapping";
			this.btnModifyMapping.Click += new System.EventHandler(this.btnModifyMapping_Click);
			//
			// btnImport
			//
			resources.ApplyResources(this.btnImport, "btnImport");
			this.btnImport.Name = "btnImport";
			this.btnImport.Click += new System.EventHandler(this.btnImport_Click);
			//
			// btn_Cancel
			//
			this.btn_Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.btn_Cancel, "btn_Cancel");
			this.btn_Cancel.Name = "btn_Cancel";
			//
			// lblFinishWOImport
			//
			this.lblFinishWOImport.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			resources.ApplyResources(this.lblFinishWOImport, "lblFinishWOImport");
			this.lblFinishWOImport.ForeColor = System.Drawing.SystemColors.ActiveCaption;
			this.lblFinishWOImport.Name = "lblFinishWOImport";
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// lblMappingLanguages
			//
			resources.ApplyResources(this.lblMappingLanguages, "lblMappingLanguages");
			this.lblMappingLanguages.Name = "lblMappingLanguages";
			//
			// label3
			//
			resources.ApplyResources(this.label3, "label3");
			this.label3.Name = "label3";
			//
			// LinguaLinksImportDlg
			//
			this.AcceptButton = this.btnImport;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.btn_Cancel;
			this.Controls.Add(this.label3);
			this.Controls.Add(this.lblMappingLanguages);
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.lblFinishWOImport);
			this.Controls.Add(this.btn_Cancel);
			this.Controls.Add(this.btnImport);
			this.Controls.Add(this.btnModifyMapping);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.listViewMapping);
			this.Controls.Add(this.btn_LinguaLinksXmlBrowse);
			this.Controls.Add(this.m_LinguaLinksXmlFileName);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.linkLabel2);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
			this.KeyPreview = true;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "LinguaLinksImportDlg";
			this.Load += new System.EventHandler(this.LinguaLinksImportDlg_Load);
			this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.LinguaLinksImportDlg_KeyUp);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.LinguaLinksImportDlg_KeyDown);
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		private void ShowFinishLabel()
		{
			if ((ModifierKeys & Keys.Shift) == Keys.Shift)
			{
				lblFinishWOImport.Visible = true;
				btnImport.Text = ITextStrings.ksProcess;
			}
			else
			{
				lblFinishWOImport.Visible = false;
				btnImport.Text = ITextStrings.ksImport;
			}
			if (m_startPhase > 1)
			{
				btnImport.Text = string.Format(ITextStrings.ksPhaseButton, m_startPhase, btnImport.Text);
			}
		}

		private void LinguaLinksImportDlg_Load(object sender, System.EventArgs e)
		{
		}

		private void linkLabel2_LinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), "khtpLinguaLinksImportLink");
		}

		private static string BaseName(string fullName)
		{
			var result = string.Empty;
			var temp = fullName.ToUpperInvariant();
			return temp.Where(t => (t >= 'A') && (t <= 'Z')).Aggregate(result, (current, t) => current + t);
		}

		private void UpdateLanguageCodes()
		{
			if (m_sLastXmlFileName == m_LinguaLinksXmlFileName.Text)
			{
				return;
			}
			m_sLastXmlFileName = m_LinguaLinksXmlFileName.Text;
			listViewMapping.Items.Clear();
			btnImport.Enabled = false;
			// default to not enabled now that there are no items
			m_nextInput = m_LinguaLinksXmlFileName.Text;
			if (!File.Exists(m_nextInput))
			{
				MessageBox.Show(
					string.Format(ITextStrings.ksLLFileNotFound, m_nextInput),
					ITextStrings.ksLLImport,
					MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
				return;
			}

			m_startPhase = 1;
			if (m_nextInput.Length > 19)
			{
				var nameTest = m_nextInput.Substring(m_nextInput.Length - 19, 19);
				switch (nameTest)
				{
					case "\\LLPhase1Output.xml":
						m_startPhase = 2;
						break;
					case "\\LLPhase2Output.xml":
						m_startPhase = 3;
						break;
					case "\\LLPhase3Output.xml":
						m_startPhase = 4;
						break;
					case "\\LLPhase4Output.xml":
						m_startPhase = 5;
						break;
					case "\\LLPhase5Output.xml":
						m_startPhase = 6;
						break;
				}
			}

			if (m_startPhase == 1)
			{
				using (var streamReader = File.OpenText(m_nextInput))
				{
					string input;
					var inWritingSystem = false;
					var inIcuLocale24 = false;
					var inName24 = false;
					var formatOkay = false;
					var wsLLCode = string.Empty;
					var wsName = string.Empty;
					//getting name for a writing system given the ICU code.
					var wsInfo = m_cache.ServiceLocator.WritingSystemManager.WritingSystems.Select(ws => new WsInfo(ws.DisplayLabel, ws.Id, string.IsNullOrEmpty(ws.LegacyMapping) ? "Windows1252<>Unicode" : ws.LegacyMapping)).ToDictionary(wsi => wsi.Id);

					while ((input = streamReader.ReadLine()) != null)
					{
						var lineDone = false;
						while (!lineDone)
						{
							int pos;
							if (!inWritingSystem)
							{
								pos = input.IndexOf("<LgWritingSystem");
								if (pos >= 0)
								{
									inWritingSystem = true;
									wsLLCode = string.Empty;
									wsName = string.Empty;
									input = input.Length >= pos + 21 ? input.Substring(pos + 21, input.Length - pos - 21) : input.Substring(pos + 16);
								}

								else
								{
									lineDone = true;
								}
							}
							if (inWritingSystem && !inIcuLocale24 && !inName24)
							{
								var pos1 = input.IndexOf("</LgWritingSystem>");
								var pos2 = input.IndexOf("<ICULocale24>");
								var pos3 = input.IndexOf("<Name24>");
								if (pos1 < 0 && pos2 < 0 && pos3 < 0)
								{
									lineDone = true;
								}
								else if (pos1 >= 0 && (pos2 < 0 || pos2 > pos1) && (pos3 < 0 || pos3 > pos1))
								{
									input = input.Substring(pos1 + 18, input.Length - pos1 - 18);
									if (wsLLCode != string.Empty)
									{
										if (wsName == string.Empty)
										{
											wsName = "<" + wsLLCode + ">";
										}
										var wsFWName = string.Empty;
										var wsEC = string.Empty;
										var wsFWCode = string.Empty;

										foreach (var kvp in wsInfo)
										{
											var wsi = kvp.Value;
											if (wsName != wsi.Name)
											{
												continue;
											}
											wsFWName = TsStringUtils.NormalizeToNFC(wsi.Name);
											wsEC = TsStringUtils.NormalizeToNFC(wsi.Map);
											wsFWCode = TsStringUtils.NormalizeToNFC(wsi.Id);
										}

										if (wsFWName == string.Empty)
										{
											foreach (var kvp in wsInfo)
											{
												var wsi = kvp.Value;
												if (BaseName(wsName) != BaseName(wsi.Name))
												{
													continue;
												}
												wsFWName = TsStringUtils.NormalizeToNFC(wsi.Name);
												wsEC = TsStringUtils.NormalizeToNFC(wsi.Map);
												wsFWCode = TsStringUtils.NormalizeToNFC(wsi.Id);
											}
										}

										var lvItem = new ListViewItem(new[] {TsStringUtils.NormalizeToNFC(wsName), wsFWName, wsEC, TsStringUtils.NormalizeToNFC(wsLLCode), wsFWCode})
										{
											Tag = wsName
										};
										listViewMapping.Items.Add(lvItem);
										formatOkay = true;
									}
									inWritingSystem = false;
								}
								else if (pos2 >= 0 && (pos3 < 0 || pos3 > pos2))
								{
									input = input.Substring(pos2 + 13, input.Length - pos2 - 13);
									inIcuLocale24 = true;
								}
								else
								{
									input = input.Substring(pos3 + 8, input.Length - pos3 - 8);
									inName24 = true;
								}
							}
							if (inIcuLocale24)
							{
								pos = input.IndexOf(">");
								if (pos < 0)
								{
									lineDone = true;
								}
								else
								{
									input = input.Substring(pos + 1, input.Length - pos - 1);
									pos = input.IndexOf("<");
									wsLLCode = input.Substring(0, pos);
									input = input.Substring(pos, input.Length - pos);
									inIcuLocale24 = false;
								}
							}
							if (inName24)
							{
								pos = input.IndexOf(">");
								if (pos < 0)
								{
									lineDone = true;
								}
								else
								{
									input = input.Substring(pos + 1, input.Length - pos - 1);
									pos = input.IndexOf("<");
									wsName = input.Substring(0, pos);
									input = input.Substring(pos, input.Length - pos);
									inName24 = false;
								}
							}
						}
					}
					streamReader.Close();
					listViewMapping_SelectedIndexChanged();
					CheckImportEnabled();
					if (!formatOkay)
					{
						ShowFinishLabel();
						// update the button before showing the msg box just in case...
						MessageBox.Show(
							string.Format(ITextStrings.ksInvalidLLFile, m_nextInput),
							ITextStrings.ksLLImport,
							MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
						return;
					}
				}
			}
			else
			{
				btnImport.Enabled = true;
			}
			ShowFinishLabel();
		}

		private void btn_LinguaLinksXmlBrowse_Click(object sender, System.EventArgs e)
		{
			var currentFile = m_LinguaLinksXmlFileName.Text;

			openFileDialog.Filter = ResourceHelper.BuildFileFilter(FileFilterType.XML, FileFilterType.AllFiles);
			openFileDialog.FilterIndex = 1;
			openFileDialog.CheckFileExists = true;
			openFileDialog.Multiselect = false;

			if (currentFile != null)
			{
				openFileDialog.InitialDirectory = currentFile;
				openFileDialog.FileName = currentFile;
			}

			openFileDialog.Title = ITextStrings.ksSelectLLXMLFile;
			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				m_LinguaLinksXmlFileName.Text = openFileDialog.FileName;
				UpdateLanguageCodes();
			}
		}

		private void listViewMapping_SelectedIndexChanged()
		{
			var selIndexes = listViewMapping.SelectedIndices;
			btnModifyMapping.Enabled = selIndexes.Count > 0;
		}

		private void listViewMapping_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			listViewMapping_SelectedIndexChanged();
		}

		private void m_LinguaLinksXmlFileName_Leave(object sender, System.EventArgs e)
		{
			UpdateLanguageCodes();
		}

		private void listViewMapping_DoubleClick(object sender, System.EventArgs e)
		{
			btnModifyMapping.PerformClick();	// same as pressing the modify button
		}

		private void btnModifyMapping_Click(object sender, System.EventArgs e)
		{
			var selIndexes = listViewMapping.SelectedIndices;
			if (selIndexes.Count < 1 || selIndexes.Count > 1)
			{
				return;
			}
			// only handle single selection at this time
			var selIndex = selIndexes[0];
			// only support 1
			var lvItem = listViewMapping.Items[selIndex];
			var app = m_propertyTable.GetValue<IApp>("App");
			using (var dlg = new LexImportWizardLanguage(m_cache, m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), app))
			{
				var llName = lvItem.Text;
				var fwName = lvItem.SubItems[1].Text;
				var ec = lvItem.SubItems[2].Text;
				var llCode = lvItem.SubItems[3].Text;
				dlg.LangToModify(llName, fwName, ec);

				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					// retrieve the new WS information from the dlg
					string fwCode;
					dlg.GetCurrentLangInfo(out llName, out fwName, out ec, out fwCode);

					// remove the one that was modified
					listViewMapping.Items.Remove(lvItem);

					// now add the modified one
					lvItem = new ListViewItem(new string[] {llName, fwName, ec, llCode, fwCode})
					{
						Tag = llName
					};
					listViewMapping.Items.Add(lvItem);
					listViewMapping.Items[listViewMapping.Items.IndexOf(lvItem)].Selected = true;
				}

				CheckImportEnabled();
			}
		}

		private void CheckImportEnabled()
		{
			var allSpecified = true;

			foreach(ListViewItem lvItem2 in listViewMapping.Items)
			{
				if (lvItem2.SubItems[2].Text == "")
				{
					allSpecified = false;
				}
			}
			btnImport.Enabled = (listViewMapping.Items.Count > 0 && allSpecified) || m_startPhase > 1;
		}

		private void label2_Click(object sender, System.EventArgs e)
		{
		}

		private void btnImport_Click(object sender, EventArgs e)
		{
			// if the shift key is down, then just build the phaseNoutput files
			var runToCompletion = ((ModifierKeys & Keys.Shift) != Keys.Shift);
			using (var dlg = new ProgressDialogWithTask(this))
			{
				dlg.AllowCancel = true;

				var languageMappings = new LanguageMapping[listViewMapping.Items.Count];
				for (var i = 0; i < listViewMapping.Items.Count; i++)
				{
					languageMappings[i] = new LanguageMapping(listViewMapping.Items[i].SubItems);
				}

				dlg.Minimum = 0;
				dlg.Maximum = 500;

				using (new WaitCursor(this, true))
				{
					// This needs to be reset when cancel is pressed with out clicking the
					// browse button.  This resolves a noted issue in the code where an exception
					// is processed when run a second time...
					m_nextInput = m_LinguaLinksXmlFileName.Text;

					var import = new LinguaLinksImport(m_cache, m_sTempDir, m_sRootDir)
					{
						NextInput = m_nextInput
					};
					import.Error += OnImportError;
					Debug.Assert(m_nextInput == m_LinguaLinksXmlFileName.Text);
					// Ensure the idle time processing for change record doesn't cause problems
					// because the import creates a record to change to.  See FWR-3700.
					var recordList = m_propertyTable.GetValue<IRecordListRepository>("RecordListRepository").ActiveRecordList;
					var fSuppressedSave = false;
					try
					{
						if (recordList != null)
						{
							fSuppressedSave = recordList.SuppressSaveOnChangeRecord;
							recordList.SuppressSaveOnChangeRecord = true;
						}
						var fSuccess = (bool)dlg.RunTask(true, import.Import, runToCompletion, languageMappings, m_startPhase);

						if (fSuccess)
						{
							MessageBox.Show(this,
								string.Format(ITextStrings.ksSuccessLoadingLL, Path.GetFileName(m_LinguaLinksXmlFileName.Text), m_cache.ProjectId.Name, Environment.NewLine, import.LogFile),
								ITextStrings.ksLLImportSucceeded,
								MessageBoxButtons.OK, MessageBoxIcon.Information);
							DialogResult = DialogResult.OK;	// only 'OK' if not exception
						}
						else
						{
							DialogResult = DialogResult.Abort; // unsuccessful import
						}

						Close();
						m_nextInput = import.NextInput;
					}
					catch (WorkerThreadException ex)
					{
						if (ex.InnerException is InvalidDataException)
						{
							// Special handling for this case...
							ShowFinishLabel();
							CheckImportEnabled();
						}
						else
						{
							Debug.WriteLine("Error: " + ex.InnerException.Message);

							MessageBox.Show(string.Format(import.ErrorMessage, ex.InnerException.Message),
								ITextStrings.ksUnhandledError,
								MessageBoxButtons.OK, MessageBoxIcon.Error);
							DialogResult = DialogResult.Cancel;	// only 'OK' if not exception
							Close();
						}
					}
					finally
					{
						if (recordList != null)
						{
							recordList.SuppressSaveOnChangeRecord = fSuppressedSave;
						}
					}
				}
			}
		}

		/// <summary>
		/// Called when an import error occurs.
		/// </summary>
		private void OnImportError(object sender, string message, string caption)
		{
			if (InvokeRequired)
			{
				Invoke(new LinguaLinksImport.ErrorHandler(OnImportError), sender, message, caption);
			}
			else
			{
				MessageBox.Show(this, message, caption, MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			}
		}

		private void LinguaLinksImportDlg_KeyDown(object sender, KeyEventArgs e)
		{
			ShowFinishLabel();
		}

		private void LinguaLinksImportDlg_KeyUp(object sender, KeyEventArgs e)
		{
			ShowFinishLabel();
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), s_helpTopic);
		}

		// class to contain 'ws' information to be put in combo boxes
		private sealed class WsInfo
		{
			public WsInfo(string name, string id, string map)
			{
				Name = name;
				Id = id;
				Map = map;
			}

			public string Name { get; }

			public string Id { get; }

			public string Map { get; }

			public override string ToString()
			{
				return Name;
			}
		}
	}
}
