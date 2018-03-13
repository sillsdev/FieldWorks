// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using System.Drawing;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Controls.XMLViews;
using LanguageExplorer.LcmUi;
using LCMBrowser.Properties;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Infrastructure;

namespace LCMBrowser
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// LCMBrowserForm Class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class LCMBrowserForm : Form, IHelpTopicProvider
	{
		#region Data members

		/// <summary>
		/// List of custom fields associated with the open project.
		/// </summary>
		public static List<CustomFields> CFields = new List<CustomFields>();

		private const string kAppCaptionFmt = "{0} - {1}";

		/// <summary>
		/// Specifies whether you want to see all properties of a class (true)
		/// or exclude the virtual properties (false).
		/// </summary>
		public static bool m_virtualFlag = false;

		/// <summary>
		/// Specifies whether you want to be able to update class properties here (true)
		/// or not (false).
		/// </summary>
		public static bool m_updateFlag = true;

		/// <summary></summary>
		private string m_appCaption;
		/// <summary></summary>
		protected string m_currOpenedProject;
		/// <summary></summary>
		protected readonly DockPanel m_dockPanel;
		/// <summary></summary>
		private List<string> m_ruFiles;

		private InspectorWnd m_InspectorWnd;
		/// <summary>
		/// Specifies whether the class was selected on the dialog (true) or not (false).
		/// </summary>
		public static bool m_dlgChanged;
		private LcmCache m_cache;
		private ILangProject m_lp;
		private ICmObjectRepository m_repoCmObject;
		private string m_fmtSelectPropsMenuText;
		private string m_fmtAddObjectMenuText;
		private ModelWnd m_modelWnd;
		private InspectorWnd m_langProjWnd;
		private InspectorWnd m_repositoryWnd;
		private int saveClid;
		private string FileName = string.Empty;
		private ToolStripTextBox m_tstxtGuidSrch;
		private ToolStripLabel m_tslblGuidSrch;
		private ISilDataAccessManaged m_silDataAccessManaged;
		private static ResourceManager s_helpResources;

		private string m_sHelpTopic = "khtpMainLCMBrowser";

		#endregion Data members

		#region Construction
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public LCMBrowserForm()
		{
			InitializeComponent();

			m_statuslabel.Text = string.Empty;
			m_sblblLoadTime.Text = string.Empty;
			m_appCaption = Text;

			m_dockPanel = new DockPanel();
			m_dockPanel.Dock = DockStyle.Fill;
			m_dockPanel.DefaultFloatWindowSize = new Size(600, 600);
			m_dockPanel.ActiveDocumentChanged += m_dockPanel_ActiveDocumentChanged;
			m_dockPanel.ContentRemoved += DockPanelContentRemoved;
			m_dockPanel.ContentAdded += DockPanelContentAdded;
			Controls.Add(m_dockPanel);
			Controls.SetChildIndex(m_dockPanel, 0);
			m_dockPanel.BringToFront();

			m_ruFiles = new List<string>();
			for (int i = 1; i <= 9; i++)
				m_ruFiles.Add(Settings.Default["RUFile" + i] as string);

			BuildRecentlyUsedFilesMenus();
			m_fmtAddObjectMenuText = cmnuAddObject.Text;
		}

		#endregion Construction

		#region Implementation of IHelpTopicProvider

		///<summary>
		/// Get the indicated help string.
		/// </summary>
		public string GetHelpString(string sPropName)
		{
			if (string.IsNullOrWhiteSpace(sPropName))
			{
				return "NullStringID";
			}
			if (s_helpResources == null)
			{
				s_helpResources = new ResourceManager("LCMBrowser.Properties.Resources", Assembly.GetExecutingAssembly());
			}

			return s_helpResources.GetString(sPropName);
		}

		///<summary>
		/// Get the name of the help file.
		/// </summary>
		public string HelpFile => Path.Combine(FwDirectoryFinder.CodeDirectory, GetHelpString("UserHelpFile"));

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Form.Load"/> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnLoad(EventArgs e)
		{
			Point pt = Settings.Default.MainWndLocation;
			if (pt != Point.Empty)
				Location = pt;

			Size sz = Settings.Default.MainWndSize;
			if (!sz.IsEmpty)
				Size = sz;

			base.OnLoad(e);

			OpenModelWindow();
			var showThem = Properties.Settings.Default.ShowCmObjectProperties;
			LCMClassList.ShowCmObjectProperties = showThem;
			m_tsbShowCmObjectProps.Checked = LCMClassList.ShowCmObjectProperties;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Form.FormClosing"/> event.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			base.OnFormClosing(e);

			int i = 1;
			foreach (string file in m_ruFiles)
				Settings.Default["RUFile" + i++] = file;

			Settings.Default.MainWndLocation = Location;
			Settings.Default.MainWndSize = Size;
			Settings.Default.Save();
			Settings.Default.ShowCmObjectProperties = m_tsbShowCmObjectProps.Checked;
			Settings.Default.Save();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Puts the specified file path at the top of recently used file list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void PutFileAtTopOfRUFileList(string newfile)
		{
			// Check if it's already at the top of the stack.
			if (newfile == m_ruFiles[0])
				return;

			// Either remove the file from list or remove the last file from the list.
			int index = m_ruFiles.IndexOf(newfile);
			if (index >= 0)
				m_ruFiles.RemoveAt(index);
			else
				m_ruFiles.RemoveAt(m_ruFiles.Count - 1);

			// Insert the file at the top of the list and rebuild the menus.
			m_ruFiles.Insert(0, newfile);
			BuildRecentlyUsedFilesMenus();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Builds the recently used files menus.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void BuildRecentlyUsedFilesMenus()
		{
			// Remove old recently used file menu items.
			for (int i = 0; i <= 8; i++)
			{
				int index = mnuFile.DropDownItems.IndexOfKey("RUF" + i);
				if (index >= 0)
					mnuFile.DropDownItems.RemoveAt(index);
			}

			// Get the index where to add recently used file names.
			int insertIndex = mnuFile.DropDownItems.IndexOf(mnuFileSep1) + 1;

			// Add the recently used file names to the file menu.
			for (int i = 8; i >= 0; i--)
			{
				string file = m_ruFiles[i];
				if (!String.IsNullOrEmpty(file))
				{
					mnuFileSep1.Visible = true;
					ToolStripMenuItem mnu = new ToolStripMenuItem();
					mnu.Name = "RUF" + i;
					mnu.Text = String.Format("&{0} {1}", i + 1, file);
					mnu.Tag = file;
					mnu.Click += HandleRUFileMenuClick;
					mnuFile.DropDownItems.Insert(insertIndex, mnu);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the user clicking on one of the recently used file menu items.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void HandleRUFileMenuClick(object sender, EventArgs l)
		{
			ToolStripMenuItem mnu = sender as ToolStripMenuItem;
			try
			{
				OpenFile(mnu.Tag as string);
			}
			catch (Exception e)
			{
				MessageBox.Show("Exception caught:" + e.Message + mnu.Tag +
					". Open up Flex for this project to create the necessary writing systems.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Opens the specified file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void OpenFile(string fileName)
		{
			if (fileName == null || !File.Exists(fileName) || m_currOpenedProject == fileName)
			{
				return;
			}
			// if we didn't clean up when we ran this before, do it now.
			if (m_cache != null && File.Exists(FileName + ".lock"))
			{
				m_cache.ServiceLocator.GetInstance<IActionHandler>().Commit();
				m_cache.Dispose();
				m_cache = null; // Don't try to use it again
			}
			// Save the filename so we can close it when we exit
			FileName = fileName;
			Cursor = Cursors.WaitCursor;
			try
			{
				var bepType = GetBEPTypeFromFileExtension(fileName);
				var isMemoryBEP = bepType == BackendProviderType.kMemoryOnly;

				var stopwatch = new Stopwatch();
				stopwatch.Start();

				// Init backend data provider
				// TODO: Get the correct ICU local for the user writing system

				var ui = new FwLcmUI(this, this);
				if (isMemoryBEP)
				{
					m_cache = LcmCache.CreateCacheWithNewBlankLangProj(new BrowserProjectId(bepType, null), "en", "en", "en", ui,
						FwDirectoryFinder.LcmDirectories, new LcmSettings());
				}
				else
				{
					using (var progressDlg = new ProgressDialogWithTask(this))
					{
						m_cache = LcmCache.CreateCacheFromExistingData(new BrowserProjectId(bepType, fileName), "en", ui, FwDirectoryFinder.LcmDirectories, new LcmSettings(), progressDlg);
					}
				}

				CFields = GetCustomFields(m_cache);
				m_lp = m_cache.LanguageProject;
				m_silDataAccessManaged = m_cache.GetManagedSilDataAccess();

				stopwatch.Stop();

				m_sblblLoadTime.Text = $"Load Time: {stopwatch.Elapsed}";

				// Close any windows from a previously opened project, if there is one.
				foreach (var dc in m_dockPanel.DocumentsToArray())
				{
					if (dc is InspectorWnd)
					{
						((InspectorWnd)dc).Close();
					}
				}

				m_langProjWnd?.Close();
				m_langProjWnd = null;
				m_repositoryWnd = null;
				MakeAppCaption(m_lp.ShortName);
				OpenLangProjWindow();
				OpenRepositoryWindow();

				PutFileAtTopOfRUFileList(fileName);

				m_currOpenedProject = fileName;
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes the application's caption.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void MakeAppCaption(string text)
		{
			Text = String.Format(kAppCaptionFmt, text, m_appCaption);
		}

		#region Dock Panel event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the ActiveDocumentChanged event of the m_dockPanel control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void m_dockPanel_ActiveDocumentChanged(object sender, EventArgs e)
		{
			InspectorWnd wnd = m_dockPanel.ActiveDocument as InspectorWnd;
			m_statuslabel.Text = (wnd == null ? String.Empty : wnd.InspectorList.Count + " Top Level Items");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the ContentAdded event of the m_dockPanel control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DockPanelContentAdded(object sender, DockContentEventArgs e)
		{
			if (e.Content is InspectorWnd)
			{
				m_tsbShowCmObjectProps.Enabled = true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the ContentRemoved event of the m_dockPanel control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DockPanelContentRemoved(object sender, DockContentEventArgs e)
		{
			foreach (var dc in m_dockPanel.DocumentsToArray())
			{
				if (dc is InspectorWnd)
				{
					return;
				}
			}

			m_tsbShowCmObjectProps.Enabled = false;
		}

		#endregion

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the openToolStripMenuItem control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void HandleFileOpenClick(object sender, EventArgs e)
		{
			using (var dlg = new OpenFileDialog())
			{
				dlg.CheckFileExists = true;
				dlg.RestoreDirectory = true;
				dlg.Title = OpenFileDlgTitle;
				dlg.ValidateNames = true;
				dlg.Multiselect = false;
				dlg.Filter = OpenFileDlgFilter;
				if (dlg.ShowDialog(this) == DialogResult.OK)
					OpenFile(dlg.FileName);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the title for the open file dialog box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string OpenFileDlgTitle => "Open FieldWorks Language Project";

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the file filter for the open file dialog box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string OpenFileDlgFilter => ResourceHelper.FileFilter(FileFilterType.FieldWorksProjectFiles);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the mnuExit control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void mnuExit_Click(object sender, EventArgs e)
		{
			Close();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the DropDownOpening event of the mnuWindow control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void mnuWindow_DropDownOpening(object sender, EventArgs e)
		{
			// Remove all the windows names at the end of the Windows menu list.
			ToolStripItemCollection wndMenus = mnuWindow.DropDownItems;
			for (int i = wndMenus.Count - 1; wndMenus[i] != mnuWindowsSep; i--)
			{
				wndMenus[i].Click -= HandleWindowMenuItemClick;
				wndMenus.RemoveAt(i);
			}

			// Now add a menu item for each document in the dock panel.
			foreach (IDockContent dc in m_dockPanel.Documents)
			{
				DockContent wnd = dc as DockContent;
				ToolStripMenuItem mnu = new ToolStripMenuItem();
				mnu.Text = wnd.Text;
				mnu.Tag = wnd;
				mnu.Checked = (m_dockPanel.ActiveContent == wnd);
				mnu.Click += HandleWindowMenuItemClick;
				mnuWindow.DropDownItems.Add(mnu);
			}

			mnuTileVertically.Enabled = (m_dockPanel.DocumentsCount > 1);
			mnuTileHorizontally.Enabled = (m_dockPanel.DocumentsCount > 1);
			mnuArrangeInline.Enabled = (m_dockPanel.DocumentsCount > 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the window menu item click.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		void HandleWindowMenuItemClick(object sender, EventArgs e)
		{
			var mnu = sender as ToolStripMenuItem;
			(mnu?.Tag as DockContent)?.Show(m_dockPanel);
		}

		#endregion Event handlers

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the new inspector window for the specified object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual InspectorWnd ShowNewInspectorWindow(object obj)
		{
			return ShowNewInspectorWindow(obj, null, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the new inspector window for the specified object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual InspectorWnd ShowNewInspectorWindow(object obj, string text)
		{
			return ShowNewInspectorWindow(obj, text, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the new inspector window for the specified object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual InspectorWnd ShowNewInspectorWindow(object obj, string text, string toolTipText)
		{
			var wnd = ShowNewInspectorWndOne();
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
			wnd = ShowNewInspectorWndTwo(obj, text, toolTipText, wnd);
			wnd.WillObjDisappearOnRefresh += HandleWillObjDisappearOnRefresh;
			return wnd;
		}

		/// ------------------------------------------------------------------------------------
		public InspectorWnd ShowNewInspectorWndOne()
		{
			InspectorWnd wnd = new InspectorWnd();
			return wnd;
		}

		/// ------------------------------------------------------------------------------------
		public InspectorWnd ShowNewInspectorWndTwo(object obj, string text, string toolTipText, InspectorWnd wnd)
		{
			wnd.Text = (!String.IsNullOrEmpty(text) ? text : GetNewInspectorWndTitle(obj));
			wnd.ToolTipText = (String.IsNullOrEmpty(toolTipText) ? wnd.Text : toolTipText);
			wnd.SetTopLevelObject(obj, GetNewInspectorList());
			wnd.InspectorGrid.ContextMenuStrip = m_cmnuGrid;
			wnd.InspectorGrid.Enter += InspectorGrid_Enter;
			wnd.InspectorGrid.Leave += InspectorGrid_Leave;
			wnd.FormClosed += HandleWindowClosed;
			wnd.Show(m_dockPanel);
			return wnd;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the title for a new inspector window being built for the specified object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string GetNewInspectorWndTitle(object obj)
		{
			if (obj == null)
			{
				return string.Empty;
			}
			if (obj is ITsString)
			{
				return $"ITsString: '{((ITsString)obj).Text}'";
			}
			var objString = obj.ToString();
			var title = objString;
			var typeName = obj.GetType().Name;
			if (!objString.StartsWith(typeName))
			{
				title = typeName;
				if (string.IsNullOrWhiteSpace(objString))
				{
					title += $": {objString}";
				}
			}
			return title;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the new inspector list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private IInspectorList GetNewInspectorList()
		{
			return new LcmInspectorList(m_cache);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Leave event of the InspectorGrid control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		void InspectorGrid_Leave(object sender, EventArgs e)
		{
			tsbShowObjInNewWnd.Enabled = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Enter event of the InspectorGrid control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		void InspectorGrid_Enter(object sender, EventArgs e)
		{
			tsbShowObjInNewWnd.Enabled = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the inspector window closed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="System.Windows.Forms.FormClosedEventArgs"/>
		/// instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void HandleWindowClosed(object sender, FormClosedEventArgs e)
		{
			if (sender is DockContent)
				((DockContent)sender).FormClosed -= HandleWindowClosed;

			if (sender is InspectorWnd)
			{
				((InspectorWnd)sender).InspectorGrid.Enter -= InspectorGrid_Enter;
				((InspectorWnd)sender).InspectorGrid.Leave -= InspectorGrid_Leave;
			}

			tsbShowObjInNewWnd.Enabled = false;

			if (sender is InspectorWnd)
			{
				((InspectorWnd)sender).WillObjDisappearOnRefresh -= HandleWillObjDisappearOnRefresh;
			}

			if (sender == m_modelWnd)
			{
				m_modelWnd = null;
			}
			else if (sender == m_langProjWnd)
			{
				m_langProjWnd = null;
			}
			else if (sender == m_repositoryWnd)
			{
				m_repositoryWnd = null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the mnuTileVertically control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void mnuTileVertically_Click(object sender, EventArgs e)
		{
			TileWindows(DockAlignment.Right);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the mnuTileHorizontally control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void mnuTileHorizontally_Click(object sender, EventArgs e)
		{
			TileWindows(DockAlignment.Bottom);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the mnuArrangeInline control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void mnuArrangeInline_Click(object sender, EventArgs e)
		{
			m_dockPanel.SuspendLayout();

			IDockContent currentWnd = m_dockPanel.ActiveDocument;
			IDockContent[] documents = m_dockPanel.DocumentsToArray();
			DockContent wndAnchor = documents[0] as DockContent;
			for (int i = documents.Length - 1; i >= 0; i--)
			{
				DockContent wnd = documents[i] as DockContent;
				wnd.DockTo(wndAnchor.Pane, DockStyle.Fill, 0);
			}

			((DockContent)currentWnd).Activate();
			m_dockPanel.ResumeLayout();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tiles the windows.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void TileWindows(DockAlignment alignment)
		{
			m_dockPanel.SuspendLayout();

			IDockContent[] documents = m_dockPanel.DocumentsToArray();
			DockContent wndAnchor = documents[0] as DockContent;
			if (wndAnchor == null)
				return;

			IDockContent currentWnd = m_dockPanel.ActiveDocument;

			for (int i = documents.Length - 1; i > 0; i--)
			{
				double proportion = 1.0 / (i + 1);
				DockContent wnd = documents[i] as DockContent;
				if (wnd != null)
					wnd.Show(m_dockPanel.Panes[0], alignment, proportion);
			}

			((DockContent)currentWnd).Activate();
			m_dockPanel.ResumeLayout();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the m_tsbShowObjInNewWnd control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_tsbShowObjInNewWnd_Click(object sender, EventArgs e)
		{
			InspectorWnd wnd = m_dockPanel.ActiveContent as InspectorWnd;
			if (wnd == null)
				return;

			IInspectorObject io = wnd.CurrentInspectorObject;
			if (io == null || io.Object == null)
				return;

			string text = io.DisplayName;
			if (text.StartsWith("[") && text.EndsWith("]"))
			{
				text = text.Trim('[', ']');
				int i;
				if (Int32.TryParse(text, out i))
					text = null;
			}

			if (text != null && text != io.DisplayType)
				text += (": " + io.DisplayType);

			m_InspectorWnd = ShowNewInspectorWindow(io.Object, text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Opening event of the grid's context menu.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void HandleOpenObjectInNewWindowContextMenuClick(object sender, CancelEventArgs e)
		{
			saveClid = 0;
			InspectorWnd wnd = m_dockPanel.ActiveContent as InspectorWnd;
			if (wnd == null)
			{
				return;
			}
			IInspectorObject io = wnd.CurrentInspectorObject;
			cmnuShowInNewWindow.Enabled = (io != null && io.Object != null);
			cmnuAddObject.Enabled = io != null && AddObjectFromHere(io);
			cmnuDeleteObject.Enabled = io?.Object is ICmObject;
			cmnuMoveObjectUp.Enabled = io != null && MoveObjectUpFromHere(io);
			cmnuMoveObjectDown.Enabled = io != null && MoveObjectDownFromHere(io);
			var type = io?.Object is ICmObject ? io.Object.GetType().Name : "???";
			if (io == null || !io.DisplayName.EndsWith("OS") && !io.DisplayName.EndsWith("OC") &&
				!io.DisplayName.EndsWith("OA") && !io.DisplayName.EndsWith("RS") &&
				!io.DisplayName.EndsWith("RC") && !io.DisplayName.EndsWith("RA"))
			{
				return;
			}
			var mdc = m_cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
			var owner = io.ParentInspectorObject.Object as ICmObject ?? ((ICmObject)io.ParentInspectorObject.OwningObject);

			var currentObj = io.Object as ICmObject ?? ((ICmObject)io.OwningObject);
			int flid;
			try
			{
				var work = StripOffTypeChars(io.DisplayName);
				flid = mdc.GetFieldId2(owner.ClassID, work, true);
			}
			catch
			{
				MessageBox.Show("No Flid for clicked line: " + io.DisplayValue);
				return;
			}

			var clid = m_cache.DomainDataByFlid.MetaDataCache.GetDstClsId(flid);
			if (io.DisplayName.EndsWith("RS") || io.DisplayName.EndsWith("RC") || io.DisplayName.EndsWith("RA"))
			{
				var dispFlag = io.DisplayName.EndsWith("RA") && io.Object != null;
				type = GetTypeForRefObjs(flid, clid, dispFlag, ref saveClid);
				cmnuAddObject.Text = $"Add New Reference of type {type}...";
				cmnuSelectProps.Text = string.Format(m_fmtSelectPropsMenuText, type);
			}
			else
			{
				switch (clid)
				{
					case 7: // cmPossibility
						if (PossOwnedByPossList(mdc, currentObj, ref clid))
						{
							type = mdc.GetClassName(clid);
						}
						break;
					case 15: // stPara
						if (StParaOwnedByScrBook(mdc, currentObj, ref clid))
						{
							type = mdc.GetClassName(clid);
						}
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
				cmnuAddObject.Text = string.Format(m_fmtAddObjectMenuText, type);
				cmnuSelectProps.Text = string.Format(m_fmtSelectPropsMenuText, type);
			}
		}

		/// <summary>
		/// Get the possible classes for objects for the selected reference property.
		/// </summary>
		private string GetTypeForRefObjs(int flid, int sclid, bool dispFlag, ref int saveClid)
		{
			var list = new List<string>();
			var mdc = m_cache.GetManagedMetaDataCache();
			var clid = int.Parse(flid.ToString().Substring(0, flid.ToString().Length - 3));
			var type = mdc.GetClassName(sclid);
			switch (type)
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
								var classes = mdc.GetAllSubclasses(sclid);
								foreach (var cl in classes)
								{
									if (cl != sclid)
									{
										list.Add(mdc.GetClassName(cl));
									}
								}

								using (var dlg1 = new RealListChooser("ClassName", list))
								{
									if (dlg1.m_chosenClass == "Cancel")
										break;
									if (mdc.GetClassId(dlg1.m_chosenClass) == 0)
									{
										MessageBox.Show($"No clid for selected class: {dlg1.m_chosenClass}");
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

		/// <summary>
		/// Check to see if the current object is owned by CmPossibilityList.
		/// </summary>
		private static bool PossOwnedByPossList(IFwMetaDataCacheManaged mdc, ICmObject owner, ref int clid)
		{
			do
			{
				if (owner.ClassID == mdc.GetClassId("CmPossibilityList")) //if the owner is CmPossibilityList
				{
					var holdposs = owner as ICmPossibilityList;
					if (holdposs == null)
					{
						MessageBox.Show("PossOwnedByPossList; holdposs is null");
						return false;
					}
					clid = holdposs.ItemClsid;
					return true;
				}
				owner = owner.Owner;
			} while (owner != null);

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the mnuOptions control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void mnuOptions_Click(object sender, EventArgs e)
		{
			using (OptionsDlg dlg = new OptionsDlg(Settings.Default.ShadeColor))
			{
				dlg.ShadingEnabled = Settings.Default.UseShading;
				dlg.SelectedColor = Settings.Default.ShadeColor;

				if (dlg.ShowDialog(this) != DialogResult.OK)
					return;

				Settings.Default.UseShading = dlg.ShadingEnabled;
				Color clrNew = Color.Empty;

				if (dlg.ShadingEnabled)
				{
					clrNew = dlg.SelectedColor;
					Settings.Default.ShadeColor = clrNew;
				}

				foreach (IDockContent dc in m_dockPanel.DocumentsToArray())
				{
					if (dc is InspectorWnd)
						((InspectorWnd)dc).InspectorGrid.ShadingColor = clrNew;
				}
			}
		}

		/// <summary>
		/// Handles the Click event of the cmnuAddObject control.
		/// </summary>
		private void CmnuAddObjectClick(object sender, EventArgs e)
		{
			ICmObject refTarget;
			ICmPossibilityList holdposs;
			int oclid;
			var ord = 0;
			int idx;
			int flid;
			var mWnd = m_dockPanel.ActiveContent as InspectorWnd;

			var io = mWnd?.CurrentInspectorObject;
			if (io == null)
			{
				return;
			}

			var type = io.DisplayName.Substring(io.DisplayName.Length - 2, 2);
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

			var owner = io.ParentInspectorObject.Object as ICmObject ?? ((ICmObject)io.ParentInspectorObject.OwningObject);

			if (owner == null)
			{
				MessageBox.Show(@"owner for add is null");
				return;
			}

			var currentObj = io.Object as ICmObject ?? io.ParentInspectorObject.Object as ICmObject;

			if (currentObj == null)
			{
				MessageBox.Show("currentObj for add is null");
				return;
			}

			var mdc = m_cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();

			try
			{
				var work = StripOffTypeChars(io.DisplayName);
				flid = mdc.GetFieldId2(owner.ClassID, work, true);
			}
			catch
			{
				MessageBox.Show(@"No Flid for clicked line: " + io.DisplayValue);
				return;
			}

			var clid = m_cache.MetaDataCacheAccessor.GetDstClsId(flid);

			if (type == "RS" || type == "RC" || type == "RA")
			{
				if (DisplayReferenceObjectsToAdd(flid, type, currentObj, out refTarget))
					if (refTarget != null)
					{
						switch (type)
						{
							case "RS":
							case "RC":
								var count = m_cache.DomainDataByFlid.get_VecSize(currentObj.Hvo, flid);
								NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
									m_cache.DomainDataByFlid.Replace(currentObj.Hvo, flid, count, count, new[] { refTarget.Hvo }, 1)
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
							MessageBox.Show($"The creation of class id {clid} failed.");
							return;
						}
					});
				}
			}

			//Refresh the display
			foreach (var dc in m_dockPanel.DocumentsToArray())
			{
				(dc as InspectorWnd)?.RefreshView("Add");
			}
		}

		/// <summary>
		/// Receives field.  Return field less OA, OC, OS, RA, RC, or RS if there.
		///  </summary>
		public static string StripOffTypeChars(string field)
		{
			if (field.EndsWith("OS") || field.EndsWith("RS") || field.EndsWith("OC") || field.EndsWith("RC") || field.EndsWith("OA") || field.EndsWith("RA"))
			{
				return field.Substring(0, field.Length - 2);
			}

			return field;
		}

		/// <summary>
		/// Handles getting the correct class (clid) to add for owning properties.
		/// </summary>
		private bool DisplayOwningObjectToAdd(IFwMetaDataCacheManaged mdc, ICmObject owner, ref int clid)
		{
			// if the chosen classname is abstract, we need to get concrete classes and let the user choose
			var list = new List<string>();

			if (!mdc.GetAbstract(clid))
			{
				return true;
			}
			if (clid == 15 && StParaOwnedByScrBook(mdc, owner, ref clid))   //stPara is abstact - see if it is a special condition
			{
				return true;
			}

			var clidSubs = mdc.GetAllSubclasses(clid);
			list.AddRange(clidSubs.Where(t => !mdc.GetAbstract(t)).Select(t => mdc.GetClassName(t)));

			if (clidSubs.Length > 1)
			{
				using (var dlg = new RealListChooser("ClassName", list))
				{
					if (dlg.m_chosenClass == "Cancel")
					{
						return false;
					}
					clid = mdc.GetClassId(dlg.m_chosenClass);  // get the clid for the selected class
					if (clid != 0)
					{
						return true;
					}
					MessageBox.Show("No clid for selected class: " + dlg.m_chosenClass);
					return false;
				}
			}
			clid = clidSubs[0];
			return true;
			// class is concrete
		}

		/// <summary>
		/// Handles getting the correct object for a reference property to add.
		/// </summary>
		private bool DisplayReferenceObjectsToAdd(int flid, string type, ICmObject currObject, out ICmObject refTarget)
		{
			var labels = new List<ObjectLabel>();
			var mdc = m_cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
			var refObjs = new HashSet<int>();

			if (m_cache.MetaDataCacheAccessor != null)
			{
				var sclid = m_cache.MetaDataCacheAccessor.GetDstClsId(flid); //signature clid of field

				if (type != "RA")
				{
					int count;
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
					{
						for (var i = 0; count > i; i++)
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
				}
				Skip:
				List<ICmPossibility> possList;
				var desiredClasses = GetClassesForRefObjs(flid, sclid, out possList);
				if (desiredClasses != null && possList == null)
				{
					if (refObjs.Count == 0)
					{
						var objects = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>()
							.AllInstances(sclid)
							.Where(obj => desiredClasses.Contains(obj.ClassID));

						labels.AddRange(objects.Select(obj => ObjectLabel.CreateObjectLabelOnly(m_cache, obj, "ObjectIdName", "best analysis")));
					}
					else
					{
						var objects = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>()
							.AllInstances(sclid)
							.Where(obj => desiredClasses.Contains(obj.ClassID) && !refObjs.Contains(obj.Hvo));
						labels.AddRange(objects.Select(obj => ObjectLabel.CreateObjectLabelOnly(m_cache, obj, "ObjectIdName", "best analysis")));
					}
					if (labels.Count == 0)
					{
						var hashList = string.Empty;
						foreach (var de in desiredClasses)
						{
							hashList = hashList + mdc.GetClassName(de) + " ";
						}
						MessageBox.Show($"No objects exist within the selected classes.  Try again.  Selected classes are: {hashList}");
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
					labels = (List<ObjectLabel>)ObjectLabel.CreateObjectLabels(m_cache, possList, "ObjectIdName", "best analysis");
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

		/// <summary>
		/// Get the possible classes for objects for the selected reference property.
		/// </summary>
		private HashSet<int> GetClassesForRefObjs(int flid, int sclid, out List<ICmPossibility> possList)
		{
			var mdc = m_cache.GetManagedMetaDataCache();
			var clid = int.Parse(flid.ToString().Substring(0, flid.ToString().Length - 3));
			var classList1 = new HashSet<int>();
			var list = new List<string>();
			possList = null;
			switch (mdc.GetClassName(sclid))
			{
				case "CmObject":
					switch (mdc.GetClassName(clid) + "_" + mdc.GetFieldName(flid))
					{
						case "LexEntryRef_ComponentLexemes":
						case "LexEntryRef_PrimaryLexemes":
						case "LexReference_Targets":
							classList1 = new HashSet<int>(mdc.GetAllSubclasses(mdc.GetClassId("LexEntry")));
							foreach (var cid in mdc.GetAllSubclasses(mdc.GetClassId("LexSense")))
							{
								classList1.Add(cid);
							}
							return classList1;

						case "Segment_Analyses":
							classList1 = new HashSet<int>(mdc.GetAllSubclasses(mdc.GetClassId("WfiGloss")));
							foreach (var cid in mdc.GetAllSubclasses(mdc.GetClassId("WfiAnalysis")))
							{
								classList1.Add(cid);
							}

							foreach (var cid in mdc.GetAllSubclasses(mdc.GetClassId("WfiWordform")))
							{
								classList1.Add(cid);
							}

							foreach (var cid in mdc.GetAllSubclasses(mdc.GetClassId("PunctuationForm")))
							{
								classList1.Add(cid);
							}
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
								var classes = mdc.GetAllSubclasses(sclid);
								foreach (var cl in classes)
								{
									if (cl != sclid)
									{
										list.Add(mdc.GetClassName(cl));
									}
								}

								using (var dlg = new RealListChooser("ClassName", list))
								{
									if (dlg.m_chosenClass == "Cancel")
									{
										return classList1;
									}
									classList1.Add(mdc.GetClassId(dlg.m_chosenClass)); // get the clid for the selected class
									if (mdc.GetClassId(dlg.m_chosenClass) == 0)
									{
										MessageBox.Show($"No clid for selected class: {dlg.m_chosenClass}");
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
								possList = new List<ICmPossibility>(m_cache.LangProject.DiscourseDataOA.ChartMarkersOA.ReallyReallyAllPossibilities);
								return classList1;
							case "ConstituentChartCellPart_Column":
							case "DsChart_Template":
								possList = new List<ICmPossibility>(m_cache.LangProject.DiscourseDataOA.ConstChartTemplOA.ReallyReallyAllPossibilities);
								return classList1;
							case "MoDerivAffMsa_AffixCategory":
							case "MoInflAffMsa_AffixCategory":
								possList = new List<ICmPossibility>(m_cache.LangProject.AffixCategoriesOA.ReallyReallyAllPossibilities);
								return classList1;
							case "CmPossibility_Status":
							case "RnGenericRec_Status":
								possList = new List<ICmPossibility>(m_cache.LangProject.StatusOA.ReallyReallyAllPossibilities);
								return classList1;
							case "CmPossibility_Confidence":
							case "RnGenericRec_Confidence":
								possList = new List<ICmPossibility>(m_cache.LangProject.ConfidenceLevelsOA.ReallyReallyAllPossibilities);
								return classList1;
							case "CmPerson_Education":
								possList = new List<ICmPossibility>(m_cache.LangProject.EducationOA.ReallyReallyAllPossibilities);
								return classList1;
							case "Text_Genres":
								possList = new List<ICmPossibility>(m_cache.LangProject.GenreListOA.ReallyReallyAllPossibilities);
								return classList1;
							case "CmPerson_Positions":
								possList = new List<ICmPossibility>(m_cache.LangProject.PositionsOA.ReallyReallyAllPossibilities);
								return classList1;
							case "CmPossibility_Restrictions":
							case "RnGenericRec_Restrictions":
								possList = new List<ICmPossibility>(m_cache.LangProject.RestrictionsOA.ReallyReallyAllPossibilities);
								return classList1;
							case "RnRoledPartic_Role":
								possList = new List<ICmPossibility>(m_cache.LangProject.RolesOA.ReallyReallyAllPossibilities);
								return classList1;
							case "TextTag_Tag":
								possList = new List<ICmPossibility>(m_cache.LangProject.TextMarkupTagsOA.ReallyReallyAllPossibilities);
								return classList1;
							case "RnGenericRec_TimeOfEvent":
								possList = new List<ICmPossibility>(m_cache.LangProject.TimeOfDayOA.ReallyReallyAllPossibilities);
								return classList1;
							case "CmTranslation_Type":
								possList = new List<ICmPossibility>(m_cache.LangProject.TranslationTagsOA.ReallyReallyAllPossibilities);
								return classList1;
							case "LexSense_DomainTypes":
								possList = new List<ICmPossibility>(m_cache.LangProject.LexDbOA.DomainTypesOA.ReallyReallyAllPossibilities);
								return classList1;
							case "LexSense_SenseType":
								possList = new List<ICmPossibility>(m_cache.LangProject.LexDbOA.SenseTypesOA.ReallyReallyAllPossibilities);
								return classList1;
							case "LexSense_Status":
								possList = new List<ICmPossibility>(m_cache.LangProject.StatusOA.ReallyReallyAllPossibilities);
								return classList1;
							case "LexSense_UsageTypes":
								possList = new List<ICmPossibility>(m_cache.LangProject.LexDbOA.UsageTypesOA.ReallyReallyAllPossibilities);
								return classList1;
							case "MoCompoundRule_ToProdRestrict":
							case "MoDerivAffMsa_FromProdRestrict":
							case "MoDerivAffMsa_ToProdRestrict":
							case "MoDerivStepMsa_ProdRestrict":
							case "MoInflAffMsa_FromProdRestrict":
							case "MoStemMsa_FromProdRestrict":
								possList = new List<ICmPossibility>(m_cache.LangProject.MorphologicalDataOA.ProdRestrictOA.ReallyReallyAllPossibilities);
								return classList1;
							case "RnGenericRec_Type":
								possList = new List<ICmPossibility>(m_cache.LangProject.ResearchNotebookOA.RecTypesOA.ReallyReallyAllPossibilities);
								return classList1;
							case "ScrScriptureNote_Categories":
								possList = new List<ICmPossibility>(m_cache.LangProject.TranslatedScriptureOA.NoteCategoriesOA.ReallyReallyAllPossibilities);
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
		/// Check to see if the current object is owned by ScrBook.
		/// </summary>
		private static bool StParaOwnedByScrBook(IFwMetaDataCacheManaged mdc, ICmObject owner, ref int clid)
		{
			do
			{
				if (owner.ClassID == mdc.GetClassId("ScrBook")) //if the owner is an object of type ScrBook
				{
					clid = mdc.GetClassId("ScrTxtPara");   //scrTextPara
					return true;
				}
				owner = owner.Owner;
			} while (owner != null);

			return false;
		}

		/// <summary>
		/// See if an object can be added under the current object.
		/// </summary>
		private bool AddObjectFromHere(IInspectorObject node)
		{
			return node.DisplayName.Substring(node.DisplayName.Length - 2, 2) == "OC" ||
				   node.DisplayName.Substring(node.DisplayName.Length - 2, 2) == "OS" ||
				   node.DisplayName.Substring(node.DisplayName.Length - 2, 2) == "RC" ||
				   node.DisplayName.Substring(node.DisplayName.Length - 2, 2) == "RS" ||
				   (node.DisplayName.EndsWith("OA") || node.DisplayName.EndsWith("RA")) &&
				   node.HasChildren == false && node.Object == null;
		}

		/// <summary>
		/// See if an object can be moved up in the current list.
		/// </summary>
		private bool MoveObjectUpFromHere(IInspectorObject node)
		{
			var nodePos = (node.DisplayName.Contains("[") ? int.Parse(node.DisplayName.Substring(1, node.DisplayName.IndexOf("]") - 1)) : 0);
			if (node.ParentInspectorObject != null)
			{
				return (node.ParentInspectorObject.DisplayName.EndsWith("OS") || node.ParentInspectorObject.DisplayName.EndsWith("RS")) &&
					   (node.OwningObject as Array) != null && (node.OwningObject as Array).Length > 1 &&
					   nodePos > 0;
			}
			return false;
		}

		/// <summary>
		/// See if an object can be moved downin the current list.
		/// </summary>
		private bool MoveObjectDownFromHere(IInspectorObject node)
		{
			var nodePos = (node.DisplayName.Contains("[") ? int.Parse(node.DisplayName.Substring(1, node.DisplayName.IndexOf("]") - 1)) : 0);
			if (node.ParentInspectorObject != null)
			{
				return (node.ParentInspectorObject.DisplayName.EndsWith("OS") || node.ParentInspectorObject.DisplayName.EndsWith("RS")) &&
					   (node.OwningObject as Array) != null && (node.OwningObject as Array).Length > 1 &&
					   nodePos < (node.OwningObject as Array).Length - 1;
			}
			return false;
		}

		/// <summary>
		/// Opens an inspector window for the LCM model.
		/// </summary>
		private void OpenModelWindow()
		{
			if (m_modelWnd == null)
			{
				m_modelWnd = new ModelWnd(m_statuslabel);
				m_modelWnd.FormClosed += HandleWindowClosed;
			}

			m_modelWnd.Show(m_dockPanel);
		}

		/// <summary>
		/// Handles the Click event of the cmnuDeleteObject control.
		/// </summary>
		private void CmnuDeleteObjectClick(object sender, EventArgs e)
		{
			var mWnd = m_dockPanel.ActiveContent as InspectorWnd;

			var io = mWnd?.CurrentInspectorObject;
			if (!(io?.Object is ICmObject))
			{
				return;
			}

			var objToDelete = (ICmObject)io.Object;
			NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
				m_cache.DomainDataByFlid.DeleteObj(objToDelete.Hvo));

			//Refresh the display
			foreach (var dc in m_dockPanel.DocumentsToArray())
			{
				(dc as InspectorWnd)?.RefreshView("Delete");
			}
		}

		/// <summary>
		/// Handles the Click event of the cmnuMoveObjectUp control.
		/// </summary>
		private void CmnuMoveObjectUpClick(object sender, EventArgs e)
		{
			int inx;

			var mWnd = m_dockPanel.ActiveContent as InspectorWnd;

			var io = mWnd?.CurrentInspectorObject;
			if (io == null)
			{
				return;
			}

			if (!io.ParentInspectorObject.DisplayName.EndsWith("OS") && !io.ParentInspectorObject.DisplayName.EndsWith("RS"))
			{
				return;
			}

			var objToMove = io.Object as ICmObject ?? io.OriginalObject as ICmObject;
			if (objToMove == null) // we're on a field
			{
				MessageBox.Show("owner object couldn't be created.");
				return;
			}

			var owner = (io.ParentInspectorObject.Object as ICmObject ?? io.ParentInspectorObject.OriginalObject as ICmObject) ??
						io.ParentInspectorObject.OwningObject as ICmObject;
			if (owner == null) // we're on a field
			{
				MessageBox.Show("owner object couldn't be created.");
				return;
			}

			var work = StripOffTypeChars(io.ParentInspectorObject.DisplayName);
			var mdc = m_cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
			var flid = mdc.GetFieldId2(owner.ClassID, work, true);

			if (io.ParentInspectorObject.DisplayName.EndsWith("OS"))
				NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
				{
					m_cache.DomainDataByFlid.MoveOwn(owner.Hvo, flid, objToMove.Hvo, owner.Hvo, objToMove.OwningFlid, objToMove.OwnOrd - 1);
				});
			else //for reference objects, add the reference to the new location, then delete it.
			{

				// index of object to move
				inx = m_cache.DomainDataByFlid.GetObjIndex(owner.Hvo, flid, objToMove.Hvo);
				NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
					m_cache.DomainDataByFlid.Replace(owner.Hvo, flid, inx - 1, inx - 1, new[] { objToMove.Hvo }, 1)
				);

				NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
					m_cache.DomainDataByFlid.Replace(owner.Hvo, flid, inx + 1, inx + 2, null, 0)
				);
			}


			//Refresh the display
			foreach (var dc in m_dockPanel.DocumentsToArray().OfType<InspectorWnd>())
			{
				(dc).RefreshView("Up");
			}
		}

		/// <summary>
		/// Handles the Click event of the cmnuMoveObjectDown control.
		/// </summary>
		private void CmnuMoveObjectDownClick(object sender, EventArgs e)
		{
			int inx;
			int inx2;
			string work;

			var mWnd = m_dockPanel.ActiveContent as InspectorWnd;
			var io = mWnd?.CurrentInspectorObject;
			if (io == null)
			{
				return;
			}

			var objToMove = io.Object as ICmObject ?? io.OriginalObject as ICmObject;
			if (objToMove == null) // we're on a field
			{
				MessageBox.Show("object to move couldn't be created.");
				return;
			}

			var owner = (io.ParentInspectorObject.Object as ICmObject ?? io.ParentInspectorObject.OriginalObject as ICmObject) ??
						io.ParentInspectorObject.OwningObject as ICmObject;
			if (owner == null) // we're on a field
			{
				MessageBox.Show("owner object couldn't be created.");
				return;
			}

			if (!io.ParentInspectorObject.DisplayName.EndsWith("OS") && !io.ParentInspectorObject.DisplayName.EndsWith("RS"))
			{
				return;
			}

			if (io.ParentInspectorObject.DisplayName.EndsWith("OS") || io.ParentInspectorObject.DisplayName.EndsWith("RS"))
			{
				work = io.ParentInspectorObject.DisplayName.Substring(0, io.ParentInspectorObject.DisplayName.Length - 2);
			}
			else
			{
				work = io.DisplayName;
			}

			var mdc = m_cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
			var flid = mdc.GetFieldId2(owner.ClassID, work, true);

			if (io.ParentInspectorObject.DisplayName.EndsWith("OS"))
				NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
				{
					m_cache.DomainDataByFlid.MoveOwn(owner.Hvo, flid, objToMove.Hvo, owner.Hvo, objToMove.OwningFlid, objToMove.OwnOrd + 2);
				});
			else //for reference objects, add the reference to the new location, then delete it.
			{
				// index of object to move
				inx = m_cache.DomainDataByFlid.GetObjIndex(owner.Hvo, flid, objToMove.Hvo);
				var cnt = m_cache.DomainDataByFlid.get_VecSize(owner.Hvo, flid);
				inx2 = Math.Min(cnt, inx + 2);


				NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
					m_cache.DomainDataByFlid.Replace(owner.Hvo, flid, inx2,
					   inx2, new[] { objToMove.Hvo }, 1)
				);

				NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
					m_cache.DomainDataByFlid.Replace(owner.Hvo, flid, inx, inx + 1, null, 0)
				);
			}
			//Refresh the display
			foreach (var dc in m_dockPanel.DocumentsToArray())
			{
				(dc as InspectorWnd)?.RefreshView("Down");
			}
		}

		/// <summary>
		/// Handles the Click event of the cmnuSelectProps control.
		/// </summary>
		private void CmnuSelectPropsClick(object sender, EventArgs e)
		{
			var wnd = m_dockPanel.ActiveContent as InspectorWnd;
			var io = wnd?.CurrentInspectorObject;
			if (!(io?.Object is ICmObject))
			{
				return;
			}

			ShowPropertySelectorDialog(io.Object as ICmObject);
		}

		/// <summary>
		/// Shows the property selector dialog.
		/// </summary>
		private void ShowPropertySelectorDialog(ICmObject cmObj)
		{
			using (var dlg = new ClassPropertySelector(cmObj))
			{
				if (dlg.ShowDialog(this) != DialogResult.OK)
				{
					return;
				}

				foreach (var dc in m_dockPanel.DocumentsToArray().OfType<InspectorWnd>())
				{
					(dc).RefreshView();
				}
			}
		}

		/// <summary>
		/// Handles the Click event of the m_tsbShowCmObjectProps control.
		/// </summary>
		private void MTsbShowCmObjectPropsClick(object sender, EventArgs e)
		{
			m_tsbShowCmObjectProps.Checked = !m_tsbShowCmObjectProps.Checked;
			LCMClassList.ShowCmObjectProperties = m_tsbShowCmObjectProps.Checked;

			foreach (var dc in m_dockPanel.DocumentsToArray())
			{
				(dc as InspectorWnd)?.RefreshView();
			}
		}

		/// <summary>
		/// Handles the KeyPress event of the tstxtGuidSrch control.
		/// </summary>
		private void TstxtGuidSrchKeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar != (char)Keys.Enter)
			{
				return;
			}

			if (m_cache == null)
			{
				return;
			}

			var guid = new Guid(m_tstxtGuidSrch.Text.Trim());
			ICmObject obj;
			if (m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().TryGetObject(guid, out obj))
			{
				var wnd = ShowNewInspectorWindow(obj, obj.ToString());
				wnd.ToolTipText = wnd.Text + Environment.NewLine + "Guid: " + m_tstxtGuidSrch.Text.Trim();
			}

			e.KeyChar = (char)0;
			e.Handled = true;
		}

		/// <summary>
		/// Save to the database; does NOT force clearing undo stack.
		/// </summary>
		private void MnuSaveFileLcmClick(object sender, EventArgs e)
		{
			m_cache?.ServiceLocator.GetInstance<IUndoStackManager>().Save();
		}

		/// <summary>
		/// Handles the Click event of the mnuToolsAllowEdit control.
		/// </summary>
		private void MnuToolsAllowEditClick(object sender, EventArgs e)
		{
			if (mnuToolsAllowEdit.Checked)
			{
				mnuToolsAllowEdit.Checked = false;
				m_updateFlag = false;
			}
			else
			{
				mnuToolsAllowEdit.Checked = true;
				m_updateFlag = true;
			}
		}

		/// <summary>
		/// Handles the DropDownOpening event of the viewToolStripMenuItem control.
		/// </summary>
		private void MnuViewDropDownOpening(object sender, EventArgs e)
		{
			mnuViewLangProject.Enabled = m_lp != null;
			mnuViewRepositories.Enabled = m_cache != null;
		}

		/// <summary>
		/// Handles the Click event of the MnuViewLcmModelClick control.
		/// </summary>
		private void MnuViewLcmModelClick(object sender, EventArgs e)
		{
			OpenModelWindow();
		}

		/// <summary>
		/// Handles the Click event of the mnuViewLangProject control.
		/// </summary>
		private void MnuViewLangProjectClick(object sender, EventArgs e)
		{
			OpenLangProjWindow();
		}

		/// <summary>
		/// Handles the Click event of the mnuViewRepositories control.
		/// </summary>
		private void MnuViewRepositoriesClick(object sender, EventArgs e)
		{
			OpenRepositoryWindow();
		}

		/// <summary>
		/// Handles the Click event of the mnuDisplayVirtual control.
		/// </summary>
		private void MnuDisplayVirtualClick(object sender, EventArgs e)
		{
			if (mnuDisplayVirtual.Checked)
			{
				mnuDisplayVirtual.Checked = false;
				m_virtualFlag = false;
			}
			else
			{
				mnuDisplayVirtual.Checked = true;
				m_virtualFlag = true;
			}

			//Refresh the display
			foreach (var dc in m_dockPanel.DocumentsToArray())
			{
				(dc as InspectorWnd)?.RefreshView();
			}

			m_modelWnd.AfterSelectMethod();
		}

		/// <summary>
		/// Handles the Click event of the mnuClassProperties control.
		/// </summary>
		private void MnuClassPropertiesClick(object sender, EventArgs e)
		{
			ShowPropertySelectorDialog(null);
		}

		/// <summary>
		/// Opens an inspector window with the language project as the top level object.
		/// </summary>
		private void OpenLangProjWindow()
		{
			if (m_langProjWnd != null)
			{
				m_langProjWnd.Show(m_dockPanel);
			}
			else
			{
				m_langProjWnd = ShowNewInspectorWindow(m_lp, m_lp.ShortName + ": LangProj", null);
			}
		}

		/// <summary>
		/// Opens an inspector window with a collection of the language project's
		/// repositories as the top level object.
		/// </summary>
		private void OpenRepositoryWindow()
		{
			if (m_repositoryWnd != null)
			{
				m_repositoryWnd.Show(m_dockPanel);
			}
			else
			{
				var repositories = LCMClassList.RepositoryTypes.Select(repoType => m_cache.ServiceLocator.GetInstance(repoType)).ToList();

				// Go through all the service types and find those that are repositories. For each
				// repository type, get its instance from the service locator and store that in a list.
				m_repositoryWnd = ShowNewInspectorWindow(repositories, m_cache.ProjectId.UiName + ": Repositories", null);
			}
		}
		/// <summary>
		/// Gets the BEP type from the specified file path.
		/// </summary>
		private static BackendProviderType GetBEPTypeFromFileExtension(string pathname)
		{
			switch (Path.GetExtension(pathname).ToLower())
			{
				default:
					return BackendProviderType.kMemoryOnly;
				case LcmFileHelper.ksFwDataXmlFileExtension:
					return BackendProviderType.kXML;

			}
		}

		/// <summary>
		/// Gets the custom fields defined in the project file.
		/// </summary>
		private static List<CustomFields> GetCustomFields(LcmCache cache)
		{
			var type = string.Empty;
			var list = new List<CustomFields>();
			foreach (var fd in FieldDescription.FieldDescriptors(cache))
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
							type = "SIL.LCModel.Core.Cellar.GenDate";
							break;
						case "ReferenceAtomic":
							type = "ICmPossibility";
							break;
						case "ReferenceCollection":
							type = "LcmReferenceCollection<ICmPossibility>";
							break;
						case "MinObj":
							type = "IStText";
							break;

						case "MultiUnicode":
							// This is a guess
							type = "WfiWordForm";
							break;
						default:
							MessageBox.Show($"In 'GetCustomFields, Type was unknown: {fd.Type.ToString()} ");
							break;
					}

					var cf = new CustomFields(fd.Name, fd.Class, fd.Id, type);
					list.Add(cf);
				}
			}
			return (list.Count > 0 ? list : null);
		}

		/// <summary>
		/// Handles the LangProj CellValueChanged event of the DataGrid control in the Inspector object.
		/// </summary>
		void LangProjInspectorGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
		{
			UpdateLcmProp(sender, m_langProjWnd);
		}

		/// <summary>
		/// UpdateLcmProp - update LCM with changed values in the repository and LangProject windows.
		/// /// </summary>
		private void UpdateLcmProp(object sender, InspectorWnd mWnd)
		{
			var sendObj = (InspectorGrid)sender;
			if (mWnd.CurrentInspectorObject.DisplayValue == sendObj.EditingControl.Text)
			{
				return;     // nothing changed so return
			}
			var dType = mWnd.CurrentInspectorObject.DisplayType;

			if (dType != "System.String" && dType != "System.Int32" && dType != "System.Boolean" &&
				dType != "System.DateTime" && dType != "SIL.FieldWorks.Common.FwUtils.GenDate.PrecisionType")
			{
				return;
			}

			PropertyInfo pi;
			ICmObject obj;
			int hvo;
			if (!BuildLcmPINeeded(mWnd.CurrentInspectorObject, out pi, out obj, out hvo))
			{
				throw new ApplicationException("The necessary Info to update the LCM wasn't obtained.");
			}

			var intNewVal = 0;
			var dtNewVal = new DateTime();
			var guidNewVal = new Guid();
			var genDate = new GenDate();
			GenDate genDate1;
			string strNewVal;

			var node = mWnd.CurrentInspectorObject;
			switch (dType)
			{
				case "System.Int32":
					try
					{
						intNewVal = int.Parse(sendObj.EditingControl.Text);
						node.DisplayValue = sendObj.EditingControl.Text;
					}
					catch
					{
						MessageBox.Show($"Value entered is not a valid integer: {sendObj.EditingControl.Text}");
						node.DisplayValue = node.DisplayValue;
						return;
					}

					if (node.ParentInspectorObject.DisplayType.IndexOf("GenDate") > 0)
					{
						if (ValidateGenDate(node, node.ParentInspectorObject, obj, pi, intNewVal, out genDate))
						{
							UpdateValues(node.ParentInspectorObject, obj, pi, "GenDate", string.Empty, intNewVal, genDate, dtNewVal, guidNewVal, false);
							node.ParentInspectorObject.DisplayValue = genDate.ToLongString();
							return;
						}
						MessageBox.Show("GenDate Format (Update Integer) is invalid.");
						return;
					}

					UpdateValues(node, obj, pi, "Integer", "", intNewVal, genDate, dtNewVal, guidNewVal, false);
					break;
				case "System.Guid":
					node.DisplayValue = sendObj.EditingControl.Text;
					guidNewVal = new Guid(sendObj.EditingControl.Text);
					UpdateValues(node, obj, pi, "Guid", string.Empty, 0, genDate, dtNewVal, guidNewVal, false);
					break;
				case "System.Boolean":
					bool boolNewVal;
					try
					{
						boolNewVal = bool.Parse(sendObj.EditingControl.Text);
						node.DisplayValue = sendObj.EditingControl.Text;
					}
					catch
					{
						MessageBox.Show($"Value entered is not a valid boolean: {sendObj.EditingControl.Text}");
						node.DisplayValue = node.DisplayValue;
						return;
					}
					if (node.ParentInspectorObject.DisplayType.IndexOf("GenDate") > 0)
					{
						if (node.ParentInspectorObject != null && node.ParentInspectorObject.Flid > 0)
						{
							genDate1 = m_silDataAccessManaged.get_GenDateProp(obj.Hvo, node.ParentInspectorObject.Flid);
						}
						else
						{
							genDate1 = (GenDate)pi.GetValue(obj, null);
						}
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
								MessageBox.Show($"GenDate boolean not IsAd or IsEmpty: {node.DisplayName}");
								return;
						}

						node.ParentInspectorObject.DisplayValue = genDate.ToLongString();
						UpdateValues(node.ParentInspectorObject, obj, pi, "GenDate", string.Empty, intNewVal, genDate, dtNewVal, guidNewVal, boolNewVal);
						break;
					}

					UpdateValues(node, obj, pi, "Boolean", "", 0, genDate, dtNewVal, guidNewVal, boolNewVal);

					break;
				case "System.String":
					for (var l = node.Level; l > 0; l--)
					{
						if (ParentIsType(ref node, "TsString"))
						{
							MessageBox.Show("This type of string cannot be editted because it is under a TsString");
							return;
						}
					}
					node = mWnd.CurrentInspectorObject;
					mWnd.CurrentInspectorObject.DisplayValue = sendObj.EditingControl.Text;
					strNewVal = sendObj.EditingControl.Text;
					UpdateValues(node, obj, pi, "String", strNewVal, 0, genDate, dtNewVal, guidNewVal, false);
					break;
				case "System.DateTime":
					if (DateTime.TryParse(sendObj.EditingControl.Text, out dtNewVal))
					{
						node.DisplayValue = sendObj.EditingControl.Text;
						UpdateValues(node, obj, pi, "DateTime", string.Empty, 0, genDate, dtNewVal, guidNewVal, false);

						break;
					}
					MessageBox.Show("Date Format is invalid.");
					node.DisplayValue = node.DisplayValue;
					return;
				case "SIL.FieldWorks.Common.FwUtils.GenDate.PrecisionType":
					strNewVal = sendObj.EditingControl.Text;
					GenDate.PrecisionType newPreType;
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

					if (node.ParentInspectorObject != null && node.ParentInspectorObject.Flid > 0)
					{
						genDate1 = m_silDataAccessManaged.get_GenDateProp(obj.Hvo, node.ParentInspectorObject.Flid);
					}
					else
					{
						genDate1 = (GenDate)pi.GetValue(obj, null);
					}
					node.DisplayValue = strNewVal;
					try
					{
						genDate = new GenDate(newPreType, genDate1.Month, genDate1.Day, genDate1.Year, genDate1.IsAD);
					}
					catch
					{
						MessageBox.Show("Gendate with new Precision is invalid.");
						return;
					}
					node.ParentInspectorObject.DisplayValue = genDate.ToLongString();
					UpdateValues(node.ParentInspectorObject, obj, pi, "GenDate", string.Empty, 0, genDate, dtNewVal, guidNewVal, false);
					break;
				default:
					throw new ApplicationException("Illegal type");
			}
		}

		/// <summary>
		/// Updates the object.  Custom fields update the cache, others the PropertyInfo.
		/// </summary>
		private void UpdateValues(IInspectorObject node, ICmObject obj, PropertyInfo pi, string operation, string strVal, int intVal, GenDate genVal, DateTime dtVal, Guid guidVal, bool boolVal)
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
							{
								pi.SetValue(obj, strVal, null);
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
							m_silDataAccessManaged.SetGenDate(obj.Hvo, node.Flid, genVal);
						}
						else if (pi == null && node != null)
						{
							if (GetPI(node, StripOffTypeChars(node.DisplayName), obj, out pi))
							{
								pi.SetValue(obj, genVal, null);
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
							m_silDataAccessManaged.SetDateTime(obj.Hvo, node.Flid, dtVal);
						}
						else if (pi == null && node != null)
						{
							if (GetPI(node, StripOffTypeChars(node.DisplayName), obj, out pi))
							{
								pi.SetValue(obj, dtVal, null);
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
							{
								pi.SetValue(obj, boolVal, null);
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
						   {
							   pi.SetValue(obj, intVal, null);
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
						   {
							   pi.SetValue(obj, guidVal, null);
						   }
					   }
					   else
					   {
						   pi.SetValue(obj, guidVal, null);
					   }
				   });
					break;
				default:
					MessageBox.Show($"Operation passed to UpdateValues is invalid: {operation}");
					break;
			}
		}

		private static bool GetPI(IInspectorObject node, string fieldName, ICmObject obj, out PropertyInfo pi)
		{
			var type = obj.GetType();

			if (!(node != null && node.Flid > 0))  //custom properties don't have PropertyInfo
			{
				pi = type.GetProperty(fieldName,
					BindingFlags.Instance |
					BindingFlags.Public |
					BindingFlags.FlattenHierarchy);
				if (pi == null)
				{
					MessageBox.Show("The PI Object to get property info from is null.");
					return false;
				}
			}
			else
			{
				pi = null;
			}
			return true;
		}

		/// <summary>
		/// Gets the property info if it's available.
		/// </summary>
		private bool BuildLcmPINeeded(IInspectorObject node, out PropertyInfo pi, out ICmObject obj, out int hvo)
		{
			var fieldName = string.Empty;
			hvo = 0;

			for (var l = node.Level; l > 0; l--)
			{
				if (GetHvoNode(ref node, ref hvo, ref fieldName))
				{
					break;
				}
				if (l == 0)
				{
					MessageBox.Show($"The hvo could not be created for {node.DisplayName}");
					pi = null; obj = null; hvo = 0;
					return false;
				}
			}

			obj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
			if (obj == null)
			{
				MessageBox.Show("The LCM Object to update is null.");
				pi = null;
				return false;
			}

			return GetPI(node, fieldName, obj, out pi);
		}

		/// <summary>
		/// Gets the hvo node and column name if it's available at this level
		/// </summary>
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

		/// <summary>
		/// Checks if the parent of the current node is of a certain type
		/// </summary>
		private bool ParentIsType(ref IInspectorObject node, string type)
		{
			if (node.DisplayType.IndexOf(type) > 0)
			{
				return true;
			}
			node = node.ParentInspectorObject;
			return false;
		}

		/// <summary>
		/// Handles determining whether or not the specified object will disappear if the
		/// view it's in is refresh. For our purposes here, we assume that properties that
		/// are part of the CmObject base class will disappear (unless the property is a guid).
		/// </summary>
		static bool HandleWillObjDisappearOnRefresh(object sender, IInspectorObject io)
		{
			// Check if the selected object will disappear after refreshing the grid.
			return (io.DisplayName != "Guid" && LCMClassList.IsCmObjectProperty(io.DisplayName));
		}

		/// <summary>
		/// Verify that, with the changes made, the gendate is still valid.
		/// </summary>
		private bool ValidateGenDate(IInspectorObject io, IInspectorObject ioParent, ICmObject obj, PropertyInfo pi, int mdy, out GenDate genDate)
		{
			DateTime dt1, dt;
			GenDate genDate1;
			if (io.ParentInspectorObject != null && io.ParentInspectorObject.Flid > 0)
			{
				genDate1 = m_silDataAccessManaged.get_GenDateProp(obj.Hvo, io.ParentInspectorObject.Flid);
			}
			else
			{
				genDate1 = (GenDate)pi.GetValue(obj, null);
			}

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
					MessageBox.Show($"GenDate Integer passed is not Month, Day or Year: {io.DisplayName}");
					genDate = genDate1;
					return false;
			}
		}

		/// <summary>
		/// Looks through the grid display and update the display integers of the current GenDate to 0.
		/// </summary>
		private void ResetDisplayDates(IInspectorObject node, InspectorWnd mWnd, string type, string value, int hvo)
		{
			var genDateFlag = false;

			for (var i = 0; 1 < mWnd.InspectorList.Count; i++)
			{
				if (genDateFlag)
				{
					if (mWnd.InspectorList[i].DisplayName == "Day")
					{
						mWnd.InspectorList[i].DisplayValue = value;
					}
					else if (mWnd.InspectorList[i].DisplayName == "Month")
					{
						mWnd.InspectorList[i].DisplayValue = value;
						if (type == "IsAD")
						{
							break;
						}
					}
					else if (mWnd.InspectorList[i].DisplayName == "Year")
					{
						mWnd.InspectorList[i].DisplayValue = value;
						break;
					}
				}

				if (mWnd.InspectorList[i].DisplayName == node.ParentInspectorObject.DisplayName &&
					hvo == ((ICmObject)node.ParentInspectorObject.OwningObject).Hvo)
				{
					genDateFlag = true;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Repository CellValueChanged event of the DataGrid control in the Inspector object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void RepositoryInspectorGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
		{
			UpdateLcmProp(sender, m_repositoryWnd);
		}
	}
}