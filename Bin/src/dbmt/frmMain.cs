using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Data.SqlClient;


namespace dbmt
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class frmMain : System.Windows.Forms.Form
	{
		/*private bool m_fIsAppClosing = false;
		public bool IsAppClosing
		{
			get { return m_fIsAppClosing; }
		}*/

		private System.Windows.Forms.MainMenu mnuMain;
		private System.Windows.Forms.MenuItem mnuFile;
		private System.Windows.Forms.MenuItem mnuFileExit;
		private System.Windows.Forms.MenuItem mnuFileConnect;
		private System.Windows.Forms.ToolBar tbMain;
		private System.Windows.Forms.MenuItem mnuQuery;
		private System.Windows.Forms.MenuItem mnuQueryExecute;
		private System.Windows.Forms.MenuItem mnuQueryCancel;
		private System.Windows.Forms.ToolBarButton tbbNew;
		private System.Windows.Forms.ToolBarButton tbbOpen;
		private System.Windows.Forms.ToolBarButton tbbSave;
		private System.Windows.Forms.ToolBarButton tbbSep1;
		private System.Windows.Forms.ToolBarButton tbbCut;
		private System.Windows.Forms.ToolBarButton tbbCopy;
		private System.Windows.Forms.ToolBarButton tbbPaste;
		private System.Windows.Forms.ToolBarButton tbbSep2;
		private System.Windows.Forms.ToolBarButton tbbFind;
		private System.Windows.Forms.ToolBarButton tbbSep3;
		private System.Windows.Forms.ToolBarButton tbbExecute;
		private System.Windows.Forms.ToolBarButton tbbCancelExecute;
		private System.Windows.Forms.MenuItem mnuFileNew;
		private System.Windows.Forms.MenuItem mnuFileOpen;
		private System.Windows.Forms.MenuItem mnuFileSave;
		private System.Windows.Forms.ToolBarButton tbbSep4;
		private ComboBoxEx cboDatabases;
		private System.Windows.Forms.MenuItem mnuEdit;
		private System.Windows.Forms.MenuItem mnuEditUndo;
		private System.Windows.Forms.MenuItem mnuEditRedo;
		private System.Windows.Forms.MenuItem mnuEditSep1;
		private System.Windows.Forms.MenuItem mnuEditCut;
		private System.Windows.Forms.MenuItem mnuEditCopy;
		private System.Windows.Forms.MenuItem mnuEditPaste;
		private System.Windows.Forms.MenuItem mnuEditSep2;
		private System.Windows.Forms.MenuItem mnuEditSelectAll;
		private System.Windows.Forms.MenuItem mnuFileSep1;
		private System.Windows.Forms.MenuItem mnuFileSep2;
		private System.Windows.Forms.MenuItem mnuWindow;
		private System.Windows.Forms.MenuItem mnuWindowCascade;
		private System.Windows.Forms.MenuItem mnuWindowHoriz;
		private System.Windows.Forms.MenuItem mnuWindowVert;
		private System.Windows.Forms.ImageList ilToolbar;
		private System.Windows.Forms.ImageList ilDatabases;
		private System.Windows.Forms.MenuItem mnuHelp;
		private System.Windows.Forms.MenuItem mnuHelpAbout;
		private System.ComponentModel.IContainer components;

		public frmMain()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null)
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
			this.components = new System.ComponentModel.Container();
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(frmMain));
			this.mnuMain = new System.Windows.Forms.MainMenu();
			this.mnuFile = new System.Windows.Forms.MenuItem();
			this.mnuFileConnect = new System.Windows.Forms.MenuItem();
			this.mnuFileSep1 = new System.Windows.Forms.MenuItem();
			this.mnuFileNew = new System.Windows.Forms.MenuItem();
			this.mnuFileOpen = new System.Windows.Forms.MenuItem();
			this.mnuFileSave = new System.Windows.Forms.MenuItem();
			this.mnuFileSep2 = new System.Windows.Forms.MenuItem();
			this.mnuFileExit = new System.Windows.Forms.MenuItem();
			this.mnuEdit = new System.Windows.Forms.MenuItem();
			this.mnuEditUndo = new System.Windows.Forms.MenuItem();
			this.mnuEditRedo = new System.Windows.Forms.MenuItem();
			this.mnuEditSep1 = new System.Windows.Forms.MenuItem();
			this.mnuEditCut = new System.Windows.Forms.MenuItem();
			this.mnuEditCopy = new System.Windows.Forms.MenuItem();
			this.mnuEditPaste = new System.Windows.Forms.MenuItem();
			this.mnuEditSep2 = new System.Windows.Forms.MenuItem();
			this.mnuEditSelectAll = new System.Windows.Forms.MenuItem();
			this.mnuQuery = new System.Windows.Forms.MenuItem();
			this.mnuQueryExecute = new System.Windows.Forms.MenuItem();
			this.mnuQueryCancel = new System.Windows.Forms.MenuItem();
			this.mnuWindow = new System.Windows.Forms.MenuItem();
			this.mnuWindowCascade = new System.Windows.Forms.MenuItem();
			this.mnuWindowHoriz = new System.Windows.Forms.MenuItem();
			this.mnuWindowVert = new System.Windows.Forms.MenuItem();
			this.mnuHelp = new System.Windows.Forms.MenuItem();
			this.mnuHelpAbout = new System.Windows.Forms.MenuItem();
			this.tbMain = new System.Windows.Forms.ToolBar();
			this.tbbNew = new System.Windows.Forms.ToolBarButton();
			this.tbbOpen = new System.Windows.Forms.ToolBarButton();
			this.tbbSave = new System.Windows.Forms.ToolBarButton();
			this.tbbSep1 = new System.Windows.Forms.ToolBarButton();
			this.tbbCut = new System.Windows.Forms.ToolBarButton();
			this.tbbCopy = new System.Windows.Forms.ToolBarButton();
			this.tbbPaste = new System.Windows.Forms.ToolBarButton();
			this.tbbSep2 = new System.Windows.Forms.ToolBarButton();
			this.tbbFind = new System.Windows.Forms.ToolBarButton();
			this.tbbSep3 = new System.Windows.Forms.ToolBarButton();
			this.tbbExecute = new System.Windows.Forms.ToolBarButton();
			this.tbbCancelExecute = new System.Windows.Forms.ToolBarButton();
			this.tbbSep4 = new System.Windows.Forms.ToolBarButton();
			this.ilToolbar = new System.Windows.Forms.ImageList(this.components);
			this.cboDatabases = new dbmt.ComboBoxEx();
			this.ilDatabases = new System.Windows.Forms.ImageList(this.components);
			this.SuspendLayout();
			//
			// mnuMain
			//
			this.mnuMain.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																					this.mnuFile,
																					this.mnuEdit,
																					this.mnuQuery,
																					this.mnuWindow,
																					this.mnuHelp});
			//
			// mnuFile
			//
			this.mnuFile.Index = 0;
			this.mnuFile.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																					this.mnuFileConnect,
																					this.mnuFileSep1,
																					this.mnuFileNew,
																					this.mnuFileOpen,
																					this.mnuFileSave,
																					this.mnuFileSep2,
																					this.mnuFileExit});
			this.mnuFile.Text = "&File";
			//
			// mnuFileConnect
			//
			this.mnuFileConnect.Index = 0;
			this.mnuFileConnect.Text = "Connect...";
			this.mnuFileConnect.Click += new System.EventHandler(this.mnuFileConnect_Click);
			//
			// mnuFileSep1
			//
			this.mnuFileSep1.Index = 1;
			this.mnuFileSep1.Text = "-";
			//
			// mnuFileNew
			//
			this.mnuFileNew.Index = 2;
			this.mnuFileNew.Shortcut = System.Windows.Forms.Shortcut.CtrlN;
			this.mnuFileNew.Text = "&New...";
			this.mnuFileNew.Click += new System.EventHandler(this.mnuFileNew_Click);
			//
			// mnuFileOpen
			//
			this.mnuFileOpen.Index = 3;
			this.mnuFileOpen.Shortcut = System.Windows.Forms.Shortcut.CtrlO;
			this.mnuFileOpen.Text = "&Open...";
			this.mnuFileOpen.Click += new System.EventHandler(this.mnuFileOpen_Click);
			//
			// mnuFileSave
			//
			this.mnuFileSave.Index = 4;
			this.mnuFileSave.Shortcut = System.Windows.Forms.Shortcut.CtrlS;
			this.mnuFileSave.Text = "&Save...";
			this.mnuFileSave.Click += new System.EventHandler(this.mnuFileSave_Click);
			//
			// mnuFileSep2
			//
			this.mnuFileSep2.Index = 5;
			this.mnuFileSep2.Text = "-";
			//
			// mnuFileExit
			//
			this.mnuFileExit.Index = 6;
			this.mnuFileExit.Shortcut = System.Windows.Forms.Shortcut.AltF4;
			this.mnuFileExit.Text = "E&xit";
			this.mnuFileExit.Click += new System.EventHandler(this.mnuFileExit_Click);
			//
			// mnuEdit
			//
			this.mnuEdit.Index = 1;
			this.mnuEdit.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																					this.mnuEditUndo,
																					this.mnuEditRedo,
																					this.mnuEditSep1,
																					this.mnuEditCut,
																					this.mnuEditCopy,
																					this.mnuEditPaste,
																					this.mnuEditSep2,
																					this.mnuEditSelectAll});
			this.mnuEdit.Text = "&Edit";
			this.mnuEdit.Click += new System.EventHandler(this.mnuEdit_Click);
			//
			// mnuEditUndo
			//
			this.mnuEditUndo.Index = 0;
			this.mnuEditUndo.Text = "Undo\tCtrl+Z";
			this.mnuEditUndo.Click += new System.EventHandler(this.mnuEditUndo_Click);
			//
			// mnuEditRedo
			//
			this.mnuEditRedo.Index = 1;
			this.mnuEditRedo.Text = "Redo\tCtrl+Y";
			this.mnuEditRedo.Click += new System.EventHandler(this.mnuEditRedo_Click);
			//
			// mnuEditSep1
			//
			this.mnuEditSep1.Index = 2;
			this.mnuEditSep1.Text = "-";
			//
			// mnuEditCut
			//
			this.mnuEditCut.Index = 3;
			this.mnuEditCut.Text = "Cut\tCtrl+X";
			this.mnuEditCut.Click += new System.EventHandler(this.mnuEditCut_Click);
			//
			// mnuEditCopy
			//
			this.mnuEditCopy.Index = 4;
			this.mnuEditCopy.Text = "Copy\tCtrl+Y";
			this.mnuEditCopy.Click += new System.EventHandler(this.mnuEditCopy_Click);
			//
			// mnuEditPaste
			//
			this.mnuEditPaste.Index = 5;
			this.mnuEditPaste.Text = "Paste\tCtrl+V";
			this.mnuEditPaste.Click += new System.EventHandler(this.mnuEditPaste_Click);
			//
			// mnuEditSep2
			//
			this.mnuEditSep2.Index = 6;
			this.mnuEditSep2.Text = "-";
			//
			// mnuEditSelectAll
			//
			this.mnuEditSelectAll.Index = 7;
			this.mnuEditSelectAll.Text = "Select All\tCtrl+A";
			this.mnuEditSelectAll.Click += new System.EventHandler(this.mnuEditSelectAll_Click);
			//
			// mnuQuery
			//
			this.mnuQuery.Index = 2;
			this.mnuQuery.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																					 this.mnuQueryExecute,
																					 this.mnuQueryCancel});
			this.mnuQuery.Text = "&Query";
			//
			// mnuQueryExecute
			//
			this.mnuQueryExecute.Index = 0;
			this.mnuQueryExecute.Shortcut = System.Windows.Forms.Shortcut.F5;
			this.mnuQueryExecute.Text = "&Execute";
			this.mnuQueryExecute.Click += new System.EventHandler(this.mnuQueryExecute_Click);
			//
			// mnuQueryCancel
			//
			this.mnuQueryCancel.Index = 1;
			this.mnuQueryCancel.Text = "&Cancel Executing Query\tAlt+Break";
			this.mnuQueryCancel.Click += new System.EventHandler(this.mnuQueryCancel_Click);
			//
			// mnuWindow
			//
			this.mnuWindow.Index = 3;
			this.mnuWindow.MdiList = true;
			this.mnuWindow.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																					  this.mnuWindowCascade,
																					  this.mnuWindowHoriz,
																					  this.mnuWindowVert});
			this.mnuWindow.Text = "&Window";
			//
			// mnuWindowCascade
			//
			this.mnuWindowCascade.Index = 0;
			this.mnuWindowCascade.Text = "&Cascade";
			this.mnuWindowCascade.Click += new System.EventHandler(this.mnuWindowCascade_Click);
			//
			// mnuWindowHoriz
			//
			this.mnuWindowHoriz.Index = 1;
			this.mnuWindowHoriz.Text = "Tile &Horizontally";
			this.mnuWindowHoriz.Click += new System.EventHandler(this.mnuWindowHoriz_Click);
			//
			// mnuWindowVert
			//
			this.mnuWindowVert.Index = 2;
			this.mnuWindowVert.Text = "Tile &Vertically";
			this.mnuWindowVert.Click += new System.EventHandler(this.mnuWindowVert_Click);
			//
			// mnuHelp
			//
			this.mnuHelp.Index = 4;
			this.mnuHelp.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																					this.mnuHelpAbout});
			this.mnuHelp.Text = "&Help";
			//
			// mnuHelpAbout
			//
			this.mnuHelpAbout.Index = 0;
			this.mnuHelpAbout.Text = "&About...";
			this.mnuHelpAbout.Click += new System.EventHandler(this.mnuHelpAbout_Click);
			//
			// tbMain
			//
			this.tbMain.Appearance = System.Windows.Forms.ToolBarAppearance.Flat;
			this.tbMain.Buttons.AddRange(new System.Windows.Forms.ToolBarButton[] {
																					  this.tbbNew,
																					  this.tbbOpen,
																					  this.tbbSave,
																					  this.tbbSep1,
																					  this.tbbCut,
																					  this.tbbCopy,
																					  this.tbbPaste,
																					  this.tbbSep2,
																					  this.tbbFind,
																					  this.tbbSep3,
																					  this.tbbExecute,
																					  this.tbbCancelExecute,
																					  this.tbbSep4});
			this.tbMain.ButtonSize = new System.Drawing.Size(18, 22);
			this.tbMain.DropDownArrows = true;
			this.tbMain.ImageList = this.ilToolbar;
			this.tbMain.Location = new System.Drawing.Point(0, 0);
			this.tbMain.Name = "tbMain";
			this.tbMain.ShowToolTips = true;
			this.tbMain.Size = new System.Drawing.Size(736, 27);
			this.tbMain.TabIndex = 1;
			this.tbMain.ButtonClick += new System.Windows.Forms.ToolBarButtonClickEventHandler(this.tbMain_ButtonClick);
			//
			// tbbNew
			//
			this.tbbNew.Enabled = false;
			this.tbbNew.ImageIndex = 0;
			this.tbbNew.ToolTipText = "New Query (Ctrl+N)";
			//
			// tbbOpen
			//
			this.tbbOpen.Enabled = false;
			this.tbbOpen.ImageIndex = 1;
			this.tbbOpen.ToolTipText = "Load SQL Script (Ctrl+O)";
			//
			// tbbSave
			//
			this.tbbSave.Enabled = false;
			this.tbbSave.ImageIndex = 2;
			this.tbbSave.ToolTipText = "Save Query/Result (Ctrl+S)";
			//
			// tbbSep1
			//
			this.tbbSep1.Style = System.Windows.Forms.ToolBarButtonStyle.Separator;
			//
			// tbbCut
			//
			this.tbbCut.Enabled = false;
			this.tbbCut.ImageIndex = 3;
			this.tbbCut.ToolTipText = "Cut (Ctrl+X)";
			//
			// tbbCopy
			//
			this.tbbCopy.Enabled = false;
			this.tbbCopy.ImageIndex = 4;
			this.tbbCopy.ToolTipText = "Copy (Ctrl+C)";
			//
			// tbbPaste
			//
			this.tbbPaste.Enabled = false;
			this.tbbPaste.ImageIndex = 5;
			this.tbbPaste.ToolTipText = "Paste (Ctrl+V)";
			//
			// tbbSep2
			//
			this.tbbSep2.Style = System.Windows.Forms.ToolBarButtonStyle.Separator;
			//
			// tbbFind
			//
			this.tbbFind.Enabled = false;
			this.tbbFind.ImageIndex = 6;
			this.tbbFind.ToolTipText = "Find (Ctrl+F)";
			//
			// tbbSep3
			//
			this.tbbSep3.Style = System.Windows.Forms.ToolBarButtonStyle.Separator;
			//
			// tbbExecute
			//
			this.tbbExecute.Enabled = false;
			this.tbbExecute.ImageIndex = 7;
			this.tbbExecute.ToolTipText = "Execute Query (F5)";
			//
			// tbbCancelExecute
			//
			this.tbbCancelExecute.Enabled = false;
			this.tbbCancelExecute.ImageIndex = 8;
			this.tbbCancelExecute.ToolTipText = "Cancel Query Execution (Alt+Break)";
			//
			// tbbSep4
			//
			this.tbbSep4.Style = System.Windows.Forms.ToolBarButtonStyle.Separator;
			//
			// ilToolbar
			//
			this.ilToolbar.ImageSize = new System.Drawing.Size(16, 15);
			this.ilToolbar.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("ilToolbar.ImageStream")));
			this.ilToolbar.TransparentColor = System.Drawing.Color.Transparent;
			//
			// cboDatabases
			//
			this.cboDatabases.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this.cboDatabases.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cboDatabases.Enabled = false;
			this.cboDatabases.ImageList = null;
			this.cboDatabases.ItemHeight = 15;
			this.cboDatabases.Location = new System.Drawing.Point(242, 3);
			this.cboDatabases.Name = "cboDatabases";
			this.cboDatabases.Size = new System.Drawing.Size(176, 21);
			this.cboDatabases.TabIndex = 3;
			this.cboDatabases.SelectedIndexChanged += new System.EventHandler(this.cboDatabases_SelectedIndexChanged);
			//
			// ilDatabases
			//
			this.ilDatabases.ImageSize = new System.Drawing.Size(20, 15);
			this.ilDatabases.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("ilDatabases.ImageStream")));
			this.ilDatabases.TransparentColor = System.Drawing.Color.Transparent;
			//
			// frmMain
			//
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(736, 569);
			this.Controls.Add(this.cboDatabases);
			this.Controls.Add(this.tbMain);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.IsMdiContainer = true;
			this.Menu = this.mnuMain;
			this.Name = "frmMain";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Database Maintenance Tool";
			this.Closing += new System.ComponentModel.CancelEventHandler(this.frmMain_Closing);
			this.Load += new System.EventHandler(this.frmMain_Load);
			this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.Run(new frmMain());
		}

		private void frmMain_Load(object sender, System.EventArgs e)
		{
			Globals.MainForm = this;

			this.cboDatabases.ImageList = this.ilDatabases;

			this.Show();

			frmConnect oForm = new frmConnect();
			oForm.ShowDialog(this);

			Application.Idle += new EventHandler(Application_Idle);

			Globals.LoadAppSettings();
		}

		private void mnuFileConnect_Click(object sender, System.EventArgs e)
		{
			frmConnect oForm = new frmConnect();
			oForm.ShowDialog(this);
		}

		private void mnuFileExit_Click(object sender, System.EventArgs e)
		{
			this.Close();
		}

		private void tbMain_ButtonClick(object sender, System.Windows.Forms.ToolBarButtonClickEventArgs e)
		{
			if (e.Button == this.tbbCancelExecute)
				DoCancelExecute();
			else if (e.Button == this.tbbCopy)
				DoCopy();
			else if (e.Button == this.tbbCut)
				DoCut();
			else if (e.Button == tbbExecute)
				DoExecute();
			else if (e.Button == this.tbbFind)
				DoFind();
			else if (e.Button == this.tbbNew)
				DoNew();
			else if (e.Button == this.tbbOpen)
				DoOpen();
			else if (e.Button == this.tbbPaste)
				DoPaste();
			else if (e.Button == this.tbbSave)
				DoSave();
		}

		private void mnuQueryExecute_Click(object sender, System.EventArgs e)
		{
			if (this.ActiveMdiChild is frmConnection)
				((frmConnection)this.ActiveMdiChild).DoExecute();
		}

		private void Application_Idle(object sender, EventArgs e)
		{
			if (this.ActiveMdiChild is frmConnection)
			{
				frmConnection oForm = (frmConnection)this.ActiveMdiChild;

				mnuQueryExecute.Enabled = !oForm.IsExecuting;
				mnuQueryCancel.Enabled = oForm.IsExecuting;

				tbbNew.Enabled = true;
				tbbOpen.Enabled = true;
				tbbSave.Enabled = true;

				tbbCut.Enabled = oForm.HasSelection;
				tbbCopy.Enabled = oForm.HasSelection;

				IDataObject oData = null;
				try
				{
					oData = Clipboard.GetDataObject();
				}
				catch
				{
					// Do nothing.
				}
				if (oData == null)
					tbbPaste.Enabled = false;
				else
					tbbPaste.Enabled = oData.GetDataPresent(DataFormats.Text);

				tbbFind.Enabled = true;
				tbbExecute.Enabled = !oForm.IsExecuting;
				tbbCancelExecute.Enabled = oForm.IsExecuting;

				cboDatabases.Enabled = true;
			}
			else
			{
				mnuQueryExecute.Enabled = false;
				mnuQueryCancel.Enabled = false;

				tbbNew.Enabled = true;
				tbbOpen.Enabled = true;
				tbbSave.Enabled = false;
				tbbCut.Enabled = false;
				tbbCopy.Enabled = false;
				tbbPaste.Enabled = false;
				tbbFind.Enabled = false;
				tbbExecute.Enabled = false;
				tbbCancelExecute.Enabled = false;

				cboDatabases.Enabled = false;
			}
		}

		public void SetActiveConnection(SqlConnection oConn)
		{
			if (oConn == null)
			{
				cboDatabases.Items.Clear();
				return;
			}

			SqlDataReader oReader = null;
			try
			{
				SqlCommand oCommand = oConn.CreateCommand();
				oCommand.CommandText =
					"select name, case when dbid <= 4 then 1 else 0 end as IsMasterDB " +
					"from master.dbo.sysdatabases " +
					"order by case when dbid <= 4 then 1 else 0 end, name";
				oReader = oCommand.ExecuteReader();

				cboDatabases.Items.Clear();

				string sCurrentDatabase = oConn.Database;
				while (oReader.Read())
				{
					string sDatabase = oReader[0].ToString();
					int iItem = cboDatabases.AddItem(sDatabase, (int)oReader[1]);
					if (sCurrentDatabase == sDatabase)
						cboDatabases.SelectedIndex = iItem;
				}
			}
			finally
			{
				if (oReader != null)
				{
					oReader.Close();
					oReader = null;
				}
			}
		}

		public void UpdateExecuteState(frmConnection oForm)
		{
			if (oForm == this.ActiveMdiChild)
			{
				tbbExecute.Enabled = !oForm.IsExecuting;
				tbbCancelExecute.Enabled = oForm.IsExecuting;
			}
		}

		private void cboDatabases_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if (this.ActiveMdiChild is frmConnection)
			{
				((frmConnection)this.ActiveMdiChild).DoDatabaseChange(cboDatabases.Text);
			}
		}

		private void mnuFileNew_Click(object sender, System.EventArgs e)
		{
			DoNew();
		}

		private void mnuFileOpen_Click(object sender, System.EventArgs e)
		{
			DoOpen();
		}

		private void mnuFileSave_Click(object sender, System.EventArgs e)
		{
			DoSave();
		}

		private void mnuEditUndo_Click(object sender, System.EventArgs e)
		{
			SendKeys.Send("^z");
		}

		private void mnuEditRedo_Click(object sender, System.EventArgs e)
		{
			SendKeys.Send("^y");
		}

		private void mnuEditCut_Click(object sender, System.EventArgs e)
		{
			SendKeys.Send("^x");
		}

		private void mnuEditCopy_Click(object sender, System.EventArgs e)
		{
			SendKeys.Send("^c");
		}

		private void mnuEditPaste_Click(object sender, System.EventArgs e)
		{
			SendKeys.Send("^v");
		}

		private void mnuEditSelectAll_Click(object sender, System.EventArgs e)
		{
			SendKeys.Send("^a");
		}

		private void mnuEditFind_Click(object sender, System.EventArgs e)
		{
			// TODO
		}

		private void mnuEditFindNext_Click(object sender, System.EventArgs e)
		{
			// TODO
		}

		private void mnuEditGotoLine_Click(object sender, System.EventArgs e)
		{
			// TODO
		}

		private void mnuQueryCancel_Click(object sender, System.EventArgs e)
		{
			DoCancelExecute();
		}

		private void mnuQueryResultsText_Click(object sender, System.EventArgs e)
		{
			// TODO
		}

		private void mnuQueryResultsGrid_Click(object sender, System.EventArgs e)
		{
			// TODO
		}

		private void mnuQueryResultsFile_Click(object sender, System.EventArgs e)
		{
			// TODO
		}

		private void mnuWindowCascade_Click(object sender, System.EventArgs e)
		{
			this.LayoutMdi(MdiLayout.Cascade);
		}

		private void mnuWindowHoriz_Click(object sender, System.EventArgs e)
		{
			this.LayoutMdi(MdiLayout.TileHorizontal);
		}

		private void mnuWindowVert_Click(object sender, System.EventArgs e)
		{
			this.LayoutMdi(MdiLayout.TileVertical);
		}

		private void mnuEdit_Click(object sender, System.EventArgs e)
		{
			mnuEditCopy.Enabled = tbbCopy.Enabled;
			mnuEditCut.Enabled = tbbCut.Enabled;
			mnuEditPaste.Enabled = tbbPaste.Enabled;
			mnuEditRedo.Enabled = true;
			mnuEditUndo.Enabled = true;
		}

		private void frmMain_Closing(object sender, CancelEventArgs e)
		{
			/*m_fIsAppClosing = true;

			bool fCanClose = true;

			if (this.ActiveMdiChild is frmConnection)
				fCanClose = ((frmConnection)this.ActiveMdiChild).CanCloseConnection();

			if (fCanClose)
			{
				foreach (Form oForm in this.MdiChildren)
				{
					if (oForm is frmConnection && oForm != this.ActiveMdiChild)
					{
						fCanClose = ((frmConnection)oForm).CanCloseConnection();
						if (!fCanClose)
							break;
					}
				}
			}

			e.Cancel = !fCanClose;
			m_fIsAppClosing = false;*/
		}

		private void mnuHelpAbout_Click(object sender, System.EventArgs e)
		{
			frmAbout oForm = new frmAbout();
			oForm.ShowDialog(this);
		}

		private void DoCancelExecute()
		{
			if (this.ActiveMdiChild is frmConnection)
				((frmConnection)this.ActiveMdiChild).DoCancelExecute();
		}

		private void DoCopy()
		{
			if (this.ActiveMdiChild is frmConnection)
				((frmConnection)this.ActiveMdiChild).DoCopy();
		}

		private void DoCut()
		{
			if (this.ActiveMdiChild is frmConnection)
				((frmConnection)this.ActiveMdiChild).DoCut();
		}

		private void DoExecute()
		{
			if (this.ActiveMdiChild is frmConnection)
				((frmConnection)this.ActiveMdiChild).DoExecute();
		}

		private void DoFind()
		{
			if (this.ActiveMdiChild is frmConnection)
				((frmConnection)this.ActiveMdiChild).DoFind();
		}

		private void DoNew()
		{
			if (this.ActiveMdiChild is frmConnection)
			{
				((frmConnection)this.ActiveMdiChild).DoNew();
			}
			else
			{
				frmConnect oForm = new frmConnect();
				oForm.ShowDialog(this);
			}
		}

		private void DoOpen()
		{
			if (this.ActiveMdiChild is frmConnection)
				((frmConnection)this.ActiveMdiChild).DoOpen();
		}

		private void DoPaste()
		{
			if (this.ActiveMdiChild is frmConnection)
				((frmConnection)this.ActiveMdiChild).DoPaste();
		}

		private void DoSave()
		{
			if (this.ActiveMdiChild is frmConnection)
				((frmConnection)this.ActiveMdiChild).DoSave();
		}
	}
}
