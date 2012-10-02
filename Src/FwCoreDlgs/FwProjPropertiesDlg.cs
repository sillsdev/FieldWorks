// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwProjPropertiesDlg.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.Drawing;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using System.IO;

namespace SIL.FieldWorks.FwCoreDlgs
{
	#region IFwProjPropertiesDlg interface
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Interface for FwProjPropertiesDlg
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[ComVisible(true)]
	[Guid("6CDA8F26-6C3E-4147-B8B6-2277592188DD")]
	public interface IFwProjPropertiesDlg
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the dialog as a modal dialog
		/// </summary>
		/// <returns>A DialogResult value</returns>
		/// ------------------------------------------------------------------------------------
		int ShowDlg();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the cache
		/// </summary>
		/// <param name="ode"></param>
		/// <param name="mdc"></param>
		/// <param name="oleDbAccess"></param>
		/// <param name="tool">IFwTool object used to open/close application window</param>
		/// <param name="helpTopicProvider">IHelpTopicProvider object used to get help
		/// information</param>
		/// <param name="strmLog">optional log file stream</param>
		/// <param name="hvoProj">Hvo of the Language Project</param>
		/// <param name="hvoRoot">Hvo of the root object</param>
		/// <param name="wsUser">user interface writing system id</param>
		/// <param name="stylesheet">stylesheet for FwTextBox</param>
		/// ------------------------------------------------------------------------------------
		void Initialize(IOleDbEncap ode, IFwMetaDataCache mdc, IVwOleDbDa oleDbAccess,
			IFwTool tool, IHelpTopicProvider helpTopicProvider, IStream strmLog, int hvoProj,
			int hvoRoot, int wsUser, IVwStylesheet stylesheet);


		/// <summary>
		/// Return true if something in the active writing system lists changed.
		/// </summary>
		/// <returns></returns>
		bool WritingSystemsChanged();

		/// <summary>
		/// Return true if the project name changed.
		/// </summary>
		/// <returns></returns>
		bool ProjectNameChanged();

		/// <summary>
		/// Return true if the external link directory changed.
		/// </summary>
		/// <returns></returns>
		bool ExternalLinkChanged();

