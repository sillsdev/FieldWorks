using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Windows.Forms;
using FwRemoteDatabaseConnector;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FDO.Infrastructure.Impl;
using SIL.FieldWorks.Resources;
using SIL.ObjectBrowser;
using SIL.Utils;
using WeifenLuo.WinFormsUI.Docking;
using XCore;

namespace FDOBrowser
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// FDOBrowserForm Class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class FDOBrowserForm : ObjectBrowser, IHelpTopicProvider
	{
		#region Data members

		/// <summary>
		/// List of custom fields associated with the open project.
		/// </summary>
		public static List<CustomFields> CFields = new List<CustomFields>();
		/// <summary>
		/// Specifies whether the class was selected on the dialog (true) or not (false).
		/// </summary>
		public static bool m_dlgChanged;
		private FdoCache m_cache;
		private ILangProject m_lp;
		private ICmObjectRepository m_repoCmObject;
		private string m_fmtSelectPropsMenuText;
		private string m_fmtAddObjectMenuText;
		private ModelWnd m_modelWnd;
		private InspectorWnd m_langProjWnd;
		private InspectorWnd m_repositoryWnd;
		private int saveClid;
		private string FileName = string.Empty;
		private ThreadHelper m_ThreadHelper;
		//private FwTextBox m_textBox;
		//private System.Windows.Forms.Panel panel1;

		ToolStripMenuItem m_cmnuAddObject;
		ToolStripMenuItem m_cmnuMoveObjectUp;
		ToolStripMenuItem m_cmnuMoveObjectDown;
		ToolStripMenuItem m_cmnuDeleteObject;
		ToolStripMenuItem m_cmnuSelectProps;
		ToolStripButton m_tsbShowCmObjectProps;
		ToolStripMenuItem mnuDb4oToXml;
		ToolStripMenuItem m_mnuView;
		ToolStripMenuItem m_mnuSaveFileFDO;
		ToolStripMenuItem m_mnuDisplayVirtual;
		ToolStripMenuItem m_mnuToolsAllowEdit;
		ToolStripMenuItem m_mnuViewfDOModel;
		ToolStripMenuItem m_mnuViewLangProject;
		ToolStripMenuItem m_mnuViewRepositories;
		ToolStripMenuItem m_mnuClassProperties;
		ToolStripTextBox m_tstxtGuidSrch;
		ToolStripLabel m_tslblGuidSrch;

		private static ResourceManager s_helpResources;

		private string m_sHelpTopic = "khtpMainFDOBrowser";

		#endregion Data members

		#region Construction
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FDOBrowserForm()
		{
			InitializeComponent();
			SetupCustomMenusAndToolbarItems();
		}

		/// ------------------------------------------------------------------------------------
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			OpenModelWindow();
			FDOClassList.ShowCmObjectProperties = Properties.Settings.Default.ShowCmObjectProperties;
			m_tsbShowCmObjectProps.Checked = FDOClassList.ShowCmObjectProperties;
		}

		#region SetupCustomMenusAndToolbarItems
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Setups the custom menus and toolbar items.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="ToolStripMenuItem gets added to ContextMenuStrip's item collection and disposed there")]
		private void SetupCustomMenusAndToolbarItems()
		{
			#region Grid context menu items


			m_cmnuAddObject = new ToolStripMenuItem();
			m_cmnuAddObject.Name = "cmnuAddObject";
			m_cmnuAddObject.Size = new System.Drawing.Size(254, 22);
			m_cmnuAddObject.Text = "Add New Object of type {0}...";
			m_cmnuAddObject.Click += CmnuAddObjectClick;
			m_fmtAddObjectMenuText = m_cmnuAddObject.Text;

			m_cmnuGrid.Items.Insert(1, new ToolStripSeparator());
			m_cmnuGrid.Items.Insert(2, m_cmnuAddObject);

			m_cmnuDeleteObject = new ToolStripMenuItem();
			m_cmnuDeleteObject.Name = "cmnuDeleteObject";
			m_cmnuDeleteObject.Size = new System.Drawing.Size(254, 22);
			m_cmnuDeleteObject.Text = "Delete the selected object.";
			m_cmnuDeleteObject.Click += CmnuDeleteObjectClick;

			m_cmnuGrid.Items.Insert(3, m_cmnuDeleteObject);

			m_cmnuMoveObjectUp = new ToolStripMenuItem();
			m_cmnuMoveObjectUp.Name = "cmnuMoveObjectUp";
			m_cmnuMoveObjectUp.Size = new System.Drawing.Size(254, 22);
			m_cmnuMoveObjectUp.Text = "Move Object Up";
			m_cmnuMoveObjectUp.Click += CmnuMoveObjectUpClick;

			m_cmnuMoveObjectDown = new ToolStripMenuItem();
			m_cmnuMoveObjectDown.Name = "cmnuMoveObjectDown";
			m_cmnuMoveObjectDown.Size = new System.Drawing.Size(254, 22);
			m_cmnuMoveObjectDown.Text = "Move Object Down";
			m_cmnuMoveObjectDown.Click += CmnuMoveObjectDownClick;
			//m_cmnuMoveObjectDown.Image =;

			m_cmnuGrid.Items.Add(new ToolStripSeparator());
			m_cmnuGrid.Items.Add(m_cmnuMoveObjectUp);
			m_cmnuGrid.Items.Add(m_cmnuMoveObjectDown);

			m_cmnuSelectProps = new ToolStripMenuItem();
			m_cmnuSelectProps.Name = "cmnuSelectProps";
			m_cmnuSelectProps.Size = new System.Drawing.Size(254, 22);
			m_cmnuSelectProps.Text = "Select Displayed Properties for {0}...";
			m_cmnuSelectProps.Click += CmnuSelectPropsClick;
			m_fmtSelectPropsMenuText = m_cmnuSelectProps.Text;

			m_cmnuGrid.Items.Add(new ToolStripSeparator());
			m_cmnuGrid.Items.Add(m_cmnuSelectProps);

			#endregion

			#region Toolbar items
			m_tsbShowCmObjectProps = new ToolStripButton();
			m_tsbShowCmObjectProps.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			m_tsbShowCmObjectProps.Enabled = false;
			m_tsbShowCmObjectProps.Image = FDOBrowser.Properties.Resources.kimidShowCmObjectProperties;
			m_tsbShowCmObjectProps.ImageTransparentColor = System.Drawing.Color.Magenta;
			m_tsbShowCmObjectProps.Name = "m_tsbShowCmObjectProps";
			m_tsbShowCmObjectProps.Size = new System.Drawing.Size(23, 22);
			m_tsbShowCmObjectProps.Text = "Show CmObject Properties";
			m_tsbShowCmObjectProps.Click += MTsbShowCmObjectPropsClick;
			int i = m_navigationToolStrip.Items.IndexOf(tsbShowObjInNewWnd);
			if (i >= 0)
				m_navigationToolStrip.Items.Insert(i, m_tsbShowCmObjectProps);

			m_tstxtGuidSrch = new ToolStripTextBox();
			m_tstxtGuidSrch.Alignment = ToolStripItemAlignment.Right;
			m_tstxtGuidSrch.Name = "tstxtGuidSrch";
			m_tstxtGuidSrch.Size = new System.Drawing.Size(250, 25);
			m_tstxtGuidSrch.KeyPress += TstxtGuidSrchKeyPress;
			m_navigationToolStrip.Items.Add(m_tstxtGuidSrch);

			m_tslblGuidSrch = new ToolStripLabel();
			m_tslblGuidSrch.Alignment = ToolStripItemAlignment.Right;
			m_tslblGuidSrch.Name = "tslblGuidSrch";
			m_tslblGuidSrch.Size = new System.Drawing.Size(74, 22);
			m_tslblGuidSrch.Text = "GUID Search:";
			m_navigationToolStrip.Items.Add(m_tslblGuidSrch);

			#endregion

			#region Menus
			mnuOpenFile.Text = "&Open Language Project...";
			//
			// mnuSaveFileFDO
			//
			m_mnuSaveFileFDO = new ToolStripMenuItem();
			m_mnuSaveFileFDO.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
			m_mnuSaveFileFDO.Name = "mnuSaveFileFDO";
			m_mnuSaveFileFDO.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
			m_mnuSaveFileFDO.Size = new System.Drawing.Size(230, 22);
			m_mnuSaveFileFDO.Text = "&Save Current Language Project";
			m_mnuSaveFileFDO.Click += MnuSaveFileFdoClick;
			//
			// mnuToolsAllowEdit
			//
			m_mnuToolsAllowEdit = new ToolStripMenuItem();
			m_mnuToolsAllowEdit.Name = "mnuToolsAllowEdit";
			m_mnuToolsAllowEdit.Size = new System.Drawing.Size(230, 22);
			m_mnuToolsAllowEdit.Text = "&Allow Edits";
			m_mnuToolsAllowEdit.Checked = true;
			m_mnuToolsAllowEdit.Click += MnuToolsAllowEditClick;
			//
			// mnuDisplayVirtual
			//
			m_mnuDisplayVirtual = new ToolStripMenuItem();
			m_mnuDisplayVirtual.Name = "mnuDisplayVirtual";
			m_mnuDisplayVirtual.Size = new System.Drawing.Size(230, 22);
			m_mnuDisplayVirtual.Text = "Display &Virtual Properties";
			m_mnuDisplayVirtual.Click += MnuDisplayVirtualClick;
			m_mnuDisplayVirtual.Checked = false;

			//
			// mnuViewfDOModel
			//
			m_mnuViewfDOModel = new ToolStripMenuItem();
			m_mnuViewfDOModel.Image = FDOBrowser.Properties.Resources.kimidFDOModel;
			m_mnuViewfDOModel.Name = "mnuViewfDOModel";
			m_mnuViewfDOModel.Size = new System.Drawing.Size(230, 22);
			m_mnuViewfDOModel.Text = "&FDO Model";
			m_mnuViewfDOModel.Click += MnuViewfDoModelClick;
			//
			// mnuViewLangProject
			//
			m_mnuViewLangProject = new ToolStripMenuItem();
			m_mnuViewLangProject.Name = "mnuViewLangProject";
			m_mnuViewLangProject.Size = new System.Drawing.Size(230, 22);
			m_mnuViewLangProject.Text = "&Language Project";
			m_mnuViewLangProject.Click += MnuViewLangProjectClick;
			//
			// mnuViewRepositories
			//
			m_mnuViewRepositories = new ToolStripMenuItem();
			m_mnuViewRepositories.Name = "mnuViewRepositories";
			m_mnuViewRepositories.Size = new System.Drawing.Size(230, 22);
			m_mnuViewRepositories.Text = "Language Project &Repositories";
			m_mnuViewRepositories.Click += MnuViewRepositoriesClick;
			//
			// mnuClassProperties
			//
			m_mnuClassProperties = new ToolStripMenuItem();
			m_mnuClassProperties.Name = "mnuClassProperties";
			m_mnuClassProperties.Size = new System.Drawing.Size(230, 22);
			m_mnuClassProperties.Text = "Class &Properties...";
			m_mnuClassProperties.Click += MnuClassPropertiesClick;

			//mnuDb4oToXml
			//

			mnuDb4oToXml = new ToolStripMenuItem();
			mnuDb4oToXml.Name = "mnuDb4oToXml";
			mnuDb4oToXml.Size = new System.Drawing.Size(230, 22);
			mnuDb4oToXml.Text = "Extract Db4o file contents to XML...";
			mnuDb4oToXml.Checked = false;
			mnuDb4oToXml.Click += mnuDb4oToXmlClicked;
			// mnuView
			//
			m_mnuView = new ToolStripMenuItem();
			m_mnuView.Name = "mnuView";
			m_mnuView.Size = new System.Drawing.Size(44, 20);
			m_mnuView.Text = "&View";
			m_mnuView.DropDownOpening += MnuViewDropDownOpening;
			m_mnuView.DropDownItems.Add(m_mnuViewfDOModel);
			m_mnuView.DropDownItems.Add(m_mnuViewLangProject);
			m_mnuView.DropDownItems.Add(m_mnuViewRepositories);
			m_mnuView.DropDownItems.Add(new ToolStripSeparator());
			m_mnuView.DropDownItems.Add(m_mnuDisplayVirtual);
			m_mnuView.DropDownItems.Add(m_mnuClassProperties);

			i = m_mainMenu.Items.IndexOf(mnuTools);
			if (i >= 0)
				m_mainMenu.Items.Insert(i, m_mnuView);

			//
			// mnuTools
			//
			this.mnuTools.DropDownItems.Add(m_mnuToolsAllowEdit);

			this.mnuTools.DropDownItems.Add(mnuDb4oToXml);

			//
			// mnuFile
			//
			this.mnuFile.DropDownItems.Insert(1, m_mnuSaveFileFDO);


			//
			// FwTextBox
			/*
			this.m_textBox = new FwTextBox();
			this.m_textBox.AdjustStringHeight = true;
			this.m_textBox.WordWrap = true;
			this.m_textBox.controlID = null;
			this.m_textBox.Name = "m_textBox";
			this.m_textBox.Enabled = false;
			this.m_textBox.TabStop = false;
			this.m_textBox.AccessibleName = "FwTextBox";
			this.m_textBox.BackColor = System.Drawing.SystemColors.Control; // not implemented: System.Drawing.Color.Transparent;
			this.m_textBox.HasBorder = false;
			this.m_textBox.Location = new System.Drawing.Point(5, 5);
			this.m_textBox.Size = new System.Drawing.Size(304, 184);
			this.m_textBox.TabIndex = 0;
			this.m_textBox.TabStop = false;
			this.m_textBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_textBox.Visible = true;
			// Finish init of the FwTextBox
			m_textBox.WritingSystemFactory = m_cache.WritingSystemFactory;
			m_textBox.WritingSystemCode = m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle;
			m_textBox.Text = " "; // default (maybe should persist?)
			//m_textBox.GotFocus += new EventHandler(m_TextBox_GotFocus);


			panel1.Controls.Add(m_textBox);
			// panel1
			//
			//resources.ApplyResources(this.panel1, "panel1");
			//this.panel1.Name = "panel1";*/


			#endregion
		}

		#endregion

		#endregion Construction

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Form.FormClosing"/> event.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			base.OnFormClosing(e);
			Properties.Settings.Default.ShowCmObjectProperties = m_tsbShowCmObjectProps.Checked;
			Properties.Settings.Default.Save();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Opens the specified file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OpenFile(string fileName)
		{
			if (fileName == null || !File.Exists(fileName) || m_currOpenedProject == fileName)
				return;
			if (m_cache != null)        // if we didn't clean up when we ran this before, do it now.
				if (File.Exists(FileName + ".lock"))
				{
					m_cache.ServiceLocator.GetInstance<IActionHandler>().Commit();
					m_cache.Dispose();
					m_cache = null; // Don't try to use it again
				}

			FileName = fileName;				// Save the filename so we can close it when we exit
			Cursor = Cursors.WaitCursor;
			try
			{
				var bepType = GetBEPTypeFromFileExtension(fileName);
				var isMemoryBEP = bepType == FDOBackendProviderType.kMemoryOnly;

				var stopwatch = new Stopwatch();
				stopwatch.Start();

				// Init backend data provider
				// TODO: Get the correct ICU local for the user writing system
				if (m_ThreadHelper == null)
					m_ThreadHelper = new ThreadHelper();

				if (isMemoryBEP)
					m_cache = FdoCache.CreateCacheWithNewBlankLangProj(new BrowserProjectId(bepType, null), "en", "en", "en", m_ThreadHelper, new FdoBrowserUserAction());
				else
				{
					using (ProgressDialogWithTask progressDlg = new ProgressDialogWithTask(this, m_ThreadHelper))
					{
						m_cache = FdoCache.CreateCacheFromExistingData(new BrowserProjectId(bepType, fileName), "en", progressDlg, new FdoBrowserUserAction());
					}
				}
			   // var v = m_cache.
				//var map = v.m_identityMap;
				CFields = GetCustomFields(m_cache);
				m_lp = m_cache.LanguageProject;

				stopwatch.Stop();

				m_sblblLoadTime.Text = string.Format("Load Time: {0}", stopwatch.Elapsed);

				// Close any windows from a previously opened project, if there is one.
				foreach (IDockContent dc in m_dockPanel.DocumentsToArray())
				{
					if (dc is InspectorWnd)
						((InspectorWnd)dc).Close();
				}

				if (m_langProjWnd != null)
				m_langProjWnd.Close();
				m_langProjWnd = null;
				m_repositoryWnd = null;
				MakeAppCaption(m_lp.ShortName);
				OpenLangProjWindow();
				OpenRepositoryWindow();
				base.OpenFile(fileName);
				m_currOpenedProject = fileName;
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Opens an inspector window for the FDO model.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void OpenModelWindow()
		{
			if (m_modelWnd == null)
			{
				m_modelWnd = new ModelWnd(m_statuslabel);
				m_modelWnd.FormClosed += HandleWindowClosed;
			}

			m_modelWnd.Show(m_dockPanel);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Opens an inspector window with the language project as the top level object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void OpenLangProjWindow()
		{
			if (m_langProjWnd != null)
				m_langProjWnd.Show(m_dockPanel);
			else
			{
				m_langProjWnd = ShowNewInspectorWindow(m_lp,
					m_lp.ShortName + ": LangProj", null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Opens an inspector window with a collection of the language project's
		/// repositories as the top level object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void OpenRepositoryWindow()
		{
			if (m_repositoryWnd != null)
				m_repositoryWnd.Show(m_dockPanel);
			else
			{
				var repositories = FDOClassList.RepositoryTypes.Select(repoType =>
					   m_cache.ServiceLocator.GetInstance(repoType)).ToList();

				// Go through all the service types and find those that are repositories. For each
				// repository type, get its instance from the service locator and store that in a list.

				m_repositoryWnd = ShowNewInspectorWindow(repositories,
					m_cache.ProjectId.UiName + ": Repositories", null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the title for the open file dialog box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override string OpenFileDlgTitle
		{
			get { return "Open FieldWorks Language Project"; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the file filter for the open file dialog box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override string OpenFileDlgFilter
		{
			get { return ResourceHelper.FileFilter(FileFilterType.FieldWorksProjectFiles); }
		}

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the BEP type from the specified file path.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static FDOBackendProviderType GetBEPTypeFromFileExtension(string pathname)
		{
			switch (Path.GetExtension(pathname).ToLower())
			{
				default:
					return FDOBackendProviderType.kMemoryOnly;
				case FwFileExtensions.ksFwDataXmlFileExtension:
					return FDOBackendProviderType.kXML;
				case FwFileExtensions.ksFwDataDb4oFileExtension:
					return FDOBackendProviderType.kDb4oClientServer;

			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the ContentAdded event of the m_dockPanel control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void DockPanelContentAdded(object sender, DockContentEventArgs e)
		{
			base.DockPanelContentAdded(sender, e);
			if (e.Content is InspectorWnd)
				m_tsbShowCmObjectProps.Enabled = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the ContentRemoved event of the m_dockPanel control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void DockPanelContentRemoved(object sender, DockContentEventArgs e)
		{
			base.DockPanelContentRemoved(sender, e);

			foreach (IDockContent dc in m_dockPanel.DocumentsToArray())
			{
				if (dc is InspectorWnd)
					return;
			}

			m_tsbShowCmObjectProps.Enabled = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the mnuViewfDOModel control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void MnuViewfDoModelClick(object sender, EventArgs e)
		{
			OpenModelWindow();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the mnuViewLangProject control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void MnuViewLangProjectClick(object sender, EventArgs e)
		{
			OpenLangProjWindow();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the mnuViewRepositories control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void MnuViewRepositoriesClick(object sender, EventArgs e)
		{
			OpenRepositoryWindow();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the DropDownOpening event of the viewToolStripMenuItem control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void MnuViewDropDownOpening(object sender, EventArgs e)
		{
			m_mnuViewLangProject.Enabled = (m_lp != null);
			m_mnuViewRepositories.Enabled = (m_cache != null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the KeyPress event of the tstxtGuidSrch control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.KeyPressEventArgs"/> instance
		/// containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void TstxtGuidSrchKeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar != (char)Keys.Enter)
				return;

			if (m_cache == null)
				return;

			try
			{
				Guid guid = new Guid(m_tstxtGuidSrch.Text.Trim());
				ICmObject obj =
					m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(guid);

				if (obj != null)
				{
					InspectorWnd wnd = ShowNewInspectorWindow(obj,obj.ToString());
					wnd.ToolTipText = wnd.Text + Environment.NewLine + "Guid: " + m_tstxtGuidSrch.Text.Trim();
				}

				e.KeyChar = (char)0;
				e.Handled = true;
			}
			catch { }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the custom fields defined in the project file.
		/// </summary>
		/// <param name="m_cache">The FDO Cache</param>
		/// ------------------------------------------------------------------------------------
		private List<CustomFields> GetCustomFields(FdoCache m_cache)
		{
			string type = "";
			List<CustomFields> list = new List<CustomFields>();            // m_CustomFields
			// FieldDescription.ClearDataAbout(m_cache);
			foreach (FieldDescription fd in FieldDescription.FieldDescriptors(m_cache))
			{
				if (fd.IsCustomField)
				{
					switch (fd.Type.ToString())
					{
						case "String":
							type = "ITsString";
							break;
						case "Integer":
							type = "System.Int32";
							break;
						case "GenDate":
							type = "SIL.FieldWorks.Common.FwUtils.GenDate";
							break;
						case "ReferenceAtomic":
							type = "ICmPossibility";
							break;
						case "ReferenceCollection":
							type = "FdoReferenceCollection<ICmPossibility>";
							break;
						case "MinObj":
							type = "IStText";
							break;

						case "MultiUnicode":
							// This is a guess
							type = "WfiWordForm";
							break;
						default:
							MessageBox.Show(string.Format("In 'GetCustomFields, Type was unknown: {0} ", fd.Type.ToString()));
							break;
					}

					CustomFields cf = new CustomFields(fd.Name, fd.Class, fd.Id, type);
					list.Add(cf);
				}
			}
			return (list.Count > 0? list: null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the window menu item click.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		void HandleWindowMenuItemClick(object sender, EventArgs e)
		{
			ToolStripMenuItem mnu = sender as ToolStripMenuItem;
			if (mnu != null && mnu.Tag is DockContent)
				((DockContent)mnu.Tag).Show(m_dockPanel);
		}

		#endregion Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the title for a new inspector window being built for the specified object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override string GetNewInspectorWndTitle(object obj)
		{
			var tss = obj as ITsString;
			return (tss == null ? base.GetNewInspectorWndTitle(obj) :
				string.Format("ITsString: '{0}'", tss.Text));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the new inspector list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override IInspectorList GetNewInspectorList()
		{
			return new FdoInspectorList(m_cache);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the new inspector window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override InspectorWnd ShowNewInspectorWindow(object obj, string text, string toolTipText)
		{
			InspectorWnd wnd = ShowNewInspectorWndOne();
			if (m_updateFlag)
			{
				if (text.Contains("LangProj"))
				{
					wnd.InspectorGrid.ReadOnly = false;
					wnd.InspectorGrid.CellValueChanged += LangProjInspectorGrid_CellValueChanged;
				}
				else if (text.Contains("Repositories"))
				{
					wnd.InspectorGrid.ReadOnly = false;
					wnd.InspectorGrid.CellValueChanged += RepositoryInspectorGrid_CellValueChanged;
				}
			}
			wnd = base.ShowNewInspectorWndTwo(obj, text, toolTipText, wnd);
			wnd.WillObjDisappearOnRefresh += HandleWillObjDisappearOnRefresh;
			return wnd;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles determining whether or not the specified object will disappear if the
		/// view it's in is refresh. For our purposes here, we assume that properties that
		/// are part of the CmObject base class will disappear (unless the property is a guid).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool HandleWillObjDisappearOnRefresh(object sender, IInspectorObject io)
		{
			// Check if the selected object will disappear after refreshing the grid.
			return (io.DisplayName != "Guid" && FDOClassList.IsCmObjectProperty(io.DisplayName));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the inspector window closed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.FormClosedEventArgs"/>
		/// instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void HandleWindowClosed(object sender, FormClosedEventArgs e)
		{
			base.HandleWindowClosed(sender, e);

			if (sender is InspectorWnd)
				((InspectorWnd)sender).WillObjDisappearOnRefresh -= HandleWillObjDisappearOnRefresh;

			if (sender == m_modelWnd)
				m_modelWnd = null;
			else if (sender == m_langProjWnd)
				m_langProjWnd = null;
			else if (sender == m_repositoryWnd)
				m_repositoryWnd = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the m_tsbShowCmObjectProps control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void MTsbShowCmObjectPropsClick(object sender, EventArgs e)
		{
			m_tsbShowCmObjectProps.Checked = !m_tsbShowCmObjectProps.Checked;
			FDOClassList.ShowCmObjectProperties = m_tsbShowCmObjectProps.Checked;

			foreach (IDockContent dc in m_dockPanel.DocumentsToArray())
			{
				if (dc is InspectorWnd)
					((InspectorWnd)dc).RefreshView();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Opening event of the grid's context menu.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void HandleOpenObjectInNewWindowContextMenuClick(object sender, CancelEventArgs e)
		{
			int clid = 0, flid = 0;
			string className = "", work ="";
			ICmObject owner, currentObj;

			saveClid = 0;
			base.HandleOpenObjectInNewWindowContextMenuClick(sender, e);
			InspectorWnd wnd = m_dockPanel.ActiveContent as InspectorWnd;
			if (wnd != null)
			{
				IInspectorObject io = wnd.CurrentInspectorObject;
				m_cmnuAddObject.Enabled = (io != null && AddObjectFromHere(io));
				m_cmnuDeleteObject.Enabled = (io != null && io.Object is ICmObject);
				m_cmnuMoveObjectUp.Enabled = (io != null && MoveObjectUpFromHere(io));
				m_cmnuMoveObjectDown.Enabled = (io != null && MoveObjectDownFromHere(io));
				string type = (io != null && io.Object is ICmObject ? io.Object.GetType().Name : "???");
				if (io != null &&
					(io.DisplayName.EndsWith("OS") || io.DisplayName.EndsWith("OC") ||
					 io.DisplayName.EndsWith("OA") || io.DisplayName.EndsWith("RS") ||
					 io.DisplayName.EndsWith("RC") || io.DisplayName.EndsWith("RA")))
				{
					var mdc = m_cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();

					owner = io.ParentInspectorObject.Object as ICmObject;
					if (owner == null) // we're on a field
						owner = ((ICmObject)io.ParentInspectorObject.OwningObject);

					currentObj = io.Object as ICmObject;
					if (currentObj == null) // we're on a field
						currentObj = ((ICmObject)io.OwningObject);
					try
					{
						work = StripOffTypeChars(io.DisplayName);
						flid = mdc.GetFieldId2(owner.ClassID, work, true);
					}
					catch
					{
						MessageBox.Show("No Flid for clicked line: " +  io.DisplayValue);
						return;
					}

					clid = m_cache.DomainDataByFlid.MetaDataCache.GetDstClsId(flid);
					if (io.DisplayName.EndsWith("RS") || io.DisplayName.EndsWith("RC") ||
						io.DisplayName.EndsWith("RA"))
					{
						bool dispFlag = io.DisplayName.EndsWith("RA") && io.Object != null;
						type = GetTypeForRefObjs(flid, clid, dispFlag, ref saveClid);
						m_cmnuAddObject.Text = string.Format("Add New Reference of type {0}...", type);
						m_cmnuSelectProps.Text = string.Format(m_fmtSelectPropsMenuText, type);
					}
					else
					{
						switch (clid)
						{
							case 7: // cmPossibility
								if (PossOwnedByPossList(mdc, currentObj, ref clid))
									type = mdc.GetClassName(clid);
								break;
							case 15: // stPara
								if (StParaOwnedByScrBook(mdc, currentObj, ref clid))
									type = mdc.GetClassName(clid);
								break;
							default:
								try
								{
									type = mdc.GetClassName(clid);
								}
								catch
								{
									MessageBox.Show("No class name for clid: " + clid);
									return;
								}
								break;
						}
						m_cmnuAddObject.Text = string.Format(m_fmtAddObjectMenuText, type);
						m_cmnuSelectProps.Text = string.Format(m_fmtSelectPropsMenuText, type);
					}
				}

			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the mnuClassProperties control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void MnuClassPropertiesClick(object sender, EventArgs e)
		{
			ShowPropertySelectorDialog(null);
		}

		private void mnuDb4oToXmlClicked(object sender, EventArgs e)
		{
			using (var choose = new FileInOutChooser())
			{
				choose.ShowDialog();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the cmnuSelectProps control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CmnuSelectPropsClick(object sender, EventArgs e)
		{
			InspectorWnd wnd = m_dockPanel.ActiveContent as InspectorWnd;
			if (wnd == null)
				return;

			IInspectorObject io = wnd.CurrentInspectorObject;
			if (io == null || !(io.Object is ICmObject))
				return;

			ShowPropertySelectorDialog(io.Object as ICmObject);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the cmnuAddObject control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CmnuAddObjectClick(object sender, EventArgs e)
		{
			ICmObject owner, currentObj, refTarget;
			ICmPossibilityList holdposs;
			int clid = 0, oclid = 0, ord = 0, idx = 0, flid = 0;
			string newClassName = "", type = "", work = "", intName = "";

			var mWnd = m_dockPanel.ActiveContent as InspectorWnd;
			if (mWnd == null)
				return;

			var io = mWnd.CurrentInspectorObject;
			if (io == null)
				return;

			type = io.DisplayName.Substring(io.DisplayName.Length - 2, 2);
			if (type != "RS" && type != "RC" && type != "RA")
			{
				switch (type)
				{
					case "OA":
						ord = -2;
						break;
					case "OC":
						ord = -1;
						break;
					case "OS":
						ord = 0;
						break;
					default:
						MessageBox.Show("The parent of an added object must be OA, OC, or OS.  It's " + type);
						break;
				}
			}

			owner = io.ParentInspectorObject.Object as ICmObject;
			if (owner == null) // we're on a field
				owner = ((ICmObject)io.ParentInspectorObject.OwningObject);

			if (owner == null)
			{
				MessageBox.Show("owner for add is null");
				return;
			}

			currentObj = io.Object as ICmObject;
			if (currentObj == null) // we're on a field
				currentObj = io.ParentInspectorObject.Object as ICmObject;

			if (currentObj == null)
			{
				MessageBox.Show("currentObj for add is null");
				return;
			}

			var	mdc = m_cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();

			try
			{
				work = StripOffTypeChars(io.DisplayName);
				flid = mdc.GetFieldId2(owner.ClassID, work, true);
			}
			catch
			{
				MessageBox.Show("No Flid for clicked line: " +  io.DisplayValue);
				return;
			}

			clid = m_cache.MetaDataCacheAccessor.GetDstClsId(flid);  // sigbature clid

			if (type == "RS" || type == "RC" || type == "RA")
			{
				if (DisplayReferenceObjectsToAdd(flid, type, currentObj, out refTarget))
					if (refTarget != null)
					{
						switch (type)
						{
							case "RS":
							case "RC":
								int count = m_cache.DomainDataByFlid.get_VecSize(currentObj.Hvo, flid);
								NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
									m_cache.DomainDataByFlid.Replace(currentObj.Hvo, flid, count,
									   count, new int[] {refTarget.Hvo}, 1)
								);
								break;
							case "RA":
								NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
									m_cache.DomainDataByFlid.SetObjProp(currentObj.Hvo, flid, refTarget.Hvo)
								);
								break;

							default:
								break;
						}
					}
				}
				else
				{
					// if the classname is abstract, we need to get concrete classes and let the user choose
					if (DisplayOwningObjectToAdd(mdc, currentObj, ref clid))
					{
						// Add a new owned class
						NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
						{
							try
							{
								m_cache.DomainDataByFlid.MakeNewObject(clid, currentObj.Hvo, flid, ord);
							}
							catch
							{
								MessageBox.Show("The creation of class id " + clid.ToString() + " failed.");
								return;
							}
						});
					}
				}

			//Refresh the display

			foreach (IDockContent dc in m_dockPanel.DocumentsToArray())
			{
				if (dc is InspectorWnd)
					((InspectorWnd)dc).RefreshView("Add");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles getting the correct class (clid) to add for owning properties.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool DisplayOwningObjectToAdd(IFwMetaDataCacheManaged mdc, ICmObject owner, ref int clid)
		{
			// if the chosen classname is abstract, we need to get concrete classes and let the user choose
			var list = new List<string>();

			if (mdc.GetAbstract(clid))
			{
				if (clid == 15)   //stPara is abstact - see if it is a special condition
					if (StParaOwnedByScrBook(mdc, owner, ref clid))
					{
						return true;
					}

				int[] clidSubs = mdc.GetAllSubclasses(clid);
				for (int i = 0; i < clidSubs.Length; ++i)
				{
					if (!mdc.GetAbstract(clidSubs[i]))
					{
						list.Add(mdc.GetClassName(clidSubs[i]));
					}
				}

				if (clidSubs.Length > 1)
				{
					using (RealListChooser dlg = new RealListChooser("ClassName", list))
					{
						if (dlg.m_chosenClass == "Cancel")
							return false;
						else
						{
							clid = mdc.GetClassId(dlg.m_chosenClass);  // get the clid for the selected class
							if (clid == 0)
							{
								MessageBox.Show("No clid for selected class: " + dlg.m_chosenClass);
								return false;
							}
							else
								return true;
						}
					}
				}
				clid = clidSubs[0];
				return true;
			}
			else   // class is concrete
			{
				if (clid == 7)   //cmPossibility
					if (PossOwnedByPossList(mdc, owner, ref clid))
						return true;

				return true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles getting the correct object for a reference property to add.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool DisplayReferenceObjectsToAdd(int flid, string type, ICmObject currObject, out ICmObject refTarget)
		{
			var possList = new List<ICmPossibility>();
			var labels = new List<ObjectLabel>();
			var	mdc = m_cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
			var refObjs = new HashSet<int>();
			int count = 0;

			if (m_cache.MetaDataCacheAccessor != null)
			{
				int sclid = m_cache.MetaDataCacheAccessor.GetDstClsId(flid); //signature clid of field

				if (type != "RA")
				{
					try
					{
						count = m_cache.DomainDataByFlid.get_VecSize(currObject.Hvo, flid);
					}
					catch
					{
						MessageBox.Show("Can't get count for reference sequence or collection.");
						goto Skip;
					}

					if (count > 0)
						for (int i = 0; count > i; i++)
						{
							try
							{
								refObjs.Add(m_cache.DomainDataByFlid.get_VecItem(currObject.Hvo, flid, i));
							}
							catch
							{
								//MessageBox.Show("Can't add index i " + i.ToString() + " to refObjs.");
							}
						}
				}
			Skip:
				var desiredClasses = GetClassesForRefObjs(flid, sclid, out possList);
				if (desiredClasses != null && possList == null)
				{
					if (refObjs.Count == 0)
					{
						var objects = from obj in m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().AllInstances(sclid)
									  where desiredClasses.Contains(obj.ClassID)
									  select obj;

						labels.AddRange(objects.Select(obj => ObjectLabel.CreateObjectLabelOnly(m_cache, obj,
																								"ObjectIdName", "best analysis")));
					}
					else
					{
						var objects = from obj in m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().AllInstances(sclid)
									  where desiredClasses.Contains(obj.ClassID) && !refObjs.Contains(obj.Hvo)
									  select obj;
						labels.AddRange(objects.Select(obj => ObjectLabel.CreateObjectLabelOnly(m_cache, obj,
																								"ObjectIdName", "best analysis")));
					}
					if (labels.Count == 0)
					{
						var hashList = "";
						foreach (int de in desiredClasses) hashList = hashList + mdc.GetClassName(de) + " ";
						MessageBox.Show("No objects exist within the selected classes.  Try again.  Selected classes are: "+
										hashList);
					}
					else
					{
						using (var dlg = new SimpleListChooser(m_cache, null, null, labels, null, "Object List", null))
						{
							var res = dlg.ShowDialog();
							if (res == DialogResult.Cancel)
							{
								refTarget = null;
								dlg.Close();
								return false;
							}
							if (dlg.ChosenOne != null)
							{
								refTarget = dlg.ChosenOne.Object;
								dlg.Close();
								return true;
							}
						}
					}
				}
				else
				{
					labels =
						(List<ObjectLabel>)
						ObjectLabel.CreateObjectLabels(m_cache, possList.Cast<ICmObject>(), "ObjectIdName", "best analysis");
					using (var dlg = new SimpleListChooser(m_cache, null, null, labels, null, "Object List", null))
					{
						var res = dlg.ShowDialog();
						if (res == DialogResult.Cancel)
						{
							refTarget = null;
							dlg.Close();
							return false;
						}
						if (dlg.ChosenOne != null)
						{
							refTarget = dlg.ChosenOne.Object;
							dlg.Close();
							return true;
						}
					}
				}
			}
			refTarget = null;
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the possible classes for objects for the selected reference property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private HashSet<int> GetClassesForRefObjs(int flid, int sclid, out List<ICmPossibility> possList)
		{
			var mdc = (IFwMetaDataCacheManaged)m_cache.MetaDataCacheAccessor;

			var clid = Int32.Parse(flid.ToString().Substring(0,flid.ToString().Length-3));

			IEnumerable<ObjectLabel> labels = new List<ObjectLabel>();;
			var classList1 = new HashSet<int>();
			var list = new List<string>();
			possList = null;
			switch(mdc.GetClassName(sclid))
			{
				case "CmObject":
				switch (mdc.GetClassName(clid) + "_" + mdc.GetFieldName(flid))
				{
					case "LexEntryRef_ComponentLexemes":
					case "LexEntryRef_PrimaryLexemes":
					case "LexReference_Targets":
						classList1 = new HashSet<int>(mdc.GetAllSubclasses(mdc.GetClassId("LexEntry")));
						foreach (int cid in mdc.GetAllSubclasses(mdc.GetClassId("LexSense"))) classList1.Add(cid);
						return classList1;

					case "Segment_Analyses":
						classList1 = new HashSet<int>(mdc.GetAllSubclasses(mdc.GetClassId("WfiGloss")));
						foreach (int cid in mdc.GetAllSubclasses(mdc.GetClassId("WfiAnalysis"))) classList1.Add(cid);
						foreach (int cid in mdc.GetAllSubclasses(mdc.GetClassId("WfiWordform"))) classList1.Add(cid);
						foreach (int cid in mdc.GetAllSubclasses(mdc.GetClassId("PunctuationForm"))) classList1.Add(cid);
						return classList1;
					// No work here yet since the following two areas are unimplemented so far.
					// For now they can fall into the default (per FWR-890)
					//case "MoPhonolRuleApp_Rule":
					//    classList1 = new HashSet<int>(mdc.GetAllSubclasses(mdc.GetClassId("PhSegmentRule")));
					//    return classList1;
					//case "PhPhonRuleFeat_Item":
					//    classList1 = new HashSet<int>(mdc.GetAllSubclasses(mdc.GetClassId("MoInflClass")));
					//    foreach (int cid in mdc.GetAllSubclasses(mdc.GetClassId("FsSymFeatVal"))) classList1.Add(cid);
					//    foreach (int cid in mdc.GetAllSubclasses(mdc.GetClassId("CmPossibility"))) classList1.Add(cid);
					//    return classList1;
					case "MoPhonolRuleApp_Rule":
					case "PhPhonRuleFeat_Item":
					case "StTxtPara_TextObjects":
					case "CmAnnotation_InstanceOf":
					case "CmBaseAnnotation_BeginObject":
					case "CmBaseAnnotation_EndObject":
					case "CmBaseAnnotation_OtherObjects":
					default:
						if (saveClid == 0)
						{
							int[] classes = mdc.GetAllSubclasses(sclid);
							foreach (int cl in classes)
							{
								if (cl != sclid)
									list.Add(mdc.GetClassName(cl));
							}

							using (RealListChooser dlg = new RealListChooser("ClassName", list))
							{
								if (dlg.m_chosenClass == "Cancel")
									return classList1;
								classList1.Add(mdc.GetClassId(dlg.m_chosenClass)); // get the clid for the selected class
								if (mdc.GetClassId(dlg.m_chosenClass) == 0)
								{
									MessageBox.Show("No clid for selected class: " + dlg.m_chosenClass);
									return classList1;
								}
								return classList1;
							}
						}
						classList1 = new HashSet<int>(mdc.GetAllSubclasses(saveClid));
						return classList1;
				}   // switch for properties with a signature of CmObject

				case "CmPossibility":
				{
					switch (mdc.GetClassName(clid) + "_" + mdc.GetFieldName(flid))
					{
						case "ConstChartTag_Tag":
							possList = new List<ICmPossibility>
								(m_cache.LangProject.DiscourseDataOA.ChartMarkersOA.ReallyReallyAllPossibilities);
							return classList1;
						case "ConstituentChartCellPart_Column":
						case "DsChart_Template":
							possList = new List<ICmPossibility>
								(m_cache.LangProject.DiscourseDataOA.ConstChartTemplOA.ReallyReallyAllPossibilities);
							return classList1;
						case "MoDerivAffMsa_AffixCategory":
						case "MoInflAffMsa_AffixCategory":
							possList = new List<ICmPossibility>
								(m_cache.LangProject.AffixCategoriesOA.ReallyReallyAllPossibilities);
							return classList1;
						case "CmPossibility_Status":
						case "RnGenericRec_Status":
							possList = new List<ICmPossibility>
								(m_cache.LangProject.StatusOA.ReallyReallyAllPossibilities);
							return classList1;
						case "CmPossibility_Confidence":
						case "RnGenericRec_Confidence":
							possList = new List<ICmPossibility>
								(m_cache.LangProject.ConfidenceLevelsOA.ReallyReallyAllPossibilities);
							return classList1;
						case "CmPerson_Education":
							possList = new List<ICmPossibility>
								(m_cache.LangProject.EducationOA.ReallyReallyAllPossibilities);
							return classList1;
						case "Text_Genres":
							possList = new List<ICmPossibility>
								(m_cache.LangProject.GenreListOA.ReallyReallyAllPossibilities);
							return classList1;
						case "CmPerson_Positions":
							possList = new List<ICmPossibility>
								(m_cache.LangProject.PositionsOA.ReallyReallyAllPossibilities);
							return classList1;
						case "CmPossibility_Restrictions":
						case "RnGenericRec_Restrictions":
							possList = new List<ICmPossibility>
								(m_cache.LangProject.RestrictionsOA.ReallyReallyAllPossibilities);
							return classList1;
						case "RnRoledPartic_Role":
							possList = new List<ICmPossibility>
								(m_cache.LangProject.RolesOA.ReallyReallyAllPossibilities);
							return classList1;
						case "TextTag_Tag":
							possList = new List<ICmPossibility>
								(m_cache.LangProject.TextMarkupTagsOA.ReallyReallyAllPossibilities);
							return classList1;
						case "RnGenericRec_TimeOfEvent":
							possList = new List<ICmPossibility>
								(m_cache.LangProject.TimeOfDayOA.ReallyReallyAllPossibilities);
							return classList1;
						case "CmTranslation_Type":
							possList = new List<ICmPossibility>
								(m_cache.LangProject.TranslationTagsOA.ReallyReallyAllPossibilities);
							return classList1;
						case "LexSense_DomainTypes":
							possList = new List<ICmPossibility>
								(m_cache.LangProject.LexDbOA.DomainTypesOA.ReallyReallyAllPossibilities);
							return classList1;
						case "LexSense_SenseType":
							possList = new List<ICmPossibility>
								(m_cache.LangProject.LexDbOA.SenseTypesOA.ReallyReallyAllPossibilities);
							return classList1;
						case "LexSense_Status":
							possList = new List<ICmPossibility>
								(m_cache.LangProject.StatusOA.ReallyReallyAllPossibilities);
							return classList1;
						case "LexSense_UsageTypes":
							possList = new List<ICmPossibility>
								(m_cache.LangProject.LexDbOA.UsageTypesOA.ReallyReallyAllPossibilities);
							return classList1;
						case "MoCompoundRule_ToProdRestrict":
						case "MoDerivAffMsa_FromProdRestrict":
						case "MoDerivAffMsa_ToProdRestrict":
						case "MoDerivStepMsa_ProdRestrict":
						case "MoInflAffMsa_FromProdRestrict":
						case "MoStemMsa_FromProdRestrict":
							possList = new List<ICmPossibility>
								(m_cache.LangProject.MorphologicalDataOA.ProdRestrictOA.ReallyReallyAllPossibilities);
							return classList1;
						case "RnGenericRec_Type":
							possList = new List<ICmPossibility>
								(m_cache.LangProject.ResearchNotebookOA.RecTypesOA.ReallyReallyAllPossibilities);
							return classList1;
						case "ScrScriptureNote_Categories":
							possList = new List<ICmPossibility>
								(m_cache.LangProject.TranslatedScriptureOA.NoteCategoriesOA.ReallyReallyAllPossibilities);
							return classList1;
						case "CmOverlay_PossItems":
						case "RnGenericRec_PhraseTags":
						default:
							classList1 = new HashSet<int>(mdc.GetAllSubclasses(mdc.GetClassId("CmPossibility")));
							return classList1;
					}  //cmPossibility fields Switch
				} //CmPossibilityLabel case
				default:
					classList1 = new HashSet<int>(mdc.GetAllSubclasses(sclid));
					return classList1;
			}  //classes switch
		}

		/// <summary>
		///
		/// </summary>
		/// <typeparam name="T1"></typeparam>
		/// <param name="p"></param>
		/// <exception cref="T:System.NotImplementedException"></exception>
		/// <returns></returns>
		private static IEnumerable<ICmObject> IEnumerable<T1>(int[] p)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the possible classes for objects for the selected reference property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string GetTypeForRefObjs(int flid, int sclid, bool dispFlag, ref int saveClid)
		{
			var list = new List<string>();
			var mdc = (IFwMetaDataCacheManaged)m_cache.MetaDataCacheAccessor;

			var clid = Int32.Parse(flid.ToString().Substring(0,flid.ToString().Length-3));
			IEnumerable<ObjectLabel> labels = new List<ObjectLabel>();;
			var classList1 = new HashSet<int>();
			string type = mdc.GetClassName(sclid);
			switch(type)
			{
				case "CmObject":
					switch (mdc.GetClassName(clid) + "_" + mdc.GetFieldName(flid))
					{
						case "LexEntryRef_ComponentLexemes":
						case "LexEntryRef_PrimaryLexemes":
						case "LexReference_Targets":
							return "LexEntry or LexSense";

						case "Segment_Analyses":
							return "IAnalysis";
						// No work here yet since the following two areas are unimplemented so far.
						// For now they can fall into the default (per FWR-890)
						//case "MoPhonolRuleApp_Rule":
						//    return "PhSegmentRule";
						//case "PhPhonRuleFeat_Item":
						//    return "MoInflClass, FsSymFeatVal or CmPossibility";
						case "MoPhonolRuleApp_Rule":
						case "PhPhonRuleFeat_Item":
						case "StTxtPara_TextObjects":
						case "CmAnnotation_InstanceOf":
						case "CmBaseAnnotation_BeginObject":
						case "CmBaseAnnotation_EndObject":
						case "CmBaseAnnotation_OtherObjects":
						default:
							if (!dispFlag)
							{
								int[] classes = mdc.GetAllSubclasses(sclid);
								foreach (int cl in classes)
								{
									if (cl != sclid)
										list.Add(mdc.GetClassName(cl));
								}

								using (RealListChooser dlg1 = new RealListChooser("ClassName", list))
								{
									if (dlg1.m_chosenClass == "Cancel")
										break;
									if (mdc.GetClassId(dlg1.m_chosenClass) == 0)
									{
										MessageBox.Show("No clid for selected class: " + dlg1.m_chosenClass);
										break;
									}
									saveClid = mdc.GetClassId(dlg1.m_chosenClass);
									return dlg1.m_chosenClass; // get the clid for the selected class
								}
							}
							break;
					}   // switch for properties with a signature of CmObject
					break;
				case "CmPossibility":
				{
					switch (mdc.GetClassName(clid) + "_" + mdc.GetFieldName(flid))
					{
						case "CmOverlay_PossItems":
						case "RnGenericRec_PhraseTags":
						case "ConstChartTag_Tag":
						case "ConstituentChartCellPart_Column":
						case "DsChart_Template":
						case "MoDerivAffMsa_AffixCategory":
						case "MoInflAffMsa_AffixCategory":
						case "CmPossibility_Status":
						case "RnGenericRec_Status":
						case "CmPossibility_Confidence":
						case "RnGenericRec_Confidence":
						case "CmPerson_Education":
						case "Text_Genres":
						case "CmPerson_Positions":
						case "CmPossibility_Restrictions":
						case "RnGenericRec_Restrictions":
						case "RnRoledPartic_Role":
						case "TextTag_Tag":
						case "RnGenericRec_TimeOfEvent":
						case "CmTranslation_Type":
						case "LexSense_DomainTypes":
						case "LexSense_SenseType":
						case "LexSense_Status":
						case "LexSense_UsageTypes":
						case "MoCompoundRule_ToProdRestrict":
						case "MoDerivAffMsa_FromProdRestrict":
						case "MoDerivAffMsa_ToProdRestrict":
						case "MoDerivStepMsa_ProdRestrict":
						case "MoInflAffMsa_FromProdRestrict":
						case "MoStemMsa_ProdRestrict":
						case "RnGenericRec_Type":
						case "ScrScriptureNote_Categories":
							return "CmPossibility";
					}  //cmPossibility fields Switch
				} //CmPossibilityLabel case
					break;
				default:
					return type;
			}  //classes switch
			return type;
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if the current object is owned by CmPossibilityList.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static bool PossOwnedByPossList(IFwMetaDataCacheManaged mdc, ICmObject owner, ref int clid)
		{
			ICmPossibilityList holdposs;

			do
			{
				if (owner.ClassID == mdc.GetClassId("CmPossibilityList")) //if the owner is CmPossibilityList
				{
					holdposs = owner as ICmPossibilityList;
					if (holdposs == null)
					{
						MessageBox.Show("PossOwnedByPossList; holdposs is null: ");
						return false;
					}
					clid = holdposs.ItemClsid;
					return true;
				}
				else
					owner = owner.Owner;
			}
			while (owner != null);
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if the current object is owned by ScrBook.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static bool StParaOwnedByScrBook(IFwMetaDataCacheManaged mdc, ICmObject owner, ref int clid)
		{
			do
			{
				if (owner.ClassID == mdc.GetClassId("ScrBook")) //if the owner is an object of type ScrBook
				{
					clid = mdc.GetClassId("ScrTxtPara");   //scrTextPara
					return true;
				}
				else
					owner = owner.Owner;
			}
			while (owner != null);
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the cmnuMoveObjectUp control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CmnuMoveObjectUpClick(object sender, EventArgs e)
		{
			int flid = 0, inx = 0;
			string work = "";

			var mWnd = m_dockPanel.ActiveContent as InspectorWnd;
			if (mWnd == null)
				return;

			var io = mWnd.CurrentInspectorObject;
			if (io == null)
				return;

			if (!io.ParentInspectorObject.DisplayName.EndsWith("OS") &&
				!io.ParentInspectorObject.DisplayName.EndsWith("RS"))
				return;

			var objToMove = io.Object as ICmObject;
			if (objToMove == null) // we're on a field
				objToMove = io.OriginalObject as ICmObject;
			if (objToMove == null) // we're on a field
			{
				MessageBox.Show("owner object couldn't be created.");
				return;
			}

			var owner = io.ParentInspectorObject.Object as ICmObject;
			if (owner == null) // we're on a field
				owner = io.ParentInspectorObject.OriginalObject as ICmObject;
			if (owner == null) // we're on a field
				owner = io.ParentInspectorObject.OwningObject as ICmObject;
			if (owner == null) // we're on a field
			{
				MessageBox.Show("owner object couldn't be created.");
				return;
			}

			work = StripOffTypeChars(io.ParentInspectorObject.DisplayName);

			var mdc = m_cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();

			flid = mdc.GetFieldId2(owner.ClassID, work, true);

			if (io.ParentInspectorObject.DisplayName.EndsWith("OS"))
				NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
				{
					m_cache.DomainDataByFlid.MoveOwn(owner.Hvo, flid, objToMove.Hvo, owner.Hvo, objToMove.OwningFlid, objToMove.OwnOrd-1);
				});
			else //for reference objects, add the reference to the new location, then delete it.
			{

				// index of object to move
				inx = m_cache.DomainDataByFlid.GetObjIndex(owner.Hvo, flid, objToMove.Hvo);


				NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
					m_cache.DomainDataByFlid.Replace(owner.Hvo, flid, inx - 1,
					   inx - 1, new int[] { objToMove.Hvo }, 1)
				);

				NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
					m_cache.DomainDataByFlid.Replace(owner.Hvo, flid, inx + 1, inx + 2, null, 0)
				);
			}


			//Refresh the display

			foreach (InspectorWnd dc in m_dockPanel.DocumentsToArray().OfType<InspectorWnd>())
			{
				(dc).RefreshView("Up");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Receives field.  Return field less OA, OC, OS, RA, RC, or RS if there.
		///  </summary>
		/// ------------------------------------------------------------------------------------
		public static string StripOffTypeChars(String field)
		{
			if (field.EndsWith("OS") || field.EndsWith("RS") || field.EndsWith("OC") || field.EndsWith("RC") ||
				field.EndsWith("OA") || field.EndsWith("RA"))
				return field.Substring(0, field.Length - 2);

			return field;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the cmnuMoveObjectDown control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CmnuMoveObjectDownClick(object sender, EventArgs e)
		{
			int flid = 0, inx = 0, inx2 = 0, cnt = 0;
			string work = "";

			var mWnd = m_dockPanel.ActiveContent as InspectorWnd;
			if (mWnd == null)
				return;

			var io = mWnd.CurrentInspectorObject;
			if (io == null)
				return;

			var objToMove = io.Object as ICmObject;
			if (objToMove == null) // we're on a field
				objToMove = io.OriginalObject as ICmObject;
			if (objToMove == null) // we're on a field
			{
				MessageBox.Show("object to move couldn't be created.");
				return;
			}

			var owner = io.ParentInspectorObject.Object as ICmObject;
			if (owner == null) // we're on a field
				owner = io.ParentInspectorObject.OriginalObject as ICmObject;
			if (owner == null) // we're on a field
				owner = io.ParentInspectorObject.OwningObject as ICmObject;
			if (owner == null) // we're on a field
			{
				MessageBox.Show("owner object couldn't be created.");
				return;
			}

			if (!io.ParentInspectorObject.DisplayName.EndsWith("OS") &&
				!io.ParentInspectorObject.DisplayName.EndsWith("RS"))
				return;

			if (io.ParentInspectorObject.DisplayName.EndsWith("OS") || io.ParentInspectorObject.DisplayName.EndsWith("RS"))
				work = io.ParentInspectorObject.DisplayName.Substring(0, io.ParentInspectorObject.DisplayName.Length - 2);
			else
				work = io.DisplayName;

			var mdc = m_cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();

			flid = mdc.GetFieldId2(owner.ClassID, work, true);

			if (io.ParentInspectorObject.DisplayName.EndsWith("OS"))
				NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
				{
					m_cache.DomainDataByFlid.MoveOwn(owner.Hvo, flid, objToMove.Hvo, owner.Hvo, objToMove.OwningFlid, objToMove.OwnOrd + 2);
				});
			else //for reference objects, add the reference to the new location, then delete it.
			{
				// index of object to move
				inx = m_cache.DomainDataByFlid.GetObjIndex(owner.Hvo, flid, objToMove.Hvo);
				cnt = m_cache.DomainDataByFlid.get_VecSize(owner.Hvo, flid);
				inx2 = Math.Min(cnt, inx + 2);


				NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
					m_cache.DomainDataByFlid.Replace(owner.Hvo, flid, inx2,
					   inx2, new int[] {objToMove.Hvo}, 1)
				);

				NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
					m_cache.DomainDataByFlid.Replace(owner.Hvo, flid, inx, inx + 1, null, 0)
				);
			}
			//Refresh the display

			foreach (var dc in m_dockPanel.DocumentsToArray())
			{
				if (dc is InspectorWnd)
					((InspectorWnd)dc).RefreshView("Down");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the cmnuDeleteObject control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CmnuDeleteObjectClick(object sender, EventArgs e)
		{
			string work = "";
			int seq = 0, flid = 0;

			var mWnd = m_dockPanel.ActiveContent as InspectorWnd;
			if (mWnd == null)
				return;

			var io = mWnd.CurrentInspectorObject;
			if (io == null || !(io.Object is ICmObject))
				return;

			var objToDelete = (ICmObject)io.Object;
			NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
				m_cache.DomainDataByFlid.DeleteObj(objToDelete.Hvo));

			//Refresh the display
			foreach (IDockContent dc in m_dockPanel.DocumentsToArray())
			{
				if (dc is InspectorWnd)
					((InspectorWnd)dc).RefreshView("Delete");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the mnuDisplayVirtual control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void MnuDisplayVirtualClick(object sender, EventArgs e)
		{
			if (m_mnuDisplayVirtual.Checked)
			{
				m_mnuDisplayVirtual.Checked = false;
				ObjectBrowser.m_virtualFlag = false;
			}
			else
			{
				m_mnuDisplayVirtual.Checked = true;
				ObjectBrowser.m_virtualFlag = true;
			}

			//Refresh the display

			foreach (IDockContent dc in m_dockPanel.DocumentsToArray())
			{
				if (dc is InspectorWnd)
					((InspectorWnd)dc).RefreshView();
			}

			m_modelWnd.AfterSelectMethod();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the mnuToolsAllowEdit control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void MnuToolsAllowEditClick(object sender, EventArgs e)
		{
			if (m_mnuToolsAllowEdit.Checked)
			{
				m_mnuToolsAllowEdit.Checked = false;
				ObjectBrowser.m_updateFlag = false;
			}
			else
			{
				m_mnuToolsAllowEdit.Checked = true;
				ObjectBrowser.m_updateFlag = true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save to the database; does NOT force clearing undo stack.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void MnuSaveFileFdoClick(object sender, EventArgs e)
		{
			if (m_cache != null)
				m_cache.ServiceLocator.GetInstance<IUndoStackManager>().Save();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Repository CellValueChanged event of the DataGrid control in the Inspector object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void RepositoryInspectorGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
		{
			UpdateFdoProp(sender, m_repositoryWnd);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// UpdateFdoProp - update Fdo with changed values in the repository and LangProject windows.
		/// /// </summary>
		/// ------------------------------------------------------------------------------------

		private void UpdateFdoProp(object sender, InspectorWnd mWnd)
		{
			System.DateTime dtNewVal = new DateTime();
			System.Guid guidNewVal = new Guid();
			System.Boolean boolNewVal = false;
			GenDate genDate = new GenDate();
			GenDate genDate1;
			IInspectorObject node;
			System.Reflection.PropertyInfo pi = null;
			GenDate.PrecisionType newPreType = new GenDate.PrecisionType();
			ICmObject obj;
			int intNewVal = 0, hvo = 0;
			string strNewVal = "", dType = "";

			var sendObj = (InspectorGrid)sender;
			if (mWnd.CurrentInspectorObject.DisplayValue == sendObj.EditingControl.Text)
				return;     // nothing changed so return
			dType = mWnd.CurrentInspectorObject.DisplayType;

			if (dType != "System.String" && dType != "System.Int32" && dType != "System.Boolean" &&
				dType != "System.DateTime" && dType != "SIL.FieldWorks.Common.FwUtils.GenDate.PrecisionType")
				return;

			if (!BuildFdoPINeeded(mWnd.CurrentInspectorObject, out pi, out obj, out hvo))
			 {
				throw new System.ApplicationException("The necessary Info to update the Fdo wasn't obtained.");
			}

			node = mWnd.CurrentInspectorObject;
			switch (dType)
			{
				case "System.Int32":
					try
					{
						intNewVal = Int32.Parse(sendObj.EditingControl.Text);
						node.DisplayValue = sendObj.EditingControl.Text;
					}
					catch
					{
						MessageBox.Show("Value entered is not a valid integer: " + sendObj.EditingControl.Text);
						node.DisplayValue = node.DisplayValue;
						return;
					}

					if (node.ParentInspectorObject.DisplayType.IndexOf("GenDate") > 0)
					{
						if (ValidateGenDate(node, node.ParentInspectorObject, obj,
											pi, intNewVal, out genDate))
						{
							UpdateValues(node.ParentInspectorObject, obj, pi, "GenDate", "", intNewVal, genDate, dtNewVal, guidNewVal, boolNewVal);
							node.ParentInspectorObject.DisplayValue = genDate.ToLongString();
							return;
						}
						MessageBox.Show("GenDate Format (Update Integer) is invalid.");
						return;
					}

					UpdateValues(node, obj, pi, "Integer", "", intNewVal, genDate, dtNewVal, guidNewVal, boolNewVal);
					break;
				case "System.Guid":
					node.DisplayValue = sendObj.EditingControl.Text;
					guidNewVal = new Guid(sendObj.EditingControl.Text);
					UpdateValues(node, obj, pi, "Guid", "", 0, genDate, dtNewVal, guidNewVal, boolNewVal);
					break;
				case "System.Boolean":
					try
					{
						boolNewVal = bool.Parse(sendObj.EditingControl.Text);
						node.DisplayValue = sendObj.EditingControl.Text;
					}
					catch
					{
						MessageBox.Show("Value entered is not a valid boolean: " + sendObj.EditingControl.Text);
						node.DisplayValue = node.DisplayValue;
						return;
					}
					if (node.ParentInspectorObject.DisplayType.IndexOf("GenDate") > 0)
					{
						if (node.ParentInspectorObject != null && node.ParentInspectorObject.Flid >0)
							genDate1 = ((ISilDataAccessManaged)m_cache.DomainDataByFlid).get_GenDateProp(obj.Hvo, node.ParentInspectorObject.Flid);
						else
							genDate1 = (GenDate)pi.GetValue(obj, null);
						switch (node.DisplayName)
						{
							case "IsEmpty":
								if (boolNewVal)
								{
									genDate = new GenDate(genDate1.Precision, 0, 0, 0, genDate1.IsAD);
									ResetDisplayDates(node, mWnd, "IsEmpty", "0", hvo);
								}
								else
								{
									genDate = new GenDate(genDate1.Precision, 1, 1, 1, genDate1.IsAD);
									ResetDisplayDates(node, mWnd, "IsEmpty", "1", hvo);
								}
								break;
							case "IsAD":
								if (boolNewVal)
								{
									genDate = new GenDate(genDate1.Precision, 1, 1, genDate1.Year, true);
									ResetDisplayDates(node, mWnd, "IsAD", "1", hvo);
								}
								else
								{
									genDate = new GenDate(genDate1.Precision, 0, 0, genDate1.Year, false);
									ResetDisplayDates(node, mWnd, "IsAD", "0", hvo);
								}
								break;
							default:
								MessageBox.Show("GenDate boolean not IsAd or IsEmpty: " + node.DisplayName);
								genDate = genDate1;
								return;
						}

						node.ParentInspectorObject.DisplayValue = genDate.ToLongString();
						UpdateValues(node.ParentInspectorObject, obj, pi, "GenDate", "", intNewVal,
															 genDate, dtNewVal, guidNewVal, boolNewVal);
						break;
					}

					UpdateValues(node, obj, pi, "Boolean", "", 0, genDate, dtNewVal, guidNewVal,
																						   boolNewVal);

					break;
					case "System.String":
						for(int l = node.Level; l > 0; l--)
						{
							if (ParentIsType(ref node, "TsString"))
							{
								MessageBox.Show("This type of string cannot be editted because it is under a TsString");
								return;
							}
						}
						node = mWnd.CurrentInspectorObject;
						mWnd.CurrentInspectorObject.DisplayValue = sendObj.EditingControl.Text;
						strNewVal = sendObj.EditingControl.Text.ToString();
						UpdateValues(node, obj, pi, "String", strNewVal, 0, genDate, dtNewVal,
																					   guidNewVal, false);
						break;
					case "System.DateTime":
						if (DateTime.TryParse(sendObj.EditingControl.Text, out dtNewVal))
						{
							node.DisplayValue = sendObj.EditingControl.Text;
							UpdateValues(node, obj, pi, "DateTime", "", 0, genDate, dtNewVal,
																					   guidNewVal, false);

							break;
						}
						MessageBox.Show("Date Format is invalid.");
						node.DisplayValue = node.DisplayValue;
						return;
					case "SIL.FieldWorks.Common.FwUtils.GenDate.PrecisionType":
						strNewVal = sendObj.EditingControl.Text;
						switch (strNewVal)
						{
							case "Before":
								newPreType = GenDate.PrecisionType.Before;
								break;
							case "Exact":
								newPreType = GenDate.PrecisionType.Exact;
								break;
							case "Approximate":
								newPreType = GenDate.PrecisionType.Approximate;
								break;
							case "After":
								newPreType = GenDate.PrecisionType.After;
								break;
							default:
								MessageBox.Show("Precision must be: Before, After, Exact or Approximate.");
								node.DisplayValue = node.DisplayValue;
								return;
						}
						if (node.ParentInspectorObject != null &&
							node.ParentInspectorObject.Flid >0)
							genDate1 = ((ISilDataAccessManaged)m_cache.DomainDataByFlid).get_GenDateProp(obj.Hvo,
																		  node.ParentInspectorObject.Flid);
						else
							genDate1 = (GenDate)pi.GetValue(obj, null);
						node.DisplayValue = strNewVal;
						try
						{
							genDate = new GenDate(newPreType, genDate1.Month, genDate1.Day, genDate1.Year,
																							genDate1.IsAD);
						}
						catch
						{
							MessageBox.Show("Gendate with new Precision is invalid.");
							return;
						}
						node.ParentInspectorObject.DisplayValue = genDate.ToLongString();
						UpdateValues(node.ParentInspectorObject, obj, pi, "GenDate", "", 0, genDate,
																			 dtNewVal, guidNewVal, false);
						break;
					default:
						throw new ApplicationException("Illegal type");
				}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the object.  Custom fields update the cache, others the PropertyInfo.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateValues(IInspectorObject node, ICmObject obj, PropertyInfo pi,
				   string operation, string strVal, int intVal, GenDate genVal,
				   DateTime dtVal, Guid guidVal, Boolean boolVal)
		{
			switch (operation)
			{
				case "String":
					NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor,
					() =>
					{
						if (node != null && node.Flid > 0)
						{
							m_cache.DomainDataByFlid.set_UnicodeProp(obj.Hvo, node.Flid, strVal);
						}
						else if (pi == null && node != null)
						{
							if (GetPI(node, StripOffTypeChars(node.DisplayName), obj, out pi))
								pi.SetValue(obj, strVal, null);
							else
							{
								return;
							}
						}
						else
						{
							pi.SetValue(obj, strVal, null);
						}
					});
					break;
				case "GenDate":
					NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor,
					() =>
					{
						if (node != null && node.Flid > 0)
						{
							((ISilDataAccessManaged) m_cache.DomainDataByFlid).SetGenDate(obj.Hvo,
														   node.Flid, genVal);
						}
						else if (pi == null && node != null)
						{
							if (GetPI(node, StripOffTypeChars(node.DisplayName), obj, out pi))
								pi.SetValue(obj, genVal, null);
							else
							{
								return;
							}
						}
						else
						{
							pi.SetValue(obj, genVal, null);
						}
					});
					break;
				case "DateTime":
					NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor,
					() =>
					{
						if (node != null && node.Flid > 0)
						{
							((ISilDataAccessManaged) m_cache.DomainDataByFlid).SetDateTime(obj.Hvo,
																				   node.Flid, dtVal);
						}
						else if (pi == null && node != null)
						{
							if (GetPI(node, StripOffTypeChars(node.DisplayName), obj, out pi))
								pi.SetValue(obj, dtVal, null);
							else
							{
								return;
							}
						}
						else
						{
							pi.SetValue(obj, dtVal, null);
						}
					});
					break;
				case "Boolean":
					NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor,
					() =>
					{
						if (node != null && node.Flid > 0)
						{
							m_cache.DomainDataByFlid.SetBoolean(obj.Hvo, node.Flid, boolVal);
						}
						else if (pi == null && node != null)
						{
							if (GetPI(node, StripOffTypeChars(node.DisplayName), obj, out pi))
								pi.SetValue(obj, boolVal, null);
							else
							{
								return;
							}
						}
						else
						{
							pi.SetValue(obj, boolVal, null);
						}
					});
				break;
				case "Integer":
					NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor,
				   () =>
					{
						if (node != null && node.Flid > 0)
						{
							m_cache.DomainDataByFlid.SetInt(obj.Hvo, node.Flid, intVal);
						}
						else if (pi == null && node != null)
						{
							if (GetPI(node, StripOffTypeChars(node.DisplayName), obj, out pi))
								pi.SetValue(obj, intVal, null);
							else
							{
								return;
							}
						}
						else
						{
							pi.SetValue(obj, intVal, null);
						}
					});
					break;
				case "Guid":
					NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor,
				   () =>
					{
						if (node != null && node.Flid > 0)
						{
							m_cache.DomainDataByFlid.SetGuid(obj.Hvo, node.Flid, guidVal);
						}
						else if (pi == null && node != null)
						{
							if (GetPI(node, StripOffTypeChars(node.DisplayName), obj, out pi))
								pi.SetValue(obj, guidVal, null);
							else
							{
								return;
							}
						}
						else
						{
							pi.SetValue(obj, guidVal, null);
						}
					});
				break;
				default:
					MessageBox.Show(string.Format("Operation passed to UpdateValues is invalid: {0}", operation));
					break;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verify that, with the changes made, the gendate is still valid.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool ValidateGenDate(IInspectorObject io, IInspectorObject ioParent, ICmObject obj,
										PropertyInfo pi, int mdy, out GenDate genDate)
		{
			System.DateTime dt1, dt;
			var genDate1 = new GenDate();
			string tempDate = "";
			if (io.ParentInspectorObject != null && io.ParentInspectorObject.Flid > 0)
				genDate1 = ((ISilDataAccessManaged)m_cache.DomainDataByFlid).get_GenDateProp(obj.Hvo, io.ParentInspectorObject.Flid);
			else
				genDate1 = (GenDate)pi.GetValue(obj, null);

			switch (io.DisplayName)
			{
				case "Day":
					try
					{
						genDate = new GenDate(genDate1.Precision, genDate1.Month, mdy, genDate1.Year, genDate1.IsAD);
						io.DisplayValue = mdy.ToString();
						return true;
					}
					catch
					{
						MessageBox.Show("GenDate day is invalid");
						genDate = genDate1;
						io.DisplayValue = genDate1.Day.ToString();
						return false;
					}
				case "Month":
					try
					{
						genDate = new GenDate(genDate1.Precision, mdy, genDate1.Day, genDate1.Year, genDate1.IsAD);
						io.DisplayValue = mdy.ToString();
						return true;
					}
					catch
					{
						MessageBox.Show("GenDate month is invalid");
						genDate = genDate1;
						io.DisplayValue = genDate1.Month.ToString();
						return false;
					}
				case "Year":
					try
					{
						genDate = new GenDate(genDate1.Precision, genDate1.Month, genDate1.Day, mdy, genDate1.IsAD);
						io.DisplayValue = mdy.ToString();
						return true;
					}
					catch
					{
						MessageBox.Show("GenDate year is invalid");
						genDate = genDate1;
						io.DisplayValue = genDate1.Year.ToString();
						return false;
					}
				default:
					MessageBox.Show("GenDate Integer passed is not Month, Day or Year: " + io.DisplayName);
					genDate = genDate1;
					return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks if the parent of the current node is of a certain type
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool ParentIsType(ref IInspectorObject node, string type)
		{
			if (node.DisplayType.IndexOf(type) > 0)
				return true;
			else
			{
				node = node.ParentInspectorObject;
				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Looks through the grid display and update the display integers of the current GenDate to 0.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ResetDisplayDates(IInspectorObject node, InspectorWnd mWnd, string type,
									   string value, int hvo)
		{
			bool m_genDate_flag = false;

			for (int i = 0; 1 < mWnd.InspectorList.Count; i++)
			{
				if (m_genDate_flag)
				{
					if (mWnd.InspectorList[i].DisplayName == "Day")
						mWnd.InspectorList[i].DisplayValue = value;
					else if (mWnd.InspectorList[i].DisplayName == "Month")
					{
						mWnd.InspectorList[i].DisplayValue = value;
						if (type == "IsAD")
						break;
					}
					else if (mWnd.InspectorList[i].DisplayName == "Year")
					{
						mWnd.InspectorList[i].DisplayValue = value;
						break;
					}
				}

				if (mWnd.InspectorList[i].DisplayName == node.ParentInspectorObject.DisplayName &&
					hvo == ((ICmObject)node.ParentInspectorObject.OwningObject).Hvo)
					m_genDate_flag = true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the hvo node and column name if it's available at this level
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool GetHvoNode(ref IInspectorObject node, ref int hvo, ref string fieldName)
		{
			try
			{
				hvo = ((ICmObject)node.OwningObject).Hvo;
				fieldName = node.DisplayName;
				return true;
			}
			catch
			{
				node = node.ParentInspectorObject;
				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the property info if it's available.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool BuildFdoPINeeded(IInspectorObject node, out PropertyInfo pi, out ICmObject obj,
										 out int hvo)
		{
			string fieldName = "";
			hvo = 0;

			for (int l = node.Level; l > 0; l--)
			{
				if (GetHvoNode(ref node, ref hvo, ref fieldName))
					break;
				if (l == 0)
				{
					MessageBox.Show("The hvo could not be created for " + node.DisplayName);
					pi = null; obj = null; hvo = 0;
					return false;
				}
			}

			obj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
			if (obj == null)
			{
				MessageBox.Show("The FDO Object to update is null.");
				pi = null;
				return false;
			}

			if (!GetPI(node, fieldName, obj, out pi)) return false;

			return true;
		}

		private bool GetPI(IInspectorObject node, string fieldName, ICmObject obj, out PropertyInfo pi)
		{
			System.Type type = obj.GetType();

			if (!(node != null && node.Flid > 0))  //custom properties don't have PropertyInfo
			{
				pi = type.GetProperty(fieldName,
									  System.Reflection.BindingFlags.Instance |
									  System.Reflection.BindingFlags.Public |
									  System.Reflection.BindingFlags.FlattenHierarchy);
				if (pi == null)
				{
					MessageBox.Show("The PI Object to get property info from is null.");
					return false;
				}
			}
			else
				pi = null;
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// See if an object can be added under the current object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool AddObjectFromHere(IInspectorObject node)
		{
			string fieldName = "";

			if (node.DisplayName.Substring(node.DisplayName.Length - 2, 2) == "OC" ||
					node.DisplayName.Substring(node.DisplayName.Length - 2, 2) == "OS" ||
					node.DisplayName.Substring(node.DisplayName.Length - 2, 2) == "RC" ||
					node.DisplayName.Substring(node.DisplayName.Length - 2, 2) == "RS")
				return true;
			return ((node.DisplayName.EndsWith("OA") || node.DisplayName.EndsWith("RA")) &&
				   node.HasChildren == false && node.Object == null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// See if an object can be moved up in the current list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool MoveObjectUpFromHere(IInspectorObject node)
		{
			int nodePos = 0;

			nodePos = (node.DisplayName.Contains("[") ? Int32.Parse(node.DisplayName.Substring(1, node.DisplayName.IndexOf("]") - 1)) : 0);
			if (node.ParentInspectorObject != null)
				return (node.ParentInspectorObject.DisplayName.EndsWith("OS") || node.ParentInspectorObject.DisplayName.EndsWith("RS")) &&
					   (node.OwningObject as Array) != null && (node.OwningObject as Array).Length > 1 &&
					   nodePos > 0;
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// See if an object can be moved downin the current list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool MoveObjectDownFromHere(IInspectorObject node)
		{
			int nodePos = 0;

			nodePos = (node.DisplayName.Contains("[")? Int32.Parse(node.DisplayName.Substring(1,node.DisplayName.IndexOf("]")-1)): 0);
			if (node.ParentInspectorObject != null)
				return (node.ParentInspectorObject.DisplayName.EndsWith("OS") || node.ParentInspectorObject.DisplayName.EndsWith("RS")) &&
					   (node.OwningObject as Array) != null && (node.OwningObject as Array).Length > 1 &&
					   nodePos < (node.OwningObject as Array).Length-1;
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the LangProj CellValueChanged event of the DataGrid control in the Inspector object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void LangProjInspectorGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
		{
			UpdateFdoProp(sender, m_langProjWnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the property selector dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ShowPropertySelectorDialog(ICmObject cmObj)
		{
			using (var dlg = new ClassPropertySelector(cmObj))
			{
				if (dlg.ShowDialog(this) != DialogResult.OK) return;

				foreach (var dc in m_dockPanel.DocumentsToArray().OfType<InspectorWnd>())
				{
					(dc).RefreshView();
				}
			}
		}

		#region Implementation of IHelpTopicProvider

		///<summary>
		/// Get the indicated help string.
		/// </summary>
		public string GetHelpString(string sPropName)
		{
			if (s_helpResources == null)
			{
				s_helpResources = new ResourceManager(
					"FDOBrowser.Properties.Resources", Assembly.GetExecutingAssembly());
			}
			if (sPropName == null)
				return "NullStringID";
			return s_helpResources.GetString(sPropName);
		}

		///<summary>
		/// Get the name of the help file.
		/// </summary>
		public string HelpFile
		{
			get { return Path.Combine(DirectoryFinder.FWCodeDirectory, GetHelpString("UserHelpFile")); }
		}

		#endregion
	}
}
