// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ExportDialog.cs
// Responsibility: Steve McConnel
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------

using System;
using System.Drawing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Xml;

using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using XCore;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.FXT;
using SIL.FieldWorks.FDO.Cellar;
using Palaso.WritingSystems;
using SIL.FieldWorks.Resources;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// ExportDialog implements an XML-configurable set of export choices.
	/// The base class here implements the main lexicon export (and thus has some specific
	/// behavior we may want to refactor into a subclass one day).
	/// Override ConfigurationFilePath to give the path to a file (like the one in the example here)
	/// from the FW root directory. This file specifies what will appear in the export options.
	/// You will typically also need to override the actual Export process, unless it is
	/// a standard FXT export.
	/// </summary>
	public class ExportDialog : Form, IFWDisposable
	{
		protected FDO.FdoCache m_cache;
		protected Mediator m_mediator;
		private System.Windows.Forms.Label label1;
		protected System.Windows.Forms.ColumnHeader columnHeader1;
		protected System.Windows.Forms.ColumnHeader columnHeader2;
		private System.Windows.Forms.Button btnExport;
		private System.Windows.Forms.Button btnCancel;
		protected System.Windows.Forms.ListView m_exportList;
		private System.Windows.Forms.RichTextBox m_description;
		private XDumper m_dumper;
		private IAdvInd4 m_progressDlg;
		private System.Windows.Forms.Button buttonHelp;

		protected string m_helpTopic;
		private System.Windows.Forms.HelpProvider helpProvider;

		// This stores the values of the format, configured, filtered, and sorted attributes of
		// the toplevel <template> element.
		enum FxtTypes
		{
			kftFxt = 0,
			kftConfigured = 1,
			kftReversal = 2
		}
		private struct FxtType
		{
			public string m_sFormat;
			public FxtTypes m_ft;
			public string m_sDataType;
			public string m_sXsltFiles;
			public string m_path; // Used to keep track of items after they are sorted.
		}
		private List<FxtType> m_rgFxtTypes = new List<FxtType>(8);

		private ConfiguredExport m_ce = null;
		private XmlSeqView m_seqView = null;
		private XmlVc m_xvc = null;
		private int m_hvoRootObj = 0;
		private int m_clidRootObj = 0;
		private CheckBox m_chkExportPictures;
		/// <summary>Flag whether to include picture and media files in the export.</summary>
		private bool m_fExportPicturesAndMedia = false;
		/// <summary>
		/// This is set true whenever the check value for m_chkExportPictures is retrieved
		/// for a LIFT export.
		/// </summary>
		private bool m_fLiftExportPicturesSet = false;

		private const string ksLiftExportPicturesPropertyName = "LIFT-ExportPictures";

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public ExportDialog(Mediator mediator)
		{
			m_mediator = mediator;
			m_cache = (FdoCache)mediator.PropertyTable.GetValue("cache");

			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			m_helpTopic = "khtpExportLexicon";

			this.helpProvider = new System.Windows.Forms.HelpProvider();
			this.helpProvider.HelpNamespace = FwApp.App.HelpFile;
			this.helpProvider.SetHelpKeyword(this, FwApp.App.GetHelpString(m_helpTopic, 0));
			this.helpProvider.SetHelpNavigator(this, System.Windows.Forms.HelpNavigator.Topic);

			// Determine whether we can support "configured" type export by trying to obtain
			// the XmlVc for an XmlDocView.  Also obtain the database id and class id of the
			// root object.

			object objCurrentControl;
			objCurrentControl = m_mediator.PropertyTable.GetValue("currentContentControlObject", null);
			//XmlDocView docView = objCurrentControl as XmlDocView;
			//if (docView == null)
			//{
			//    XCore.MultiPane xmp = objCurrentControl as XCore.MultiPane;
			//    if (xmp != null)
			//        docView = xmp.FirstVisibleControl as XmlDocView;
			//}
			XmlDocView docView = FindXmlDocView(objCurrentControl as Control);
			if (docView != null)
				m_seqView = docView.Controls[0] as XmlSeqView;
			if (m_seqView != null)
				m_xvc = m_seqView.Vc;

			CmObject cmo = m_mediator.PropertyTable.GetValue("ActiveClerkSelectedObject", null)
				as CmObject;
			if (cmo != null)
			{
				m_hvoRootObj = cmo.OwnerHVO;
				if (m_hvoRootObj != 0)
					m_clidRootObj = m_cache.GetClassOfObject(m_hvoRootObj);
			}

			m_chkExportPictures.Checked = false;
			m_chkExportPictures.Visible = false;
			m_chkExportPictures.Enabled = false;
			m_fExportPicturesAndMedia = false;
		}

		/// <summary>
		/// If one exists, find an XmlDocView control no matter how deeply it's embedded.
		/// </summary>
		/// <param name="control"></param>
		/// <returns></returns>
		private XmlDocView FindXmlDocView(Control control)
		{
			if (control == null)
				return null;
			if (control is XmlDocView)
				return control as XmlDocView;
			foreach (Control c in control.Controls)
			{
				XmlDocView xdv = FindXmlDocView(c);
				if (xdv != null)
					return xdv;
			}
			return null;
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ExportDialog));
			this.btnExport = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.m_exportList = new System.Windows.Forms.ListView();
			this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
			this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
			this.m_description = new System.Windows.Forms.RichTextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.buttonHelp = new System.Windows.Forms.Button();
			this.m_chkExportPictures = new System.Windows.Forms.CheckBox();
			this.SuspendLayout();
			//
			// btnExport
			//
			resources.ApplyResources(this.btnExport, "btnExport");
			this.btnExport.Name = "btnExport";
			this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
			//
			// btnCancel
			//
			resources.ApplyResources(this.btnCancel, "btnCancel");
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			//
			// m_exportList
			//
			resources.ApplyResources(this.m_exportList, "m_exportList");
			this.m_exportList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.columnHeader1,
			this.columnHeader2});
			this.m_exportList.FullRowSelect = true;
			this.m_exportList.HideSelection = false;
			this.m_exportList.MinimumSize = new System.Drawing.Size(256, 183);
			this.m_exportList.MultiSelect = false;
			this.m_exportList.Name = "m_exportList";
			this.m_exportList.Sorting = System.Windows.Forms.SortOrder.Ascending;
			this.m_exportList.UseCompatibleStateImageBehavior = false;
			this.m_exportList.View = System.Windows.Forms.View.Details;
			this.m_exportList.SelectedIndexChanged += new System.EventHandler(this.m_exportList_SelectedIndexChanged);
			//
			// columnHeader1
			//
			resources.ApplyResources(this.columnHeader1, "columnHeader1");
			//
			// columnHeader2
			//
			resources.ApplyResources(this.columnHeader2, "columnHeader2");
			//
			// m_description
			//
			resources.ApplyResources(this.m_description, "m_description");
			this.m_description.Name = "m_description";
			this.m_description.ReadOnly = true;
			this.m_description.LinkClicked += new System.Windows.Forms.LinkClickedEventHandler(this.m_description_LinkClicked);
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			//
			// buttonHelp
			//
			resources.ApplyResources(this.buttonHelp, "buttonHelp");
			this.buttonHelp.Name = "buttonHelp";
			this.buttonHelp.Click += new System.EventHandler(this.buttonHelp_Click);
			//
			// m_chkExportPictures
			//
			resources.ApplyResources(this.m_chkExportPictures, "m_chkExportPictures");
			this.m_chkExportPictures.Name = "m_chkExportPictures";
			this.m_chkExportPictures.UseVisualStyleBackColor = true;
			this.m_chkExportPictures.CheckedChanged += new System.EventHandler(this.m_chkExportPictures_CheckedChanged);
			//
			// ExportDialog
			//
			this.AcceptButton = this.btnExport;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.btnCancel;
			this.Controls.Add(this.m_chkExportPictures);
			this.Controls.Add(this.buttonHelp);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.m_description);
			this.Controls.Add(this.m_exportList);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnExport);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ExportDialog";
			this.ShowIcon = false;
			this.Load += new System.EventHandler(this.ExportDialog_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		private void btnExport_Click(object sender, System.EventArgs e)
		{
			if (m_exportList.SelectedItems.Count == 0)
				return;

			if (ItemDisabled((string)m_exportList.SelectedItems[0].Tag))
				return;

			if (!PrepareForExport())
				return;

			bool fLiftExport = m_exportList.SelectedItems[0].SubItems[2].Text == "lift";
			string sFileName = null;
			string sDirectory = null;
			if (fLiftExport)
			{
				using (FolderBrowserDialog dlg = new FolderBrowserDialog())
				{
					dlg.Tag = xWorksStrings.ksChooseLIFTFolderTitle;	// can't set title !!??
					dlg.Description = String.Format(xWorksStrings.ksChooseLIFTExportFolder,
						m_exportList.SelectedItems[0].SubItems[1].Text);
					dlg.ShowNewFolderButton = true;
					dlg.RootFolder = Environment.SpecialFolder.Desktop;
					dlg.SelectedPath = m_mediator.PropertyTable.GetStringProperty("ExportDir",
						System.Environment.GetFolderPath(Environment.SpecialFolder.Personal));
					if (dlg.ShowDialog(this) != DialogResult.OK)
						return;
					sDirectory = dlg.SelectedPath;
				}
				string sFile = Path.GetFileName(sDirectory);
				sFileName = Path.Combine(sDirectory, sFile + ".lift");
				string sMsg = null;
				MessageBoxButtons btns = MessageBoxButtons.OKCancel;
				if (File.Exists(sFileName))
				{
					sMsg = xWorksStrings.ksLIFTAlreadyExists;
					btns = MessageBoxButtons.OKCancel;
				}
				else
				{
					string[] rgfiles = Directory.GetFiles(sDirectory);
					if (rgfiles.Length > 0)
					{
						sMsg = xWorksStrings.ksLIFTFolderNotEmpty;
						btns = MessageBoxButtons.YesNo;
					}
				}
				if (!String.IsNullOrEmpty(sMsg))
				{
					using (LiftExportMessageDlg dlg = new LiftExportMessageDlg(sMsg, btns))
					{
						if (dlg.ShowDialog(this) != DialogResult.OK)
							return;
					}
				}
			}
			else
			{
				using (SaveFileDialog dlg = new SaveFileDialog())
				{
					dlg.AddExtension = true;
					dlg.DefaultExt = m_exportList.SelectedItems[0].SubItems[2].Text;
					dlg.Filter = m_exportList.SelectedItems[0].SubItems[3].Text;
					dlg.Title = String.Format(xWorksStrings.ExportTo0, m_exportList.SelectedItems[0].SubItems[1].Text);
					dlg.InitialDirectory = m_mediator.PropertyTable.GetStringProperty("ExportDir",
						System.Environment.GetFolderPath(Environment.SpecialFolder.Personal));
					if (dlg.ShowDialog(this) != DialogResult.OK)
						return;
					sFileName = dlg.FileName;
					sDirectory = Path.GetDirectoryName(sFileName);
				}
			}
			m_mediator.PropertyTable.SetProperty("ExportDir", sDirectory);
			m_mediator.PropertyTable.SetPropertyPersistence("ExportDir", true);
			if (fLiftExport) // Fixes LT-9437 Crash exporting a discourse chart (or interlinear too!)
			{
				DoExport(sFileName, true);
				ExportWsAsLDML(sDirectory);
			}
			else
			{
				DoExport(sFileName); // Musn't use the 2 parameter version here or overrides get messed up.
			}
		}

		private void ExportWsAsLDML(string sDirectory)
		{
			LdmlInFolderWritingSystemStore ldmlstore = new LdmlInFolderWritingSystemStore(sDirectory);
			ldmlstore.DontAddDefaultDefinitions = true;
			Set<int> rgwsWritten = new Set<int>();
			foreach (LgWritingSystem lgws in m_cache.LangProject.VernWssRC)
			{
				ExportLDML(lgws, ldmlstore);
				rgwsWritten.Add(lgws.Hvo);
			}
			foreach (LgWritingSystem lgws in m_cache.LangProject.AnalysisWssRC)
			{
				if (!rgwsWritten.Contains(lgws.Hvo))
				{
					ExportLDML(lgws, ldmlstore);
					rgwsWritten.Add(lgws.Hvo);
				}
			}
			foreach (NamedWritingSystem nws in m_cache.LangProject.GetPronunciationWritingSystems())
			{
				if (nws.Hvo > 0 && !rgwsWritten.Contains(nws.Hvo))
				{
					LgWritingSystem lgws = LgWritingSystem.CreateFromDBObject(m_cache, nws.Hvo) as LgWritingSystem;
					ExportLDML(lgws, ldmlstore);
					rgwsWritten.Add(nws.Hvo);
				}
			}
			foreach (ReversalIndex ri in m_cache.LangProject.LexDbOA.ReversalIndexesOC)
			{
				if (ri.WritingSystemRAHvo > 0 && !rgwsWritten.Contains(ri.WritingSystemRAHvo))
				{
					ExportLDML(ri.WritingSystemRA as LgWritingSystem, ldmlstore);
					rgwsWritten.Add(ri.WritingSystemRAHvo);
				}
			}
		}

		private void ExportLDML(LgWritingSystem lgws, LdmlInFolderWritingSystemStore ldmlstore)
		{
			foreach (WritingSystemDefinition wsdT in ldmlstore.WritingSystemDefinitions)
			{
				// Don't bother changing an existing LDML file.
				if (wsdT.RFC4646 == lgws.RFC4646bis)
					return;
			}
			string sICU = lgws.ICULocale;
			string sLang;
			string sScript;
			string sCountry;
			string sVariant;
			Icu.UErrorCode err = Icu.UErrorCode.U_ZERO_ERROR;
			Icu.GetLanguageCode(sICU, out sLang, out err);
			if (sLang.Length > 3 && sLang.StartsWith("x"))
				sLang = sLang.Insert(1, "-");
			Icu.GetScriptCode(sICU, out sScript, out err);
			Icu.GetCountryCode(sICU, out sCountry, out err);
			Icu.GetVariantCode(sICU, out sVariant, out err);
			if (sVariant == "IPA")
				sVariant = "fonipa";
			Debug.Assert(err == Icu.UErrorCode.U_ZERO_ERROR);
			string sKeyboard;
			if (String.IsNullOrEmpty(lgws.KeymanKeyboard))
				sKeyboard = GetKeyboardName(lgws.Locale);
			else
				sKeyboard = lgws.KeymanKeyboard;
			string sSortUsing = null;
			string sSortRules = null;
			if (lgws.CollationsOS.Count > 0)
			{
				try
				{
					ILgCollation coll = lgws.CollationsOS[0];
					string sResName = coll.IcuResourceName;
					string sRules = coll.ICURules;
					int lcid = coll.WinLCID;
					if (!String.IsNullOrEmpty(sRules))
					{
						sSortUsing = "CustomICU";
						sSortRules = sRules;
					}
					else if (!String.IsNullOrEmpty(sResName))
					{
						sSortUsing = "OtherLanguage";
						sSortRules = sResName;
					}
					else if (lcid != 0)
					{
						sSortUsing = "OtherLanguage";
						System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo(lcid);
						sSortRules = ci.Name;
					}
				}
				catch
				{
					// This try-catch shouldn't be needed, but as LT-9545 points out, creating
					// the collation can crash for some non-repeatable reason.  It's happened
					// twice on my machine, and once each for a couple of testers.
				}
			}
			WritingSystemDefinition wsd = new WritingSystemDefinition(sLang);
			wsd.Script = sScript;
			wsd.Region = sCountry;
			wsd.Variant = sVariant;
			wsd.LanguageName = lgws.Name.UserDefaultWritingSystem;
			wsd.Abbreviation = lgws.Abbr.UserDefaultWritingSystem;
			wsd.RightToLeftScript = lgws.RightToLeft;
			wsd.DefaultFontName = lgws.DefaultSerif;
			wsd.DefaultFontSize = 12;		// pure guesswork - we need a stylesheet or a model change!
			wsd.Keyboard = sKeyboard;
			if (!String.IsNullOrEmpty(sSortUsing))
			{
				wsd.SortUsing = sSortUsing;
				wsd.SortRules = sSortRules;
			}
			//wsd.NativeName = null;
			//wsd.SpellCheckingId = null;
			//wsd.StoreID = null;
			//wsd.VersionDescription = null;
			//wsd.VersionNumber = null;
			try
			{
				ldmlstore.SaveDefinition(wsd);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(String.Format("Error writing LDML file: lgws.RFC={0}, wsd.RFC={1}; error: {2}",
					lgws.RFC4646bis, wsd.RFC4646, ex.Message));
			}
		}

		private string GetKeyboardName(int lcid)
		{
			try
			{
				ILgLanguageEnumerator lenum = LgLanguageEnumeratorClass.Create();
				lenum.Init();
				int id = 0;
				string name;
				do
				{
					lenum.Next(out id, out name);
					if (id == lcid)
						return name;
				} while (id != 0);
			}
			catch
			{
			}
			return null;
		}

		/// <summary>
		/// Allow custom final preparation before asking for file.  See LT-8403.
		/// </summary>
		/// <returns>true iff export can proceed further</returns>
		protected virtual bool PrepareForExport()
		{
			return true;
		}

		/// <summary>
		/// This version is overridden by (currently) Interlinear and Discourse Chart exports.
		/// </summary>
		/// <param name="outPath"></param>
		protected virtual void DoExport(string outPath)
		{
			DoExport(outPath, false);
		}

		protected void DoExport(string outPath, bool fLiftOutput)
		{
			string fxtPath = (string)m_exportList.SelectedItems[0].Tag;
			FxtType ft = m_rgFxtTypes[FxtIndex(fxtPath)];
			using (new SIL.FieldWorks.Common.Utils.WaitCursor(this))
			{
				using (ProgressDialogWithTask progressDlg = new ProgressDialogWithTask(this))
				{
					try
					{
						progressDlg.Title = String.Format(xWorksStrings.Exporting0,
							m_exportList.SelectedItems[0].SubItems[0].Text);
						progressDlg.Message = xWorksStrings.Exporting_;

						switch (ft.m_ft)
						{
							case FxtTypes.kftFxt:
								using (m_dumper = new XDumper(m_cache))
								{
									m_dumper.UpdateProgress +=
										new XDumper.ProgressHandler(OnDumperUpdateProgress);
									m_dumper.SetProgressMessage +=
										new EventHandler<XDumper.MessageArgs>(OnDumperSetProgressMessage);
									progressDlg.SetRange(0, m_dumper.GetProgressMaximum());
									progressDlg.CancelButtonVisible = true;
									progressDlg.Restartable = true;
									progressDlg.ProgressBarStyle = ProgressBarStyle.Continuous;
									progressDlg.Cancel += new CancelHandler(Onm_progressDlg_Cancel);

									progressDlg.RunTask(true, new BackgroundTaskInvoker(ExportFxt),
										outPath, fxtPath, fLiftOutput);
								}
								break;
							case FxtTypes.kftConfigured:
							case FxtTypes.kftReversal:
								progressDlg.SetRange(0, m_seqView.ObjectCount);
								progressDlg.CancelButtonVisible = true;
								progressDlg.Cancel += new CancelHandler(ce_Cancel);

								progressDlg.RunTask(true, new BackgroundTaskInvoker(ExportConfiguredDocView),
									outPath, fxtPath, ft);
								break;
						}
					}
					catch (WorkerThreadException e)
					{
						if (e.InnerException is CancelException)
						{
							MessageBox.Show(this, e.InnerException.Message);
							m_ce = null;
						}
						else
						{
							string msg = xWorksStrings.ErrorExporting_ProbablyBug + "\n" + e.InnerException.Message;
							MessageBox.Show(this, msg);
						}
					}
					finally
					{
						m_progressDlg = null;
						m_dumper = null;
						this.Close();
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exports as FXT.
		/// </summary>
		/// <param name="progressDialog">The progress dialog.</param>
		/// <param name="parameters">The parameters: 1) output file, 2) template file path.
		/// </param>
		/// <returns>Always null.</returns>
		/// ------------------------------------------------------------------------------------
		private object ExportFxt(IAdvInd4 progressDialog, params object[] parameters)
		{
			Debug.Assert(parameters.Length == 3);
			m_progressDlg = progressDialog;
			string outPath = (string)parameters[0];
			string fxtPath = (string)parameters[1];
			bool fLiftOutput = (bool)parameters[2];
			MapFlidsInDumperAsNeeded(fxtPath);
#if DEBUG
			DateTime dtStart = DateTime.Now;
#endif
			using (TextWriter w = new StreamWriter(outPath))
			{
				m_dumper.ExportPicturesAndMedia = m_fExportPicturesAndMedia;
				m_dumper.Go(m_cache.LangProject as CmObject, fxtPath, w);
			}
#if DEBUG
			DateTime dtExport = DateTime.Now;
#endif
			if (fLiftOutput)
			{
				try
				{
					progressDialog.Message = String.Format(xWorksStrings.ksValidatingOutputFile,
						Path.GetFileName(outPath));
					ValidationProgress prog = new ValidationProgress(progressDialog);
					LiftIO.Validation.Validator.CheckLiftWithPossibleThrow(outPath, prog);
				}
				catch (LiftIO.LiftFormatException lfe)
				{
					// Show the pretty yellow semi-crash dialog box, with instructions for the
					// user to report the bug.
					SIL.Utils.ErrorReporter.ReportException(
						new Exception(xWorksStrings.ksLiftExportBugReport, lfe), this, false);
				}
			}
#if DEBUG
			DateTime dtValidate = DateTime.Now;
			TimeSpan exportDelta = new TimeSpan(dtExport.Ticks - dtStart.Ticks);
			TimeSpan validateDelta = new TimeSpan(dtValidate.Ticks - dtExport.Ticks);
			Debug.WriteLine(String.Format("Export time = {0}, Validation time = {1}",
				exportDelta, validateDelta));
#endif
			return null;
		}

		private void MapFlidsInDumperAsNeeded(string fxtPath)
		{
			XmlDocument document = new XmlDocument();
			document.Load(fxtPath);
			XmlNode xnDoc = document.SelectSingleNode(".//FxtDocumentDescription");
			string sDataLabel = XmlUtils.GetAttributeValue(xnDoc, "dataLabel");
			// A more robust check for the type of fxt output and the clerk would be nice...
			if (sDataLabel.ToLowerInvariant() == "wordforms")
			{
				RecordClerk clerk = RecordClerk.FindClerk(m_mediator, "concordanceWords");
				if (clerk != null)
				{
					int hvoWFI = m_cache.GetObjProperty(m_cache.LangProject.Hvo, (int)SIL.FieldWorks.FDO.LangProj.LangProject.LangProjectTags.kflidWordformInventory);
					int cwfi = m_cache.GetVectorSize(hvoWFI, (int)WordformInventory.WordformInventoryTags.kflidWordforms);
					int cwfi2 = m_cache.GetVectorSize(clerk.OwningObject.Hvo, clerk.OwningFlid);
					if (cwfi != cwfi2)
					{
						m_dumper.MapFlids((int)WordformInventory.WordformInventoryTags.kflidWordforms, clerk.OwningFlid);
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exports the configured doc view.
		/// </summary>
		/// <param name="progressDlg">The progress dialog.</param>
		/// <param name="parameters">The parameters.</param>
		/// <returns>Always null.</returns>
		/// ------------------------------------------------------------------------------------
		private object ExportConfiguredDocView(IAdvInd4 progressDlg, params object[] parameters)
		{
			Debug.Assert(parameters.Length == 3);
			m_progressDlg = progressDlg;
			if (m_xvc == null)
				return null;

			string outPath = (string)parameters[0];
			string fxtPath = (string)parameters[1];
			FxtType ft = (FxtType)parameters[2];

			try
			{
				m_cache.EnableBulkLoadingIfPossible(true);

				using (TextWriter w = new StreamWriter(outPath))
				{
					FileInfo outFile = new FileInfo(outPath);
#if DEBUG
					string dirPath = Path.GetTempPath();
					int copyCount = 1;
					string s = string.Format("Starting Configured Export at {0}",
						System.DateTime.Now.ToLongTimeString());
					Debug.WriteLine(s);
#endif
					m_ce = new ConfiguredExport(null, m_cache.MainCacheAccessor, m_hvoRootObj);
					m_ce.Initialize(m_cache, w, ft.m_sDataType, ft.m_sFormat, outPath);
					m_ce.UpdateProgress += new SIL.FieldWorks.Common.Controls.ConfiguredExport.ProgressHandler(ce_UpdateProgress);
					m_xvc.Display(m_ce, m_hvoRootObj, m_seqView.RootFrag);
					m_ce.Finish(ft.m_sDataType);
					w.Close();
#if DEBUG
					s = string.Format("Finished Configured Export Dump at {0}",
						System.DateTime.Now.ToLongTimeString());
					Debug.WriteLine(s);
#endif
					if (ft.m_sXsltFiles != null && ft.m_sXsltFiles.Length != 0)
					{
						string[] rgsXslts = ft.m_sXsltFiles.Split(new char[] { ';' });
						int cXslts = rgsXslts.GetLength(0);
						progressDlg.Position = 0;
						progressDlg.SetRange(0, cXslts);
						progressDlg.Message =
							xWorksStrings.ProcessingIntoFinalForm;
						int idx = fxtPath.LastIndexOfAny(new char[] { '/', '\\' });
						if (idx < 0)
							idx = 0;
						else
							++idx;
						string basePath = fxtPath.Substring(0, idx);
						for (int ix = 0; ix <= cXslts; ++ix)
						{
#if DEBUG
							File.Copy(outPath, Path.Combine(dirPath, "DebugOnlyExportStage" + copyCount + ".txt"), true);
							copyCount++;
							if (ix < cXslts)
								s = String.Format("Starting Configured Export XSLT file {0} at {1}",
									rgsXslts[ix], System.DateTime.Now.ToLongTimeString());
							else
								s = String.Format("Starting final postprocess phase at {0}",
									System.DateTime.Now.ToLongTimeString());
							Debug.WriteLine(s);
#endif
							if (ix < cXslts)
							{
								string sXsltPath = basePath + rgsXslts[ix];
								m_ce.PostProcess(sXsltPath, outPath, ix + 1);
							}
							else
							{
								m_ce.PostProcess(null, outPath, ix + 1);
							}
							progressDlg.Step(0);
						}
					}

					if (ft.m_sFormat.ToLowerInvariant() == "xhtml")
					{
						IVwStylesheet vss = null;
						if (m_seqView.RootBox != null)
							vss = m_seqView.RootBox.Stylesheet;
						m_ce.WriteCssFile(Path.ChangeExtension(outPath, ".css"), vss);
					}
					m_ce = null;
#if DEBUG
					File.Copy(outPath, Path.Combine(dirPath, "DebugOnlyExportStage" + copyCount + ".txt"), true);
					s = string.Format("Totally Finished Configured Export at {0}",
						System.DateTime.Now.ToLongTimeString());
					Debug.WriteLine(s);
#endif
				}
			}
			finally
			{
				m_cache.EnableBulkLoadingIfPossible(false);
			}
			return null;
		}

		private void btnCancel_Click(object sender, System.EventArgs e)
		{
			this.Close();
		}

		private void ExportDialog_Load(object sender, System.EventArgs e)
		{
			string p = Path.Combine(SIL.FieldWorks.Common.Utils.DirectoryFinder.FWCodeDirectory, ConfigurationFilePath);
			if (Directory.Exists(p))
				AddFxts(Directory.GetFiles(p, "*.xml"));
		}

		protected virtual string ConfigurationFilePath
		{
			get { return @"Language Explorer\Export Templates"; }
		}

		protected void AddFxts(string[] filePaths)
		{
			Debug.Assert(filePaths != null);

			foreach (string path in filePaths)
			{
				if (path.EndsWith(".xml~"))
					continue;	// ignore editor backup files.
				XmlDocument document = new XmlDocument();
				// If we have an xml file that can't be loaded, ignore it.
				try
				{
					document.Load(path);
				}
				catch
				{
					continue;
				}
				XmlNode node = document.SelectSingleNode("//FxtDocumentDescription");
				if (node == null)
					continue;
				string dataLabel = XmlUtils.GetOptionalAttributeValue(node,"dataLabel", "unknown");
				string formatLabel = XmlUtils.GetOptionalAttributeValue(node,"formatLabel", "unknown");
				string defaultExtension = XmlUtils.GetOptionalAttributeValue(node,"defaultExtension", "txt");
				string sDefaultFilter = ResourceHelper.FileFilter(FileFilterType.AllFiles);
				string filter = XmlUtils.GetOptionalAttributeValue(node,"filter", sDefaultFilter);
				string description = node.InnerText;
				if (description!=null)
					description = description.Trim();
				if (description==null || description.Length==0)
					description = xWorksStrings.NoDescriptionForItem;
				ListViewItem item = new ListViewItem(new string[]{dataLabel, formatLabel, defaultExtension, filter, description});
				item.Tag = path;
				m_exportList.Items.Add(item);
				ConfigureItem(document, item, node);
			}

			// Select the first available one
			foreach (ListViewItem lvi in m_exportList.Items)
			{
				if (!ItemDisabled((string)lvi.Tag))
				{
					lvi.Selected = true;
					break;
				}
			}

		}

		/// <summary>
		/// Store the attributes of the <template> element.
		/// Override (often to do nothing) if not configuring an FXT export process.
		/// </summary>
		/// <param name="document"></param>
		/// <param name="item"></param>
		/// <param name="ddNode"></param>
		protected virtual void ConfigureItem(XmlDocument document, ListViewItem item, XmlNode ddNode)
		{
			XmlNode templateRootNode = document.SelectSingleNode("//template");
			Debug.Assert(templateRootNode != null, "FXT files must always have a <template> node somewhere.");
			FxtType ft;
			ft.m_sFormat = XmlUtils.GetOptionalAttributeValue(templateRootNode, "format", "xml");
			string sType = XmlUtils.GetOptionalAttributeValue(templateRootNode, "type", "fxt");
			switch (sType)
			{
				case "fxt":
					ft.m_ft = FxtTypes.kftFxt;
					break;
				case "configured":
					ft.m_ft = FxtTypes.kftConfigured;
					break;
				case "reversal":
					ft.m_ft = FxtTypes.kftReversal;
					break;
				default:
					Debug.Assert(false, "Invalid type attribute value for the template element");
					ft.m_ft = FxtTypes.kftFxt;
					break;
			}
			ft.m_sDataType = XmlUtils.GetOptionalAttributeValue(templateRootNode, "datatype", "UNKNOWN");
			ft.m_sXsltFiles = XmlUtils.GetOptionalAttributeValue(templateRootNode, "xslt", null);
			ft.m_path = (string)item.Tag;
			m_rgFxtTypes.Add(ft);
			// We can't actually disable a list item, but we can make it look and act like it's
			// disabled.
			if (ItemDisabled(ft.m_ft))
				item.ForeColor = SystemColors.GrayText;
		}

		private void m_exportList_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if (m_exportList.SelectedItems.Count == 0)
				return;

			m_fExportPicturesAndMedia = false;
			m_description.Text = m_exportList.SelectedItems[0].SubItems[4].Text;
			if (ItemDisabled((string)m_exportList.SelectedItems[0].Tag))
			{
				m_description.ForeColor = SystemColors.GrayText;
				btnExport.Enabled = false;
				m_chkExportPictures.Visible = false;
				m_chkExportPictures.Enabled = false;
			}
			else
			{
				m_description.ForeColor = SystemColors.ControlText;
				btnExport.Enabled = true;
				if (m_exportList.SelectedItems[0].SubItems[2].Text == "lift")
				{
					m_chkExportPictures.Visible = true;
					m_chkExportPictures.Enabled = true;
					if (!m_fLiftExportPicturesSet)
					{
						m_chkExportPictures.Checked = m_mediator.PropertyTable.GetBoolProperty(ksLiftExportPicturesPropertyName, true);
						m_fLiftExportPicturesSet = true;
					}
					m_fExportPicturesAndMedia = m_chkExportPictures.Checked;
				}
				else
				{
					m_chkExportPictures.Visible = false;
					m_chkExportPictures.Enabled = false;
				}
			}
		}

		protected int FxtIndex(string tag)
		{
			for (int i = 0; i < m_rgFxtTypes.Count; i++)
			{
				if (m_rgFxtTypes[i].m_path == tag)
					return i;
			}
			return 0;
		}

		protected virtual bool ItemDisabled(string tag)
		{
			return ItemDisabled(m_rgFxtTypes[FxtIndex(tag)].m_ft);
		}

		private bool ItemDisabled(FxtTypes ft)
		{
			switch (ft)
			{
				case FxtTypes.kftFxt:
					return false;
				case FxtTypes.kftConfigured:
					return m_xvc == null || m_clidRootObj != LexDb.kclsidLexDb;
				case FxtTypes.kftReversal:
					return m_xvc == null || m_clidRootObj != ReversalIndex.kclsidReversalIndex;
				default:
					return true;
			}
		}

		private void Onm_progressDlg_Cancel(object sender)
		{
			if (m_dumper != null)
				m_dumper.Cancel();
		}

		private void OnDumperUpdateProgress(object sender)
		{
			Debug.Assert(m_progressDlg != null);
			m_progressDlg.Step(0);
		}

		private void OnDumperSetProgressMessage(object sender, XDumper.MessageArgs e)
		{
			Debug.Assert(m_progressDlg != null);
			string sMsg = xWorksStrings.ResourceManager.GetString(e.MessageId, xWorksStrings.Culture);
			if (!String.IsNullOrEmpty(sMsg))
				m_progressDlg.Message = sMsg;
			m_progressDlg.Position = 0;
			m_progressDlg.SetRange(0, e.Max);
		}

		private void buttonHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(FwApp.App, m_helpTopic);
		}

		private void ce_UpdateProgress(object sender)
		{
			Debug.Assert(m_progressDlg != null);
			m_progressDlg.Step(0);
		}

		private void ce_Cancel(object sender)
		{
			if (m_ce != null)
				m_ce.Cancel();
		}

		private void m_chkExportPictures_CheckedChanged(object sender, EventArgs e)
		{
			m_mediator.PropertyTable.SetProperty(ksLiftExportPicturesPropertyName, m_chkExportPictures.Checked);
			m_mediator.PropertyTable.SetPropertyPersistence(ksLiftExportPicturesPropertyName, true);
			m_fExportPicturesAndMedia = m_chkExportPictures.Checked;
		}

		private void m_description_LinkClicked(object sender, LinkClickedEventArgs e)
		{
			Process.Start(e.LinkText);
		}
	}
}