		/// <summary>
		/// Call this when done with the dialog. It is especially important
		/// for C++ apps to call this so the dialog gets disposed
		/// and the FwTool reference gets released; otherwise, DN will run
		/// until the dialog gets collected, typically forever.
		/// </summary>
		void DisposeDialog();
	}
	#endregion //IFwProjPropertiesDlg interface

	#region FwProjPropertiesDlg dialog
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for FwProjPropertiesDlg.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[ProgId("FwCoreDlgs.FwProjPropertiesDlg")]
	// Key attribute to hide the "clutter" from System.Windows.Forms.Form
	[ClassInterface(ClassInterfaceType.None)]
	[GuidAttribute("8DE20735-108A-4ed7-BCA7-9BCB57F4BC95")]
	[ComVisible(true)]
	public class FwProjPropertiesDlg : Form, IFWDisposable, IFwProjPropertiesDlg
	{
		#region Data members
		/// <summary>Index of the tab for user properties account</summary>
		protected const int kGeneralTab = 0;
		/// <summary>Index of the tab for user features</summary>
		protected const int kWritingSystemTab = 1;
		/// <summary>Index of the tab for user properties account</summary>
		protected const int kExternalLinksTab = 2;

		private FdoCache m_cache;
		private bool m_cacheMadeLocally = false;
		private ILangProject m_langProj;
		private IHelpTopicProvider m_helpTopicProvider;
		/// <summary></summary>
		protected Label m_lblProjName;
		/// <summary></summary>
		protected Label m_lblProjCreatedDate;
		/// <summary></summary>
		protected Label m_lblProjModifiedDate;
		/// <summary></summary>
		protected TextBox m_txtProjName;
		/// <summary></summary>
		protected TextBox m_txtProjDescription;
		/// <summary></summary>
		protected ToolTip m_toolTip;
		/// <summary></summary>
		protected CheckedListBox m_lstAnalWs;
		/// <summary></summary>
		protected CheckedListBox m_lstVernWs;
		private ContextMenuStrip m_cmnuAddWs;
		private ToolStripMenuItem menuItem2;
		private TextBox txtExtLnkEdit;
		private FolderBrowserDialog folderBrowserDlg;
		private System.ComponentModel.IContainer components;

		/// <summary>This allows us to modify a writing system even if that results in merging
		/// two writing systems.  In that situation, we need to shutdown all windows and
		/// database connections, and to reopen one main window for the program.
		/// </summary>
		protected IFwTool m_tool;
		/// <summary></summary>
		protected IStream m_strmLog;
		/// <summary></summary>
		protected int m_hvoProj;
		/// <summary></summary>
		protected int m_hvoRoot;
		/// <summary></summary>
		protected int m_wsUser;
		/// <summary></summary>
		protected IVwStylesheet m_stylesheet;
		/// <summary>A change in writing systems has been made that may affect
		/// current displays.</summary>
		protected bool m_fWsChanged;
		/// <summary>A change in a sort spec on a writing system has been made that may affect
		/// a record list.</summary>
		protected bool m_fWsSortChanged;
		/// <summary>
		/// A change in writing systems has been made that invalidates any existing rendering
		/// engine for the affected writing systems.
		/// </summary>
		protected bool m_fNewRendering;
		/// <summary>A change in the project name has changed which may affect
		/// title bars.</summary>
		protected bool m_fProjNameChanged;

		private HelpProvider helpProvider1;
		/// <summary>database server name</summary>
		protected Label m_lblServerName;
		private TabControl m_tabControl;
		/// <summary></summary>
		protected Button m_btnAnalMoveUp;
		/// <summary></summary>
		protected Button m_btnAnalMoveDown;
		/// <summary></summary>
		protected Button m_btnVernMoveUp;
		/// <summary></summary>
		protected Button m_btnVernMoveDown;
		/// <summary></summary>
		protected Button m_btnOK;
		/// <summary></summary>
		protected Button m_btnDelAnalWs;
		/// <summary></summary>
		protected Button m_btnDelVernWs;
		/// <summary></summary>
		protected Button m_btnModifyAnalWs;
		/// <summary></summary>
		protected Button m_btnModifyVernWs;
		private Button m_btnAddAnalWs;
		private Button m_btnAddVernWs;
		/// <summary>A change in the External Link directory has been made.</summary>
		protected bool m_fExtLnkChanged;
		private IApp m_app;
		/// <summary>The project name when we entered the dialog.</summary>
		protected string m_sOrigProjName;
		#endregion

		#region Construction and initialization
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwProjPropertiesDlg()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates and initializes a new instance of the FwProjProperties class. Accepts an
		/// FdoCache that encapsulates a DB connection.
		/// </summary>
		/// <param name="cache">Accessor for data cache and DB connection</param>
		/// <param name="app">The application (can be <c>null</c>)</param>
		/// <param name="tool">IFwTool object used to open/close application window</param>
		/// <param name="helpTopicProvider">IHelpTopicProvider object used to get help
		/// information</param>
		/// <param name="strmLog">optional log file stream</param>
		/// <param name="hvoProj">Hvo of the Language Project</param>
		/// <param name="hvoRoot">Hvo of the root object</param>
		/// <param name="wsUser">user interface writing system id</param>
		/// <param name="stylesheet">this is used for the FwTextBox</param>
		/// ------------------------------------------------------------------------------------
		public FwProjPropertiesDlg(FdoCache cache, IApp app,
			IFwTool tool, IHelpTopicProvider helpTopicProvider, IStream strmLog, int hvoProj,
			int hvoRoot, int wsUser, IVwStylesheet stylesheet): this()
		{
			if (cache == null)
				throw new ArgumentNullException("cache", "Null Cache passed to FwProjProperties");

			m_cache = cache;
			m_app = app;
			m_tool = tool;
			m_helpTopicProvider = helpTopicProvider;
			m_strmLog = strmLog;
			m_hvoProj = hvoProj;
			m_hvoRoot = hvoRoot;
			m_wsUser = wsUser;
			m_stylesheet = stylesheet;

			Initialize();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the FwProjProperties class. Clients written in .Net with an FdoCache
		/// should use the version of the constructor that accepts an FdoCache. COM clients that
		/// do not have an FdoCache should use the default constructor and then call this method
		/// to initialize the object.
		/// </summary>
		/// <param name="ode"></param>
		/// <param name="mdc"></param>
		/// <param name="oleDbAccess"></param>
		/// <param name="tool">IFwTool object used to open/close application window</param>
		/// <param name="helpTopicProvider">IHelpTopicProvider object used to get help
		/// information</param>
		/// <param name="strmLog">optional log file stream</param>
		/// <param name="hvoProj">Hvo of the Language Project</param>
		/// <param name="hvoRoot">Hvo of the root object</param>
		/// <param name="wsUser">user interface writing system id</param>
		/// <param name="stylesheet">stylesheet for FwTextBox</param>
		/// ------------------------------------------------------------------------------------
		public void Initialize(IOleDbEncap ode, IFwMetaDataCache mdc, IVwOleDbDa oleDbAccess,
			IFwTool tool, IHelpTopicProvider helpTopicProvider, IStream strmLog, int hvoProj,
			int hvoRoot, int wsUser, IVwStylesheet stylesheet)
		{
			CheckDisposed();

			m_cache = new FdoCache(ode, mdc, oleDbAccess);
			m_cacheMadeLocally = true;
			m_tool = tool;
			m_helpTopicProvider = helpTopicProvider;
			m_strmLog = strmLog;
			m_hvoProj = hvoProj;
			m_hvoRoot = hvoRoot;
			m_wsUser = wsUser;
			m_stylesheet = stylesheet;

			Initialize();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void Initialize()
		{
			m_langProj = m_cache.LangProject;
			InitializeWsTab();
			InitializeGeneralTab();
			txtExtLnkEdit.Text = m_langProj.ExternalLinkRootDir;
			m_fExtLnkChanged = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeGeneralTab()
		{
			m_txtProjName.Text = m_lblProjName.Text = m_sOrigProjName = m_cache.DatabaseName;
			m_lblServerName.Text = m_cache.ServerName;
			m_lblProjCreatedDate.Text = m_langProj.DateCreated.ToString();
			m_lblProjModifiedDate.Text = m_langProj.DateModified.ToString();
			m_txtProjDescription.Text = m_langProj.Description.UserDefaultWritingSystem.Text;
			m_fProjNameChanged = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeWsTab()
		{
			using (new WaitCursor(this))
			{
				// Ensure we have up to date information.  See LT-8718.
				bool fDiff = m_langProj.CurVernWssRS.UpdateIfCached();
				fDiff |= m_langProj.CurAnalysisWssRS.UpdateIfCached();
				m_langProj.VernWssRC.UpdateIfCached();
				m_langProj.AnalysisWssRC.UpdateIfCached();

				// Add writing system names to the vernacular list.
				foreach (ILgWritingSystem ws in m_langProj.CurVernWssRS)
					m_lstVernWs.Items.Add(ws, true);

				// Add writing system names to the analysis list.
				foreach (ILgWritingSystem ws in m_langProj.CurAnalysisWssRS)
					m_lstAnalWs.Items.Add(ws, true);

				// Now add the unchecked (or not current) writing systems to the vern. list.
				foreach (ILgWritingSystem ws in m_langProj.VernWssRC)
				{
					if (!m_lstVernWs.Items.Contains(ws))
						m_lstVernWs.Items.Add(ws, false);
				}

				// Now add the unchecked (or not current) writing systems to the anal. list.
				foreach (ILgWritingSystem ws in m_langProj.AnalysisWssRC)
				{
					if (!m_lstAnalWs.Items.Contains(ws))
						m_lstAnalWs.Items.Add(ws, false);
				}

				// Select the first item in the vernacular writing system list.
				if (m_lstVernWs.Items.Count > 0)
					m_lstVernWs.SelectedIndex = 0;

				// Select the first item in the analysis writing system list.
				if (m_lstAnalWs.Items.Count > 0)
					m_lstAnalWs.SelectedIndex = 0;
				m_fWsChanged = fDiff;
				m_fNewRendering = false;
				m_fWsSortChanged = false;
				UpdateOKButton();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Used to start the dlg with the WS page being used.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void StartWithWSPage()
		{
			CheckDisposed();

			m_tabControl.SelectedIndex = kWritingSystemTab;
		}
		#endregion

		#region Dispose stuff
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		~FwProjPropertiesDlg()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			// release unmanaged COM objects regardless of disposing flag
			if (m_tool != null)
			{
				if (Marshal.IsComObject(m_tool))
					Marshal.ReleaseComObject(m_tool);
				m_tool = null;
			}
			if (m_strmLog != null)
			{
				if (Marshal.IsComObject(m_strmLog))
					Marshal.ReleaseComObject(m_strmLog);
				m_strmLog = null;
			}

			if (disposing)
			{
				// release managed objects
				// We may have made the cache from COM objects given to us by a COM client.
				// In that case, we have to dispose it.
				if (m_cacheMadeLocally && m_cache != null)
					m_cache.Dispose();

				//if (m_helpTopicProvider != null && m_helpTopicProvider is IDisposable) // No, since the client provides it.
				//	(m_helpTopicProvider as IDisposable).Dispose();

				if (components != null)
					components.Dispose();
			}
			m_helpTopicProvider = null;
			m_cache = null;

			base.Dispose(disposing);
		}
		#endregion

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Windows.Forms.Button m_btnHelp;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FwProjPropertiesDlg));
			System.Windows.Forms.Button m_btnCancel;
			System.Windows.Forms.TabPage m_tpGeneral;
			SIL.FieldWorks.Common.Controls.LineControl lineControl3;
			SIL.FieldWorks.Common.Controls.LineControl lineControl2;
			SIL.FieldWorks.Common.Controls.LineControl lineControl1;
			System.Windows.Forms.PictureBox m_picLangProgFileBox;
			System.Windows.Forms.Label label3;
			System.Windows.Forms.Label label7;
			System.Windows.Forms.Label label1;
			System.Windows.Forms.Label label5;
			System.Windows.Forms.Label label2;
			System.Windows.Forms.TabPage m_tpWritingSystems;
			System.Windows.Forms.Label label9;
			System.Windows.Forms.Label label8;
			System.Windows.Forms.Label label6;
			System.Windows.Forms.TabPage m_tpExternalLinks;
			System.Windows.Forms.Label label13;
			System.Windows.Forms.Button btnExtLnkBrowse;
			System.Windows.Forms.Label label12;
			System.Windows.Forms.Label label11;
			System.Windows.Forms.Label label10;
			this.m_txtProjDescription = new System.Windows.Forms.TextBox();
			this.m_lblProjModifiedDate = new System.Windows.Forms.Label();
			this.m_lblProjCreatedDate = new System.Windows.Forms.Label();
			this.m_lblServerName = new System.Windows.Forms.Label();
			this.m_lblProjName = new System.Windows.Forms.Label();
			this.m_txtProjName = new System.Windows.Forms.TextBox();
			this.m_btnAnalMoveUp = new System.Windows.Forms.Button();
			this.m_btnAnalMoveDown = new System.Windows.Forms.Button();
			this.m_btnDelAnalWs = new System.Windows.Forms.Button();
			this.m_btnModifyAnalWs = new System.Windows.Forms.Button();
			this.m_btnAddAnalWs = new System.Windows.Forms.Button();
			this.m_btnVernMoveUp = new System.Windows.Forms.Button();
			this.m_btnVernMoveDown = new System.Windows.Forms.Button();
			this.m_btnDelVernWs = new System.Windows.Forms.Button();
			this.m_btnModifyVernWs = new System.Windows.Forms.Button();
			this.m_btnAddVernWs = new System.Windows.Forms.Button();
			this.m_lstAnalWs = new System.Windows.Forms.CheckedListBox();
			this.m_lstVernWs = new System.Windows.Forms.CheckedListBox();
			this.txtExtLnkEdit = new System.Windows.Forms.TextBox();
			this.m_btnOK = new System.Windows.Forms.Button();
			this.m_tabControl = new System.Windows.Forms.TabControl();
			this.m_toolTip = new System.Windows.Forms.ToolTip(this.components);
			this.m_cmnuAddWs = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.menuItem2 = new System.Windows.Forms.ToolStripMenuItem();
			this.folderBrowserDlg = new System.Windows.Forms.FolderBrowserDialog();
			this.helpProvider1 = new System.Windows.Forms.HelpProvider();
			m_btnHelp = new System.Windows.Forms.Button();
			m_btnCancel = new System.Windows.Forms.Button();
			m_tpGeneral = new System.Windows.Forms.TabPage();
			lineControl3 = new SIL.FieldWorks.Common.Controls.LineControl();
			lineControl2 = new SIL.FieldWorks.Common.Controls.LineControl();
			lineControl1 = new SIL.FieldWorks.Common.Controls.LineControl();
			m_picLangProgFileBox = new System.Windows.Forms.PictureBox();
			label3 = new System.Windows.Forms.Label();
			label7 = new System.Windows.Forms.Label();
			label1 = new System.Windows.Forms.Label();
			label5 = new System.Windows.Forms.Label();
			label2 = new System.Windows.Forms.Label();
			m_tpWritingSystems = new System.Windows.Forms.TabPage();
			label9 = new System.Windows.Forms.Label();
			label8 = new System.Windows.Forms.Label();
			label6 = new System.Windows.Forms.Label();
			m_tpExternalLinks = new System.Windows.Forms.TabPage();
			label13 = new System.Windows.Forms.Label();
			btnExtLnkBrowse = new System.Windows.Forms.Button();
			label12 = new System.Windows.Forms.Label();
			label11 = new System.Windows.Forms.Label();
			label10 = new System.Windows.Forms.Label();
			m_tpGeneral.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(m_picLangProgFileBox)).BeginInit();
			m_tpWritingSystems.SuspendLayout();
			m_tpExternalLinks.SuspendLayout();
			this.m_tabControl.SuspendLayout();
			this.m_cmnuAddWs.SuspendLayout();
			this.SuspendLayout();
			//
			// m_btnHelp
			//
			resources.ApplyResources(m_btnHelp, "m_btnHelp");
			this.helpProvider1.SetHelpString(m_btnHelp, resources.GetString("m_btnHelp.HelpString"));
			m_btnHelp.Name = "m_btnHelp";
			this.helpProvider1.SetShowHelp(m_btnHelp, ((bool)(resources.GetObject("m_btnHelp.ShowHelp"))));
			m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// m_btnCancel
			//
			resources.ApplyResources(m_btnCancel, "m_btnCancel");
			m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.helpProvider1.SetHelpString(m_btnCancel, resources.GetString("m_btnCancel.HelpString"));
			m_btnCancel.Name = "m_btnCancel";
			this.helpProvider1.SetShowHelp(m_btnCancel, ((bool)(resources.GetObject("m_btnCancel.ShowHelp"))));
			//
			// m_tpGeneral
			//
			m_tpGeneral.Controls.Add(lineControl3);
			m_tpGeneral.Controls.Add(lineControl2);
			m_tpGeneral.Controls.Add(lineControl1);
			m_tpGeneral.Controls.Add(m_picLangProgFileBox);
			m_tpGeneral.Controls.Add(this.m_txtProjDescription);
			m_tpGeneral.Controls.Add(label3);
			m_tpGeneral.Controls.Add(this.m_lblProjModifiedDate);
			m_tpGeneral.Controls.Add(label7);
			m_tpGeneral.Controls.Add(this.m_lblProjCreatedDate);
			m_tpGeneral.Controls.Add(label1);
			m_tpGeneral.Controls.Add(label5);
			m_tpGeneral.Controls.Add(this.m_lblServerName);
			m_tpGeneral.Controls.Add(label2);
			m_tpGeneral.Controls.Add(this.m_lblProjName);
			m_tpGeneral.Controls.Add(this.m_txtProjName);
			resources.ApplyResources(m_tpGeneral, "m_tpGeneral");
			m_tpGeneral.Name = "m_tpGeneral";
			this.helpProvider1.SetShowHelp(m_tpGeneral, ((bool)(resources.GetObject("m_tpGeneral.ShowHelp"))));
			m_tpGeneral.UseVisualStyleBackColor = true;
			//
			// lineControl3
			//
			lineControl3.BackColor = System.Drawing.Color.Transparent;
			lineControl3.ForeColor = System.Drawing.SystemColors.ControlDark;
			lineControl3.ForeColor2 = System.Drawing.Color.Transparent;
			lineControl3.LinearGradientMode = System.Drawing.Drawing2D.LinearGradientMode.Horizontal;
			resources.ApplyResources(lineControl3, "lineControl3");
			lineControl3.Name = "lineControl3";
			this.helpProvider1.SetShowHelp(lineControl3, ((bool)(resources.GetObject("lineControl3.ShowHelp"))));
			//
			// lineControl2
			//
			lineControl2.BackColor = System.Drawing.Color.Transparent;
			lineControl2.ForeColor = System.Drawing.SystemColors.ControlDark;
			lineControl2.ForeColor2 = System.Drawing.Color.Transparent;
			lineControl2.LinearGradientMode = System.Drawing.Drawing2D.LinearGradientMode.Horizontal;
			resources.ApplyResources(lineControl2, "lineControl2");
			lineControl2.Name = "lineControl2";
			this.helpProvider1.SetShowHelp(lineControl2, ((bool)(resources.GetObject("lineControl2.ShowHelp"))));
			//
			// lineControl1
			//
			lineControl1.BackColor = System.Drawing.Color.Transparent;
			lineControl1.ForeColor = System.Drawing.SystemColors.ControlDark;
			lineControl1.ForeColor2 = System.Drawing.Color.Transparent;
			lineControl1.LinearGradientMode = System.Drawing.Drawing2D.LinearGradientMode.Horizontal;
			resources.ApplyResources(lineControl1, "lineControl1");
			lineControl1.Name = "lineControl1";
			//
			// m_picLangProgFileBox
			//
			resources.ApplyResources(m_picLangProgFileBox, "m_picLangProgFileBox");
			m_picLangProgFileBox.Name = "m_picLangProgFileBox";
			this.helpProvider1.SetShowHelp(m_picLangProgFileBox, ((bool)(resources.GetObject("m_picLangProgFileBox.ShowHelp"))));
			m_picLangProgFileBox.TabStop = false;
			//
			// m_txtProjDescription
			//
			this.helpProvider1.SetHelpString(this.m_txtProjDescription, resources.GetString("m_txtProjDescription.HelpString"));
			resources.ApplyResources(this.m_txtProjDescription, "m_txtProjDescription");
			this.m_txtProjDescription.Name = "m_txtProjDescription";
			this.helpProvider1.SetShowHelp(this.m_txtProjDescription, ((bool)(resources.GetObject("m_txtProjDescription.ShowHelp"))));
			//
			// label3
			//
			resources.ApplyResources(label3, "label3");
			label3.Name = "label3";
			this.helpProvider1.SetShowHelp(label3, ((bool)(resources.GetObject("label3.ShowHelp"))));
			//
			// m_lblProjModifiedDate
			//
			resources.ApplyResources(this.m_lblProjModifiedDate, "m_lblProjModifiedDate");
			this.m_lblProjModifiedDate.Name = "m_lblProjModifiedDate";
			this.helpProvider1.SetShowHelp(this.m_lblProjModifiedDate, ((bool)(resources.GetObject("m_lblProjModifiedDate.ShowHelp"))));
			//
			// label7
			//
			resources.ApplyResources(label7, "label7");
			label7.Name = "label7";
			this.helpProvider1.SetShowHelp(label7, ((bool)(resources.GetObject("label7.ShowHelp"))));
			//
			// m_lblProjCreatedDate
			//
			resources.ApplyResources(this.m_lblProjCreatedDate, "m_lblProjCreatedDate");
			this.m_lblProjCreatedDate.Name = "m_lblProjCreatedDate";
			this.helpProvider1.SetShowHelp(this.m_lblProjCreatedDate, ((bool)(resources.GetObject("m_lblProjCreatedDate.ShowHelp"))));
			//
			// label1
			//
			resources.ApplyResources(label1, "label1");
			label1.Name = "label1";
			this.helpProvider1.SetShowHelp(label1, ((bool)(resources.GetObject("label1.ShowHelp"))));
			//
			// label5
			//
			resources.ApplyResources(label5, "label5");
			label5.Name = "label5";
			this.helpProvider1.SetShowHelp(label5, ((bool)(resources.GetObject("label5.ShowHelp"))));
			//
			// m_lblServerName
			//
			resources.ApplyResources(this.m_lblServerName, "m_lblServerName");
			this.m_lblServerName.Name = "m_lblServerName";
			this.helpProvider1.SetShowHelp(this.m_lblServerName, ((bool)(resources.GetObject("m_lblServerName.ShowHelp"))));
			//
			// label2
			//
			resources.ApplyResources(label2, "label2");
			label2.Name = "label2";
			this.helpProvider1.SetShowHelp(label2, ((bool)(resources.GetObject("label2.ShowHelp"))));
			//
			// m_lblProjName
			//
			resources.ApplyResources(this.m_lblProjName, "m_lblProjName");
			this.m_lblProjName.Name = "m_lblProjName";
			this.helpProvider1.SetShowHelp(this.m_lblProjName, ((bool)(resources.GetObject("m_lblProjName.ShowHelp"))));
			//
			// m_txtProjName
			//
			this.helpProvider1.SetHelpString(this.m_txtProjName, resources.GetString("m_txtProjName.HelpString"));
			resources.ApplyResources(this.m_txtProjName, "m_txtProjName");
			this.m_txtProjName.Name = "m_txtProjName";
			this.helpProvider1.SetShowHelp(this.m_txtProjName, ((bool)(resources.GetObject("m_txtProjName.ShowHelp"))));
			this.m_txtProjName.TextChanged += new System.EventHandler(this.m_txtProjName_TextChanged);
			//
			// m_tpWritingSystems
			//
			m_tpWritingSystems.Controls.Add(this.m_btnAnalMoveUp);
			m_tpWritingSystems.Controls.Add(this.m_btnAnalMoveDown);
			m_tpWritingSystems.Controls.Add(this.m_btnDelAnalWs);
			m_tpWritingSystems.Controls.Add(this.m_btnModifyAnalWs);
			m_tpWritingSystems.Controls.Add(this.m_btnAddAnalWs);
			m_tpWritingSystems.Controls.Add(this.m_btnVernMoveUp);
			m_tpWritingSystems.Controls.Add(this.m_btnVernMoveDown);
			m_tpWritingSystems.Controls.Add(this.m_btnDelVernWs);
			m_tpWritingSystems.Controls.Add(this.m_btnModifyVernWs);
			m_tpWritingSystems.Controls.Add(this.m_btnAddVernWs);
			m_tpWritingSystems.Controls.Add(this.m_lstAnalWs);
			m_tpWritingSystems.Controls.Add(this.m_lstVernWs);
			m_tpWritingSystems.Controls.Add(label9);
			m_tpWritingSystems.Controls.Add(label8);
			m_tpWritingSystems.Controls.Add(label6);
			resources.ApplyResources(m_tpWritingSystems, "m_tpWritingSystems");
			m_tpWritingSystems.Name = "m_tpWritingSystems";
			this.helpProvider1.SetShowHelp(m_tpWritingSystems, ((bool)(resources.GetObject("m_tpWritingSystems.ShowHelp"))));
			m_tpWritingSystems.UseVisualStyleBackColor = true;
			//
			// m_btnAnalMoveUp
			//
			resources.ApplyResources(this.m_btnAnalMoveUp, "m_btnAnalMoveUp");
			this.helpProvider1.SetHelpString(this.m_btnAnalMoveUp, resources.GetString("m_btnAnalMoveUp.HelpString"));
			this.m_btnAnalMoveUp.Name = "m_btnAnalMoveUp";
			this.helpProvider1.SetShowHelp(this.m_btnAnalMoveUp, ((bool)(resources.GetObject("m_btnAnalMoveUp.ShowHelp"))));
			this.m_btnAnalMoveUp.Click += new System.EventHandler(this.m_btnAnalMoveUp_Click);
			//
			// m_btnAnalMoveDown
			//
			resources.ApplyResources(this.m_btnAnalMoveDown, "m_btnAnalMoveDown");
			this.helpProvider1.SetHelpString(this.m_btnAnalMoveDown, resources.GetString("m_btnAnalMoveDown.HelpString"));
			this.m_btnAnalMoveDown.Name = "m_btnAnalMoveDown";
			this.helpProvider1.SetShowHelp(this.m_btnAnalMoveDown, ((bool)(resources.GetObject("m_btnAnalMoveDown.ShowHelp"))));
			this.m_btnAnalMoveDown.Click += new System.EventHandler(this.m_btnAnalMoveDown_Click);
			//
			// m_btnDelAnalWs
			//
			this.helpProvider1.SetHelpString(this.m_btnDelAnalWs, resources.GetString("m_btnDelAnalWs.HelpString"));
			resources.ApplyResources(this.m_btnDelAnalWs, "m_btnDelAnalWs");
			this.m_btnDelAnalWs.Name = "m_btnDelAnalWs";
			this.helpProvider1.SetShowHelp(this.m_btnDelAnalWs, ((bool)(resources.GetObject("m_btnDelAnalWs.ShowHelp"))));
			this.m_btnDelAnalWs.Click += new System.EventHandler(this.m_btnDelAnalWs_Click);
			//
			// m_btnModifyAnalWs
			//
			this.helpProvider1.SetHelpString(this.m_btnModifyAnalWs, resources.GetString("m_btnModifyAnalWs.HelpString"));
			resources.ApplyResources(this.m_btnModifyAnalWs, "m_btnModifyAnalWs");
			this.m_btnModifyAnalWs.Name = "m_btnModifyAnalWs";
			this.helpProvider1.SetShowHelp(this.m_btnModifyAnalWs, ((bool)(resources.GetObject("m_btnModifyAnalWs.ShowHelp"))));
			this.m_btnModifyAnalWs.Click += new System.EventHandler(this.m_btnModifyAnalWs_Click);
			//
			// m_btnAddAnalWs
			//
			this.helpProvider1.SetHelpString(this.m_btnAddAnalWs, resources.GetString("m_btnAddAnalWs.HelpString"));
			resources.ApplyResources(this.m_btnAddAnalWs, "m_btnAddAnalWs");
			this.m_btnAddAnalWs.Name = "m_btnAddAnalWs";
			this.helpProvider1.SetShowHelp(this.m_btnAddAnalWs, ((bool)(resources.GetObject("m_btnAddAnalWs.ShowHelp"))));
			this.m_btnAddAnalWs.Click += new System.EventHandler(this.m_btnAddAnalWs_Click);
			//
			// m_btnVernMoveUp
			//
			resources.ApplyResources(this.m_btnVernMoveUp, "m_btnVernMoveUp");
			this.helpProvider1.SetHelpString(this.m_btnVernMoveUp, resources.GetString("m_btnVernMoveUp.HelpString"));
			this.m_btnVernMoveUp.Name = "m_btnVernMoveUp";
			this.helpProvider1.SetShowHelp(this.m_btnVernMoveUp, ((bool)(resources.GetObject("m_btnVernMoveUp.ShowHelp"))));
			this.m_btnVernMoveUp.Click += new System.EventHandler(this.m_btnVernMoveUp_Click);
			//
			// m_btnVernMoveDown
			//
			resources.ApplyResources(this.m_btnVernMoveDown, "m_btnVernMoveDown");
			this.helpProvider1.SetHelpKeyword(this.m_btnVernMoveDown, global::SIL.FieldWorks.FwCoreDlgs.FwCoreDlgs.kstidOpen);
			this.helpProvider1.SetHelpString(this.m_btnVernMoveDown, resources.GetString("m_btnVernMoveDown.HelpString"));
			this.m_btnVernMoveDown.Name = "m_btnVernMoveDown";
			this.helpProvider1.SetShowHelp(this.m_btnVernMoveDown, ((bool)(resources.GetObject("m_btnVernMoveDown.ShowHelp"))));
			this.m_btnVernMoveDown.Click += new System.EventHandler(this.m_btnVernMoveDown_Click);
			//
			// m_btnDelVernWs
			//
			this.helpProvider1.SetHelpString(this.m_btnDelVernWs, resources.GetString("m_btnDelVernWs.HelpString"));
			resources.ApplyResources(this.m_btnDelVernWs, "m_btnDelVernWs");
			this.m_btnDelVernWs.Name = "m_btnDelVernWs";
			this.helpProvider1.SetShowHelp(this.m_btnDelVernWs, ((bool)(resources.GetObject("m_btnDelVernWs.ShowHelp"))));
			this.m_btnDelVernWs.Click += new System.EventHandler(this.m_btnDelVernWs_Click);
			//
			// m_btnModifyVernWs
			//
			this.helpProvider1.SetHelpString(this.m_btnModifyVernWs, resources.GetString("m_btnModifyVernWs.HelpString"));
			resources.ApplyResources(this.m_btnModifyVernWs, "m_btnModifyVernWs");
			this.m_btnModifyVernWs.Name = "m_btnModifyVernWs";
			this.helpProvider1.SetShowHelp(this.m_btnModifyVernWs, ((bool)(resources.GetObject("m_btnModifyVernWs.ShowHelp"))));
			this.m_btnModifyVernWs.Click += new System.EventHandler(this.m_btnModifyVernWs_Click);
			//
			// m_btnAddVernWs
			//
			this.helpProvider1.SetHelpString(this.m_btnAddVernWs, resources.GetString("m_btnAddVernWs.HelpString"));
			resources.ApplyResources(this.m_btnAddVernWs, "m_btnAddVernWs");
			this.m_btnAddVernWs.Name = "m_btnAddVernWs";
			this.helpProvider1.SetShowHelp(this.m_btnAddVernWs, ((bool)(resources.GetObject("m_btnAddVernWs.ShowHelp"))));
			this.m_btnAddVernWs.Click += new System.EventHandler(this.m_btnAddVernWs_Click);
			//
			// m_lstAnalWs
			//
			this.helpProvider1.SetHelpString(this.m_lstAnalWs, resources.GetString("m_lstAnalWs.HelpString"));
			resources.ApplyResources(this.m_lstAnalWs, "m_lstAnalWs");
			this.m_lstAnalWs.Name = "m_lstAnalWs";
			this.helpProvider1.SetShowHelp(this.m_lstAnalWs, ((bool)(resources.GetObject("m_lstAnalWs.ShowHelp"))));
			this.m_lstAnalWs.ThreeDCheckBoxes = true;
			this.m_lstAnalWs.SelectedIndexChanged += new System.EventHandler(this.m_lstAnalWs_SelectedIndexChanged);
			this.m_lstAnalWs.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.m_lstAnalWs_ItemCheck);
			//
			// m_lstVernWs
			//
			this.helpProvider1.SetHelpString(this.m_lstVernWs, resources.GetString("m_lstVernWs.HelpString"));
			resources.ApplyResources(this.m_lstVernWs, "m_lstVernWs");
			this.m_lstVernWs.Name = "m_lstVernWs";
			this.helpProvider1.SetShowHelp(this.m_lstVernWs, ((bool)(resources.GetObject("m_lstVernWs.ShowHelp"))));
			this.m_lstVernWs.ThreeDCheckBoxes = true;
			this.m_lstVernWs.SelectedIndexChanged += new System.EventHandler(this.m_lstVernWs_SelectedIndexChanged);
			this.m_lstVernWs.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.m_lstVernWs_ItemCheck);
			//
			// label9
			//
			resources.ApplyResources(label9, "label9");
			label9.Name = "label9";
			this.helpProvider1.SetShowHelp(label9, ((bool)(resources.GetObject("label9.ShowHelp"))));
			//
			// label8
			//
			resources.ApplyResources(label8, "label8");
			label8.Name = "label8";
			this.helpProvider1.SetShowHelp(label8, ((bool)(resources.GetObject("label8.ShowHelp"))));
			//
			// label6
			//
			resources.ApplyResources(label6, "label6");
			label6.Name = "label6";
			this.helpProvider1.SetShowHelp(label6, ((bool)(resources.GetObject("label6.ShowHelp"))));
			//
			// m_tpExternalLinks
			//
			m_tpExternalLinks.Controls.Add(label13);
			m_tpExternalLinks.Controls.Add(btnExtLnkBrowse);
			m_tpExternalLinks.Controls.Add(this.txtExtLnkEdit);
			m_tpExternalLinks.Controls.Add(label12);
			m_tpExternalLinks.Controls.Add(label11);
			m_tpExternalLinks.Controls.Add(label10);
			resources.ApplyResources(m_tpExternalLinks, "m_tpExternalLinks");
			m_tpExternalLinks.Name = "m_tpExternalLinks";
			this.helpProvider1.SetShowHelp(m_tpExternalLinks, ((bool)(resources.GetObject("m_tpExternalLinks.ShowHelp"))));
			m_tpExternalLinks.UseVisualStyleBackColor = true;
			//
			// label13
			//
			resources.ApplyResources(label13, "label13");
			label13.Name = "label13";
			this.helpProvider1.SetShowHelp(label13, ((bool)(resources.GetObject("label13.ShowHelp"))));
			//
			// btnExtLnkBrowse
			//
			this.helpProvider1.SetHelpString(btnExtLnkBrowse, resources.GetString("btnExtLnkBrowse.HelpString"));
			resources.ApplyResources(btnExtLnkBrowse, "btnExtLnkBrowse");
			btnExtLnkBrowse.Name = "btnExtLnkBrowse";
			this.helpProvider1.SetShowHelp(btnExtLnkBrowse, ((bool)(resources.GetObject("btnExtLnkBrowse.ShowHelp"))));
			btnExtLnkBrowse.Click += new System.EventHandler(this.btnExtLnkBrowse_Click);
			//
			// txtExtLnkEdit
			//
			this.helpProvider1.SetHelpString(this.txtExtLnkEdit, resources.GetString("txtExtLnkEdit.HelpString"));
			resources.ApplyResources(this.txtExtLnkEdit, "txtExtLnkEdit");
			this.txtExtLnkEdit.Name = "txtExtLnkEdit";
			this.helpProvider1.SetShowHelp(this.txtExtLnkEdit, ((bool)(resources.GetObject("txtExtLnkEdit.ShowHelp"))));
			//
			// label12
			//
			resources.ApplyResources(label12, "label12");
			label12.Name = "label12";
			this.helpProvider1.SetShowHelp(label12, ((bool)(resources.GetObject("label12.ShowHelp"))));
			//
			// label11
			//
			resources.ApplyResources(label11, "label11");
			label11.Name = "label11";
			this.helpProvider1.SetShowHelp(label11, ((bool)(resources.GetObject("label11.ShowHelp"))));
			//
			// label10
			//
			resources.ApplyResources(label10, "label10");
			label10.Name = "label10";
			this.helpProvider1.SetShowHelp(label10, ((bool)(resources.GetObject("label10.ShowHelp"))));
			//
			// m_btnOK
			//
			resources.ApplyResources(this.m_btnOK, "m_btnOK");
			this.m_btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.helpProvider1.SetHelpString(this.m_btnOK, resources.GetString("m_btnOK.HelpString"));
			this.m_btnOK.Name = "m_btnOK";
			this.helpProvider1.SetShowHelp(this.m_btnOK, ((bool)(resources.GetObject("m_btnOK.ShowHelp"))));
			this.m_btnOK.Click += new System.EventHandler(this.m_btnOK_Click);
			//
			// m_tabControl
			//
			this.m_tabControl.Controls.Add(m_tpGeneral);
			this.m_tabControl.Controls.Add(m_tpWritingSystems);
			this.m_tabControl.Controls.Add(m_tpExternalLinks);
			resources.ApplyResources(this.m_tabControl, "m_tabControl");
			this.m_tabControl.Name = "m_tabControl";
			this.m_tabControl.SelectedIndex = 0;
			this.helpProvider1.SetShowHelp(this.m_tabControl, ((bool)(resources.GetObject("m_tabControl.ShowHelp"))));
			//
			// m_cmnuAddWs
			//
			this.m_cmnuAddWs.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.menuItem2});
			this.m_cmnuAddWs.Name = "m_cmnuAddWs";
			resources.ApplyResources(this.m_cmnuAddWs, "m_cmnuAddWs");
			//
			// menuItem2
			//
			this.menuItem2.Name = "menuItem2";
			resources.ApplyResources(this.menuItem2, "menuItem2");
			//
			// folderBrowserDlg
			//
			resources.ApplyResources(this.folderBrowserDlg, "folderBrowserDlg");
			//
			// FwProjPropertiesDlg
			//
			this.AcceptButton = this.m_btnOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = m_btnCancel;
			this.Controls.Add(this.m_tabControl);
			this.Controls.Add(m_btnCancel);
			this.Controls.Add(m_btnHelp);
			this.Controls.Add(this.m_btnOK);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.helpProvider1.SetHelpString(this, global::SIL.FieldWorks.FwCoreDlgs.FwCoreDlgs.kstidOpen);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FwProjPropertiesDlg";
			this.helpProvider1.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			this.ShowInTaskbar = false;
			m_tpGeneral.ResumeLayout(false);
			m_tpGeneral.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(m_picLangProgFileBox)).EndInit();
			m_tpWritingSystems.ResumeLayout(false);
			m_tpWritingSystems.PerformLayout();
			m_tpExternalLinks.ResumeLayout(false);
			m_tpExternalLinks.PerformLayout();
			this.m_tabControl.ResumeLayout(false);
			this.m_cmnuAddWs.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		#region implementation of IFwProjPropertiesDlg
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the dialog as a modal dialog
		/// </summary>
		/// <returns>A DialogResult value</returns>
		/// ------------------------------------------------------------------------------------
		public int ShowDlg()
		{
			CheckDisposed();

			return (int)ShowDialog();
		}

		/// <summary>
		/// Return true if something in the active writing system lists changed.
		/// </summary>
		/// <returns></returns>
		public bool WritingSystemsChanged()
		{
			CheckDisposed();

			return m_fWsChanged;
		}

		/// <summary>
		/// Return true if a sort specification changed in a writing system.
		/// </summary>
		/// <returns></returns>
		public bool SortChanged()
		{
			CheckDisposed();

			return m_fWsSortChanged;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns <c>true</c> if, for one or more writing systems, a font or a font property
		/// changed, or the Right-To-Left flag changed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool NewRenderingNeeded()
		{
			CheckDisposed();

			return m_fNewRendering;
		}

		/// <summary>
		/// Return true if the project name changed.
		/// </summary>
		/// <returns></returns>
		public bool ProjectNameChanged()
		{
			CheckDisposed();

			return m_fProjNameChanged;
		}

		/// <summary>
		/// Return true if the external link directory changed.
		/// </summary>
		/// <returns></returns>
		public bool ExternalLinkChanged()
		{
			CheckDisposed();

			return m_fExtLnkChanged;
		}

		/// <summary>
		/// Dispose of the dialog when done with it.
		/// </summary>
		public void DisposeDialog()
		{
			CheckDisposed();

			Dispose(true);
		}
		#endregion

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_lstVernWs_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			m_btnVernMoveDown.Enabled = (m_lstVernWs.SelectedIndex <
				m_lstVernWs.Items.Count - 1);
			m_btnVernMoveUp.Enabled = (m_lstVernWs.SelectedIndex > 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_lstAnalWs_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			m_btnAnalMoveDown.Enabled = (m_lstAnalWs.SelectedIndex <
				m_lstAnalWs.Items.Count - 1);
			m_btnAnalMoveUp.Enabled = (m_lstAnalWs.SelectedIndex > 0);
		}

		#endregion

		#region Button Click Events
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void m_btnOK_Click(object sender, System.EventArgs e)
		{
			using (new WaitCursor(this))
			{
				// save Vernacular Ws
				SaveWs(true);
				// save Analysis Ws
				SaveWs(false);

				if (m_txtProjName.Text != m_sOrigProjName)
				{
					DialogResult dr = MessageBox.Show(FwCoreDlgs.kstidChangingProjectName,
						FwCoreDlgs.kstidWarning, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
					if (dr == DialogResult.Yes)
					{
						if (m_langProj.Name.UserDefaultWritingSystem != m_txtProjName.Text)
							m_langProj.Name.UserDefaultWritingSystem = m_txtProjName.Text;
						m_fProjNameChanged = true;
					}
				}
				if (m_langProj.Description.GetAlternative(m_cache.DefaultUserWs).Text != m_txtProjDescription.Text)
					m_langProj.Description.SetUserDefaultWritingSystem(m_txtProjDescription.Text);

				m_langProj.CacheDefaultWritingSystems();

				string sOldExtLinkRootDir = m_langProj.ExternalLinkRootDir;
				string sNewExtLinkRootDir = txtExtLnkEdit.Text;
				if (!String.IsNullOrEmpty(sNewExtLinkRootDir) &&
					sOldExtLinkRootDir != sNewExtLinkRootDir)
				{
					m_langProj.ExtLinkRootDir = sNewExtLinkRootDir;
					// Create the directory if it doesn't exist.
					if (!Directory.Exists(sNewExtLinkRootDir))
						Directory.CreateDirectory(sNewExtLinkRootDir);
					m_fExtLnkChanged = true;
				}

				// Don't process WritingSystemsChanged here since it gets handled in different
				// ways by calling apps and we don't want the extra overhread here.

				Close();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void m_btnVernMoveDown_Click(object sender, System.EventArgs e)
		{
			MoveListItem(m_lstVernWs, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void m_btnVernMoveUp_Click(object sender, System.EventArgs e)
		{
			MoveListItem(m_lstVernWs, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void m_btnAnalMoveDown_Click(object sender, System.EventArgs e)
		{
			MoveListItem(m_lstAnalWs, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void m_btnAnalMoveUp_Click(object sender, System.EventArgs e)
		{
			MoveListItem(m_lstAnalWs, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void m_btnAddVernWs_Click(object sender, System.EventArgs e)
		{
			ShowAddWsContextMenu(m_lstVernWs, m_btnAddVernWs, m_cmnuAddNewWsFromSelectedVernWs_Click);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void m_btnAddAnalWs_Click(object sender, System.EventArgs e)
		{
			ShowAddWsContextMenu(m_lstAnalWs, m_btnAddAnalWs, m_cmnuAddNewWsFromSelectedAnalWs_Click);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates and displays the context menu for adding a writing system.
		/// </summary>
		/// <param name="listToAddTo">List of writing systems (vernacular/analysis)</param>
		/// <param name="button">Add Writing System button (vernacular/analysis)</param>
		/// <param name="clickHandlerNewWsFromSelected">the handler for adding a new ws related to the selected ws</param>
		/// ------------------------------------------------------------------------------------
		private void ShowAddWsContextMenu(CheckedListBox listToAddTo, Button button, EventHandler clickHandlerNewWsFromSelected)
		{
			using (new WaitCursor(this))
			{
				ShowAddWsContextMenu(m_cmnuAddWs, m_langProj.GetAllNamedWritingSystems(),
					listToAddTo, button, m_cmnuAddWs_Click, m_cmnuAddWs_Click,
					clickHandlerNewWsFromSelected, GetCurrentSelectedWs(listToAddTo));
			}
		}

		static internal void ShowAddWsContextMenu(ContextMenuStrip cmnuAddWs,
			Set<NamedWritingSystem> wssToAdd, ListBox listToAddTo, Button button,
			EventHandler clickHandlerExistingWs, EventHandler clickHandlerNewWs,
			EventHandler clickHandlerNewWsFromSelected, IWritingSystem selectedWs)
		{
			try
			{
				PopulateWsContextMenu(cmnuAddWs, wssToAdd, listToAddTo, clickHandlerExistingWs, clickHandlerNewWs,
					clickHandlerNewWsFromSelected, selectedWs);
				cmnuAddWs.Show(button, new Point(0, button.Height));
			}
			catch (Exception e)
			{
				Control owner = cmnuAddWs.FindForm().Owner;
				MessageBox.Show(owner,
					string.Format(ResourceHelper.GetResourceString("kstidMiscErrorWithMessage"), e.Message),
					ResourceHelper.GetResourceString("kstidMiscError"));
			}
		}

		static internal void PopulateWsContextMenu(ContextMenuStrip cmnuAddWs, Set<NamedWritingSystem> wssToAdd,
			ListBox listToAddTo, EventHandler clickHandlerExistingWs, EventHandler clickHandlerNewWs,
			EventHandler clickHandlerNewWsFromSelected, IWritingSystem selectedWs)
		{
			cmnuAddWs.Items.Clear();
			cmnuAddWs.Tag = listToAddTo;
			// Add the "Writing system for <language>..." menu item.
			Set<NamedWritingSystem> relatedWss = new Set<NamedWritingSystem>();
			if (clickHandlerNewWsFromSelected != null && selectedWs != null)
			{
				// Populate Context Menu with related wss.
				relatedWss = WritingSystemPropertiesDialog.GetRelatedWss(wssToAdd, selectedWs);
				AddExistingWssToContextMenu(cmnuAddWs, relatedWss, listToAddTo, clickHandlerExistingWs);
				LanguageDefinition langDef = new LanguageDefinition(selectedWs);
				ToolStripItem tsiNewWs = new ToolStripMenuItem(String.Format(FwCoreDlgs.ksWsNewFromExisting, langDef.LocaleName),
					null, clickHandlerNewWsFromSelected);
				cmnuAddWs.Items.Add(tsiNewWs);
			}

			// Add a separator and the "New..." menu item.
			if (clickHandlerNewWs != null)
			{
				AddExistingWssToContextMenu(cmnuAddWs, wssToAdd.Difference(relatedWss), listToAddTo, clickHandlerExistingWs);
				// Convert from Set to List, since the Set can't sort.
				if (cmnuAddWs.Items.Count > 0)
					cmnuAddWs.Items.Add(new ToolStripSeparator());
				ToolStripItem tsiNewWs = new ToolStripMenuItem(FwCoreDlgs.ksWsNew, null, clickHandlerNewWs);
				cmnuAddWs.Items.Add(tsiNewWs);
			}
		}

		private static void AddExistingWssToContextMenu(ContextMenuStrip cmnuAddWs,
			Set<NamedWritingSystem> wssToAdd, ListBox listToAddExistingTo, EventHandler clickHandlerExistingWs)
		{
			List<NamedWritingSystem> al = new List<NamedWritingSystem>(wssToAdd.ToArray());
			al.Sort();
			bool fAddDivider = cmnuAddWs.Items.Count > 0;
			foreach (NamedWritingSystem namedWs in al)
			{
				// Make sure we only add language names (actually ICULocales in case any
				// language names happen to be identical) that aren't already in the list box.
				bool fFound = false;
				for (int iws = 0; iws < listToAddExistingTo.Items.Count; ++iws)
				{
					object item = listToAddExistingTo.Items[iws];
					if (item is LgWritingSystem)
					{
						LgWritingSystem lws = (LgWritingSystem)item;
						if (lws.ICULocale == namedWs.IcuLocale)
						{
							fFound = true;
							break;
						}
					}
					else
					{
						// just compare the full names.
						if (item.ToString() == namedWs.ToString())
						{
							fFound = true;
							break;
						}
					}
				}
				if (!fFound)
				{
					if (fAddDivider)
					{
						cmnuAddWs.Items.Add(new ToolStripSeparator());
						fAddDivider = false;
					}
					WsMenuItem mnu = new WsMenuItem(namedWs, listToAddExistingTo, clickHandlerExistingWs);
					cmnuAddWs.Items.Add(mnu);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void m_btnDelVernWs_Click(object sender, System.EventArgs e)
		{
			DeleteListItem(m_lstVernWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void m_btnDelAnalWs_Click(object sender, System.EventArgs e)
		{
			DeleteListItem(m_lstAnalWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays the WritingSystemPropertiesDialog dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DisplayModifyWritingSystemProperties(bool addNewForLangOfSelectedWs)
		{
			IWritingSystem lgWsSelected = GetCurrentSelectedWs();

			DialogResult = DialogResult.None;
			using (WritingSystemPropertiesDialog wsProps = new WritingSystemPropertiesDialog(m_cache, m_helpTopicProvider, m_stylesheet))
			{
				wsProps.SetupForWsMerges(m_tool, m_strmLog, m_hvoProj, m_hvoRoot, m_wsUser);
				wsProps.OnAboutToMergeWritingSystems += new EventHandler(wsProps_OnAboutToMergeWritingSystems);
				wsProps.ActiveWss = GetAllActiveWssInDialog();
				if (!wsProps.TrySetupDialog(lgWsSelected))
					return;
				if (addNewForLangOfSelectedWs)
					wsProps.AddNewWsForLanguage();
				if (wsProps.ShowDialog() == DialogResult.OK)
				{
					// Note: wsProps can get Disposed if user performs a merge, since wsProps_OnAboutToMergeWritingSystems()
					// will Close our dialog and set the result to Abort.
					if (DialogResult != DialogResult.Abort)
						NoteChangesAndUpdateCache(wsProps);
					m_selectedList.Invalidate();
				}
			}
		}

		private IWritingSystem GetCurrentSelectedWs()
		{
			ListBox selectedList = m_selectedList;
			return GetCurrentSelectedWs(selectedList);
		}

		private IWritingSystem GetCurrentSelectedWs(ListBox selectedList)
		{
			LgWritingSystem wsSelected = (LgWritingSystem)selectedList.SelectedItem;
			if (wsSelected == null)
				return null;
			ILgWritingSystemFactory lgwsf = m_cache.LanguageWritingSystemFactoryAccessor;
			IWritingSystem lgWsSelected = lgwsf.get_Engine(wsSelected.ICULocale);
			return lgWsSelected;
		}

		private void NoteChangesAndUpdateCache(WritingSystemPropertiesDialog wsProps)
		{
			m_fWsChanged = wsProps.IsChanged;
			m_fNewRendering = wsProps.NewRenderingNeeded;
			m_fWsSortChanged = wsProps.SortChanged;

			if (wsProps.IsChanged)
			{
				m_cache.ResetLanguageEncodings();
				List<LanguageDefinition> newlyAdded = wsProps.NewlyAddedLanguageDefns();
				foreach (LanguageDefinition langDef in newlyAdded)
				{
					ILgWritingSystem ws = new LgWritingSystem(m_cache,
						m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(langDef.CurrentFullLocale()));
					AddWsToList(ws, m_selectedList);
				}
				foreach (LanguageDefinition langDef in wsProps.FinalLanguageDefns)
				{
					if (langDef.HasPendingMerge() || newlyAdded.Contains(langDef))
						continue;

					int wsIdOld = langDef.WsOriginal;
					// The display name may have changed and that means the name of the WS changed.
					// Therefore we have to call UpdatePropIfCached so that the new names
					// display in the dialog.
					m_cache.VwOleDbDaAccessor.UpdatePropIfCached(wsIdOld,
						(int)LgWritingSystem.LgWritingSystemTags.kflidName,
						(int)CellarModuleDefns.kcptMultiUnicode, langDef.WsUi);
					m_cache.VwOleDbDaAccessor.UpdatePropIfCached(wsIdOld,
						(int)LgWritingSystem.LgWritingSystemTags.kflidICULocale,
						(int)CellarModuleDefns.kcptUnicode, langDef.WsUi);
				}
			}
		}

		void wsProps_OnAboutToMergeWritingSystems(object sender, EventArgs e)
		{
			NoteChangesAndUpdateCache(sender as WritingSystemPropertiesDialog);
			// We need to merge this writing system into an existing one in the DB.
			DialogResult = DialogResult.Abort;

			// This is weird and annoying, but if we don't empty these puppies out, we crash.
			m_lstAnalWs.Items.Clear();
			m_lstVernWs.Items.Clear();

			Close();
		}



		private CheckedListBox m_selectedList = null;
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void m_btnModifyVernWs_Click(object sender, System.EventArgs e)
		{
			DisplayModifyWritingSystemProperties(m_lstVernWs, false);
		}

		private void DisplayModifyWritingSystemProperties(CheckedListBox selectedListBox,
			bool addNewForLangOfSelectedWs)
		{
			using (new WaitCursor(this))
			{
				try
				{
					m_selectedList = selectedListBox;
					DisplayModifyWritingSystemProperties(addNewForLangOfSelectedWs);
				}
				finally
				{
					m_selectedList = null;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void m_btnModifyAnalWs_Click(object sender, System.EventArgs e)
		{
			DisplayModifyWritingSystemProperties(m_lstAnalWs, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// External Link Browse
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void btnExtLnkBrowse_Click(object sender, System.EventArgs e)
		{
			folderBrowserDlg.SelectedPath = txtExtLnkEdit.Text;
			if(folderBrowserDlg.ShowDialog() == DialogResult.OK)
				txtExtLnkEdit.Text = folderBrowserDlg.SelectedPath;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle Help button click. Show Help.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_btnHelp_Click(object sender, System.EventArgs e)
		{
			string topicKey = null;

			switch (this.m_tabControl.SelectedIndex)
			{
				case kGeneralTab:
					topicKey = "ProjectProperties_General";
					break;
				case kWritingSystemTab:
					topicKey = "ProjectProperties_WritingSystem";
					break;
				case kExternalLinksTab:
					topicKey = "ProjectProperties_ExternalLinks";
					break;
			}

			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "UserHelpFile", topicKey);
		}
		#endregion

		#region Misc. Events
				/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void m_cmnuAddNewWsFromSelectedAnalWs_Click(object sender, System.EventArgs e)
		{
			DisplayModifyWritingSystemProperties(m_lstAnalWs, true);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected void m_cmnuAddNewWsFromSelectedVernWs_Click(object sender, System.EventArgs e)
		{
			DisplayModifyWritingSystemProperties(m_lstVernWs, true);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void m_cmnuAddWs_Click(object sender, System.EventArgs e)
		{
			// Get the index of the last menu item, which should be the index of the "New..."
			// option.
			ILgWritingSystem lws = null;
			WsMenuItem mnuItem = null;

			// If the menu item chosen wasn't the "New..." option, add the writing system.
			if (sender is WsMenuItem)
			{
				mnuItem = (WsMenuItem)sender;
				lws = mnuItem.WritingSystem.GetLgWritingSystem(m_cache);
			}
			else
			{
				InvokeWritingSystemWizard(m_cmnuAddWs,
					out lws, m_cache, m_helpTopicProvider);
			}
			if (lws != null)
			{
				ListBox listToAddTo = m_cmnuAddWs.Tag as ListBox;
				AddWsToList(lws, listToAddTo);
				UpdateButtons(listToAddTo);
			}
		}

		private void InvokeWritingSystemWizard(ContextMenuStrip cmnuAddWs, out ILgWritingSystem lws,
			FdoCache cache, IHelpTopicProvider helpTopicProvider)
		{
			lws = null;
			using (new WaitCursor(this))
			{
				using (WritingSystemWizard wiz = new WritingSystemWizard())
				{
					wiz.Init(cache.LanguageWritingSystemFactoryAccessor, helpTopicProvider);
					if (wiz.ShowDialog() == DialogResult.OK)
					{
						// The engine from the wizard isn't the real one, so it doesn't have an id.
						IWritingSystem wsEngine = wiz.WritingSystem();
						string strws = wsEngine.IcuLocale;
						ILgWritingSystemFactory wsf = cache.LanguageWritingSystemFactoryAccessor;
						wsEngine = wsf.get_Engine(strws);
						cache.ResetLanguageEncodings();
						lws = LgWritingSystem.CreateFromDBObject(cache, wsEngine.WritingSystem);
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_lstVernWs_ItemCheck(object sender, System.Windows.Forms.ItemCheckEventArgs e)
		{
			// Don't allow the last item to be unchecked
			// Leaving incase desired for future GUI changes
			//			if (m_lstVernWs.CheckedItems.Count == 1 && e.NewValue == CheckState.Unchecked)
			//			{
			//				e.NewValue = CheckState.Checked;
			//				return;	// no change to the ok button status
			//			}

			bool OkToExit = false;
			if (m_lstAnalWs.CheckedItems.Count >= 1)
			{
				if (e.NewValue == CheckState.Checked || m_lstVernWs.CheckedItems.Count > 1)
					OkToExit = true;
			}
			m_btnOK.Enabled = OkToExit;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_lstAnalWs_ItemCheck(object sender, System.Windows.Forms.ItemCheckEventArgs e)
		{
			bool OkToExit = false;
			if (m_lstVernWs.CheckedItems.Count >= 1)
			{
				if (e.NewValue == CheckState.Checked || m_lstAnalWs.CheckedItems.Count > 1)
					OkToExit = true;
			}
			m_btnOK.Enabled = OkToExit;
		}
		#endregion

		#region other methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateOKButton()
		{
			m_btnOK.Enabled = (m_lstVernWs.CheckedItems.Count >= 1 &&
				m_lstAnalWs.CheckedItems.Count >= 1);
			// Fix the Delete buttons as well while we're here.
			m_btnDelVernWs.Enabled = (m_lstVernWs.Items.Count >= 1);
			m_btnDelAnalWs.Enabled = (m_lstAnalWs.Items.Count >= 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="ws"></param>
		/// <param name="list"></param>
		/// ------------------------------------------------------------------------------------
		static internal protected void AddWsToList(ILgWritingSystem ws, ListBox list)
		{
			if (list is CheckedListBox)
				(list as CheckedListBox).Items.Add(ws, true);
			else
				list.Items.Add(ws);
			list.SelectedItem = ws;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="list"></param>
		protected void UpdateButtons(ListBox list)
		{
			(list == m_lstAnalWs ? m_btnDelAnalWs : m_btnDelVernWs).Enabled = true;
			(list == m_lstAnalWs ? m_btnAnalMoveUp : m_btnVernMoveUp).Enabled = true;
			(list == m_lstAnalWs ? m_btnAnalMoveDown : m_btnVernMoveDown).Enabled = true;
			IWritingSystem ws = GetCurrentSelectedWs(list);
			(list == m_lstAnalWs ? m_btnModifyAnalWs : m_btnModifyVernWs).Enabled = (ws != null);

			UpdateOKButton();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="list"></param>
		/// ------------------------------------------------------------------------------------
		protected void DeleteListItem(CheckedListBox list)
		{
			int index = list.SelectedIndex;

			if (list == m_lstAnalWs )//&& list.SelectedItem.ToString() == m_cache.DefaultUserWs)
			{
				LgWritingSystem ws = (LgWritingSystem)list.SelectedItem;
				string selICULocale = ws.ICULocale;
				// Save UI code for future when it's changed from "en" to UI
//				int wsUser = m_cache.LanguageWritingSystemFactoryAccessor.UserWs;
//				string icuUILocale = m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(wsUser);
//				if (icuUILocale == selICULocale)
				if ("en" == selICULocale && BringUpEnglishWarningMsg() != DialogResult.Yes)
					return;
			}

			list.Items.RemoveAt(index);

			if(index < list.Items.Count)
			{
				// restore the selection to the correct place
				list.SelectedIndex = index;
			}
			else if(list.Items.Count > 0)
			{
				// user deleted the last item in the list so move the selection up one
				list.SelectedIndex = list.Items.Count - 1;
			}
			else if(list.Items.Count == 0)
			{
				// user deleted all the items in the list so disable the OK button
				(list == m_lstAnalWs ? m_btnDelAnalWs : m_btnDelVernWs).Enabled = false;
				(list == m_lstAnalWs ? m_btnModifyAnalWs : m_btnModifyVernWs).Enabled = false;
				(list == m_lstAnalWs ? m_btnAnalMoveUp : m_btnVernMoveUp).Enabled = false;
				(list == m_lstAnalWs ? m_btnAnalMoveDown : m_btnVernMoveDown).Enabled = false;
			}
			UpdateOKButton();	// only OK to exit if each one has a checked ws
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Brings the up english warning MSG.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual DialogResult BringUpEnglishWarningMsg()
		{
			string caption = FwCoreDlgs.ksRemovingEnWs;
			string msg = FwCoreDlgs.ksRemovingEnWsMsg;

			DialogResult dr = MessageBox.Show(msg, caption, MessageBoxButtons.YesNo,
				MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);

			return dr;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="list"></param>
		/// <param name="moveDown"></param>
		/// ------------------------------------------------------------------------------------
		protected void MoveListItem(CheckedListBox list, bool moveDown)
		{
			System.Diagnostics.Debug.Assert(list != null);

			int index = list.SelectedIndex;
			bool isChecked = list.GetItemChecked(index);

			// Don't allow moving if there is no current item or moving down when the current
			// item is the last item in the list or moving up when the current item is the first
			// item in the list.
			if (index < 0 || (moveDown && index == list.Items.Count - 1) ||
				(!moveDown && index == 0))
				return;

			// Store the selected writing system and remove it.
			LgWritingSystem ws = (LgWritingSystem)list.SelectedItem;
			list.Items.RemoveAt(index);

			// Determine the new place in the list for the stored writing system. Then
			// insert it in its new location in the list.
			index += (moveDown ? 1 : -1);
			list.Items.Insert(index, ws);

			// Now restore the writing system's checked state and select it.
			list.SetItemChecked(index, isChecked);
			list.SelectedIndex = index;
		}

		/// <summary>
		/// Generate the list of current writing systems in both vernacular and analysis
		/// whose state is maintained by this dialog and changed by Add/Hide options.
		/// </summary>
		/// <returns></returns>
		Set<int> GetAllActiveWssInDialog()
		{
			Set<int> allActiveInDialog = new Set<int>();
			foreach (LgWritingSystem lgws in m_lstVernWs.Items)
			{
				allActiveInDialog.Add(lgws.Hvo);
			}
			foreach (LgWritingSystem lgws in m_lstAnalWs.Items)
			{
				allActiveInDialog.Add(lgws.Hvo);
			}
			return allActiveInDialog;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save the new list of writing systems to the database
		/// </summary>
		/// <param name="saveVern"><c>true</c> to save the list of vernacular writing systems,
		/// <c>false</c> to save analysis writing systems.</param>
		/// ------------------------------------------------------------------------------------
		protected void SaveWs(bool saveVern)
		{
			FdoReferenceSequence<ILgWritingSystem> currList = saveVern ?
				m_langProj.CurVernWssRS :
				m_langProj.CurAnalysisWssRS;

			FdoReferenceCollection<ILgWritingSystem> allList = saveVern ?
				m_langProj.VernWssRC :
				m_langProj.AnalysisWssRC;

			CheckedListBox lstBox = saveVern ? m_lstVernWs : m_lstAnalWs;

			// See if there are any changes
			int i = 0;
			bool fChanged = false;
			int[] checkedHvos = new int[lstBox.Items.Count];
			int it = 0;
			if (allList.Count == lstBox.Items.Count)
			{
				Set<int> allHvos = new Set<int>();
				foreach (ILgWritingSystem lws in allList)
					allHvos.Add(lws.Hvo);
				it = 0;
				for (i = 0; i < allList.Count; ++i)
				{
					ILgWritingSystem lws = (ILgWritingSystem)lstBox.Items[i];
					if (!allHvos.Contains(lws.Hvo))
					{
						fChanged = true;
						break;
					}
					if (lstBox.GetItemChecked(i))
					{
						checkedHvos.SetValue(lws.Hvo, it++);
					}
				}
			}
			else
				fChanged = true;

			if (currList.Count == it)
			{
				for (i = 0; i < currList.Count; ++i)
				{
					if (currList[i].Hvo != (int)checkedHvos.GetValue(i))
					{
						fChanged = true;
						break;
					}
				}
			}
			else
				fChanged = true;

			if (!fChanged)
				return; // No changes, so don't update the database.

			m_fWsChanged = true;
			int cBad = 0;
			// Remove all the items from the current writing systems list.
			while(currList.Count > 0)
			{
				try
				{
					currList.RemoveAt(0);
				}
				catch
				{
					++cBad;
					break;
				}
			}

			// Remove all the items from the writing systems list.
			i = 0;
			int[] hvos = new int[allList.Count];
			foreach (ILgWritingSystem lws in allList)
			{
				hvos[i++] = lws.Hvo;
			}
			for (i = 0; i < hvos.Length; i++)
			{
				try
				{
					allList.Remove(hvos[i]);
				}
				catch
				{
					++cBad;
				}
			}

			// TODO: we should use the database Sync$ table (through related methods on FdoCache)
			// to signal other programs, and to check before trying and failing.  This probably
			// take at least a couple of days to get working properly.  Meanwhile, the try/catch
			// and message box puts a barely effective patch on LT-2368.

			if (cBad != 0)
			{
				string sMsg = saveVern ?
					FwCoreDlgs.ksVernWsChangedMsg : FwCoreDlgs.ksAnalWsChangedMsg;
				MessageBox.Show(this, sMsg, FwCoreDlgs.ksFwWsSyncProblem);
			}

			// Now update the DB with the new (possibly new, that is) writing systems.
			for (i = 0; i < lstBox.Items.Count; i++)
			{
				if (lstBox.GetItemChecked(i))
					currList.Append((ILgWritingSystem)lstBox.Items[i]);
				allList.Add((ILgWritingSystem)lstBox.Items[i]);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the name when the data changes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_txtProjName_TextChanged(object sender, System.EventArgs e)
		{
			FwNewLangProject.CheckForValidProjectName(m_txtProjName);
			m_txtProjName.Text = m_txtProjName.Text.Normalize();
			m_btnOK.Enabled = m_txtProjName.Text.Trim().Length > 0;
			m_lblProjName.Text = m_txtProjName.Text;
		}

		#endregion

		#region Writing System Menu Item
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// We subclass the menu item so we can store a NamedWritingSystem for each menu item in
		/// the Add writing system popup list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal class WsMenuItem : ToolStripMenuItem
		{
			private NamedWritingSystem m_ws;
			private ListBox m_list;

			/// --------------------------------------------------------------------------------
			/// <summary>
			///
			/// </summary>
			/// <param name="ws"></param>
			/// <param name="list"></param>
			/// <param name="handler">OnClick event handler</param>
			/// --------------------------------------------------------------------------------
			public WsMenuItem(NamedWritingSystem ws, ListBox list, EventHandler handler)
				: base(ws.ToString(), null, handler)
			{
				m_ws = ws;
				m_list = list;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			///
			/// </summary>
			/// --------------------------------------------------------------------------------
			public NamedWritingSystem WritingSystem
			{
				get
				{
					return m_ws;
				}
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			///
			/// </summary>
			/// --------------------------------------------------------------------------------
			public ListBox ListBox
			{
				get
				{
					return m_list;
				}
			}
		}

		#endregion
	}
	#endregion //FwProjPropertiesDlg dialog
}
