using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using LCMBrowser.Properties;
using SIL.LCModel.Core.KernelInterfaces;

namespace LCMBrowser
{
	/// <summary>
	/// Class doc.
	/// </summary>
	partial class LCMBrowserForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				components?.Dispose();
				m_InspectorWnd?.Dispose();
				if (File.Exists(FileName + ".lock"))
				{
					m_cache.ServiceLocator.GetInstance<IActionHandler>().Commit();
					m_cache.Dispose();
				}
			}
			m_InspectorWnd = null;
			m_cache = null;

			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.m_statusStrip = new System.Windows.Forms.StatusStrip();
			this.m_statuslabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_sblblLoadTime = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_mainMenu = new System.Windows.Forms.MenuStrip();
			this.mnuFile = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuOpenFile = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuSaveFileLCM = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuFileSep1 = new System.Windows.Forms.ToolStripSeparator();
			this.mnuExit = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuView = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuViewLcmModel = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuViewLangProject = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuViewRepositories = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripSeparator();
			this.mnuDisplayVirtual = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuClassProperties = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuTools = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuOptions = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuToolsAllowEdit = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuWindow = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuTileVertically = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuTileHorizontally = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuArrangeInline = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuWindowsSep = new System.Windows.Forms.ToolStripSeparator();
			this.m_navigationToolStrip = new System.Windows.Forms.ToolStrip();
			this.tsbOpen = new System.Windows.Forms.ToolStripButton();
			this.tsSep1 = new System.Windows.Forms.ToolStripSeparator();
			this.tsbShowObjInNewWnd = new System.Windows.Forms.ToolStripButton();
			this.m_tsbShowCmObjectProps = new System.Windows.Forms.ToolStripButton();
			this.tstxtGuidSrch = new System.Windows.Forms.ToolStripTextBox();
			this.tslblGuidSrch = new System.Windows.Forms.ToolStripLabel();
			this.m_cmnuGrid = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.cmnuShowInNewWindow = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
			this.cmnuAddObject = new System.Windows.Forms.ToolStripMenuItem();
			this.cmnuDeleteObject = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
			this.cmnuMoveObjectUp = new System.Windows.Forms.ToolStripMenuItem();
			this.cmnuMoveObjectDown = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
			this.cmnuSelectProps = new System.Windows.Forms.ToolStripMenuItem();
			this.m_statusStrip.SuspendLayout();
			this.m_mainMenu.SuspendLayout();
			this.m_navigationToolStrip.SuspendLayout();
			this.m_cmnuGrid.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_statusStrip
			// 
			this.m_statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_statuslabel,
            this.m_sblblLoadTime});
			this.m_statusStrip.Location = new System.Drawing.Point(0, 545);
			this.m_statusStrip.Name = "m_statusStrip";
			this.m_statusStrip.Size = new System.Drawing.Size(872, 22);
			this.m_statusStrip.TabIndex = 0;
			// 
			// m_statuslabel
			// 
			this.m_statuslabel.Name = "m_statuslabel";
			this.m_statuslabel.Size = new System.Drawing.Size(843, 17);
			this.m_statuslabel.Spring = true;
			this.m_statuslabel.Text = "Some Object";
			this.m_statuslabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// m_sblblLoadTime
			// 
			this.m_sblblLoadTime.AccessibleRole = System.Windows.Forms.AccessibleRole.ScrollBar;
			this.m_sblblLoadTime.Name = "m_sblblLoadTime";
			this.m_sblblLoadTime.Size = new System.Drawing.Size(14, 17);
			this.m_sblblLoadTime.Text = "#";
			// 
			// m_mainMenu
			// 
			this.m_mainMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuFile,
            this.mnuView,
            this.mnuTools,
            this.mnuWindow});
			this.m_mainMenu.Location = new System.Drawing.Point(0, 0);
			this.m_mainMenu.Name = "m_mainMenu";
			this.m_mainMenu.Size = new System.Drawing.Size(872, 24);
			this.m_mainMenu.TabIndex = 0;
			this.m_mainMenu.Text = "menuStrip1";
			// 
			// mnuFile
			// 
			this.mnuFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuOpenFile,
            this.mnuSaveFileLCM,
            this.mnuFileSep1,
            this.mnuExit});
			this.mnuFile.Name = "mnuFile";
			this.mnuFile.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.mnuFile.Size = new System.Drawing.Size(37, 20);
			this.mnuFile.Text = "&File";
			this.mnuFile.ToolTipText = "Open FieldWorks Language Project";
			// 
			// mnuOpenFile
			// 
			this.mnuOpenFile.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
			this.mnuOpenFile.Name = "mnuOpenFile";
			this.mnuOpenFile.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.mnuOpenFile.Size = new System.Drawing.Size(276, 22);
			this.mnuOpenFile.Text = "&Open Language Project...";
			this.mnuOpenFile.Click += new System.EventHandler(this.HandleFileOpenClick);
			// 
			// mnuSaveFileLCM
			// 
			this.mnuSaveFileLCM.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
			this.mnuSaveFileLCM.Name = "mnuSaveFileLCM";
			this.mnuSaveFileLCM.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
			this.mnuSaveFileLCM.Size = new System.Drawing.Size(276, 22);
			this.mnuSaveFileLCM.Text = "&Save Current Language Project";
			this.mnuSaveFileLCM.Click += new System.EventHandler(this.MnuSaveFileLcmClick);
			// 
			// mnuFileSep1
			// 
			this.mnuFileSep1.Name = "mnuFileSep1";
			this.mnuFileSep1.Size = new System.Drawing.Size(273, 6);
			this.mnuFileSep1.Visible = false;
			// 
			// mnuExit
			// 
			this.mnuExit.Name = "mnuExit";
			this.mnuExit.Size = new System.Drawing.Size(276, 22);
			this.mnuExit.Text = "E&xit";
			this.mnuExit.Click += new System.EventHandler(this.mnuExit_Click);
			// 
			// mnuView
			// 
			this.mnuView.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuViewLcmModel,
            this.mnuViewLangProject,
            this.mnuViewRepositories,
            this.toolStripMenuItem4,
            this.mnuDisplayVirtual,
            this.mnuClassProperties});
			this.mnuView.Name = "mnuView";
			this.mnuView.Size = new System.Drawing.Size(44, 20);
			this.mnuView.Text = "&View";
			this.mnuView.DropDownOpening += new System.EventHandler(this.MnuViewDropDownOpening);
			// 
			// mnuViewLcmModel
			// 
			this.mnuViewLcmModel.Image = global::LCMBrowser.Properties.Resources.kimidLCMModel;
			this.mnuViewLcmModel.Name = "mnuViewLcmModel";
			this.mnuViewLcmModel.Size = new System.Drawing.Size(233, 22);
			this.mnuViewLcmModel.Text = "&LCM Model";
			this.mnuViewLcmModel.Click += new System.EventHandler(this.MnuViewLcmModelClick);
			// 
			// mnuViewLangProject
			// 
			this.mnuViewLangProject.Name = "mnuViewLangProject";
			this.mnuViewLangProject.Size = new System.Drawing.Size(233, 22);
			this.mnuViewLangProject.Text = "&Language Project";
			this.mnuViewLangProject.Click += new System.EventHandler(this.MnuViewLangProjectClick);
			// 
			// mnuViewRepositories
			// 
			this.mnuViewRepositories.Name = "mnuViewRepositories";
			this.mnuViewRepositories.Size = new System.Drawing.Size(233, 22);
			this.mnuViewRepositories.Text = "Language Project &Repositories";
			this.mnuViewRepositories.Click += new System.EventHandler(this.MnuViewRepositoriesClick);
			// 
			// toolStripMenuItem4
			// 
			this.toolStripMenuItem4.Name = "toolStripMenuItem4";
			this.toolStripMenuItem4.Size = new System.Drawing.Size(230, 6);
			// 
			// mnuDisplayVirtual
			// 
			this.mnuDisplayVirtual.Name = "mnuDisplayVirtual";
			this.mnuDisplayVirtual.Size = new System.Drawing.Size(233, 22);
			this.mnuDisplayVirtual.Text = "Display &Virtual Properties";
			this.mnuDisplayVirtual.Click += new System.EventHandler(this.MnuDisplayVirtualClick);
			// 
			// mnuClassProperties
			// 
			this.mnuClassProperties.Name = "mnuClassProperties";
			this.mnuClassProperties.Size = new System.Drawing.Size(233, 22);
			this.mnuClassProperties.Text = "Class &Properties...";
			this.mnuClassProperties.Click += new System.EventHandler(this.MnuClassPropertiesClick);
			// 
			// mnuTools
			// 
			this.mnuTools.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuOptions,
            this.mnuToolsAllowEdit});
			this.mnuTools.Name = "mnuTools";
			this.mnuTools.Size = new System.Drawing.Size(47, 20);
			this.mnuTools.Text = "&Tools";
			// 
			// mnuOptions
			// 
			this.mnuOptions.Name = "mnuOptions";
			this.mnuOptions.Size = new System.Drawing.Size(132, 22);
			this.mnuOptions.Text = "&Options...";
			this.mnuOptions.Click += new System.EventHandler(this.mnuOptions_Click);
			// 
			// mnuToolsAllowEdit
			// 
			this.mnuToolsAllowEdit.Checked = true;
			this.mnuToolsAllowEdit.CheckState = System.Windows.Forms.CheckState.Checked;
			this.mnuToolsAllowEdit.Name = "mnuToolsAllowEdit";
			this.mnuToolsAllowEdit.Size = new System.Drawing.Size(132, 22);
			this.mnuToolsAllowEdit.Text = "&Allow Edits";
			this.mnuToolsAllowEdit.Click += new System.EventHandler(this.MnuToolsAllowEditClick);
			// 
			// mnuWindow
			// 
			this.mnuWindow.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuTileVertically,
            this.mnuTileHorizontally,
            this.mnuArrangeInline,
            this.mnuWindowsSep});
			this.mnuWindow.Name = "mnuWindow";
			this.mnuWindow.Size = new System.Drawing.Size(63, 20);
			this.mnuWindow.Text = "&Window";
			this.mnuWindow.DropDownOpening += new System.EventHandler(this.mnuWindow_DropDownOpening);
			// 
			// mnuTileVertically
			// 
			this.mnuTileVertically.Image = global::LCMBrowser.Properties.Resources.kimidTileVertically;
			this.mnuTileVertically.Name = "mnuTileVertically";
			this.mnuTileVertically.Size = new System.Drawing.Size(160, 22);
			this.mnuTileVertically.Text = "Tile &Vertically";
			this.mnuTileVertically.Click += new System.EventHandler(this.mnuTileVertically_Click);
			// 
			// mnuTileHorizontally
			// 
			this.mnuTileHorizontally.Image = global::LCMBrowser.Properties.Resources.kimidTileHorizontally;
			this.mnuTileHorizontally.Name = "mnuTileHorizontally";
			this.mnuTileHorizontally.Size = new System.Drawing.Size(160, 22);
			this.mnuTileHorizontally.Text = "Tile &Horizontally";
			this.mnuTileHorizontally.Click += new System.EventHandler(this.mnuTileHorizontally_Click);
			// 
			// mnuArrangeInline
			// 
			this.mnuArrangeInline.Image = global::LCMBrowser.Properties.Resources.kimidArrangeInline;
			this.mnuArrangeInline.Name = "mnuArrangeInline";
			this.mnuArrangeInline.Size = new System.Drawing.Size(160, 22);
			this.mnuArrangeInline.Text = "Arrange &Inline";
			this.mnuArrangeInline.Click += new System.EventHandler(this.mnuArrangeInline_Click);
			// 
			// mnuWindowsSep
			// 
			this.mnuWindowsSep.Name = "mnuWindowsSep";
			this.mnuWindowsSep.Size = new System.Drawing.Size(157, 6);
			// 
			// m_navigationToolStrip
			// 
			this.m_navigationToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.m_navigationToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsbOpen,
            this.tsSep1,
            this.tsbShowObjInNewWnd,
            this.m_tsbShowCmObjectProps,
            this.tstxtGuidSrch,
            this.tslblGuidSrch});
			this.m_navigationToolStrip.Location = new System.Drawing.Point(0, 24);
			this.m_navigationToolStrip.Name = "m_navigationToolStrip";
			this.m_navigationToolStrip.Size = new System.Drawing.Size(872, 25);
			this.m_navigationToolStrip.TabIndex = 1;
			// 
			// tsbOpen
			// 
			this.tsbOpen.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.tsbOpen.Image = global::LCMBrowser.Properties.Resources.kimidOpenPRoject;
			this.tsbOpen.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tsbOpen.Name = "tsbOpen";
			this.tsbOpen.Size = new System.Drawing.Size(23, 22);
			this.tsbOpen.Text = "Open...";
			this.tsbOpen.ToolTipText = "Open Language Project";
			this.tsbOpen.Click += new System.EventHandler(this.HandleFileOpenClick);
			// 
			// tsSep1
			// 
			this.tsSep1.Name = "tsSep1";
			this.tsSep1.Size = new System.Drawing.Size(6, 25);
			// 
			// tsbShowObjInNewWnd
			// 
			this.tsbShowObjInNewWnd.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.tsbShowObjInNewWnd.Enabled = false;
			this.tsbShowObjInNewWnd.Image = global::LCMBrowser.Properties.Resources.kimidShowObjectInNewWindow;
			this.tsbShowObjInNewWnd.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tsbShowObjInNewWnd.Name = "tsbShowObjInNewWnd";
			this.tsbShowObjInNewWnd.Size = new System.Drawing.Size(23, 22);
			this.tsbShowObjInNewWnd.Text = "Show Object in New Window";
			this.tsbShowObjInNewWnd.Click += new System.EventHandler(this.m_tsbShowObjInNewWnd_Click);
			// 
			// m_tsbShowCmObjectProps
			// 
			this.m_tsbShowCmObjectProps.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_tsbShowCmObjectProps.Enabled = false;
			this.m_tsbShowCmObjectProps.Image = global::LCMBrowser.Properties.Resources.kimidShowCmObjectProperties;
			this.m_tsbShowCmObjectProps.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_tsbShowCmObjectProps.Name = "m_tsbShowCmObjectProps";
			this.m_tsbShowCmObjectProps.Size = new System.Drawing.Size(23, 22);
			this.m_tsbShowCmObjectProps.Text = "Show CmObject Properties";
			this.m_tsbShowCmObjectProps.Click += new System.EventHandler(this.MTsbShowCmObjectPropsClick);
			// 
			// tstxtGuidSrch
			// 
			this.tstxtGuidSrch.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.tstxtGuidSrch.Name = "tstxtGuidSrch";
			this.tstxtGuidSrch.Size = new System.Drawing.Size(250, 25);
			this.tstxtGuidSrch.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TstxtGuidSrchKeyPress);
			// 
			// tslblGuidSrch
			// 
			this.tslblGuidSrch.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.tslblGuidSrch.Name = "tslblGuidSrch";
			this.tslblGuidSrch.Size = new System.Drawing.Size(75, 22);
			this.tslblGuidSrch.Text = "GUID Search:";
			// 
			// m_cmnuGrid
			// 
			this.m_cmnuGrid.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.cmnuShowInNewWindow,
            this.toolStripMenuItem1,
            this.cmnuAddObject,
            this.cmnuDeleteObject,
            this.toolStripMenuItem2,
            this.cmnuMoveObjectUp,
            this.cmnuMoveObjectDown,
            this.toolStripMenuItem3,
            this.cmnuSelectProps});
			this.m_cmnuGrid.Name = "cmnuGrid";
			this.m_cmnuGrid.Size = new System.Drawing.Size(260, 154);
			this.m_cmnuGrid.Opening += new System.ComponentModel.CancelEventHandler(this.HandleOpenObjectInNewWindowContextMenuClick);
			// 
			// cmnuShowInNewWindow
			// 
			this.cmnuShowInNewWindow.Image = global::LCMBrowser.Properties.Resources.kimidShowObjectInNewWindow;
			this.cmnuShowInNewWindow.Name = "cmnuShowInNewWindow";
			this.cmnuShowInNewWindow.Size = new System.Drawing.Size(259, 22);
			this.cmnuShowInNewWindow.Text = "Show Object in New Window";
			this.cmnuShowInNewWindow.Click += new System.EventHandler(this.m_tsbShowObjInNewWnd_Click);
			// 
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new System.Drawing.Size(256, 6);
			// 
			// cmnuAddObject
			// 
			this.cmnuAddObject.Name = "cmnuAddObject";
			this.cmnuAddObject.Size = new System.Drawing.Size(259, 22);
			this.cmnuAddObject.Text = "Add New Object of type {0}...";
			this.cmnuAddObject.Click += new System.EventHandler(this.CmnuAddObjectClick);
			// 
			// cmnuDeleteObject
			// 
			this.cmnuDeleteObject.Name = "cmnuDeleteObject";
			this.cmnuDeleteObject.Size = new System.Drawing.Size(259, 22);
			this.cmnuDeleteObject.Text = "Delete the selected object.";
			this.cmnuDeleteObject.Click += new System.EventHandler(this.CmnuDeleteObjectClick);
			// 
			// toolStripMenuItem2
			// 
			this.toolStripMenuItem2.Name = "toolStripMenuItem2";
			this.toolStripMenuItem2.Size = new System.Drawing.Size(256, 6);
			// 
			// cmnuMoveObjectUp
			// 
			this.cmnuMoveObjectUp.Name = "cmnuMoveObjectUp";
			this.cmnuMoveObjectUp.Size = new System.Drawing.Size(259, 22);
			this.cmnuMoveObjectUp.Text = "Move Object Up";
			this.cmnuMoveObjectUp.Click += new System.EventHandler(this.CmnuMoveObjectUpClick);
			// 
			// cmnuMoveObjectDown
			// 
			this.cmnuMoveObjectDown.Name = "cmnuMoveObjectDown";
			this.cmnuMoveObjectDown.Size = new System.Drawing.Size(259, 22);
			this.cmnuMoveObjectDown.Text = "Move Object Down";
			this.cmnuMoveObjectDown.Click += new System.EventHandler(this.CmnuMoveObjectDownClick);
			// 
			// toolStripMenuItem3
			// 
			this.toolStripMenuItem3.Name = "toolStripMenuItem3";
			this.toolStripMenuItem3.Size = new System.Drawing.Size(256, 6);
			// 
			// cmnuSelectProps
			// 
			this.cmnuSelectProps.Name = "cmnuSelectProps";
			this.cmnuSelectProps.Size = new System.Drawing.Size(259, 22);
			this.cmnuSelectProps.Text = "Select Displayed Properties for {0}...";
			this.cmnuSelectProps.Click += new System.EventHandler(this.CmnuSelectPropsClick);
			// 
			// LCMBrowserForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(872, 567);
			this.Location = new System.Drawing.Point(0, 0);
			this.Controls.Add(this.m_statusStrip);
			this.Controls.Add(this.m_navigationToolStrip);
			this.Controls.Add(this.m_mainMenu);
			this.IsMdiContainer = true;
			this.MainMenuStrip = this.m_mainMenu;
			this.Name = "LCMBrowserForm";
			this.Text = "LCM Browser";
			this.m_statusStrip.ResumeLayout(false);
			this.m_statusStrip.PerformLayout();
			this.m_mainMenu.ResumeLayout(false);
			this.m_mainMenu.PerformLayout();
			this.m_navigationToolStrip.ResumeLayout(false);
			this.m_navigationToolStrip.PerformLayout();
			this.m_cmnuGrid.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
		private ToolStripSeparator mnuWindowsSep;
		private ToolStripSeparator tsSep1;
		/// <summary />
		protected ToolStripMenuItem mnuOpenFile;
		/// <summary />
		protected ToolStrip m_navigationToolStrip;
		/// <summary />
		protected StatusStrip m_statusStrip;
		/// <summary />
		protected MenuStrip m_mainMenu;
		/// <summary />
		protected ToolStripMenuItem mnuWindow;
		/// <summary />
		protected ToolStripButton tsbShowObjInNewWnd;
		/// <summary />
		protected ContextMenuStrip m_cmnuGrid;
		/// <summary />
		protected ToolStripMenuItem mnuTools;
		/// <summary />
		protected ToolStripMenuItem mnuOptions;
		/// <summary />
		protected ToolStripMenuItem mnuFile;
		/// <summary />
		protected ToolStripMenuItem mnuExit;
		/// <summary />
		protected ToolStripMenuItem mnuTileVertically;
		/// <summary />
		protected ToolStripMenuItem mnuTileHorizontally;
		/// <summary />
		protected ToolStripMenuItem mnuArrangeInline;
		/// <summary />
		protected ToolStripButton tsbOpen;
		/// <summary />
		protected ToolStripMenuItem cmnuShowInNewWindow;
		/// <summary />
		protected ToolStripSeparator mnuFileSep1;
		/// <summary />
		protected ToolStripStatusLabel m_statuslabel;
		/// <summary />
		protected ToolStripStatusLabel m_sblblLoadTime;
		private ToolStripSeparator toolStripMenuItem1;
		private ToolStripMenuItem cmnuAddObject;
		private ToolStripMenuItem cmnuDeleteObject;
		private ToolStripSeparator toolStripMenuItem2;
		private ToolStripMenuItem cmnuMoveObjectUp;
		private ToolStripMenuItem cmnuMoveObjectDown;
		private ToolStripSeparator toolStripMenuItem3;
		private ToolStripMenuItem cmnuSelectProps;
		private ToolStripButton m_tsbShowCmObjectProps;
		private ToolStripTextBox tstxtGuidSrch;
		private ToolStripLabel tslblGuidSrch;
		private ToolStripMenuItem mnuSaveFileLCM;
		private ToolStripMenuItem mnuToolsAllowEdit;
		private ToolStripMenuItem mnuView;
		private ToolStripMenuItem mnuViewLcmModel;
		private ToolStripMenuItem mnuViewLangProject;
		private ToolStripMenuItem mnuViewRepositories;
		private ToolStripSeparator toolStripMenuItem4;
		private ToolStripMenuItem mnuDisplayVirtual;
		private ToolStripMenuItem mnuClassProperties;
	}
}
