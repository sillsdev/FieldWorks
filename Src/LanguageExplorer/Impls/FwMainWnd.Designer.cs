using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;

namespace LanguageExplorer.Impls
{
	partial class FwMainWnd
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification = "TODO-Linux: SplitContainer.TabStop is missing from Mono")]
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FwMainWnd));
			this._menuStrip = new System.Windows.Forms.MenuStrip();
			this._fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.newFieldWorksProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
			this.projectManagementToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.fieldWorksProjectPropertiesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.backUpThisProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.restoreAProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem7 = new System.Windows.Forms.ToolStripSeparator();
			this.projectLocationsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.deleteProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.createShortcutOnDesktopToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem8 = new System.Windows.Forms.ToolStripSeparator();
			this.archiveWithRAMPSILToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem9 = new System.Windows.Forms.ToolStripSeparator();
			this.pageSetupToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.printToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem10 = new System.Windows.Forms.ToolStripSeparator();
			this.importToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.translatedListContentToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
			this.exportToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
			this.closeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._sendReceiveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.undoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.redoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem11 = new System.Windows.Forms.ToolStripSeparator();
			this.cutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.pasteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem12 = new System.Windows.Forms.ToolStripSeparator();
			this.pasteHyperlinkToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.copyLocationAsHyperlinkToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem13 = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripMenuItem14 = new System.Windows.Forms.ToolStripSeparator();
			this.selectAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.refreshToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem5 = new System.Windows.Forms.ToolStripSeparator();
			this._dataToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._insertToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._formatToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.setUpWritingSystemsToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
			this._toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem6 = new System.Windows.Forms.ToolStripSeparator();
			this.configureToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.setUpWritingSystemsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._windowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.newWindowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.languageExplorerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.trainingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.demoMoviesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.resourcesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.technicalNotesOnFieldWorksSendReceiveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.technicalNotesOnWritingSystemsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.editingLinguisticsPapersUsingXLingPaperToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
			this.reportAProblemToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.makeASuggestionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripSeparator();
			this.aboutLanguageExplorerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripStandard = new System.Windows.Forms.ToolStrip();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.undoToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.redoToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.toolStripButton_Refresh = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripContainer = new System.Windows.Forms.ToolStripContainer();
			this.mainContainer = new LanguageExplorer.Controls.CollapsingSplitContainer();
			this._leftPanel = new System.Windows.Forms.Panel();
			this._rightPanel = new System.Windows.Forms.Panel();
			this._statusbar = new System.Windows.Forms.StatusBar();
			this.statusBarPanelMessage = new System.Windows.Forms.StatusBarPanel();
			this.statusBarPanelProgress = new System.Windows.Forms.StatusBarPanel();
			this.statusBarPanelArea = new System.Windows.Forms.StatusBarPanel();
			this.statusBarPanelRecordNumber = new System.Windows.Forms.StatusBarPanel();
			this._menuStrip.SuspendLayout();
			this.toolStripStandard.SuspendLayout();
			this.toolStripContainer.ContentPanel.SuspendLayout();
			this.toolStripContainer.TopToolStripPanel.SuspendLayout();
			this.toolStripContainer.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.mainContainer)).BeginInit();
			this.mainContainer.Panel1.SuspendLayout();
			this.mainContainer.Panel2.SuspendLayout();
			this.mainContainer.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.statusBarPanelMessage)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.statusBarPanelProgress)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.statusBarPanelArea)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.statusBarPanelRecordNumber)).BeginInit();
			this.SuspendLayout();
			// 
			// _menuStrip
			// 
			this._menuStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
			this._menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._fileToolStripMenuItem,
            this._sendReceiveToolStripMenuItem,
            this._editToolStripMenuItem,
            this._viewToolStripMenuItem,
            this._dataToolStripMenuItem,
            this._insertToolStripMenuItem,
            this._formatToolStripMenuItem,
            this._toolsToolStripMenuItem,
            this._windowToolStripMenuItem,
            this._helpToolStripMenuItem});
			this._menuStrip.Location = new System.Drawing.Point(0, 0);
			this._menuStrip.Name = "_menuStrip";
			this._menuStrip.Size = new System.Drawing.Size(697, 24);
			this._menuStrip.TabIndex = 1;
			this._menuStrip.Text = "menuStrip1";
			// 
			// _fileToolStripMenuItem
			// 
			this._fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newFieldWorksProjectToolStripMenuItem,
            this.openToolStripMenuItem,
            this.toolStripMenuItem1,
            this.projectManagementToolStripMenuItem,
            this.toolStripMenuItem8,
            this.archiveWithRAMPSILToolStripMenuItem,
            this.toolStripMenuItem9,
            this.pageSetupToolStripMenuItem,
            this.printToolStripMenuItem,
            this.toolStripMenuItem10,
            this.importToolStripMenuItem,
            this.exportToolStripMenuItem,
            this.toolStripMenuItem3,
            this.closeToolStripMenuItem});
			this._fileToolStripMenuItem.Name = "_fileToolStripMenuItem";
			this._fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
			this._fileToolStripMenuItem.Text = "&File";
			// 
			// newFieldWorksProjectToolStripMenuItem
			// 
			this.newFieldWorksProjectToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("newFieldWorksProjectToolStripMenuItem.Image")));
			this.newFieldWorksProjectToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.newFieldWorksProjectToolStripMenuItem.Name = "newFieldWorksProjectToolStripMenuItem";
			this.newFieldWorksProjectToolStripMenuItem.Size = new System.Drawing.Size(215, 26);
			this.newFieldWorksProjectToolStripMenuItem.Text = "&New FieldWorks Project...";
			this.newFieldWorksProjectToolStripMenuItem.ToolTipText = "Create a new FieldWorks project.";
			this.newFieldWorksProjectToolStripMenuItem.Click += new System.EventHandler(this.File_New_FieldWorks_Project);
			// 
			// openToolStripMenuItem
			// 
			this.openToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("openToolStripMenuItem.Image")));
			this.openToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.openToolStripMenuItem.Name = "openToolStripMenuItem";
			this.openToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.openToolStripMenuItem.Size = new System.Drawing.Size(215, 26);
			this.openToolStripMenuItem.Text = "&Open...";
			this.openToolStripMenuItem.ToolTipText = "Open an existing FieldWorks project.";
			this.openToolStripMenuItem.Click += new System.EventHandler(this.File_Open);
			// 
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new System.Drawing.Size(212, 6);
			// 
			// projectManagementToolStripMenuItem
			// 
			this.projectManagementToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fieldWorksProjectPropertiesToolStripMenuItem,
            this.backUpThisProjectToolStripMenuItem,
            this.restoreAProjectToolStripMenuItem,
            this.toolStripMenuItem7,
            this.projectLocationsToolStripMenuItem,
            this.deleteProjectToolStripMenuItem,
            this.createShortcutOnDesktopToolStripMenuItem});
			this.projectManagementToolStripMenuItem.Name = "projectManagementToolStripMenuItem";
			this.projectManagementToolStripMenuItem.Size = new System.Drawing.Size(215, 26);
			this.projectManagementToolStripMenuItem.Text = "Project &Management";
			this.projectManagementToolStripMenuItem.Click += new System.EventHandler(this.File_Create_Shortcut_on_Desktop);
			// 
			// fieldWorksProjectPropertiesToolStripMenuItem
			// 
			this.fieldWorksProjectPropertiesToolStripMenuItem.Name = "fieldWorksProjectPropertiesToolStripMenuItem";
			this.fieldWorksProjectPropertiesToolStripMenuItem.Size = new System.Drawing.Size(237, 22);
			this.fieldWorksProjectPropertiesToolStripMenuItem.Text = "Field&Works Project Properties...";
			this.fieldWorksProjectPropertiesToolStripMenuItem.ToolTipText = "Edit the special properties of this FieldWorks project (such as name and writing " +
    "systems).";
			this.fieldWorksProjectPropertiesToolStripMenuItem.Click += new System.EventHandler(this.File_FieldWorks_Project_Properties);
			// 
			// backUpThisProjectToolStripMenuItem
			// 
			this.backUpThisProjectToolStripMenuItem.Name = "backUpThisProjectToolStripMenuItem";
			this.backUpThisProjectToolStripMenuItem.Size = new System.Drawing.Size(237, 22);
			this.backUpThisProjectToolStripMenuItem.Text = "&Back up this Project...";
			this.backUpThisProjectToolStripMenuItem.ToolTipText = "Back up a FieldWorks project.";
			this.backUpThisProjectToolStripMenuItem.Click += new System.EventHandler(this.File_Back_up_this_Project);
			// 
			// restoreAProjectToolStripMenuItem
			// 
			this.restoreAProjectToolStripMenuItem.Name = "restoreAProjectToolStripMenuItem";
			this.restoreAProjectToolStripMenuItem.Size = new System.Drawing.Size(237, 22);
			this.restoreAProjectToolStripMenuItem.Text = "&Restore a Project...";
			this.restoreAProjectToolStripMenuItem.ToolTipText = "Restore a FieldWorks project.";
			this.restoreAProjectToolStripMenuItem.Click += new System.EventHandler(this.File_Restore_a_Project);
			// 
			// toolStripMenuItem7
			// 
			this.toolStripMenuItem7.Name = "toolStripMenuItem7";
			this.toolStripMenuItem7.Size = new System.Drawing.Size(234, 6);
			// 
			// projectLocationsToolStripMenuItem
			// 
			this.projectLocationsToolStripMenuItem.Name = "projectLocationsToolStripMenuItem";
			this.projectLocationsToolStripMenuItem.Size = new System.Drawing.Size(237, 22);
			this.projectLocationsToolStripMenuItem.Text = "Project Locations...";
			this.projectLocationsToolStripMenuItem.Click += new System.EventHandler(this.File_Project_Location);
			// 
			// deleteProjectToolStripMenuItem
			// 
			this.deleteProjectToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("deleteProjectToolStripMenuItem.Image")));
			this.deleteProjectToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.deleteProjectToolStripMenuItem.Name = "deleteProjectToolStripMenuItem";
			this.deleteProjectToolStripMenuItem.Size = new System.Drawing.Size(237, 22);
			this.deleteProjectToolStripMenuItem.Text = "&Delete Project...";
			this.deleteProjectToolStripMenuItem.ToolTipText = "Delete FieldWorks project.";
			this.deleteProjectToolStripMenuItem.Click += new System.EventHandler(this.File_Delete_Project);
			// 
			// createShortcutOnDesktopToolStripMenuItem
			// 
			this.createShortcutOnDesktopToolStripMenuItem.Name = "createShortcutOnDesktopToolStripMenuItem";
			this.createShortcutOnDesktopToolStripMenuItem.Size = new System.Drawing.Size(237, 22);
			this.createShortcutOnDesktopToolStripMenuItem.Text = "&Create Shortcut on Desktop";
			this.createShortcutOnDesktopToolStripMenuItem.ToolTipText = "Create a desktop shortcut to this project.";
			this.createShortcutOnDesktopToolStripMenuItem.Click += new System.EventHandler(this.File_Create_Shortcut_on_Desktop);
			// 
			// toolStripMenuItem8
			// 
			this.toolStripMenuItem8.Name = "toolStripMenuItem8";
			this.toolStripMenuItem8.Size = new System.Drawing.Size(212, 6);
			// 
			// archiveWithRAMPSILToolStripMenuItem
			// 
			this.archiveWithRAMPSILToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("archiveWithRAMPSILToolStripMenuItem.Image")));
			this.archiveWithRAMPSILToolStripMenuItem.Name = "archiveWithRAMPSILToolStripMenuItem";
			this.archiveWithRAMPSILToolStripMenuItem.Size = new System.Drawing.Size(215, 26);
			this.archiveWithRAMPSILToolStripMenuItem.Text = "&Archive with RAMP (SIL)...";
			this.archiveWithRAMPSILToolStripMenuItem.ToolTipText = "Starts RAMP (if it is installed) and prepares an archive package for uploading.";
			this.archiveWithRAMPSILToolStripMenuItem.Click += new System.EventHandler(this.File_Archive_With_RAMP);
			// 
			// toolStripMenuItem9
			// 
			this.toolStripMenuItem9.Name = "toolStripMenuItem9";
			this.toolStripMenuItem9.Size = new System.Drawing.Size(212, 6);
			// 
			// pageSetupToolStripMenuItem
			// 
			this.pageSetupToolStripMenuItem.Enabled = false;
			this.pageSetupToolStripMenuItem.Name = "pageSetupToolStripMenuItem";
			this.pageSetupToolStripMenuItem.Size = new System.Drawing.Size(215, 26);
			this.pageSetupToolStripMenuItem.Text = "Page Setup...";
			this.pageSetupToolStripMenuItem.ToolTipText = "Determine page layout options for printing.";
			this.pageSetupToolStripMenuItem.Click += new System.EventHandler(this.File_Page_Setup);
			// 
			// printToolStripMenuItem
			// 
			this.printToolStripMenuItem.Enabled = false;
			this.printToolStripMenuItem.Name = "printToolStripMenuItem";
			this.printToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.P)));
			this.printToolStripMenuItem.Size = new System.Drawing.Size(215, 26);
			this.printToolStripMenuItem.Text = "&Print...";
			this.printToolStripMenuItem.ToolTipText = "Print";
			// 
			// toolStripMenuItem10
			// 
			this.toolStripMenuItem10.Name = "toolStripMenuItem10";
			this.toolStripMenuItem10.Size = new System.Drawing.Size(212, 6);
			// 
			// importToolStripMenuItem
			// 
			this.importToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.translatedListContentToolStripMenuItem1});
			this.importToolStripMenuItem.Name = "importToolStripMenuItem";
			this.importToolStripMenuItem.Size = new System.Drawing.Size(215, 26);
			this.importToolStripMenuItem.Text = "&Import";
			// 
			// translatedListContentToolStripMenuItem1
			// 
			this.translatedListContentToolStripMenuItem1.Name = "translatedListContentToolStripMenuItem1";
			this.translatedListContentToolStripMenuItem1.Size = new System.Drawing.Size(196, 22);
			this.translatedListContentToolStripMenuItem1.Text = "&Translated List Content";
			this.translatedListContentToolStripMenuItem1.Click += new System.EventHandler(this.File_Translated_List_Content);
			// 
			// exportToolStripMenuItem
			// 
			this.exportToolStripMenuItem.Enabled = false;
			this.exportToolStripMenuItem.Name = "exportToolStripMenuItem";
			this.exportToolStripMenuItem.Size = new System.Drawing.Size(215, 26);
			this.exportToolStripMenuItem.Text = "&Export...";
			this.exportToolStripMenuItem.ToolTipText = "Export this FieldWorks project to a file.";
			this.exportToolStripMenuItem.Visible = false;
			// 
			// toolStripMenuItem3
			// 
			this.toolStripMenuItem3.Name = "toolStripMenuItem3";
			this.toolStripMenuItem3.Size = new System.Drawing.Size(212, 6);
			// 
			// closeToolStripMenuItem
			// 
			this.closeToolStripMenuItem.Name = "closeToolStripMenuItem";
			this.closeToolStripMenuItem.Size = new System.Drawing.Size(215, 26);
			this.closeToolStripMenuItem.Text = "&Close";
			this.closeToolStripMenuItem.ToolTipText = "Close this project.";
			this.closeToolStripMenuItem.Click += new System.EventHandler(this.File_CloseWindow);
			// 
			// _sendReceiveToolStripMenuItem
			// 
			this._sendReceiveToolStripMenuItem.Name = "_sendReceiveToolStripMenuItem";
			this._sendReceiveToolStripMenuItem.Size = new System.Drawing.Size(90, 20);
			this._sendReceiveToolStripMenuItem.Text = "&Send/Receive";
			// 
			// _editToolStripMenuItem
			// 
			this._editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.undoToolStripMenuItem,
            this.redoToolStripMenuItem,
            this.toolStripMenuItem11,
            this.cutToolStripMenuItem,
            this.copyToolStripMenuItem,
            this.pasteToolStripMenuItem,
            this.toolStripMenuItem12,
            this.pasteHyperlinkToolStripMenuItem,
            this.copyLocationAsHyperlinkToolStripMenuItem,
            this.toolStripMenuItem13,
            this.toolStripMenuItem14,
            this.selectAllToolStripMenuItem});
			this._editToolStripMenuItem.Name = "_editToolStripMenuItem";
			this._editToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
			this._editToolStripMenuItem.Text = "&Edit";
			this._editToolStripMenuItem.DropDownOpening += new System.EventHandler(this.EditMenu_Opening);
			// 
			// undoToolStripMenuItem
			// 
			this.undoToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("undoToolStripMenuItem.Image")));
			this.undoToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.undoToolStripMenuItem.Name = "undoToolStripMenuItem";
			this.undoToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Z)));
			this.undoToolStripMenuItem.Size = new System.Drawing.Size(219, 22);
			this.undoToolStripMenuItem.Text = "&Undo";
			this.undoToolStripMenuItem.ToolTipText = "Undo previous actions.";
			// 
			// redoToolStripMenuItem
			// 
			this.redoToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("redoToolStripMenuItem.Image")));
			this.redoToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.redoToolStripMenuItem.Name = "redoToolStripMenuItem";
			this.redoToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Y)));
			this.redoToolStripMenuItem.Size = new System.Drawing.Size(219, 22);
			this.redoToolStripMenuItem.Text = "&Redo";
			this.redoToolStripMenuItem.ToolTipText = "Redo previous actions.";
			// 
			// toolStripMenuItem11
			// 
			this.toolStripMenuItem11.Name = "toolStripMenuItem11";
			this.toolStripMenuItem11.Size = new System.Drawing.Size(216, 6);
			// 
			// cutToolStripMenuItem
			// 
			this.cutToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("cutToolStripMenuItem.Image")));
			this.cutToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.cutToolStripMenuItem.Name = "cutToolStripMenuItem";
			this.cutToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.X)));
			this.cutToolStripMenuItem.Size = new System.Drawing.Size(219, 22);
			this.cutToolStripMenuItem.Text = "Cu&t";
			this.cutToolStripMenuItem.ToolTipText = "Cut";
			this.cutToolStripMenuItem.Click += new System.EventHandler(this.Edit_Cut);
			// 
			// copyToolStripMenuItem
			// 
			this.copyToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("copyToolStripMenuItem.Image")));
			this.copyToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
			this.copyToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
			this.copyToolStripMenuItem.Size = new System.Drawing.Size(219, 22);
			this.copyToolStripMenuItem.Text = "&Copy";
			this.copyToolStripMenuItem.ToolTipText = "Copy";
			this.copyToolStripMenuItem.Click += new System.EventHandler(this.Edit_Copy);
			// 
			// pasteToolStripMenuItem
			// 
			this.pasteToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("pasteToolStripMenuItem.Image")));
			this.pasteToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.pasteToolStripMenuItem.Name = "pasteToolStripMenuItem";
			this.pasteToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.V)));
			this.pasteToolStripMenuItem.Size = new System.Drawing.Size(219, 22);
			this.pasteToolStripMenuItem.Text = "&Paste";
			this.pasteToolStripMenuItem.ToolTipText = "Paste";
			this.pasteToolStripMenuItem.Click += new System.EventHandler(this.Edit_Paste);
			// 
			// toolStripMenuItem12
			// 
			this.toolStripMenuItem12.Name = "toolStripMenuItem12";
			this.toolStripMenuItem12.Size = new System.Drawing.Size(216, 6);
			// 
			// pasteHyperlinkToolStripMenuItem
			// 
			this.pasteHyperlinkToolStripMenuItem.Name = "pasteHyperlinkToolStripMenuItem";
			this.pasteHyperlinkToolStripMenuItem.Size = new System.Drawing.Size(219, 22);
			this.pasteHyperlinkToolStripMenuItem.Text = "Paste &Hyperlink";
			this.pasteHyperlinkToolStripMenuItem.ToolTipText = "Paste clipboard content as hyperlink.";
			this.pasteHyperlinkToolStripMenuItem.Click += new System.EventHandler(this.Edit_Paste_Hyperlink);
			// 
			// copyLocationAsHyperlinkToolStripMenuItem
			// 
			this.copyLocationAsHyperlinkToolStripMenuItem.Name = "copyLocationAsHyperlinkToolStripMenuItem";
			this.copyLocationAsHyperlinkToolStripMenuItem.Size = new System.Drawing.Size(219, 22);
			this.copyLocationAsHyperlinkToolStripMenuItem.Text = "Copy &Location as Hyperlink";
			this.copyLocationAsHyperlinkToolStripMenuItem.ToolTipText = "Create a hyperlink to this location and copy it to the clipboard.";
			// 
			// toolStripMenuItem13
			// 
			this.toolStripMenuItem13.Name = "toolStripMenuItem13";
			this.toolStripMenuItem13.Size = new System.Drawing.Size(216, 6);
			// 
			// toolStripMenuItem14
			// 
			this.toolStripMenuItem14.Name = "toolStripMenuItem14";
			this.toolStripMenuItem14.Size = new System.Drawing.Size(216, 6);
			// 
			// selectAllToolStripMenuItem
			// 
			this.selectAllToolStripMenuItem.Name = "selectAllToolStripMenuItem";
			this.selectAllToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.A)));
			this.selectAllToolStripMenuItem.Size = new System.Drawing.Size(219, 22);
			this.selectAllToolStripMenuItem.Text = "Select &All";
			this.selectAllToolStripMenuItem.Click += new System.EventHandler(this.Edit_Select_All);
			// 
			// _viewToolStripMenuItem
			// 
			this._viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.refreshToolStripMenuItem,
            this.toolStripMenuItem5});
			this._viewToolStripMenuItem.Name = "_viewToolStripMenuItem";
			this._viewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
			this._viewToolStripMenuItem.Text = "&View";
			// 
			// refreshToolStripMenuItem
			// 
			this.refreshToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("refreshToolStripMenuItem.Image")));
			this.refreshToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.refreshToolStripMenuItem.Name = "refreshToolStripMenuItem";
			this.refreshToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F5;
			this.refreshToolStripMenuItem.Size = new System.Drawing.Size(132, 22);
			this.refreshToolStripMenuItem.Text = "&Refresh";
			this.refreshToolStripMenuItem.ToolTipText = "Refresh the screen.";
			this.refreshToolStripMenuItem.Click += new System.EventHandler(this.View_Refresh);
			// 
			// toolStripMenuItem5
			// 
			this.toolStripMenuItem5.Name = "toolStripMenuItem5";
			this.toolStripMenuItem5.Size = new System.Drawing.Size(129, 6);
			// 
			// _dataToolStripMenuItem
			// 
			this._dataToolStripMenuItem.Name = "_dataToolStripMenuItem";
			this._dataToolStripMenuItem.Size = new System.Drawing.Size(43, 20);
			this._dataToolStripMenuItem.Text = "&Data";
			// 
			// _insertToolStripMenuItem
			// 
			this._insertToolStripMenuItem.Name = "_insertToolStripMenuItem";
			this._insertToolStripMenuItem.Size = new System.Drawing.Size(48, 20);
			this._insertToolStripMenuItem.Text = "&Insert";
			// 
			// _formatToolStripMenuItem
			// 
			this._formatToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.setUpWritingSystemsToolStripMenuItem1});
			this._formatToolStripMenuItem.Name = "_formatToolStripMenuItem";
			this._formatToolStripMenuItem.Size = new System.Drawing.Size(57, 20);
			this._formatToolStripMenuItem.Text = "F&ormat";
			// 
			// setUpWritingSystemsToolStripMenuItem1
			// 
			this.setUpWritingSystemsToolStripMenuItem1.Name = "setUpWritingSystemsToolStripMenuItem1";
			this.setUpWritingSystemsToolStripMenuItem1.Size = new System.Drawing.Size(204, 22);
			this.setUpWritingSystemsToolStripMenuItem1.Text = "Se&t up Writing Systems...";
			this.setUpWritingSystemsToolStripMenuItem1.ToolTipText = "Add, remove, or change the writing systems specified for this project.";
			this.setUpWritingSystemsToolStripMenuItem1.Click += new System.EventHandler(this.File_FieldWorks_Project_Properties);
			// 
			// _toolsToolStripMenuItem
			// 
			this._toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem6,
            this.configureToolStripMenuItem});
			this._toolsToolStripMenuItem.Name = "_toolsToolStripMenuItem";
			this._toolsToolStripMenuItem.Size = new System.Drawing.Size(48, 20);
			this._toolsToolStripMenuItem.Text = "&Tools";
			// 
			// toolStripMenuItem6
			// 
			this.toolStripMenuItem6.Name = "toolStripMenuItem6";
			this.toolStripMenuItem6.Size = new System.Drawing.Size(124, 6);
			// 
			// configureToolStripMenuItem
			// 
			this.configureToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.setUpWritingSystemsToolStripMenuItem});
			this.configureToolStripMenuItem.Name = "configureToolStripMenuItem";
			this.configureToolStripMenuItem.Size = new System.Drawing.Size(127, 22);
			this.configureToolStripMenuItem.Text = "Configure";
			// 
			// setUpWritingSystemsToolStripMenuItem
			// 
			this.setUpWritingSystemsToolStripMenuItem.Name = "setUpWritingSystemsToolStripMenuItem";
			this.setUpWritingSystemsToolStripMenuItem.Size = new System.Drawing.Size(204, 22);
			this.setUpWritingSystemsToolStripMenuItem.Text = "Se&t up Writing Systems...";
			this.setUpWritingSystemsToolStripMenuItem.ToolTipText = "Add, remove, or change the writing systems specified for this project.";
			this.setUpWritingSystemsToolStripMenuItem.Click += new System.EventHandler(this.File_FieldWorks_Project_Properties);
			// 
			// _windowToolStripMenuItem
			// 
			this._windowToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newWindowToolStripMenuItem});
			this._windowToolStripMenuItem.Name = "_windowToolStripMenuItem";
			this._windowToolStripMenuItem.Size = new System.Drawing.Size(63, 20);
			this._windowToolStripMenuItem.Text = "&Window";
			// 
			// newWindowToolStripMenuItem
			// 
			this.newWindowToolStripMenuItem.Name = "newWindowToolStripMenuItem";
			this.newWindowToolStripMenuItem.Size = new System.Drawing.Size(145, 22);
			this.newWindowToolStripMenuItem.Text = "&New Window";
			this.newWindowToolStripMenuItem.ToolTipText = "Launch a new window of this editor.";
			this.newWindowToolStripMenuItem.Click += new System.EventHandler(this.NewWindow_Clicked);
			// 
			// _helpToolStripMenuItem
			// 
			this._helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.languageExplorerToolStripMenuItem,
            this.trainingToolStripMenuItem,
            this.demoMoviesToolStripMenuItem,
            this.resourcesToolStripMenuItem,
            this.toolStripMenuItem2,
            this.reportAProblemToolStripMenuItem,
            this.makeASuggestionToolStripMenuItem,
            this.toolStripMenuItem4,
            this.aboutLanguageExplorerToolStripMenuItem});
			this._helpToolStripMenuItem.Name = "_helpToolStripMenuItem";
			this._helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
			this._helpToolStripMenuItem.Text = "&Help";
			// 
			// languageExplorerToolStripMenuItem
			// 
			this.languageExplorerToolStripMenuItem.Name = "languageExplorerToolStripMenuItem";
			this.languageExplorerToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F1;
			this.languageExplorerToolStripMenuItem.Size = new System.Drawing.Size(216, 22);
			this.languageExplorerToolStripMenuItem.Text = "&Language Explorer...";
			this.languageExplorerToolStripMenuItem.ToolTipText = "Help on using Language Explorer (only available in English).";
			this.languageExplorerToolStripMenuItem.Click += new System.EventHandler(this.Help_LanguageExplorer);
			// 
			// trainingToolStripMenuItem
			// 
			this.trainingToolStripMenuItem.Name = "trainingToolStripMenuItem";
			this.trainingToolStripMenuItem.Size = new System.Drawing.Size(216, 22);
			this.trainingToolStripMenuItem.Text = "&Training";
			this.trainingToolStripMenuItem.ToolTipText = "Training for using Language Explorer (only available in English).";
			this.trainingToolStripMenuItem.Click += new System.EventHandler(this.Help_Training);
			// 
			// demoMoviesToolStripMenuItem
			// 
			this.demoMoviesToolStripMenuItem.Name = "demoMoviesToolStripMenuItem";
			this.demoMoviesToolStripMenuItem.Size = new System.Drawing.Size(216, 22);
			this.demoMoviesToolStripMenuItem.Text = "&Demo Movies...";
			this.demoMoviesToolStripMenuItem.ToolTipText = "Run the Demo Movies.";
			this.demoMoviesToolStripMenuItem.Click += new System.EventHandler(this.Help_DemoMovies);
			// 
			// resourcesToolStripMenuItem
			// 
			this.resourcesToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.technicalNotesOnFieldWorksSendReceiveToolStripMenuItem,
            this.technicalNotesOnWritingSystemsToolStripMenuItem,
            this.editingLinguisticsPapersUsingXLingPaperToolStripMenuItem});
			this.resourcesToolStripMenuItem.Name = "resourcesToolStripMenuItem";
			this.resourcesToolStripMenuItem.Size = new System.Drawing.Size(216, 22);
			this.resourcesToolStripMenuItem.Text = "&Resources";
			// 
			// technicalNotesOnFieldWorksSendReceiveToolStripMenuItem
			// 
			this.technicalNotesOnFieldWorksSendReceiveToolStripMenuItem.Name = "technicalNotesOnFieldWorksSendReceiveToolStripMenuItem";
			this.technicalNotesOnFieldWorksSendReceiveToolStripMenuItem.Size = new System.Drawing.Size(320, 22);
			this.technicalNotesOnFieldWorksSendReceiveToolStripMenuItem.Text = "Technical Notes on FieldWorks Send-&Receive...";
			this.technicalNotesOnFieldWorksSendReceiveToolStripMenuItem.ToolTipText = "Display technical notes on FieldWorks Send/Receive (only available in English).";
			this.technicalNotesOnFieldWorksSendReceiveToolStripMenuItem.Click += new System.EventHandler(this.Help_Technical_Notes_on_FieldWorks_Send_Receive);
			// 
			// technicalNotesOnWritingSystemsToolStripMenuItem
			// 
			this.technicalNotesOnWritingSystemsToolStripMenuItem.Name = "technicalNotesOnWritingSystemsToolStripMenuItem";
			this.technicalNotesOnWritingSystemsToolStripMenuItem.Size = new System.Drawing.Size(320, 22);
			this.technicalNotesOnWritingSystemsToolStripMenuItem.Text = "Technical Notes on &Writing Systems...";
			this.technicalNotesOnWritingSystemsToolStripMenuItem.ToolTipText = "Display technical notes on Writing Systems (only available in English).";
			this.technicalNotesOnWritingSystemsToolStripMenuItem.Click += new System.EventHandler(this.Help_Training_Writing_Systems);
			// 
			// editingLinguisticsPapersUsingXLingPaperToolStripMenuItem
			// 
			this.editingLinguisticsPapersUsingXLingPaperToolStripMenuItem.Name = "editingLinguisticsPapersUsingXLingPaperToolStripMenuItem";
			this.editingLinguisticsPapersUsingXLingPaperToolStripMenuItem.Size = new System.Drawing.Size(320, 22);
			this.editingLinguisticsPapersUsingXLingPaperToolStripMenuItem.Text = "Editing Linguistics Papers Using &XLingPaper...";
			this.editingLinguisticsPapersUsingXLingPaperToolStripMenuItem.ToolTipText = "You can edit your Grammar Sketch in XLingPaper format using an XML editor (only a" +
    "vailable in English).";
			this.editingLinguisticsPapersUsingXLingPaperToolStripMenuItem.Click += new System.EventHandler(this.Help_XLingPaper);
			// 
			// toolStripMenuItem2
			// 
			this.toolStripMenuItem2.Name = "toolStripMenuItem2";
			this.toolStripMenuItem2.Size = new System.Drawing.Size(213, 6);
			// 
			// reportAProblemToolStripMenuItem
			// 
			this.reportAProblemToolStripMenuItem.Name = "reportAProblemToolStripMenuItem";
			this.reportAProblemToolStripMenuItem.Size = new System.Drawing.Size(216, 22);
			this.reportAProblemToolStripMenuItem.Text = "&Report a Problem...";
			this.reportAProblemToolStripMenuItem.Click += new System.EventHandler(this.Help_ReportProblem);
			// 
			// makeASuggestionToolStripMenuItem
			// 
			this.makeASuggestionToolStripMenuItem.Name = "makeASuggestionToolStripMenuItem";
			this.makeASuggestionToolStripMenuItem.Size = new System.Drawing.Size(216, 22);
			this.makeASuggestionToolStripMenuItem.Text = "&Make a Suggestion...";
			this.makeASuggestionToolStripMenuItem.Click += new System.EventHandler(this.Help_Make_a_Suggestion);
			// 
			// toolStripMenuItem4
			// 
			this.toolStripMenuItem4.Name = "toolStripMenuItem4";
			this.toolStripMenuItem4.Size = new System.Drawing.Size(213, 6);
			// 
			// aboutLanguageExplorerToolStripMenuItem
			// 
			this.aboutLanguageExplorerToolStripMenuItem.Name = "aboutLanguageExplorerToolStripMenuItem";
			this.aboutLanguageExplorerToolStripMenuItem.Size = new System.Drawing.Size(216, 22);
			this.aboutLanguageExplorerToolStripMenuItem.Text = "&About Language Explorer...";
			this.aboutLanguageExplorerToolStripMenuItem.ToolTipText = "Display version information about this application.";
			this.aboutLanguageExplorerToolStripMenuItem.Click += new System.EventHandler(this.Help_About_Language_Explorer);
			// 
			// toolStripStandard
			// 
			this.toolStripStandard.Dock = System.Windows.Forms.DockStyle.None;
			this.toolStripStandard.ImageScalingSize = new System.Drawing.Size(20, 20);
			this.toolStripStandard.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator2,
            this.undoToolStripButton,
            this.redoToolStripButton,
            this.toolStripButton_Refresh,
            this.toolStripSeparator1});
			this.toolStripStandard.Location = new System.Drawing.Point(4, 0);
			this.toolStripStandard.Name = "toolStripStandard";
			this.toolStripStandard.Size = new System.Drawing.Size(96, 27);
			this.toolStripStandard.TabIndex = 2;
			this.toolStripStandard.Text = "toolStripStandard";
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(6, 27);
			// 
			// undoToolStripButton
			// 
			this.undoToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.undoToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("undoToolStripButton.Image")));
			this.undoToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.undoToolStripButton.Name = "undoToolStripButton";
			this.undoToolStripButton.Size = new System.Drawing.Size(24, 24);
			this.undoToolStripButton.Text = "Undo";
			this.undoToolStripButton.ToolTipText = "Undo previous actions.";
			// 
			// redoToolStripButton
			// 
			this.redoToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.redoToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("redoToolStripButton.Image")));
			this.redoToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.redoToolStripButton.Name = "redoToolStripButton";
			this.redoToolStripButton.Size = new System.Drawing.Size(24, 24);
			this.redoToolStripButton.Text = "Redo";
			this.redoToolStripButton.ToolTipText = "Redo previous actions.";
			// 
			// toolStripButton_Refresh
			// 
			this.toolStripButton_Refresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButton_Refresh.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton_Refresh.Image")));
			this.toolStripButton_Refresh.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButton_Refresh.Name = "toolStripButton_Refresh";
			this.toolStripButton_Refresh.Size = new System.Drawing.Size(24, 24);
			this.toolStripButton_Refresh.Text = "Refresh";
			this.toolStripButton_Refresh.ToolTipText = "Refresh the screen.";
			this.toolStripButton_Refresh.Click += new System.EventHandler(this.View_Refresh);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(6, 27);
			// 
			// toolStripContainer
			// 
			this.toolStripContainer.BottomToolStripPanelVisible = false;
			// 
			// toolStripContainer.ContentPanel
			// 
			this.toolStripContainer.ContentPanel.Controls.Add(this.mainContainer);
			this.toolStripContainer.ContentPanel.Size = new System.Drawing.Size(697, 381);
			this.toolStripContainer.Dock = System.Windows.Forms.DockStyle.Fill;
			this.toolStripContainer.LeftToolStripPanelVisible = false;
			this.toolStripContainer.Location = new System.Drawing.Point(0, 24);
			this.toolStripContainer.Name = "toolStripContainer";
			this.toolStripContainer.RightToolStripPanelVisible = false;
			this.toolStripContainer.Size = new System.Drawing.Size(697, 408);
			this.toolStripContainer.TabIndex = 3;
			this.toolStripContainer.Text = "toolStripContainer1";
			// 
			// toolStripContainer.TopToolStripPanel
			// 
			this.toolStripContainer.TopToolStripPanel.Controls.Add(this.toolStripStandard);
			// 
			// mainContainer
			// 
			this.mainContainer.AccessibleName = "CollapsingSplitContainer";
			this.mainContainer.BackColor = System.Drawing.SystemColors.Control;
			this.mainContainer.Dock = System.Windows.Forms.DockStyle.Fill;
			this.mainContainer.FirstControl = this._leftPanel;
			this.mainContainer.FirstLabel = "";
			this.mainContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this.mainContainer.IsInitializing = false;
			this.mainContainer.Location = new System.Drawing.Point(0, 0);
			this.mainContainer.Name = "mainContainer";
			// 
			// mainContainer.Panel1
			// 
			this.mainContainer.Panel1.Controls.Add(this._leftPanel);
			this.mainContainer.Panel1MinSize = 16;
			// 
			// mainContainer.Panel2
			// 
			this.mainContainer.Panel2.Controls.Add(this._rightPanel);
			this.mainContainer.Panel2MinSize = 16;
			this.mainContainer.SecondControl = this._rightPanel;
			this.mainContainer.SecondLabel = "";
			this.mainContainer.Size = new System.Drawing.Size(697, 381);
			this.mainContainer.TabIndex = 0;
			this.mainContainer.TabStop = false;
			// 
			// _leftPanel
			// 
			this._leftPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this._leftPanel.Location = new System.Drawing.Point(0, 0);
			this._leftPanel.Name = "_leftPanel";
			this._leftPanel.Size = new System.Drawing.Size(50, 381);
			this._leftPanel.TabIndex = 1;
			// 
			// _rightPanel
			// 
			this._rightPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this._rightPanel.Location = new System.Drawing.Point(0, 0);
			this._rightPanel.Name = "_rightPanel";
			this._rightPanel.Size = new System.Drawing.Size(643, 381);
			this._rightPanel.TabIndex = 1;
			// 
			// _statusbar
			// 
			this._statusbar.Location = new System.Drawing.Point(0, 432);
			this._statusbar.Margin = new System.Windows.Forms.Padding(2);
			this._statusbar.Name = "_statusbar";
			this._statusbar.Panels.AddRange(new System.Windows.Forms.StatusBarPanel[] {
            this.statusBarPanelMessage,
            this.statusBarPanelProgress,
            this.statusBarPanelArea,
            this.statusBarPanelRecordNumber});
			this._statusbar.Size = new System.Drawing.Size(697, 18);
			this._statusbar.TabIndex = 4;
			// 
			// statusBarPanelMessage
			// 
			this.statusBarPanelMessage.MinWidth = 40;
			this.statusBarPanelMessage.Name = "statusBarPanelMessage";
			this.statusBarPanelMessage.Text = "Message";
			this.statusBarPanelMessage.Width = 40;
			// 
			// statusBarPanelProgress
			// 
			this.statusBarPanelProgress.MinWidth = 40;
			this.statusBarPanelProgress.Name = "statusBarPanelProgress";
			this.statusBarPanelProgress.Text = "Progress";
			this.statusBarPanelProgress.Width = 40;
			// 
			// statusBarPanelArea
			// 
			this.statusBarPanelArea.Name = "statusBarPanelArea";
			this.statusBarPanelArea.Text = "Area";
			// 
			// statusBarPanelRecordNumber
			// 
			this.statusBarPanelRecordNumber.MinWidth = 40;
			this.statusBarPanelRecordNumber.Name = "statusBarPanelRecordNumber";
			this.statusBarPanelRecordNumber.Text = "RecordNumber";
			// 
			// FwMainWnd
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(697, 450);
			this.Controls.Add(this.toolStripContainer);
			this.Controls.Add(this._menuStrip);
			this.Controls.Add(this._statusbar);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this._menuStrip;
			this.Name = "FwMainWnd";
			this.Text = "FieldWorks Language Explorer";
			this._menuStrip.ResumeLayout(false);
			this._menuStrip.PerformLayout();
			this.toolStripStandard.ResumeLayout(false);
			this.toolStripStandard.PerformLayout();
			this.toolStripContainer.ContentPanel.ResumeLayout(false);
			this.toolStripContainer.TopToolStripPanel.ResumeLayout(false);
			this.toolStripContainer.TopToolStripPanel.PerformLayout();
			this.toolStripContainer.ResumeLayout(false);
			this.toolStripContainer.PerformLayout();
			this.mainContainer.Panel1.ResumeLayout(false);
			this.mainContainer.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.mainContainer)).EndInit();
			this.mainContainer.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.statusBarPanelMessage)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.statusBarPanelProgress)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.statusBarPanelArea)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.statusBarPanelRecordNumber)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.MenuStrip _menuStrip;
		private System.Windows.Forms.ToolStripMenuItem _fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem _sendReceiveToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem _editToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem _viewToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem _dataToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem _insertToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem _formatToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem _toolsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem _windowToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem _helpToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem closeToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem languageExplorerToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem trainingToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem demoMoviesToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem resourcesToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem technicalNotesOnFieldWorksSendReceiveToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
		private System.Windows.Forms.ToolStripMenuItem reportAProblemToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem makeASuggestionToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem4;
		private System.Windows.Forms.ToolStripMenuItem aboutLanguageExplorerToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem newFieldWorksProjectToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem projectManagementToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem3;
		private System.Windows.Forms.ToolStripMenuItem fieldWorksProjectPropertiesToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem refreshToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem5;
		private System.Windows.Forms.ToolStrip toolStripStandard;
		private System.Windows.Forms.ToolStripButton toolStripButton_Refresh;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripContainer toolStripContainer;
		private System.Windows.Forms.ToolStripMenuItem configureToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem setUpWritingSystemsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem setUpWritingSystemsToolStripMenuItem1;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem6;
		private System.Windows.Forms.ToolStripMenuItem backUpThisProjectToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem restoreAProjectToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem7;
		private System.Windows.Forms.ToolStripMenuItem projectLocationsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem deleteProjectToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem createShortcutOnDesktopToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem8;
		private System.Windows.Forms.ToolStripMenuItem archiveWithRAMPSILToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem9;
		private System.Windows.Forms.ToolStripMenuItem pageSetupToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem printToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem10;
		private System.Windows.Forms.ToolStripMenuItem importToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem exportToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem translatedListContentToolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem newWindowToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem technicalNotesOnWritingSystemsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem editingLinguisticsPapersUsingXLingPaperToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem undoToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem redoToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem11;
		private System.Windows.Forms.ToolStripMenuItem cutToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem copyToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem pasteToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem12;
		private System.Windows.Forms.ToolStripButton undoToolStripButton;
		private System.Windows.Forms.ToolStripButton redoToolStripButton;
		private System.Windows.Forms.ToolStripMenuItem pasteHyperlinkToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem copyLocationAsHyperlinkToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem13;
		private System.Windows.Forms.StatusBar _statusbar;
		private StatusBarPanel statusBarPanelMessage;
		private StatusBarPanel statusBarPanelProgress;
		private StatusBarPanel statusBarPanelArea;
		private StatusBarPanel statusBarPanelRecordNumber;
		private ToolStripSeparator toolStripMenuItem14;
		private ToolStripMenuItem selectAllToolStripMenuItem;
		private ToolStripSeparator toolStripSeparator2;
		private LanguageExplorer.Controls.CollapsingSplitContainer mainContainer;
		private Panel _leftPanel;
		private Panel _rightPanel;
	}
}