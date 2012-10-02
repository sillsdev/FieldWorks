using System;
using System.Drawing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.IText;
using SIL.FieldWorks.LexText.Controls;

using ECInterfaces;
using SilEncConverters31;
using XCore;
using SIL.FieldWorks.Resources;

namespace SIL.FieldWorks.IText
{
	internal struct LanguageMapping
	{
		/// <summary></summary>
		public string LlCode;
		/// <summary></summary>
		public string LlName;
		/// <summary></summary>
		public string FwCode;
		/// <summary></summary>
		public string FwName;
		/// <summary></summary>
		public string EncodingConverter;

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:LanguageMapping"/> class.
		/// </summary>
		/// <param name="llName">Name of the ll.</param>
		/// <param name="fwName">Name of the fw.</param>
		/// <param name="ec">The ec.</param>
		/// <param name="llCode">The ll code.</param>
		/// <param name="fwCode">The fw code.</param>
		/// --------------------------------------------------------------------------------
		public LanguageMapping(ListViewItem.ListViewSubItemCollection subItems)
		{
			Debug.Assert(subItems.Count == 5);
			LlCode = subItems[LinguaLinksImportDlg.kLlCode].Text;
			LlName = subItems[LinguaLinksImportDlg.kLlName].Text;
			FwCode = subItems[LinguaLinksImportDlg.kFwCode].Text;
			FwName = subItems[LinguaLinksImportDlg.kFwName].Text;
			EncodingConverter = subItems[LinguaLinksImportDlg.kec].Text;
		}
	}

	/// <summary>
	/// Summary description for IFwImportDialog.
	/// </summary>
	public class LinguaLinksImportDlg : Form, IFWDisposable, IFwExtension
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

		protected FdoCache m_cache;
		private System.Windows.Forms.LinkLabel linkLabel2;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox m_LinguaLinksXmlFileName;
		private System.Windows.Forms.Button btn_LinguaLinksXmlBrowse;
		private System.Windows.Forms.OpenFileDialog openFileDialog;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ColumnHeader columnHeader2;
		private System.Windows.Forms.ColumnHeader columnHeader3;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.ListView listViewMapping;
		private System.Windows.Forms.Button btnModifyMapping;
		private System.Windows.Forms.Button btnImport;
		protected Mediator m_mediator;
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

		// class to contain 'ws' information to be put in combo boxes
		class WsInfo
		{
			private int m_ws;
			private string m_name;
			private string m_locale;
			private string m_map;

			public WsInfo()
			{
				m_name = ITextStrings.ksIgnore;
			}

			public WsInfo(int ws, string name, string locale, string map)
			{
				m_ws = ws;
				m_name = name;
				m_locale = locale;
				m_map = map;
			}

			public string Name
			{
				get { return m_name; }
			}

			public string Locale
			{
				get { return m_locale; }
			}

			public string KEY
			{
				get { return Locale; }
			}

			public string Map
			{
				get { return m_map; }
			}

			public override string ToString()
			{
				return Name;
			}
		}

		public LinguaLinksImportDlg()
		{
			InitializeComponent();

			// Copied from the LexImportWizard dlg Init (LexImportWizard.cs)
			// Ensure that we have the default encoding converter (to/from MS Windows Code Page
			// for Western European languages)
			SilEncConverters31.EncConverters encConv = new SilEncConverters31.EncConverters();
			System.Collections.IDictionaryEnumerator de = encConv.GetEnumerator();
			string sEncConvName = "Windows1252<>Unicode";	// REVIEW: SHOULD THIS NAME BE LOCALIZED?
			bool fMustCreateEncCnv = true;
			while (de.MoveNext())
			{
				if ((string)de.Key != null && (string)de.Key == sEncConvName)
				{
					fMustCreateEncCnv = false;
					break;
				}
			}
			if (fMustCreateEncCnv)
			{
				try
				{
					encConv.AddConversionMap(sEncConvName, "1252",
						ECInterfaces.ConvType.Legacy_to_from_Unicode, "cp", "", "",
						ECInterfaces.ProcessTypeFlags.CodePageConversion);
				}
				catch (SilEncConverters31.ECException exception)
				{
					MessageBox.Show(exception.Message, ITextStrings.ksConvMapError,
						MessageBoxButtons.OK);
				}
			}
		}

