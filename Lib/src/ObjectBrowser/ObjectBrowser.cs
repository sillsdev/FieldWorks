using System;
using System.Collections.Generic;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using System.Drawing;
using System.ComponentModel;

namespace SIL.ObjectBrowser
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// FDOBrowserForm Class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class ObjectBrowser : Form
	{
		#region Data members

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
		protected string m_appCaption;
		/// <summary></summary>
		protected string m_currOpenedProject;
		/// <summary></summary>
		protected readonly DockPanel m_dockPanel;
		/// <summary></summary>
		protected List<string> m_ruFiles;

		private InspectorWnd m_InspectorWnd;

		#endregion Data members

		#region Construction
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ObjectBrowser()
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
			m_dockPanel.BringToFront();

			m_ruFiles = new List<string>();
			for (int i = 1; i <= 9; i++)
				m_ruFiles.Add(Properties.Settings.Default["RUFile" + i] as string);

			BuildRecentlyUsedFilesMenus();
		}

		#endregion Construction

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Form.Load"/> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnLoad(EventArgs e)
		{
			Point pt = Properties.Settings.Default.MainWndLocation;
			if (pt != Point.Empty)
				Location = pt;

			Size sz = Properties.Settings.Default.MainWndSize;
			if (!sz.IsEmpty)
				Size = sz;

			base.OnLoad(e);
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
				Properties.Settings.Default["RUFile" + i++] = file;

			Properties.Settings.Default.MainWndLocation = Location;
			Properties.Settings.Default.MainWndSize = Size;
			Properties.Settings.Default.Save();
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
				if (!string.IsNullOrEmpty(file))
				{
					mnuFileSep2.Visible = true;
					ToolStripMenuItem mnu = new ToolStripMenuItem();
					mnu.Name = "RUF" + i;
					mnu.Text = string.Format("&{0} {1}", i + 1, file);
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
		protected virtual void OpenFile(string fileName)
		{
			PutFileAtTopOfRUFileList(fileName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes the application's caption.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void MakeAppCaption(string text)
		{
			Text = string.Format(kAppCaptionFmt, text, m_appCaption);
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
			m_statuslabel.Text = (wnd == null ? string.Empty : wnd.InspectorList.Count + " Top Level Items");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the ContentAdded event of the m_dockPanel control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void DockPanelContentAdded(object sender, DockContentEventArgs e)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the ContentRemoved event of the m_dockPanel control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void DockPanelContentRemoved(object sender, DockContentEventArgs e)
		{
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
		protected virtual string OpenFileDlgTitle
		{
			get { return "File Open"; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the file filter for the open file dialog box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual string OpenFileDlgFilter
		{
			get { return "All Files|*.*"; }
		}

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
			ToolStripMenuItem mnu = sender as ToolStripMenuItem;
			if (mnu != null && mnu.Tag is DockContent)
				((DockContent)mnu.Tag).Show(m_dockPanel);
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
			InspectorWnd wnd = ShowNewInspectorWndOne();

			return ShowNewInspectorWndTwo(obj, text, toolTipText, wnd);
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
			wnd.Text = (!string.IsNullOrEmpty(text) ? text : GetNewInspectorWndTitle(obj));
			wnd.ToolTipText = (string.IsNullOrEmpty(toolTipText) ? wnd.Text : toolTipText);
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
		protected virtual string GetNewInspectorWndTitle(object obj)
		{
			if (obj == null)
				return string.Empty;

			string objString = (obj.ToString() ?? string.Empty);
			string title = objString;
			string typeName = obj.GetType().Name;
			if (!objString.StartsWith(typeName))
			{
				title = typeName;
				if (objString != string.Empty)
					title += (": " + objString);
			}

			return title;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the new inspector list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual IInspectorList GetNewInspectorList()
		{
			return new GenericInspectorObjectList();
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
		protected virtual void HandleWindowClosed(object sender, FormClosedEventArgs e)
		{
			if (sender is DockContent)
				((DockContent)sender).FormClosed -= HandleWindowClosed;

			if (sender is InspectorWnd)
			{
				((InspectorWnd)sender).InspectorGrid.Enter -= InspectorGrid_Enter;
				((InspectorWnd)sender).InspectorGrid.Leave -= InspectorGrid_Leave;
			}

			tsbShowObjInNewWnd.Enabled = false;
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
				if (int.TryParse(text, out i))
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
		protected virtual void HandleOpenObjectInNewWindowContextMenuClick(object sender, CancelEventArgs e)
		{
			InspectorWnd wnd = m_dockPanel.ActiveContent as InspectorWnd;
			if (wnd != null)
			{
				IInspectorObject io = wnd.CurrentInspectorObject;
				cmnuShowInNewWindow.Enabled = (io != null && io.Object != null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the mnuOptions control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void mnuOptions_Click(object sender, EventArgs e)
		{
			using (OptionsDlg dlg = new OptionsDlg(Properties.Settings.Default.ShadeColor))
			{
				dlg.ShadingEnabled = Properties.Settings.Default.UseShading;
				dlg.SelectedColor = Properties.Settings.Default.ShadeColor;

				if (dlg.ShowDialog(this) != DialogResult.OK)
					return;

				Properties.Settings.Default.UseShading = dlg.ShadingEnabled;
				Color clrNew = Color.Empty;

				if (dlg.ShadingEnabled)
				{
					clrNew = dlg.SelectedColor;
					Properties.Settings.Default.ShadeColor = clrNew;
				}

				foreach (IDockContent dc in m_dockPanel.DocumentsToArray())
				{
					if (dc is InspectorWnd)
						((InspectorWnd)dc).InspectorGrid.ShadingColor = clrNew;
				}
			}
		}
	}
}