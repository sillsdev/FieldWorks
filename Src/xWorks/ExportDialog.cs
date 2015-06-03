// Copyright (c) 2005-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ExportDialog.cs
// Responsibility: Steve McConnel
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Xsl;
using Microsoft.Win32;

using SIL.Lift;
using SIL.Lift.Validation;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.FdoUi;
using SIL.Utils;
using SIL.Utils.FileDialog;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.FXT;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.LexText.Controls;
using XCore;

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
		protected FdoCache m_cache;
		protected Mediator m_mediator;
		private Label label1;
		protected ColumnHeader columnHeader1;
		protected ColumnHeader columnHeader2;
		private Button btnExport;
		private Button btnCancel;
		protected ListView m_exportList;
		private RichTextBox m_description;
		private XDumper m_dumper;
		protected IThreadedProgress m_progressDlg;
		private Button buttonHelp;

		protected string m_helpTopic;
		private HelpProvider helpProvider;

		// ReSharper disable InconsistentNaming
		// This stores the values of the format, configured, filtered, and sorted attributes of
		// the toplevel <template> element.
		protected internal enum FxtTypes
		{
			kftFxt = 0,
			kftConfigured = 1,
			kftReversal = 2,
			kftTranslatedLists = 3,
			kftPathway = 4,
			kftLift = 5,
			kftGrammarSketch,
			kftClassifiedDict,
			kftSemanticDomains
		}
		// ReSharper restore InconsistentNaming
		protected internal struct FxtType
		{
			public string m_sFormat;
			public FxtTypes m_ft;
			public bool m_filtered;
			public string m_sDataType;
			public string m_sXsltFiles;
			public string m_path; // Used to keep track of items after they are sorted.
		}
		protected List<FxtType> m_rgFxtTypes = new List<FxtType>(8);

		protected ConfiguredExport m_ce = null;
		protected XmlSeqView m_seqView = null;
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
		/// <summary>
		/// The data access is needed if we're doing a filtered FXT export.  (See FWR-1223.)
		/// </summary>
		ISilDataAccess m_sda = null;
		/// <summary>
		/// The clerk is needed if we're doing a filtered FXT export.  (See FWR-1223.)
		/// </summary>
		RecordClerk m_clerk = null;

		private const string ksLiftExportPicturesPropertyName = "LIFT-ExportPictures";
		/// <summary>
		/// Store the active area from which this dialog was called.
		/// </summary>
		string m_areaOrig;
		private CheckBox m_chkShowInFolder;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		private List<ListViewItem> m_exportItems;

		/// <summary>
		/// For testing only!
		/// </summary>
		internal ExportDialog()
		{
		}

		public ExportDialog(Mediator mediator)
		{
			m_mediator = mediator;
			m_cache = (FdoCache) mediator.PropertyTable.GetValue("cache");

			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			AccessibleName = GetType().Name;

			// Figure out where to locate the dlg.
			object obj = SettingsKey.GetValue("InsertX");
			if (obj != null)
			{
				var x = (int) obj;
				var y = (int) SettingsKey.GetValue("InsertY");
				var width = (int) SettingsKey.GetValue("InsertWidth", Width);
				var height = (int) SettingsKey.GetValue("InsertHeight", Height);
				var rect = new Rectangle(x, y, width, height);
				ScreenUtils.EnsureVisibleRect(ref rect);
				DesktopBounds = rect;
				StartPosition = FormStartPosition.Manual;
			}

			m_helpTopic = "khtpExportLexicon";

			helpProvider = new HelpProvider();
			helpProvider.HelpNamespace = mediator.HelpTopicProvider.HelpFile;
			helpProvider.SetHelpKeyword(this, m_mediator.HelpTopicProvider.GetHelpString(m_helpTopic));
			helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);

			// Determine whether we can support "configured" type export by trying to obtain
			// the XmlVc for an XmlDocView.  Also obtain the database id and class id of the
			// root object.

			object objCurrentControl;
			objCurrentControl = m_mediator.PropertyTable.GetValue("currentContentControlObject", null);
			InitFromMainControl(objCurrentControl);
			m_clerk = m_mediator.PropertyTable.GetValue("ActiveClerk", null) as RecordClerk;

			m_chkExportPictures.Checked = false;
			m_chkExportPictures.Visible = false;
			m_chkExportPictures.Enabled = false;
			m_fExportPicturesAndMedia = false;

			//Set  m_chkShowInFolder to it's last state.
			var showInFolder = m_mediator.PropertyTable.GetStringProperty("ExportDlgShowInFolder", "true");
			if (showInFolder.Equals("true"))
				m_chkShowInFolder.Checked = true;
			else
				m_chkShowInFolder.Checked = false;

			m_exportItems = new List<ListViewItem>();
		}

		private void InitFromMainControl(object objCurrentControl)
		{
			XmlDocView docView = FindXmlDocView(objCurrentControl as Control);
			if (docView != null)
				m_seqView = docView.Controls[0] as XmlSeqView;
			if (m_seqView != null)
			{
				m_xvc = m_seqView.Vc;
				m_sda = m_seqView.RootBox.DataAccess;
			}
			var cmo = m_mediator.PropertyTable.GetValue("ActiveClerkSelectedObject", null) as ICmObject;
			if (cmo != null)
			{
				int clidRoot;
				var newHvoRoot = SetRoot(cmo, out clidRoot);
				if (newHvoRoot > 0)
				{
					m_hvoRootObj = newHvoRoot;
					m_clidRootObj = clidRoot;
				}
			}

			XmlBrowseView browseView = FindXmlBrowseView(objCurrentControl as Control);
			if (browseView != null)
				m_sda = browseView.RootBox.DataAccess;
		}

		/// <summary>
		/// Allows process to find an appropriate root hvo and change the current root.
		/// Subclasses (e.g. NotebookExportDialog) can override.
		/// </summary>
		/// <param name="cmo"></param>
		/// <param name="clidRoot"></param>
		/// <returns>Returns -1 if root hvo doesn't need changing.</returns>
		protected virtual int SetRoot(ICmObject cmo, out int clidRoot)
		{
			clidRoot = -1;
			var hvoRoot = -1;
			// Handle LexEntries that no longer have owners.
			if (cmo is ILexEntry)
			{
				hvoRoot = m_cache.LanguageProject.LexDbOA.Hvo;
				clidRoot = m_cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries;
			}
			else if (cmo is ICmSemanticDomain)
			{
				hvoRoot = cmo.OwnerOfClass<ICmPossibilityList>().Hvo;
				clidRoot = CmPossibilityListTags.kClassId;
			}
			else if (cmo.Owner != null)
			{
				hvoRoot = cmo.Owner.Hvo;
				clidRoot = cmo.Owner.ClassID;
			}
			return hvoRoot;
		}

		/// <summary>
		/// If one exists, find an XmlDocView control no matter how deeply it's embedded.
		/// </summary>
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
		/// If one exists, find an XmlBrowseView control no matter how deeply it's embedded.
		/// </summary>
		private XmlBrowseView FindXmlBrowseView(Control control)
		{
			if (control == null)
				return null;
			if (control is XmlBrowseView)
				return control as XmlBrowseView;
			foreach (Control c in control.Controls)
			{
				XmlBrowseView xbv = FindXmlBrowseView(c);
				if (xbv != null)
					return xbv;
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
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
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
			this.m_chkShowInFolder = new System.Windows.Forms.CheckBox();
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
			// m_chkShowInFolder
			//
			resources.ApplyResources(this.m_chkShowInFolder, "m_chkShowInFolder");
			this.m_chkShowInFolder.Checked = true;
			this.m_chkShowInFolder.CheckState = System.Windows.Forms.CheckState.Checked;
			this.m_chkShowInFolder.Name = "m_chkShowInFolder";
			this.m_chkShowInFolder.UseVisualStyleBackColor = true;
			//
			// ExportDialog
			//
			this.AcceptButton = this.btnExport;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.btnCancel;
			this.Controls.Add(this.m_chkShowInFolder);
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
			this.Closed += new System.EventHandler(this.ExportDialog_Closed);
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		/// <summary>
		/// In case the requested export requires a particular view and we aren't showing it, create a temporary one
		/// and update the appropriate variables from it.
		/// If this returns a non-null value, it is a newly created object which must be disposed. (See LT-11066.)
		/// </summary>
		Control EnsureViewInfo()
		{
			string area = "lexicon";
			string tool = "lexiconDictionary";
			m_areaOrig = m_mediator.PropertyTable.GetStringProperty("areaChoice", null);
			if (m_rgFxtTypes.Count == 0)
				return null; // only non-Fxt exports available (like Discourse chart?)
			// var ft = m_rgFxtTypes[FxtIndex((string) m_exportList.SelectedItems[0].Tag)].m_ft;
			var ft = m_rgFxtTypes[FxtIndex((string)m_exportItems[0].Tag)].m_ft;
			if (m_areaOrig == "notebook")
			{
				if (ft != FxtTypes.kftConfigured)
					return null;	// nothing to do.
				area = m_areaOrig;
				tool = "notebookDocument";
			}
			else
			{
				switch (ft)
				{
					case FxtTypes.kftConfigured:
						break;
					case FxtTypes.kftReversal:
						tool = "reversalToolEditComplete";
						break;
					case FxtTypes.kftClassifiedDict:
						// Should match the tool in DistFiles/Language Explorer/Configuration/RDE/toolConfiguration.xml, the value attribute in
						// <tool label="Classified Dictionary" value="lexiconClassifiedDictionary" icon="DocumentView">.
						// We use this to create that tool and base this export on its objects and saved configuration.
						tool = "lexiconClassifiedDictionary";
						break;
					case FxtTypes.kftGrammarSketch:
						area = "grammar";
						tool = "grammarSketch";
						break;
					default:
						return null; // nothing to do.
				}
			}
			var collector = new XmlNode[1];
			var parameter = new Tuple<string, string, XmlNode[]>(area, tool, collector);
			m_mediator.SendMessage("GetContentControlParameters", parameter);
			var controlNode = collector[0];
			Debug.Assert(controlNode != null);
			XmlNode dynLoaderNode = controlNode.SelectSingleNode("dynamicloaderinfo");
			var contentAssemblyPath = XmlUtils.GetAttributeValue(dynLoaderNode, "assemblyPath");
			var contentClass = XmlUtils.GetAttributeValue(dynLoaderNode, "class");
			Control mainControl = (Control)DynamicLoader.CreateObject(contentAssemblyPath, contentClass);
			var parameters = controlNode.SelectSingleNode("parameters");
			((IxCoreColleague)mainControl).Init(m_mediator, parameters);
			InitFromMainControl(mainControl);
			return mainControl;
		}

		List<int> m_translationWritingSystems;
		List<ICmPossibilityList> m_translatedLists;
		private bool m_allQuestions; // For semantic domains, export missing translations as English?

		private void btnExport_Click(object sender, EventArgs e)
		{
			if (m_exportList.SelectedItems.Count == 0)
				return;

			//if (ItemDisabled((string)m_exportList.SelectedItems[0].Tag))
			//    return;
			m_exportItems.Clear();
			foreach (ListViewItem sel in m_exportList.SelectedItems)
				m_exportItems.Add(sel);
			var mainControl = EnsureViewInfo();
			try
			{

				if (!PrepareForExport())
					return;

				bool fLiftExport = m_exportItems[0].SubItems[2].Text == "lift";
				string sFileName;
				string sDirectory;
				if (fLiftExport)
				{
					using (var dlg = new FolderBrowserDialogAdapter())
					{
						dlg.Tag = xWorksStrings.ksChooseLIFTFolderTitle; // can't set title !!??
						dlg.Description = String.Format(xWorksStrings.ksChooseLIFTExportFolder,
							m_exportItems[0].SubItems[1].Text);
						dlg.ShowNewFolderButton = true;
						dlg.RootFolder = Environment.SpecialFolder.Desktop;
						dlg.SelectedPath = m_mediator.PropertyTable.GetStringProperty("ExportDir",
							Environment.GetFolderPath(Environment.SpecialFolder.Personal));
						if (dlg.ShowDialog(this) != DialogResult.OK)
							return;
						sDirectory = dlg.SelectedPath;
					}
					string sFile = Path.GetFileName(sDirectory);
					sFileName = Path.Combine(sDirectory, sFile + FwFileExtensions.ksLexiconInterchangeFormat);
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
					FxtType ft;
					// Note that DiscourseExportDialog doesn't add anything to m_rgFxtTypes.
					// See FWR-2506.
					if (m_rgFxtTypes.Count > 0)
					{
						string fxtPath = (string) m_exportItems[0].Tag;
						ft = m_rgFxtTypes[FxtIndex(fxtPath)];
					}
					else
					{
						// Choose a dummy value that will take the default branch of merely choosing
						// an output file.
						ft.m_ft = FxtTypes.kftConfigured;
					}
					switch (ft.m_ft)
					{
						case FxtTypes.kftTranslatedLists:
							using (var dlg = new ExportTranslatedListsDlg())
							{
								dlg.Initialize(m_mediator, m_cache,
									m_exportItems[0].SubItems[1].Text,
									m_exportItems[0].SubItems[2].Text,
									m_exportItems[0].SubItems[3].Text);
								if (dlg.ShowDialog(this) != DialogResult.OK)
									return;
								sFileName = dlg.FileName;
								sDirectory = Path.GetDirectoryName(sFileName);
								m_translationWritingSystems = dlg.SelectedWritingSystems;
								m_translatedLists = dlg.SelectedLists;
							}
							break;
						case FxtTypes.kftSemanticDomains:
							using (var dlg = new ExportSemanticDomainsDlg())
							{
								dlg.Initialize(m_cache);
								if (dlg.ShowDialog(this) != DialogResult.OK)
									return;
								m_translationWritingSystems = new List<int>();
								m_translationWritingSystems.Add(dlg.SelectedWs);
								m_allQuestions = dlg.AllQuestions;
							}
							goto default;
						case FxtTypes.kftPathway:
							ProcessPathwayExport();
							return;
						default:
							using (var dlg = new SaveFileDialogAdapter())
							{
								dlg.AddExtension = true;
								dlg.DefaultExt = m_exportItems[0].SubItems[2].Text;
								dlg.Filter = m_exportItems[0].SubItems[3].Text;
								dlg.Title = String.Format(xWorksStrings.ExportTo0, m_exportItems[0].SubItems[1].Text);
								dlg.InitialDirectory = m_mediator.PropertyTable.GetStringProperty("ExportDir",
									Environment.GetFolderPath(Environment.SpecialFolder.Personal));
								if (dlg.ShowDialog(this) != DialogResult.OK)
									return;
								sFileName = dlg.FileName;
								sDirectory = Path.GetDirectoryName(sFileName);
							}
							break;
					}
				}
				if (sDirectory != null)
				{
					m_mediator.PropertyTable.SetProperty("ExportDir", sDirectory);
					m_mediator.PropertyTable.SetPropertyPersistence("ExportDir", true);
				}
				if (fLiftExport) // Fixes LT-9437 Crash exporting a discourse chart (or interlinear too!)
				{
					DoExport(sFileName, true);
				}
				else
				{
					DoExport(sFileName); // Musn't use the 2 parameter version here or overrides get messed up.
				}
				if (m_chkShowInFolder.Checked)
				{
					OpenExportFolder(sDirectory, sFileName);
					m_mediator.PropertyTable.SetProperty("ExportDlgShowInFolder", "true");
					m_mediator.PropertyTable.SetPropertyPersistence("ExportDlgShowInFolder", true);
				}
				else
				{
					m_mediator.PropertyTable.SetProperty("ExportDlgShowInFolder", "false");
					m_mediator.PropertyTable.SetPropertyPersistence("ExportDlgShowInFolder", true);
				}
			}
			finally
			{
				if (mainControl != null)
					mainControl.Dispose();
			}
		}

		private static void OpenExportFolder(string sDirectory, string sFileName)
		{
			ProcessStartInfo processInfo = null;
			if (Environment.OSVersion.Platform == PlatformID.Unix)
			{
				// if it exists, xdg-open uses the user's preference for opening directories
				if (File.Exists("/usr/bin/xdg-open"))
				{
					processInfo = new ProcessStartInfo("/usr/bin/xdg-open", String.Format("\"{0}\"", sDirectory));
				}
				else if (File.Exists("/usr/bin/nautilus"))
				{
					processInfo = new ProcessStartInfo("/usr/bin/nautilus", String.Format("\"{0}\"", sDirectory));
				}
				else if (File.Exists("/usr/bin/krusader"))
				{
					processInfo = new ProcessStartInfo("/usr/bin/krusader", String.Format("\"{0}\"", sDirectory));
				}
				else if (File.Exists("/usr/bin/pcmanfm"))
				{
					processInfo = new ProcessStartInfo("/usr/bin/pcmanfm", String.Format("\"{0}\"", sDirectory));
				}
				else if (File.Exists("/usr/bin/gnome-commander"))
				{
					processInfo = new ProcessStartInfo("/usr/bin/gnome-commander",
						String.Format("-l \"{0}\" -r \"{1}\"", Path.GetDirectoryName(sDirectory), sDirectory));
				}
				// If the user doesn't have one of these programs installed, I give up!
			}
			else
			{
				// REVIEW: What happens if directory or filename contain spaces?
				var program = Environment.ExpandEnvironmentVariables(@"%WINDIR%\explorer.exe");
				if (program == @"\explorer.exe")
					program = @"C:\windows\explorer.exe";
				if (String.IsNullOrEmpty(sFileName))
				{
					processInfo = new ProcessStartInfo(program, String.Format(" /select,{0}", sDirectory));
				}
				else
				{
					processInfo = new ProcessStartInfo(program, String.Format(" /select,{0}", sFileName));
				}
			}
			if (processInfo != null)
			{
				using (Process.Start(processInfo))
				{
				}
			}
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
			string fxtPath = (string)m_exportItems[0].Tag;
			FxtType ft = m_rgFxtTypes[FxtIndex(fxtPath)];
			using (new WaitCursor(this))
			{
				using (var progressDlg = new ProgressDialogWithTask(this))
				{
					try
					{
						progressDlg.Title = String.Format(xWorksStrings.Exporting0,
							m_exportItems[0].SubItems[0].Text);
						progressDlg.Message = xWorksStrings.Exporting_;

						switch (ft.m_ft)
						{
							case FxtTypes.kftFxt:
								m_dumper = new XDumper(m_cache);
								m_dumper.UpdateProgress += OnDumperUpdateProgress;
								m_dumper.SetProgressMessage += OnDumperSetProgressMessage;
								progressDlg.Minimum = 0;
								progressDlg.Maximum = m_dumper.GetProgressMaximum();
								progressDlg.AllowCancel = true;
								progressDlg.Restartable = true;

								progressDlg.RunTask(true, ExportFxt, outPath, fxtPath, fLiftOutput);
								break;
							case FxtTypes.kftConfigured:
							case FxtTypes.kftReversal:
							case FxtTypes.kftClassifiedDict:
								progressDlg.Minimum = 0;
								progressDlg.Maximum = m_seqView.ObjectCount;
								progressDlg.AllowCancel = true;

								IVwStylesheet vss = m_seqView.RootBox == null ? null : m_seqView.RootBox.Stylesheet;
								progressDlg.RunTask(true, ExportConfiguredDocView,
									outPath, fxtPath, ft, vss);
								break;
							case FxtTypes.kftTranslatedLists:
								progressDlg.Minimum = 0;
								progressDlg.Maximum = m_translatedLists.Count;
								progressDlg.AllowCancel = true;

								progressDlg.RunTask(true, ExportTranslatedLists, outPath);
								break;
							case FxtTypes.kftSemanticDomains:
								// Potentially, we could count semantic domains and try to make the export update for each.
								// In practice this only takes a second or two on a typical modern computer
								// an the main export routine is borrowed from kftTranslatedLists and set up to count each
								// list as one step. For now, claiming this export just has one step seems good enough.
								progressDlg.Minimum = 0;
								progressDlg.Maximum = 1;
								progressDlg.AllowCancel = true;

								progressDlg.RunTask(true, ExportSemanticDomains, outPath, ft, fxtPath, m_allQuestions);
								break;
							case FxtTypes.kftPathway:
								break;
							case FxtTypes.kftLift:
								progressDlg.Minimum = 0;
								progressDlg.Maximum = 1000;
								progressDlg.AllowCancel = true;
								progressDlg.Restartable = true;
								progressDlg.RunTask(true, ExportLift, outPath, ft.m_filtered);
								break;
							case FxtTypes.kftGrammarSketch:
								progressDlg.Minimum = 0;
								progressDlg.Maximum = 1000;
								progressDlg.AllowCancel = true;
								progressDlg.Restartable = true;
								progressDlg.RunTask(true, ExportGrammarSketch, outPath, ft.m_sDataType, ft.m_sXsltFiles);
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
						else if (e.InnerException is LiftFormatException)
						{
							// Show the pretty yellow semi-crash dialog box, with instructions for the
							// user to report the bug.
							var app = (IApp) m_mediator.PropertyTable.GetValue("App");
							ErrorReporter.ReportException(new Exception(xWorksStrings.ksLiftExportBugReport, e.InnerException),
								app.SettingsKey, m_mediator.FeedbackInfoProvider.SupportEmailAddress, this, false);
						}
						else
						{
							string msg = xWorksStrings.ErrorExporting_ProbablyBug + Environment.NewLine + e.InnerException.Message;
							MessageBox.Show(this, msg);
						}
					}
					finally
					{
						m_progressDlg = null;
						m_dumper = null;
						Close();
					}
				}
			}
		}

		private object ExportGrammarSketch(IThreadedProgress progress, object[] args)
		{
			var outPath = (string)args[0];
			var sDataType = (string) args[1];
			var sXslts = (string) args[2];
			m_progressDlg = progress;
			var parameter = new Tuple<string, string, string>(sDataType, outPath, sXslts);
			m_mediator.SendMessage("SaveAsWebpage", parameter);
			m_progressDlg.Step(1000);
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exports as a LIFT file (possibly with one or more range files.
		/// </summary>
		/// <param name="progress">The progress dialog.</param>
		/// <param name="args">The parameters: (only 1) the output file pathname.
		/// </param>
		/// <returns>Always null.</returns>
		/// ------------------------------------------------------------------------------------
		private object ExportLift(IThreadedProgress progress, object[] args)
		{
			var outPath = (string)args[0];
			var filtered = (bool)args[1];
			m_progressDlg = progress;
			var exporter = new LiftExporter(m_cache);
			exporter.UpdateProgress += OnDumperUpdateProgress;
			exporter.SetProgressMessage += OnDumperSetProgressMessage;
			exporter.ExportPicturesAndMedia = m_fExportPicturesAndMedia;
#if DEBUG
			var dtStart = DateTime.Now;
#endif
			using (TextWriter w = new StreamWriter(outPath))
			{
				if (filtered)
				{
					exporter.ExportLift(w, Path.GetDirectoryName(outPath), m_clerk.VirtualListPublisher, m_clerk.VirtualFlid);
				}
				else
				{
					exporter.ExportLift(w, Path.GetDirectoryName(outPath));
				}
			}
			var outPathRanges = Path.ChangeExtension(outPath, ".lift-ranges");
			using (var w =  new StringWriter())
			{
				exporter.ExportLiftRanges(w);
				using (var sw = new StreamWriter(outPathRanges))
				{
					//actually write out to file
					sw.Write(w.GetStringBuilder().ToString());
					sw.Close();
				}
			}
#if DEBUG
			var dtExport = DateTime.Now;
#endif
			progress.Message = String.Format(xWorksStrings.ksValidatingOutputFile,
					Path.GetFileName(outPath));
			Validator.CheckLiftWithPossibleThrow(outPath);
#if DEBUG
			var dtValidate = DateTime.Now;
			var exportDelta = new TimeSpan(dtExport.Ticks - dtStart.Ticks);
			var validateDelta = new TimeSpan(dtValidate.Ticks - dtExport.Ticks);
			Debug.WriteLine(String.Format("Export time = {0}, Validation time = {1}",
				exportDelta, validateDelta));
#endif
			return null;
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
		private object ExportFxt(IThreadedProgress progressDialog, object[] parameters)
		{
			Debug.Assert(parameters.Length == 3);
			m_progressDlg = progressDialog;
			string outPath = (string)parameters[0];
			string fxtPath = (string)parameters[1];
			bool fLiftOutput = (bool)parameters[2];
#if DEBUG
			DateTime dtStart = DateTime.Now;
#endif
			using (TextWriter w = new StreamWriter(outPath))
			{
				m_dumper.ExportPicturesAndMedia = m_fExportPicturesAndMedia;
				if (m_sda != null && m_clerk != null)
				{
					m_dumper.VirtualDataAccess = m_sda;
					m_dumper.VirtualFlid = m_clerk.VirtualFlid;
				}
				m_dumper.Go(m_cache.LangProject, fxtPath, w);
			}
#if DEBUG
			DateTime dtExport = DateTime.Now;
#endif
			if (fLiftOutput)
			{
				progressDialog.Message = String.Format(xWorksStrings.ksValidatingOutputFile,
					Path.GetFileName(outPath));
				Validator.CheckLiftWithPossibleThrow(outPath);
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exports the configured doc view.
		/// </summary>
		/// <param name="progressDlg">The progress dialog.</param>
		/// <param name="parameters">The parameters.</param>
		/// <returns>Always null.</returns>
		/// ------------------------------------------------------------------------------------
		protected object ExportConfiguredDocView(IThreadedProgress progressDlg, object[] parameters)
		{
			Debug.Assert(parameters.Length == 4);
			m_progressDlg = progressDlg;
			if (m_xvc == null)
				return null;

			var outPath = (string) parameters[0];
			var fxtPath = (string) parameters[1];
			var ft = (FxtType) parameters[2];
			var vss = (IVwStylesheet) parameters[3];

			using (TextWriter w = new StreamWriter(outPath))
			{
				// FileInfo outFile = new FileInfo(outPath); // CS 219
#if DEBUG
				string dirPath = Path.GetTempPath();
				int copyCount = 1;
				string s = string.Format("Starting Configured Export at {0}",
					DateTime.Now.ToLongTimeString());
				Debug.WriteLine(s);
#endif
				m_ce = new ConfiguredExport(null, m_xvc.DataAccess, m_hvoRootObj);
				string sBodyClass = (m_areaOrig == "notebook") ? "notebookBody" : "dicBody";
				m_ce.Initialize(m_cache, m_mediator, w, ft.m_sDataType, ft.m_sFormat, outPath, sBodyClass);
				m_ce.UpdateProgress += ce_UpdateProgress;
				m_xvc.Display(m_ce, m_hvoRootObj, m_seqView.RootFrag);
				m_ce.Finish(ft.m_sDataType);
				w.Close();
#if DEBUG
				s = string.Format("Finished Configured Export Dump at {0}",
					DateTime.Now.ToLongTimeString());
				Debug.WriteLine(s);
#endif
				if (!string.IsNullOrEmpty(ft.m_sXsltFiles))
				{
					string[] rgsXslts = ft.m_sXsltFiles.Split(new[] { ';' });
					int cXslts = rgsXslts.GetLength(0);
					progressDlg.Position = 0;
					progressDlg.Minimum = 0;
					progressDlg.Maximum = cXslts;
					progressDlg.Message = xWorksStrings.ProcessingIntoFinalForm;
					int idx = fxtPath.LastIndexOfAny(new[] { '/', '\\' });
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
								rgsXslts[ix], DateTime.Now.ToLongTimeString());
						else
							s = String.Format("Starting final postprocess phase at {0}",
								DateTime.Now.ToLongTimeString());
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
					m_ce.WriteCssFile(Path.ChangeExtension(outPath, ".css"), vss, AllowDictionaryParagraphIndent(ft));
				}
				m_ce = null;
#if DEBUG
				File.Copy(outPath, Path.Combine(dirPath, "DebugOnlyExportStage" + copyCount + ".txt"), true);
				s = string.Format("Totally Finished Configured Export at {0}",
					DateTime.Now.ToLongTimeString());
				Debug.WriteLine(s);
#endif
			}

			return null;
		}

		// Currently we allow indented paragraph styles only for classified dictionary.
		// See the comment on XhtmlHelper.AllowDictionaryParagraphIndent for more info.
		private static bool AllowDictionaryParagraphIndent(FxtType ft)
		{
			return ft.m_ft == FxtTypes.kftClassifiedDict;
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			Close();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Registry key for settings for this Dialog.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "We're returning an object - caller responsible to dispose")]
		public RegistryKey SettingsKey
		{
			get
			{
				CheckDisposed();
				using (var regKey = FwRegistryHelper.FieldWorksRegistryKey)
					return regKey.CreateSubKey("ExportInterlinearDialog");
			}
		}

		private void ExportDialog_Closed(object sender, EventArgs e)
		{
			// Save location.
			SettingsKey.SetValue("InsertX", Location.X);
			SettingsKey.SetValue("InsertY", Location.Y);
			SettingsKey.SetValue("InsertWidth", Width);
			SettingsKey.SetValue("InsertHeight", Height);
		}

		/// <summary>
		/// Overridden to defeat the standard .NET behavior of adjusting size by
		/// screen resolution. That is bad for this dialog because we remember the size,
		/// and if we remember the enlarged size, it just keeps growing.
		/// If we defeat it, it may look a bit small the first time at high resolution,
		/// but at least it will stay the size the user sets.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnLoad(EventArgs e)
		{
			Size size = Size;
			base.OnLoad(e);
			if (Size != size)
				Size = size;
		}

		private void ExportDialog_Load(object sender, EventArgs e)
		{
			string p = FxtDirectory;
			if (Directory.Exists(p))
				AddFxts(Directory.GetFiles(p, "*.xml"));
		}

		internal string FxtDirectory
		{
			get { return Path.Combine(FwDirectoryFinder.CodeDirectory, ConfigurationFilePath); }
		}

		protected virtual string ConfigurationFilePath
		{
			get { return String.Format("Language Explorer{0}Export Templates", Path.DirectorySeparatorChar); }
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
				if (string.IsNullOrEmpty(description))
					description = xWorksStrings.NoDescriptionForItem;
				var item = new ListViewItem(new[]{dataLabel, formatLabel, defaultExtension, filter, description});
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
					m_exportItems.Add(lvi);
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
				case "classified":
					ft.m_ft = FxtTypes.kftClassifiedDict;
					break;
				case "reversal":
					ft.m_ft = FxtTypes.kftReversal;
					break;
				case "translatedList":
					ft.m_ft = FxtTypes.kftTranslatedLists;
					break;
				case "pathway":
					ft.m_ft = FxtTypes.kftPathway;
					break;
				case "LIFT":
					ft.m_ft = FxtTypes.kftLift;
					break;
				case "grammarSketch":
					ft.m_ft = FxtTypes.kftGrammarSketch;
					break;
				case "semanticDomains":
					ft.m_ft = FxtTypes.kftSemanticDomains;
					break;
				default:
					Debug.Assert(false, "Invalid type attribute value for the template element");
					ft.m_ft = FxtTypes.kftFxt;
					break;
			}
			ft.m_sDataType = XmlUtils.GetOptionalAttributeValue(templateRootNode, "datatype", "UNKNOWN");
			ft.m_sXsltFiles = XmlUtils.GetOptionalAttributeValue(templateRootNode, "xslt", null);
			ft.m_filtered = XmlUtils.GetOptionalBooleanAttributeValue(templateRootNode, "filtered", false);
			ft.m_path = (string)item.Tag;
			m_rgFxtTypes.Add(ft);
			// We can't actually disable a list item, but we can make it look and act like it's
			// disabled.
			if (ItemDisabled(ft.m_ft, ft.m_filtered))
				item.ForeColor = SystemColors.GrayText;
		}

		private void m_exportList_SelectedIndexChanged(object sender, EventArgs e)
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
			return ItemDisabled(m_rgFxtTypes[FxtIndex(tag)].m_ft, m_rgFxtTypes[FxtIndex(tag)].m_filtered);
		}

		private bool ItemDisabled(FxtTypes ft, bool isFiltered)
		{
			//enable unless the type is pathway & pathway is not installed, or if the type is lift and it is filtered, but there is no filter available, or if the filter excludes all items
			bool fFilterAvailable = DetermineIfFilterIsAvailable();
			return (ft == FxtTypes.kftPathway && !PathwayUtils.IsPathwayInstalled) ||
				   (ft == FxtTypes.kftLift && isFiltered && fFilterAvailable);
		}

		private bool DetermineIfFilterIsAvailable()
		{
			if (m_clerk == null)
			{
				return false;
			}
			return (m_clerk.VirtualListPublisher.get_VecSize(m_cache.LangProject.LexDbOA.Hvo, m_clerk.VirtualFlid) < 1);
		}
		private void OnDumperUpdateProgress(object sender)
		{
			Debug.Assert(m_progressDlg != null);
			m_progressDlg.Step(0);
			if (m_progressDlg.Canceled)
				m_dumper.Cancel();
		}

		private void OnDumperSetProgressMessage(object sender, ProgressMessageArgs e)
		{
			Debug.Assert(m_progressDlg != null);
			string sMsg = xWorksStrings.ResourceManager.GetString(e.MessageId, xWorksStrings.Culture);
			if (!String.IsNullOrEmpty(sMsg))
				m_progressDlg.Message = sMsg;
			m_progressDlg.Position = 0;
			m_progressDlg.Minimum = 0;
			m_progressDlg.Maximum = e.Max;
			if (m_progressDlg.Canceled)
				m_dumper.Cancel();
		}

		private void buttonHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_mediator.HelpTopicProvider, m_helpTopic);
		}

		private void ce_UpdateProgress(object sender)
		{
			Debug.Assert(m_progressDlg != null);
			m_progressDlg.Step(0);
			if (m_progressDlg.Canceled)
				m_ce.Cancel();
		}

		protected object ExportTranslatedLists(IThreadedProgress progressDlg, object[] parameters)
		{
			string outPath = (string)parameters[0];
			m_progressDlg = progressDlg;
			if (m_translatedLists.Count == 0 || m_translationWritingSystems.Count == 0)
				return null;
			TranslatedListsExporter exporter = new TranslatedListsExporter(m_translatedLists,
				m_translationWritingSystems, progressDlg);
			exporter.ExportLists(outPath);
			return null;
		}

		/// <summary>
		/// For testing.
		/// </summary>
		/// <param name="wss"></param>
		internal void SetTranslationWritingSystems(List<int> wss)
		{
			m_translationWritingSystems = wss;
		}

		/// <summary>
		/// for testing
		/// </summary>
		/// <param name="cache"></param>
		internal void SetCache(FdoCache cache)
		{
			m_cache = cache;
		}

		/// <summary>
		/// Do the export of the semantic domains list to an HTML document (which is given extension .doc
		/// since it is mainly intended to be opened as a Word document, since Word understands the
		/// 'page break before' concept).
		/// The signature of this method is required by the way it is used as the task of the ProgressDialog.
		/// See the first few lines for the required parameters.
		/// </summary>
		/// <param name="progressDlg"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		internal object ExportSemanticDomains(IThreadedProgress progressDlg, object[] parameters)
		{
			string outPath = (string) parameters[0];
			var ft = (FxtType) parameters[1];
			var fxtPath = (string) parameters[2];
			bool allQuestions = (bool) parameters[3];
			m_progressDlg = progressDlg;
			var lists = new List<ICmPossibilityList>();
			lists.Add(m_cache.LangProject.SemanticDomainListOA);
			var exporter = new TranslatedListsExporter(lists, m_translationWritingSystems, progressDlg);
			exporter.ExportLists(outPath);
			FxtType ft1 = ft;
#if DEBUG
			string dirPath = Path.GetTempPath();
#endif
			string xslt = ft1.m_sXsltFiles;
			progressDlg.Position = 0;
			progressDlg.Minimum = 0;
			progressDlg.Maximum = 1;
			progressDlg.Message = xWorksStrings.ProcessingIntoFinalForm;
			int idx = fxtPath.LastIndexOfAny(new[] {'/', '\\'});
			if (idx < 0)
				idx = 0;
			else
				++idx;
			string basePath = fxtPath.Substring(0, idx);
			string sXsltPath = basePath + xslt;
			string sIntermediateFile = ConfiguredExport.RenameOutputToPassN(outPath, 0);

			// The semantic domain xslt uses document('folderStart.xml') to retrieve the list of H1 topics.
			// This is not allowed by default so we must use a settings object to enable it.
			var settings = new XsltSettings(enableDocumentFunction: true, enableScript: false);
			XslCompiledTransform xsl = new XslCompiledTransform();
			xsl.Load(sXsltPath, settings, new XmlUrlResolver());
			var arguments = new XsltArgumentList();
			// If we aren't outputting english we need to ignore it (except possibly for missing items)
			bool ignoreEn = m_translationWritingSystems[0] != m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("en");
			arguments.AddParam(@"ignoreLang", @"", (ignoreEn ? @"en" : @""));
			arguments.AddParam(@"allQuestions", @"", (allQuestions ? @"1" : @"0"));
			using (var writer = FileUtils.OpenFileForWrite(outPath, Encoding.UTF8))
				xsl.Transform(sIntermediateFile, arguments, writer);
			// Deleting them deals with LT-6345,
			// which asked that they be put in the temp folder.
			// But moving them to the temp directory is better for debugging errors.
			FileUtils.MoveFileToTempDirectory(sIntermediateFile, "FieldWorks-Export");
			progressDlg.Step(0);

#if DEBUG
			string s = string.Format("Totally Finished Export Semantic domains at {0}",
				DateTime.Now.ToLongTimeString());
			Debug.WriteLine(s);
#endif
			return null; // method signature is required by ProgressDialog
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This encapsulates exporting one or more CmPossibilityLists in one or more writing
		/// systems.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public class TranslatedListsExporter
		{
			FdoCache m_cache;
			List<ICmPossibilityList> m_lists;
			Dictionary<int, string> m_mapWsCode = new Dictionary<int,string>();
			IProgress m_progress;
			int m_wsEn = 0;

			Dictionary<int, bool> m_flidsForGuids = new Dictionary<int, bool>();

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Constructor.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public TranslatedListsExporter(List<ICmPossibilityList> lists,
				List<int> writingSystems, IProgress progress)
			{
				m_lists = lists;
				if (m_lists.Count > 0)
					m_cache = m_lists[0].Cache;
				if (m_cache != null)
				{
					foreach (int ws in writingSystems)
						m_mapWsCode.Add(ws, m_cache.WritingSystemFactory.GetStrFromWs(ws));
					m_wsEn = m_cache.WritingSystemFactory.GetWsFromStr("en");
					Debug.Assert(m_wsEn != 0);
				}
				m_progress = progress;

				// These flids for List fields indicate lists that use fixed guids for their
				// (predefined) items.
				m_flidsForGuids.Add(LangProjectTags.kflidTranslationTags, true);
				m_flidsForGuids.Add(LangProjectTags.kflidAnthroList, true);
				m_flidsForGuids.Add(LangProjectTags.kflidSemanticDomainList, true);
				m_flidsForGuids.Add(LexDbTags.kflidComplexEntryTypes, true);
				m_flidsForGuids.Add(LexDbTags.kflidVariantEntryTypes, true);
				m_flidsForGuids.Add(LexDbTags.kflidMorphTypes, true);
				m_flidsForGuids.Add(RnResearchNbkTags.kflidRecTypes, true);
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Export the list(s) to the given file.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public void ExportLists(string outputFile)
			{
				if (m_wsEn == 0)
					return;
				using (TextWriter w = new StreamWriter(outputFile))
				{
					ExportTranslatedLists(w);
				}
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Export the list(s) to the given TextWriter.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public void ExportTranslatedLists(TextWriter w)
			{
				// Writing out TsStrings requires a Stream, not a Writer...
				var stream = new TextWriterStream(w);
				w.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
				w.WriteLine(String.Format("<Lists date=\"{0}\">", DateTime.Now.ToString()));
				foreach (var list in m_lists)
					ExportTranslatedList(w, stream, list);
				w.WriteLine("</Lists>");
			}

			private void ExportTranslatedList(TextWriter w, TextWriterStream stream,
				ICmPossibilityList list)
			{
				string owner = list.Owner.ClassName;
				string field = m_cache.MetaDataCacheAccessor.GetFieldName(list.OwningFlid);
				string itemClass = m_cache.MetaDataCacheAccessor.GetClassName(list.ItemClsid);
				w.WriteLine(String.Format("<List owner=\"{0}\" field=\"{1}\" itemClass=\"{2}\">",
					owner, field, itemClass));
				ExportMultiUnicode(w, list.Name);
				ExportMultiUnicode(w, list.Abbreviation);
				ExportMultiString(w, stream, list.Description);
				w.WriteLine("<Possibilities>");
				foreach (var item in list.PossibilitiesOS)
					ExportTranslatedItem(w, stream, item, list.OwningFlid);
				w.WriteLine("</Possibilities>");
				w.WriteLine("</List>");
			}

			private void ExportMultiUnicode(TextWriter w, IMultiUnicode mu)
			{
				string sEnglish = mu.get_String(m_wsEn).Text;
				if (String.IsNullOrEmpty(sEnglish))
					return;
				string sField = m_cache.MetaDataCacheAccessor.GetFieldName(mu.Flid);
				w.WriteLine(String.Format("<{0}>", sField));
				w.WriteLine(String.Format("<AUni ws=\"en\">{0}</AUni>",
					XmlUtils.MakeSafeXml(sEnglish)));
				foreach (int ws in m_mapWsCode.Keys)
				{
					string sValue = mu.get_String(ws).Text;
					if (sValue == null)
						sValue = String.Empty;
					else
						sValue = Icu.Normalize(sValue, Icu.UNormalizationMode.UNORM_NFC);
					w.WriteLine(String.Format("<AUni ws=\"{0}\">{1}</AUni>",
						m_mapWsCode[ws], XmlUtils.MakeSafeXml(sValue)));
				}
				w.WriteLine(String.Format("</{0}>", sField));
			}

			private void ExportMultiString(TextWriter w, TextWriterStream stream,
				IMultiString ms)
			{
				ITsString tssEnglish = ms.get_String(m_wsEn);
				if (tssEnglish.Length == 0)
					return;
				string sField = m_cache.MetaDataCacheAccessor.GetFieldName(ms.Flid);
				w.WriteLine(String.Format("<{0}>", sField));
				tssEnglish.WriteAsXml(stream, m_cache.WritingSystemFactory, 0, m_wsEn, false);
				foreach (int ws in m_mapWsCode.Keys)
				{
					ITsString tssValue = ms.get_String(ws);
					tssValue.WriteAsXml(stream, m_cache.WritingSystemFactory, 0, ws, false);
				}
				w.WriteLine(String.Format("</{0}>", sField));
			}

			private void ExportTranslatedItem(TextWriter w, TextWriterStream stream,
				ICmPossibility item, int listFlid)
			{
				if (m_flidsForGuids.ContainsKey(listFlid))
					w.WriteLine(String.Format("<{0} guid=\"{1}\">", item.ClassName, item.Guid));
				else
					w.WriteLine(String.Format("<{0}>", item.ClassName));
				ExportMultiUnicode(w, item.Name);
				ExportMultiUnicode(w, item.Abbreviation);
				ExportMultiString(w, stream, item.Description);
				switch (item.ClassID)
				{
					case CmLocationTags.kClassId:
						ExportLocationFields(w, item as ICmLocation);
						break;
					case CmPersonTags.kClassId:
						ExportPersonFields(w, item as ICmPerson);
						break;
					case CmSemanticDomainTags.kClassId:
						ExportSemanticDomainFields(w, stream, item as ICmSemanticDomain);
						break;
					case LexEntryTypeTags.kClassId:
						ExportLexEntryTypeFields(w, item as ILexEntryType);
						break;
					case LexRefTypeTags.kClassId:
						ExportLexRefTypeFields(w, item as ILexRefType);
						break;
					case PartOfSpeechTags.kClassId:
						ExportPartOfSpeechFields(w, item as IPartOfSpeech);
						break;
				}
				if (item.SubPossibilitiesOS.Count > 0)
				{
					w.WriteLine("<SubPossibilities>");
					foreach (var subItem in item.SubPossibilitiesOS)
						ExportTranslatedItem(w, stream, subItem, listFlid);
					w.WriteLine("</SubPossibilities>");
				}
				w.WriteLine(String.Format("</{0}>", item.ClassName));
			}

			private void ExportLocationFields(TextWriter w, ICmLocation item)
			{
				if (item != null)
					ExportMultiUnicode(w, item.Alias);
			}

			private void ExportPersonFields(TextWriter w, ICmPerson item)
			{
				if (item != null)
					ExportMultiUnicode(w, item.Alias);
			}

			private void ExportSemanticDomainFields(TextWriter w, TextWriterStream stream,
				ICmSemanticDomain item)
			{
				if (item != null && item.QuestionsOS.Count > 0)
				{
					w.WriteLine("<Questions>");
					foreach (var domainQ in item.QuestionsOS)
					{
						w.WriteLine("<CmDomainQ>");
						ExportMultiUnicode(w, domainQ.Question);
						ExportMultiUnicode(w, domainQ.ExampleWords);
						ExportMultiString(w, stream, domainQ.ExampleSentences);
						w.WriteLine("</CmDomainQ>");
					}
					w.WriteLine("</Questions>");
				}
			}

			private void ExportLexEntryTypeFields(TextWriter w, ILexEntryType item)
			{
				if (item != null)
					ExportMultiUnicode(w, item.ReverseAbbr);
			}

			private void ExportLexRefTypeFields(TextWriter w, ILexRefType item)
			{
				if (item != null)
				{
					ExportMultiUnicode(w, item.ReverseName);
					ExportMultiUnicode(w, item.ReverseAbbreviation);
				}
			}

			private void ExportPartOfSpeechFields(TextWriter w, IPartOfSpeech item)
			{
				// TODO: handle any other part of speech fields that we decide need to
				// be localizable.
			}
		}

		private void m_chkExportPictures_CheckedChanged(object sender, EventArgs e)
		{
			m_mediator.PropertyTable.SetProperty(ksLiftExportPicturesPropertyName, m_chkExportPictures.Checked);
			m_mediator.PropertyTable.SetPropertyPersistence(ksLiftExportPicturesPropertyName, true);
			m_fExportPicturesAndMedia = m_chkExportPictures.Checked;
		}

		private void m_description_LinkClicked(object sender, LinkClickedEventArgs e)
		{
			using (Process.Start(e.LinkText))
			{
			}
		}

		/// <summary>
		/// Perform a Pathway export, bringing up the Pathway configuration dialog, exporting
		/// one or more XHTML files, and then postprocessing as requested.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "applicationKey is a reference")]
		private void ProcessPathwayExport()
		{
			IApp app = (IApp)m_mediator.PropertyTable.GetValue("App");
			string cssDialog = Path.Combine(PathwayUtils.PathwayInstallDirectory, "CssDialog.dll");
			var sf = ReflectionHelper.CreateObject(cssDialog, "SIL.PublishingSolution.Contents", null);
			Debug.Assert(sf != null);
			FdoCache cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
			ReflectionHelper.SetProperty(sf, "DatabaseName", cache.ProjectId.Name);
			bool fContentsExists = SelectOption("ReversalIndexXHTML");
			if (fContentsExists)
			{
				// Inform Pathway if the reversal index is empty (or doesn't exist).  See FWR-3283.
				var riGuid = ReversalIndexEntryUi.GetObjectGuidIfValid(m_mediator, "ReversalIndexGuid");
				if (!riGuid.Equals(Guid.Empty))
				{
					try
					{
						IReversalIndex ri = m_cache.ServiceLocator.GetObject(riGuid) as IReversalIndex;
						fContentsExists = ri.EntriesOC.Count > 0;
					}
					catch
					{
						fContentsExists = false; // Can't get an index if we have a bad guid.
					}
				}
			}
			ReflectionHelper.SetProperty(sf, "ExportReversal", fContentsExists);
			ReflectionHelper.SetProperty(sf, "ReversalExists", fContentsExists);
			ReflectionHelper.SetProperty(sf, "GrammarExists", false);

			DialogResult result = (DialogResult)ReflectionHelper.GetResult(sf, "ShowDialog");
			if (result == DialogResult.Cancel)
				return;

			const string MainXhtml = "main.xhtml";
			const string ExpCss = "main.css";
			const string RevXhtml = "FlexRev.xhtml";

			string strOutputPath = (string)ReflectionHelper.GetProperty(sf, "OutputLocationPath");
			string strDictionaryName = (string)ReflectionHelper.GetProperty(sf, "DictionaryName");
			string outPath = Path.Combine(strOutputPath, strDictionaryName);

			bool fExistingDirectoryInput = (bool)ReflectionHelper.GetProperty(sf, "ExistingDirectoryInput");
			if (fExistingDirectoryInput)
			{
				string inputPath = (string)ReflectionHelper.GetProperty(sf, "ExistingDirectoryLocationPath");
				if (inputPath != outPath)
				{
					string dirFilter = string.Empty;
					if (strOutputPath == inputPath)
					{
						dirFilter = strDictionaryName;
					}
					try
					{
						if (!Folders.Copy(inputPath, outPath, dirFilter, app.ApplicationName))
							return;
					}
					catch (Exception ex)
					{

						MessageBox.Show(ex.Message);
						return;
					}
				}
			}

			if (!Folders.CreateDirectory(outPath, app.ApplicationName))
				return;

			string mainFullName = Path.Combine(outPath, MainXhtml);
			string revFullXhtml = Path.Combine(outPath, RevXhtml);
			if (!(bool)ReflectionHelper.GetProperty(sf, "ExportMain"))
				mainFullName = "";
			if (!(bool)ReflectionHelper.GetProperty(sf, "ExportReversal"))
				revFullXhtml = "";

			switch (result)
			{
				// No = Skip export of data from Flex but still prepare exported output (ODT, PDF or whatever)
				case DialogResult.No:
					break;

				case DialogResult.Yes:
					if (!DoFlexExports(ExpCss, mainFullName, revFullXhtml))
					{
						this.Close();
						return;
					}
					break;
			}

			string psExport = Path.Combine(PathwayUtils.PathwayInstallDirectory, "PsExport.dll");
			var exporter = ReflectionHelper.CreateObject(psExport, "SIL.PublishingSolution.PsExport", null);
			Debug.Assert(exporter != null);
			ReflectionHelper.SetProperty(exporter, "DataType", "Dictionary");
			ReflectionHelper.SetProperty(exporter, "ProgressBar", null);
			ReflectionHelper.CallMethod(exporter, "Export", mainFullName != "" ? mainFullName : revFullXhtml);

			RegistryKey applicationKey = app.SettingsKey;
			UsageEmailDialog.IncrementLaunchCount(applicationKey);
			Assembly assembly = exporter.GetType().Assembly;

			const string FeedbackEmailAddress = "pathway@sil.org";
			const string utilityLabel = "Pathway";

			UsageEmailDialog.DoTrivialUsageReport(utilityLabel, applicationKey, FeedbackEmailAddress,
				string.Format("1. What do you hope {0} will do for you?%0A%0A2. What languages are you working on?", utilityLabel),
				false, 1, assembly);
			UsageEmailDialog.DoTrivialUsageReport(utilityLabel, applicationKey, FeedbackEmailAddress,
				string.Format("1. Do you have suggestions to improve the program?%0A%0A2. What are you happy with?"),
				false, 10, assembly);
			UsageEmailDialog.DoTrivialUsageReport(utilityLabel, applicationKey, FeedbackEmailAddress,
				string.Format("1. What would you like to say to others about {0}?%0A%0A2. What languages have you used with {0}", utilityLabel),
				false, 40, assembly);

			this.Close();
		}

		private bool SelectOption(string exportFormat)
		{
			// LT-12279 selected a user disturbing, different menu item
			// return m_exportList.Items.Cast<ListViewItem>().Where(lvi => lvi.Tag.ToString().Contains(exportFormat));
			foreach (ListViewItem lvi in
				m_exportList.Items.Cast<ListViewItem>().Where(lvi => lvi.Tag.ToString().Contains(exportFormat)))
			{
				if (!ItemDisabled(lvi.Tag.ToString()))
				{
					m_exportItems.Insert(0, lvi);
					return true;
				}
				return false;
			}
			return false;
			/* foreach (ListViewItem lvi in m_exportList.Items)
			{
				if (lvi.Tag.ToString().Contains(exportFormat))
				{
					if (ItemDisabled(lvi.Tag.ToString()))
						return false;
					lvi.Selected = true;
					return true;
				}
			}
			return false; */
		}

		/// <summary>
		/// Export process from Fieldworks Language explorer
		/// </summary>
		/// <param name="expCss">Style sheet exported</param>
		/// <param name="mainFullName">Source of main dictionary</param>
		/// <param name="revFullXhtml">source of reversal Index if available in Xhtml format</param>
		/// <returns>True if there is something to do</returns>
		protected bool DoFlexExports(string expCss, string mainFullName, string revFullXhtml)
		{
			if (File.Exists(mainFullName))
				File.Delete(mainFullName);

			if (File.Exists(revFullXhtml))
				File.Delete(revFullXhtml);

			string currInput = string.Empty;
			try
			{
				if (mainFullName != "")
					ExportFor("ConfiguredXHTML", mainFullName);
				if (revFullXhtml != "")
					ExportFor("ReversalIndexXHTML", revFullXhtml);
			}
			catch (FileNotFoundException)
			{
				IApp app = (IApp)m_mediator.PropertyTable.GetValue("App");
				MessageBox.Show("The " + currInput + " Section may be Empty (or) Not exported", app.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;

			}
			catch (Exception)
			{
				return false;
			}

			return true;
		}

		private void ExportFor(string type, string file)
		{
			if (SelectOption(type))
			{
				Control main = EnsureViewInfo();
				try
				{
					DoExport(file);
					ValidXmlFile(file);
				}
				finally
				{
					if (main != null)
						main.Dispose();
				}
			}
		}

		/// <summary>
		/// Validating the xml file with xmldocument to avoid further processing.
		/// </summary>
		/// <param name="xml">Xml file Name for Validating</param>
		/// <exception cref="FileNotFoundException">if xml file missing</exception>
		/// <exception cref="XmlException">if xml file won't load</exception>
		protected static void ValidXmlFile(string xml)
		{
			if (!File.Exists(xml))
				throw new FileNotFoundException();
			XmlDocument xDoc = new XmlDocument();
			xDoc.XmlResolver = FileStreamXmlResolver.GetNullResolver();
			xDoc.Load(xml);
		}
	}
}