		/// <summary>
		/// From IFwExtension
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="mediator"></param>
		public void Init(FdoCache cache, XCore.Mediator mediator)
		{
			CheckDisposed();

			m_cache = cache;
			m_mediator = mediator;
			m_sRootDir = SIL.FieldWorks.Common.Utils.DirectoryFinder.FWCodeDirectory;
			if (!m_sRootDir.EndsWith("\\"))
				m_sRootDir += "\\";
			m_sRootDir += "Language Explorer\\Import\\";

			m_sTempDir = Path.Combine(Path.GetTempPath(), "LanguageExplorer\\");
			if (!Directory.Exists(m_sTempDir))
				Directory.CreateDirectory(m_sTempDir);
			m_sLastXmlFileName = "";

			if(FwApp.App != null) // FwApp.App could be null during tests
			{
				this.helpProvider = new System.Windows.Forms.HelpProvider();
				this.helpProvider.HelpNamespace = FwApp.App.HelpFile;
				this.helpProvider.SetHelpKeyword(this, FwApp.App.GetHelpString(s_helpTopic, 0));
				this.helpProvider.SetHelpNavigator(this, System.Windows.Forms.HelpNavigator.Topic);
			}
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LinguaLinksImportDlg));
			this.linkLabel2 = new System.Windows.Forms.LinkLabel();
			this.label1 = new System.Windows.Forms.Label();
			this.m_LinguaLinksXmlFileName = new System.Windows.Forms.TextBox();
			this.btn_LinguaLinksXmlBrowse = new System.Windows.Forms.Button();
			this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
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

		#region Internal progress bar class
		internal class ProgressReporter : ProgressState, IAdvInd
		{
			public ProgressReporter(StatusBarProgressPanel panel)
				: base(panel)
			{
			}
			#region IAdvInd Members

			public void Step(int nStepAmt)
			{
				int nNewDone = PercentDone + nStepAmt;
				if (nNewDone > 100)
					nNewDone = nNewDone % 100;
				PercentDone = nNewDone;
				Breath();
			}

			#endregion

		}
		#endregion

		/// <summary>
		/// This is temporary behavior until a full import dialog/wizard is written which allows
		/// the user to specify the original LinguaLinks language project dump file.
		/// </summary>
		private void linkLabel1_LinkClicked(object sender,
			System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
		{
			// Fire up a file chooser to select an XML file.
			// Try importing the selected file.
			// Report success or failure.
			System.Windows.Forms.OpenFileDialog openFileDialog =
				new System.Windows.Forms.OpenFileDialog();
			openFileDialog.Filter = ResourceHelper.FileFilter(FileFilterType.XML);
			openFileDialog.FilterIndex = 0;
			openFileDialog.CheckFileExists = true;
			openFileDialog.Multiselect = false;
			openFileDialog.Title = ITextStrings.ksSelectLLFWXMLFile;
			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				using (new SIL.FieldWorks.Common.Utils.WaitCursor(this))
				{
					StatusBarProgressPanel panel =
						m_mediator.PropertyTable.GetValue("ProgressBar") as StatusBarProgressPanel;
					ProgressReporter prog = new ProgressReporter(panel);
					string sXmlFile = openFileDialog.FileName;
					string sLogFile = sXmlFile;
					int ich = sLogFile.LastIndexOf(".");
					if (ich >= 0)
						sLogFile.Remove(ich, sLogFile.Length - ich);
					sLogFile += "-Import.log";
					try
					{
						IFwXmlData2 fwxd2 = FwXmlDataClass.Create();
						fwxd2.Open(m_cache.ServerName, m_cache.DatabaseName);
						fwxd2.ImportMultipleXmlFields(sXmlFile, m_cache.LangProject.Hvo,
							prog as IAdvInd);
						System.Runtime.InteropServices.Marshal.ReleaseComObject(fwxd2);
						fwxd2 = null;
						MessageBox.Show(this,
							String.Format(ITextStrings.ksSuccessLoadingLL,
								sXmlFile, m_cache.DatabaseName, System.Environment.NewLine, sLogFile),
							ITextStrings.ksLLImportSucceeded,
							MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
					catch
					{
						MessageBox.Show(this,
							String.Format(ITextStrings.ksFailedLoadingLL,
								sXmlFile, m_cache.DatabaseName, System.Environment.NewLine, sLogFile),
							ITextStrings.ksLLImportFailed,
							MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			}
		}

		private void ShowFinishLabel()
		{
			//MessageBox.Show(Convert.ToString(Control.ModifierKeys));
			if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)
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
				btnImport.Text = String.Format(ITextStrings.ksPhaseButton,
					m_startPhase, btnImport.Text);
			}
		}

		private void LinguaLinksImportDlg_Load(object sender, System.EventArgs e)
		{

		}

		private void linkLabel2_LinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
		{
			ShowHelp.ShowHelpTopic(FwApp.App, "khtpLinguaLinksImportLink");
		}

		private string BaseName(string fullName)
		{
			string result;
			string temp;

			result = "";
			temp = fullName.ToUpperInvariant();
			for (int ic = 0; ic < temp.Length; ic++)
			{
				if ((temp[ic] >= 'A') && (temp[ic] <= 'Z'))
				{
					result = result + temp[ic];
				}
			}
			return result;
		}

		private void UpdateLanguageCodes()
		{
			if (m_sLastXmlFileName != m_LinguaLinksXmlFileName.Text)
			{
				m_sLastXmlFileName = m_LinguaLinksXmlFileName.Text;
				listViewMapping.Items.Clear();
				btnImport.Enabled = false;	// default to not enabled now that there are no items
				m_nextInput = m_LinguaLinksXmlFileName.Text;
				if (!File.Exists(m_nextInput))
				{
					MessageBox.Show(
						String.Format(ITextStrings.ksLLFileNotFound, m_nextInput),
						ITextStrings.ksLLImport,
						MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
					return;
				}

				StringUtils.InitIcuDataDir();	// used for normalizing strings to NFC
				m_startPhase = 1;
				string nameTest;
				if (m_nextInput.Length > 19)
				{
					nameTest = m_nextInput.Substring(m_nextInput.Length - 19, 19);
					if (nameTest == "\\LLPhase1Output.xml")
					{
						m_startPhase = 2;
					}
					else if (nameTest == "\\LLPhase2Output.xml")
					{
						m_startPhase = 3;
					}
					else if (nameTest == "\\LLPhase3Output.xml")
					{
						m_startPhase = 4;
					}
				}

				if (m_startPhase == 1)
				{
					StreamReader streamReader = File.OpenText(m_nextInput);
					String input;

					bool inWritingSystem = false;
					bool inIcuLocale24 = false;
					bool inName24 = false;
					bool lineDone = false;
					bool formatOkay = false;
					int pos, pos1, pos2, pos3;
					string wsLLCode = "";
					string wsName = "";
					Dictionary<string, WsInfo> wsInfo = new Dictionary<string,WsInfo>();	// Dictioanry of wsInfo

					//getting name for a writing system given the ICU code.
					ILgWritingSystemFactory wsf = m_cache.LanguageWritingSystemFactoryAccessor;
					int wsUser = wsf.UserWs;
					int wsVern = m_cache.DefaultVernWs;
					IWritingSystem ws = wsf.get_EngineOrNull(wsVern);

					// getting list of writing systems to eventually populate a list box
					int cws = wsf.NumberOfWs;
					using (ArrayPtr ptr = MarshalEx.ArrayToNative(cws, typeof(int)))
					{
						wsf.GetWritingSystems(ptr, cws);
						int[] vws = (int[])MarshalEx.NativeToArray(ptr, cws, typeof(int));
						for (int iws = 0; iws < cws; iws++)
						{
							if (vws[iws] == 0)
								continue;
							ws = wsf.get_EngineOrNull(vws[iws]);
							if (ws == null)
								continue;
							string name = ws.get_UiName(wsUser);
							string icuLocal = ws.IcuLocale;
							string mapName = ws.LegacyMapping;
							if ((mapName == null) || (mapName == ""))
							{
								mapName = "Windows1252<>Unicode";	// REVIEW: SHOULD THIS NAME BE LOCALIZED?
							}
							WsInfo wsi = new WsInfo(vws[iws], name, icuLocal, mapName);
							wsInfo.Add(wsi.KEY, wsi);
						}
					}

					while ((input=streamReader.ReadLine())!=null)
					{
						lineDone = false;
						while (!lineDone)
						{
							if (!inWritingSystem)
							{
								pos = input.IndexOf("<LgWritingSystem");
								if (pos >= 0)
								{
									inWritingSystem = true;
									wsLLCode = "";
									wsName = "";
									if (input.Length >= pos + 21)
										input = input.Substring(pos + 21, input.Length - pos - 21);
									else
										input = input.Substring(pos + 16);
								}

								else
								{
									lineDone = true;
								}
							}
							if (inWritingSystem && !inIcuLocale24 && !inName24)
							{
								pos1 = input.IndexOf("</LgWritingSystem>");
								pos2 = input.IndexOf("<ICULocale24>");
								pos3 = input.IndexOf("<Name24>");
								if (pos1 < 0 && pos2 < 0 && pos3 < 0)
								{
									lineDone = true;
								}
								else if (pos1 >= 0 && (pos2 < 0 || pos2 > pos1) && (pos3 < 0 || pos3 > pos1))
								{
									input = input.Substring(pos1 + 18, input.Length - pos1 - 18);
									if (wsLLCode != "")
									{
										if (wsName == "")
										{
											wsName = "<" + wsLLCode + ">";
										}
										string wsFWName = "";
										string wsEC = "";
										string wsFWCode = "";

										foreach (KeyValuePair<string, WsInfo> kvp in wsInfo)
										{
											WsInfo wsi = kvp.Value;
											if (wsName == wsi.Name)
											{
												wsFWName = StringUtils.NormalizeToNFC(wsi.Name);
												wsEC = StringUtils.NormalizeToNFC(wsi.Map);
												wsFWCode = StringUtils.NormalizeToNFC(wsi.Locale);
											}
										}

										if (wsFWName == "")
										{
											foreach (KeyValuePair<string, WsInfo> kvp in wsInfo)
											{
												WsInfo wsi = kvp.Value;
												if (BaseName(wsName) == BaseName(wsi.Name))
												{
													wsFWName = StringUtils.NormalizeToNFC(wsi.Name);
													wsEC = StringUtils.NormalizeToNFC(wsi.Map);
													wsFWCode = StringUtils.NormalizeToNFC(wsi.Locale);
												}
											}
										}

										ListViewItem lvItem = new ListViewItem(new string[] {StringUtils.NormalizeToNFC(wsName), wsFWName, wsEC, StringUtils.NormalizeToNFC(wsLLCode), wsFWCode});
										lvItem.Tag = wsName;
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
						ShowFinishLabel();	// update the button before showing the msg box just in case...
						MessageBox.Show(
							String.Format(ITextStrings.ksInvalidLLFile, m_nextInput),
							ITextStrings.ksLLImport,
							MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
						return;
					}
				}
				else
				{
					btnImport.Enabled = true;
				}
				ShowFinishLabel();
			}
		}

		private void btn_LinguaLinksXmlBrowse_Click(object sender, System.EventArgs e)
		{
			string currentFile = m_LinguaLinksXmlFileName.Text;

			openFileDialog.Filter = ResourceHelper.BuildFileFilter(new FileFilterType[] {
				FileFilterType.XML, FileFilterType.AllFiles });
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

		///// <summary>
		///// (IFwExtension)Shows the dialog as a modal dialog
		///// </summary>
		///// <returns>A DialogResult value</returns>
		//public System.Windows.Forms.DialogResult ShowDialog(IWin32Window owner)
		//{
		//		CheckDisposed();
		//
		//   return base.ShowDialog(owner);
		//}

		private void listViewMapping_SelectedIndexChanged()
		{
			ListView.SelectedIndexCollection selIndexes = listViewMapping.SelectedIndices;
			if (selIndexes.Count > 0)
				btnModifyMapping.Enabled = true;
			else
				btnModifyMapping.Enabled = false;
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
			string llName, fwName, ec, fwCode, llCode;
			ListViewItem lvItem;

			ListView.SelectedIndexCollection selIndexes = listViewMapping.SelectedIndices;
			if (selIndexes.Count < 1 || selIndexes.Count > 1)
				return;	// only handle single selection at this time

			int selIndex = selIndexes[0];	// only support 1
			lvItem = listViewMapping.Items[selIndex];
			LexImportWizardLanguage dlg = new LexImportWizardLanguage(m_cache);
			llName = lvItem.Text;
			fwName = lvItem.SubItems[1].Text;
			ec = lvItem.SubItems[2].Text;
			llCode = lvItem.SubItems[3].Text;
			dlg.LangToModify(llName, fwName, ec);

			if (dlg.ShowDialog(this) == DialogResult.OK)
			{
				// retrieve the new WS information from the dlg
				dlg.GetCurrentLangInfo(out llName, out fwName, out ec, out fwCode);

				// remove the one that was modified
				listViewMapping.Items.Remove(lvItem);

				// now add the modified one
				lvItem = new ListViewItem(new string[] {llName, fwName, ec, llCode, fwCode});
				lvItem.Tag = llName;
				listViewMapping.Items.Add(lvItem);
				int ii = listViewMapping.Items.IndexOf(lvItem);
				listViewMapping.Items[ii].Selected = true;
			}

			CheckImportEnabled();
		}

		private void CheckImportEnabled()
		{
			bool allSpecified = true;

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

		private void btnImport_Click(object sender, System.EventArgs e)
		{
			// if the shift key is down, then just build the phaseNoutput files
			bool runToCompletion = ((Control.ModifierKeys & Keys.Shift) != Keys.Shift);
			using (ProgressDialogWithTask dlg = new ProgressDialogWithTask(this))
			{
				dlg.CancelButtonVisible = true;

				LanguageMapping[] languageMappings = new LanguageMapping[listViewMapping.Items.Count];
				for (int i = 0; i < listViewMapping.Items.Count; i++)
					languageMappings[i] = new LanguageMapping(listViewMapping.Items[i].SubItems);

				dlg.SetRange(0, 500);

				using (new WaitCursor(this, true))
				{
					// This needs to be reset when cancel is pressed with out clicking the
					// browse button.  This resolves a noted issue in the code where an exception
					// is processed when run a second time...
					m_nextInput = m_LinguaLinksXmlFileName.Text;

					LinguaLinksImport import = new LinguaLinksImport(m_cache, m_sTempDir, m_sRootDir);
					import.NextInput = m_nextInput;
					import.Error += new LinguaLinksImport.ErrorHandler(OnImportError);
					dlg.Cancel += new CancelHandler(import.On_ProgressDlg_Cancel);
					Debug.Assert(m_nextInput == m_LinguaLinksXmlFileName.Text);
					try
					{
						bool fSuccess = (bool)dlg.RunTask(true, new BackgroundTaskInvoker(import.Import),
							runToCompletion, languageMappings, m_startPhase);

						if (fSuccess)
						{
							string sLogFile = m_sTempDir + "LLPhase3Output-Import.log";
							MessageBox.Show(this,
								String.Format(ITextStrings.ksSuccessLoadingLL,
									Path.GetFileName(m_LinguaLinksXmlFileName.Text),
									m_cache.DatabaseName, System.Environment.NewLine, sLogFile),
								ITextStrings.ksLLImportSucceeded,
								MessageBoxButtons.OK, MessageBoxIcon.Information);
							this.DialogResult = DialogResult.OK;	// only 'OK' if not exception
						}
						else
						{
							this.DialogResult = DialogResult.Abort; // unsuccessful import
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
							System.Diagnostics.Debug.WriteLine("Error: " + ex.InnerException.Message);

							MessageBox.Show(String.Format(import.ErrorMessage, ex.InnerException.Message),
								ITextStrings.ksUnhandledError,
								MessageBoxButtons.OK, MessageBoxIcon.Error);
							this.DialogResult = DialogResult.Cancel;	// only 'OK' if not exception
							Close();
						}
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when an import error occurs.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="message">The message.</param>
		/// <param name="caption">The caption.</param>
		/// ------------------------------------------------------------------------------------
		private void OnImportError(object sender, string message, string caption)
		{
			if (InvokeRequired)
				Invoke(new LinguaLinksImport.ErrorHandler(OnImportError), sender, message, caption);
			else
				MessageBox.Show(this, message, caption, MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
		}

		private void LinguaLinksImportDlg_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			ShowFinishLabel();
		}

		private void LinguaLinksImportDlg_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			ShowFinishLabel();
		}

		private void m_btnHelp_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(FwApp.App, s_helpTopic);
		}
	}
}
