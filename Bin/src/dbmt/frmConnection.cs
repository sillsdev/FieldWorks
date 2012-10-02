using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Win32;
using System.IO;
using System.Text;


namespace dbmt
{
	/// <summary>
	/// Summary description for frmConnection.
	/// </summary>
	public class frmConnection : System.Windows.Forms.Form
	{
		private SqlConnection m_oConn;
		private string m_sUsername;
		private bool m_fIsExecuting;
		private string m_sFilename;
		private StringBuilder m_sbMessages;

		private bool m_fCancelExecute;
		private DataSet m_oDataSet;
		private bool m_fExecuteError;
		private string m_sLastError;

		private bool m_fResizingGrids = false;
		private DataGrid m_oResizingGrid;

		public bool IsExecuting
		{
			get { return m_fIsExecuting; }
		}

		public bool IsModified
		{
			get { return stbMain.CanUndo; }
		}

		public bool HasSelection
		{
			get { return stbMain.SelectionLength > 0; }
		}

		public string Filename
		{
			get { return m_sFilename; }
		}

		private const int kdypMinResults = 150;
		private const int kdypSpacing = 3;

		private System.Windows.Forms.StatusBar sbMain;
		private System.Windows.Forms.StatusBarPanel sbpStatus;
		private System.Windows.Forms.StatusBarPanel sbpServer;
		private System.Windows.Forms.StatusBarPanel sbpUsername;
		private System.Windows.Forms.StatusBarPanel sbpDatabase;
		private System.Windows.Forms.StatusBarPanel sbpTime;
		private System.Windows.Forms.StatusBarPanel sbpRows;
		private System.Windows.Forms.StatusBarPanel sbpPosition;
		private SqlTextBox stbMain;
		private System.Windows.Forms.RichTextBox rtxtResults;
		private System.Windows.Forms.Splitter sptMain;
		private System.Windows.Forms.Panel panelBottom;
		private System.Windows.Forms.Panel panelGrids;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;


		public frmConnection(SqlConnection oConn, string sUsername)
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			m_oConn = oConn;
			m_sUsername = sUsername;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
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
			this.sbMain = new System.Windows.Forms.StatusBar();
			this.sbpStatus = new System.Windows.Forms.StatusBarPanel();
			this.sbpServer = new System.Windows.Forms.StatusBarPanel();
			this.sbpUsername = new System.Windows.Forms.StatusBarPanel();
			this.sbpDatabase = new System.Windows.Forms.StatusBarPanel();
			this.sbpTime = new System.Windows.Forms.StatusBarPanel();
			this.sbpRows = new System.Windows.Forms.StatusBarPanel();
			this.sbpPosition = new System.Windows.Forms.StatusBarPanel();
			this.stbMain = new dbmt.SqlTextBox();
			this.rtxtResults = new System.Windows.Forms.RichTextBox();
			this.sptMain = new System.Windows.Forms.Splitter();
			this.panelBottom = new System.Windows.Forms.Panel();
			this.panelGrids = new System.Windows.Forms.Panel();
			((System.ComponentModel.ISupportInitialize)(this.sbpStatus)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.sbpServer)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.sbpUsername)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.sbpDatabase)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.sbpTime)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.sbpRows)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.sbpPosition)).BeginInit();
			this.panelBottom.SuspendLayout();
			this.SuspendLayout();
			//
			// sbMain
			//
			this.sbMain.Location = new System.Drawing.Point(0, 455);
			this.sbMain.Name = "sbMain";
			this.sbMain.Panels.AddRange(new System.Windows.Forms.StatusBarPanel[] {
																					  this.sbpStatus,
																					  this.sbpServer,
																					  this.sbpUsername,
																					  this.sbpDatabase,
																					  this.sbpTime,
																					  this.sbpRows,
																					  this.sbpPosition});
			this.sbMain.ShowPanels = true;
			this.sbMain.Size = new System.Drawing.Size(608, 22);
			this.sbMain.TabIndex = 3;
			//
			// sbpStatus
			//
			this.sbpStatus.AutoSize = System.Windows.Forms.StatusBarPanelAutoSize.Spring;
			this.sbpStatus.Width = 532;
			//
			// sbpServer
			//
			this.sbpServer.AutoSize = System.Windows.Forms.StatusBarPanelAutoSize.Contents;
			this.sbpServer.Width = 10;
			//
			// sbpUsername
			//
			this.sbpUsername.AutoSize = System.Windows.Forms.StatusBarPanelAutoSize.Contents;
			this.sbpUsername.Width = 10;
			//
			// sbpDatabase
			//
			this.sbpDatabase.AutoSize = System.Windows.Forms.StatusBarPanelAutoSize.Contents;
			this.sbpDatabase.Width = 10;
			//
			// sbpTime
			//
			this.sbpTime.AutoSize = System.Windows.Forms.StatusBarPanelAutoSize.Contents;
			this.sbpTime.Width = 10;
			//
			// sbpRows
			//
			this.sbpRows.AutoSize = System.Windows.Forms.StatusBarPanelAutoSize.Contents;
			this.sbpRows.Width = 10;
			//
			// sbpPosition
			//
			this.sbpPosition.AutoSize = System.Windows.Forms.StatusBarPanelAutoSize.Contents;
			this.sbpPosition.Width = 10;
			//
			// stbMain
			//
			this.stbMain.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.stbMain.Dock = System.Windows.Forms.DockStyle.Top;
			this.stbMain.Location = new System.Drawing.Point(0, 0);
			this.stbMain.Name = "stbMain";
			this.stbMain.Size = new System.Drawing.Size(608, 264);
			this.stbMain.TabIndex = 0;
			this.stbMain.PositionChanged += new System.EventHandler(this.stbMain_PositionChanged);
			this.stbMain.DocumentChanged += new System.EventHandler(this.stbMain_DocumentChanged);
			//
			// rtxtResults
			//
			this.rtxtResults.Font = new System.Drawing.Font("Courier New", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.rtxtResults.Location = new System.Drawing.Point(320, 56);
			this.rtxtResults.Name = "rtxtResults";
			this.rtxtResults.ReadOnly = true;
			this.rtxtResults.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
			this.rtxtResults.Size = new System.Drawing.Size(184, 120);
			this.rtxtResults.TabIndex = 1;
			this.rtxtResults.Text = "";
			this.rtxtResults.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.rtxtResults_KeyPress);
			//
			// sptMain
			//
			this.sptMain.Cursor = System.Windows.Forms.Cursors.HSplit;
			this.sptMain.Dock = System.Windows.Forms.DockStyle.Top;
			this.sptMain.Location = new System.Drawing.Point(0, 264);
			this.sptMain.MinSize = 50;
			this.sptMain.Name = "sptMain";
			this.sptMain.Size = new System.Drawing.Size(608, 4);
			this.sptMain.TabIndex = 2;
			this.sptMain.TabStop = false;
			//
			// panelBottom
			//
			this.panelBottom.Controls.Add(this.panelGrids);
			this.panelBottom.Controls.Add(this.rtxtResults);
			this.panelBottom.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelBottom.Location = new System.Drawing.Point(0, 268);
			this.panelBottom.Name = "panelBottom";
			this.panelBottom.Size = new System.Drawing.Size(608, 187);
			this.panelBottom.TabIndex = 1;
			//
			// panelGrids
			//
			this.panelGrids.AutoScroll = true;
			this.panelGrids.Location = new System.Drawing.Point(40, 24);
			this.panelGrids.Name = "panelGrids";
			this.panelGrids.Size = new System.Drawing.Size(304, 104);
			this.panelGrids.TabIndex = 0;
			this.panelGrids.SizeChanged += new EventHandler(Grids_SizeChanged);
			this.panelGrids.MouseDown += new MouseEventHandler(Grids_MouseDown);
			this.panelGrids.MouseMove += new MouseEventHandler(Grids_MouseMove);
			this.panelGrids.MouseUp += new MouseEventHandler(Grids_MouseUp);
			//
			// frmConnection
			//
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(608, 477);
			this.Controls.Add(this.panelBottom);
			this.Controls.Add(this.sptMain);
			this.Controls.Add(this.stbMain);
			this.Controls.Add(this.sbMain);
			this.Name = "frmConnection";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Query";
			this.Closing += new System.ComponentModel.CancelEventHandler(this.frmConnection_Closing);
			this.Load += new System.EventHandler(this.frmConnection_Load);
			this.Closed += new System.EventHandler(this.frmConnection_Closed);
			this.Activated += new System.EventHandler(this.frmConnection_Activated);
			((System.ComponentModel.ISupportInitialize)(this.sbpStatus)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.sbpServer)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.sbpUsername)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.sbpDatabase)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.sbpTime)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.sbpRows)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.sbpPosition)).EndInit();
			this.panelBottom.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private void frmConnection_Load(object sender, System.EventArgs e)
		{
			this.Icon = Globals.MainForm.Icon;

			sbpServer.Text = m_oConn.DataSource;
			sbpDatabase.Text = m_oConn.Database;
			sbpUsername.Text = m_sUsername;
			sbpTime.Text = "0:00:00";
			sbpRows.Text = "0 rows";
			sbpPosition.Text = "Ln 1, Col 1";

			m_sFilename = "Untitled" + Globals.IncrementWindows();

			UpdateCaption();

			sptMain.Visible = false;
			panelBottom.Visible = false;
			stbMain.Dock = DockStyle.Fill;

			panelGrids.Dock = System.Windows.Forms.DockStyle.Fill;
			rtxtResults.Dock = System.Windows.Forms.DockStyle.Fill;
			rtxtResults.BringToFront();

			if (Globals.MainForm.MdiChildren.Length == 1)
				this.WindowState = FormWindowState.Maximized;

			LoadStoredProcedures();
			LoadFunctions();
		}

		private void stbMain_PositionChanged(object sender, EventArgs e)
		{
			sbpPosition.Text = "Ln " + (stbMain.CurrentLine + 1) + ", " +
				"Col " + (stbMain.CurrentCol + 1);
		}

		private string GetTimeText(long cTicks)
		{
			long cSecTotal = (int)(cTicks / 10000000);
			int cHours = (int)(cSecTotal / 3600);
			int cMinutes = (int)((cSecTotal % 3600) / 60);
			int cSeconds = (int)(cSecTotal % 60);
			return cHours + ":" + ((cMinutes < 10) ? ("0" + cMinutes) : cMinutes.ToString()) + ":" +
				((cSeconds < 10) ? ("0" + cSeconds) : cSeconds.ToString());
		}

		private void rtxtResults_KeyPress(object sender, KeyPressEventArgs e)
		{
			e.Handled = true;
		}

		public void DoCancelExecute()
		{
			if (MessageBox.Show(Globals.MainForm,
				"Are you sure you want to cancel the query execution?",
				Globals.ksAppTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) ==
				DialogResult.Yes)
			{
				m_fCancelExecute = true;
			}
		}

		public void DoCut()
		{
			SendKeys.Send("^x");
		}

		public void DoCopy()
		{
			SendKeys.Send("^c");
		}

		public void DoDatabaseChange(string sNewDatabase)
		{
			try
			{
				if (m_oConn.Database == sNewDatabase)
					return;

				m_oConn.ChangeDatabase(sNewDatabase);
				sbpDatabase.Text = m_oConn.Database;

				UpdateCaption();
			}
			catch (Exception oEx)
			{
				MessageBox.Show(this,
					"The current database could not be changed to " + sNewDatabase + ".\n\n" +
					oEx.ToString(), Globals.ksAppTitle,
					MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		private void ExecuteQuery()
		{
			m_fIsExecuting = true;

			try
			{
				string ssql = (stbMain.SelectionLength == 0 ? stbMain.Text : stbMain.SelectedText);
				SqlDataAdapter oAdapter = new SqlDataAdapter(ssql, m_oConn);
				m_oDataSet = new DataSet();

				oAdapter.SelectCommand.CommandTimeout = 0;
				m_sbMessages = new StringBuilder();
				m_oConn.InfoMessage += new SqlInfoMessageEventHandler(Conn_InfoMessage);
				oAdapter.Fill(m_oDataSet);
			}
			catch (Exception oEx)
			{
				m_fExecuteError = true;
				m_sLastError = oEx.Message;
			}

			m_fIsExecuting = false;
		}

		void Conn_InfoMessage(object sender, SqlInfoMessageEventArgs e)
		{
			m_sbMessages.AppendLine(e.Message);
		}

		public void DoExecute()
		{
			if (stbMain.Text.Length == 0)
				return;

			if (!panelBottom.Visible)
			{
				stbMain.Dock = DockStyle.Top;
				sptMain.Visible = true;
				panelBottom.Visible = true;
			}

			rtxtResults.Clear();
			rtxtResults.BringToFront();

			for (int iControl = panelGrids.Controls.Count; --iControl >= 0; )
				panelGrids.Controls.RemoveAt(iControl);

			m_oDataSet = null;
			m_fExecuteError = false;
			m_sLastError = "";
			sbpStatus.Text = "Executing query batch...";
			sbpTime.Text = GetTimeText(0);
			sbpRows.Text = "0 rows";

			try
			{
				if (m_oConn.State != ConnectionState.Open)
				{
					m_oConn.Open();
					m_oConn.ChangeDatabase(sbpDatabase.Text);
				}

				m_fCancelExecute = false;

				long cTickStart = DateTime.Now.Ticks;

				System.Threading.Thread oThread = new System.Threading.Thread(new System.Threading.ThreadStart(ExecuteQuery));
				oThread.Start();

				int cRows = 0;

				string sLastTime = "";
				while (oThread.IsAlive)
				{
					string sTime = GetTimeText(DateTime.Now.Ticks - cTickStart);
					if (sTime != sLastTime)
					{
						Globals.MainForm.UpdateExecuteState(this);
						sbpTime.Text = sTime;

						cRows = 0;
						if (m_oDataSet != null)
						{
							foreach (DataTable oTable in m_oDataSet.Tables)
								cRows += oTable.Rows.Count;
							if (cRows == 1)
								sbpRows.Text = "1 row";
							else
								sbpRows.Text = cRows.ToString("#,##0") + " rows";
						}

						sLastTime = sTime;
					}

					if (m_fCancelExecute)
					{
						try
						{
							oThread.Abort();
						}
						catch (Exception oEx)
						{
							string s = oEx.ToString();
							s = "";
						}
						m_fIsExecuting = false;
						Globals.MainForm.UpdateExecuteState(this);
					}
					else
					{
						Application.DoEvents();
						System.Threading.Thread.Sleep(200);
					}
				}

				int cTables = m_oDataSet.Tables.Count;
				int dypGrid = (cTables == 0) ? 0 : panelGrids.Height / cTables;
				if (dypGrid < kdypMinResults)
					dypGrid = kdypMinResults;
				panelGrids.AutoScrollMinSize = new Size(0, dypGrid * cTables);

				//for (int iTable = cTables; --iTable >= 0; )
				for (int iTable = 0; iTable < cTables; iTable++)
				{
					DataTable oTable = m_oDataSet.Tables[iTable];

					NumberedDataGrid oGrid = new NumberedDataGrid();
					oGrid.AllowSorting = false;
					oGrid.Size = new System.Drawing.Size(panelGrids.Width, dypGrid);
					oGrid.BackgroundColor = System.Drawing.SystemColors.Window;
					oGrid.CaptionVisible = false;
					oGrid.HeaderForeColor = System.Drawing.SystemColors.ControlText;
					oGrid.ReadOnly = true;
					oGrid.RowHeadersVisible = false;
					//oGrid.Resize += new EventHandler(Grid_Resize);
					panelGrids.Resize += new EventHandler(Grid_Resize);

					panelGrids.Controls.Add(oGrid);
					oGrid.Location = new Point(0, iTable * (dypGrid + kdypSpacing));

					oTable.DefaultView.AllowNew = false;
					oTable.DefaultView.AllowEdit = false;
					oTable.DefaultView.AllowDelete = false;

					DataGridTableStyle oTableStyle = new DataGridTableStyle();
					oTableStyle.RowHeadersVisible = true;
					oTableStyle.ReadOnly = true;
					oTableStyle.MappingName = oTable.TableName;
					oTableStyle.AllowSorting = false;

					foreach (DataColumn oColumn in oTable.Columns)
					{
						DataGridColumnStyle oColStyle = GetColStyle(oColumn);
						oTableStyle.GridColumnStyles.Add(oColStyle);
					}

					oGrid.TableStyles.Add(oTableStyle);
					oGrid.DataSource = oTable;
				}

				if (cTables == 0)
				{
					if (m_sbMessages.Length == 0)
					{
						SqlCommand oCommand = m_oConn.CreateCommand();
						oCommand.CommandText = "select @@rowcount";
						int cRecords = (int)oCommand.ExecuteScalar();
						if (cRecords == 1)
							m_sbMessages.AppendLine("(1 row affected)");
						else
							m_sbMessages.AppendLine("(" + cRecords.ToString("#,##0") + " rows affected)");
					}
					rtxtResults.Text = m_sbMessages.ToString();
					rtxtResults.BringToFront();
				}
				else
				{
					panelGrids.BringToFront();
				}

				sbpTime.Text = GetTimeText(DateTime.Now.Ticks - cTickStart);

				cRows = 0;
				foreach (DataTable oTable in m_oDataSet.Tables)
					cRows += oTable.Rows.Count;
				if (cRows == 1)
					sbpRows.Text = "1 row";
				else
					sbpRows.Text = cRows.ToString("#,##0") + " rows";

				// Since the database might have changed, update it.
				if (m_oConn.State == ConnectionState.Open)
					sbpDatabase.Text = m_oConn.Database;

				if (m_fCancelExecute)
					sbpStatus.Text = "Query batch was cancelled";
				else
					sbpStatus.Text = "Query batch completed";
			}
			catch (Exception oEx)
			{
				m_sLastError = oEx.Message;
			}

			if (m_fExecuteError && !m_fCancelExecute)
			{
				sbpStatus.Text = "Query batch completed with errors";
				rtxtResults.Clear();
				rtxtResults.SelectionColor = System.Drawing.Color.Red;
				rtxtResults.SelectedText = m_sLastError;
				rtxtResults.BringToFront();
			}

			m_fIsExecuting = false;
		}

		void Grids_MouseUp(object sender, MouseEventArgs e)
		{
			if (m_fResizingGrids)
			{
				panelGrids.Capture = false;
				m_fResizingGrids = false;
				m_oResizingGrid = null;
			}
		}

		void Grids_MouseMove(object sender, MouseEventArgs e)
		{
			Cursor.Current = Cursors.HSplit;

			if (!m_fResizingGrids || m_oResizingGrid == null)
				return;

			Point pt = panelGrids.PointToClient(Cursor.Position);
			if (pt.Y < m_oResizingGrid.Top + kdypMinResults)
				return;

			int dypHeight = 0;
			int dyp = m_oResizingGrid.Height - (pt.Y - m_oResizingGrid.Top);
			m_oResizingGrid.Height = m_oResizingGrid.Height - dyp;
			foreach (Control oControl in panelGrids.Controls)
			{
				if (oControl.Top > m_oResizingGrid.Top && oControl != m_oResizingGrid)
					oControl.Top = oControl.Top - dyp;
				dypHeight += GetHeight(oControl);
			}

			panelGrids.AutoScrollMinSize = new Size(0, dypHeight);
		}

		void Grids_MouseDown(object sender, MouseEventArgs e)
		{
			m_fResizingGrids = true;
			panelGrids.Capture = true;

			Point pt = panelGrids.PointToClient(Cursor.Position);
			int ypLastMax = -1;
			foreach (Control oControl in panelGrids.Controls)
			{
				if (oControl is DataGrid && oControl.Top > ypLastMax && oControl.Top < pt.Y)
				{
					ypLastMax = oControl.Top;
					m_oResizingGrid = (DataGrid)oControl;
				}
			}
		}

		void Grids_SizeChanged(object sender, EventArgs e)
		{
			DataGrid oLastGrid = null;
			foreach (Control oControl in panelGrids.Controls)
			{
				oControl.Width = panelGrids.Width;
				if (oControl is DataGrid)
					oLastGrid = (DataGrid)oControl;
			}

			if (oLastGrid != null)
				oLastGrid.Height = panelGrids.Height - oLastGrid.Top;
		}

		protected DataGridColumnStyle GetColStyle(DataColumn oColumn)
		{
			DataGridColumnStyle oColStyle = null;
			if (oColumn.DataType == Type.GetType("System.Byte[]"))
				oColStyle = new DataGridReadOnlyByteColumn();
			else
				oColStyle = new DataGridReadOnlyColumn();
			oColStyle.MappingName = oColumn.ColumnName;
			oColStyle.HeaderText = oColumn.ColumnName;
			oColStyle.NullText = Globals.GridNullValue;
			return oColStyle;
		}

		public void DoFind()
		{
		}

		public void DoNew()
		{
			Globals.OpenConnection(this, m_sUsername, m_oConn.DataSource,
				m_oConn.ConnectionString, false, m_oConn.Database);
		}

		public bool DoOpen()
		{
			OpenFileDialogWithEncoding oOpenDlg = new OpenFileDialogWithEncoding();
			oOpenDlg.FileName = m_sFilename;
			oOpenDlg.Title = "Open Query File";
			oOpenDlg.Filter =
				"Query Files (*.sql)|*.sql|" +
				"All Files (*.*)|*.*";

			if (oOpenDlg.ShowDialog(Globals.MainForm) == DialogResult.OK)
			{
				StreamReader oStream = new StreamReader(oOpenDlg.FileName, true);
				string sQueryText = oStream.ReadToEnd();
				oStream.Close();
				stbMain.Text = sQueryText;
				m_sFilename = oOpenDlg.FileName;
				UpdateCaption();
			}

			return false;
		}

		public void DoPaste()
		{
			SendKeys.Send("^v");
		}

		public bool DoSave()
		{
			string sFilename = m_sFilename;

			try
			{
				SaveFileDialogWithEncoding oSaveDlg = new SaveFileDialogWithEncoding();
				oSaveDlg.FileName = sFilename;
				//if (true)
				{
					oSaveDlg.Title = "Save Query As";
					oSaveDlg.Filter =
						"Query Files (*.sql)|*.sql|" +
						"All Files (*.*)|*.*";
				}
				// TODO: Fix this.
				/*else
				{
					oSaveDlg.Title = "Save Results As";
					oSaveDlg.Filter =
						"Query Files (*.sql)|*.sql|" +
						"All Files (*.*)|*.*";
				}*/

				if (oSaveDlg.ShowDialog(Globals.MainForm) == DialogResult.OK)
				{
					sFilename = oSaveDlg.FileName;
					if (oSaveDlg.EncodingType == EncodingType.ANSI)
						Globals.SaveFile(sFilename, stbMain.Text, System.Text.Encoding.Default);
					else if (oSaveDlg.EncodingType == EncodingType.UTF8)
						Globals.SaveFile(sFilename, stbMain.Text, System.Text.Encoding.UTF8);
					else if (oSaveDlg.EncodingType == EncodingType.Unicode)
						Globals.SaveFile(sFilename, stbMain.Text, System.Text.Encoding.Unicode);

					m_sFilename = sFilename;
					UpdateCaption();
				}
			}
			catch (Exception oEx)
			{
				MessageBox.Show(this,
					"The query text could not be saved to " + sFilename + ".\n\n" +
					oEx.ToString(), Globals.ksAppTitle,
					MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
			return false;
		}

		private void frmConnection_Activated(object sender, EventArgs e)
		{
			Globals.MainForm.SetActiveConnection(m_oConn);
		}

		private void Grid_Resize(object sender, EventArgs e)
		{
			int dypHeight = 0;
			foreach (Control oControl in this.panelGrids.Controls)
				dypHeight += GetHeight(oControl);
			panelGrids.AutoScrollMinSize = new Size(0, dypHeight);
		}

		private int GetHeight(Control oControl)
		{
			if (oControl is NumberedDataGrid)
				return (oControl.Height < kdypMinResults) ? kdypMinResults : oControl.Height;

			int dypHeight = 0;
			foreach (Control oSubControl in oControl.Controls)
			{
				if (oSubControl is Panel || oSubControl is NumberedDataGrid)
					dypHeight += GetHeight(oSubControl);
				if (oSubControl is Splitter)
					dypHeight += oSubControl.Height;
			}
			return dypHeight;
		}

		private void LoadStoredProcedures()
		{
			SqlDataReader oReader = null;
			try
			{
				SqlCommand oCommand = m_oConn.CreateCommand();
				oCommand.CommandText =
					"select name from master.dbo.sysobjects where type in ('p', 'x')";
				oReader = oCommand.ExecuteReader();
				while (oReader.Read())
					stbMain.AddKeyword((string)oReader[0], System.Drawing.Color.Brown, false, false);
				oReader.Close();

				oCommand.CommandText =
					"select name from master.dbo.sysobjects where type = 's'";
				oReader = oCommand.ExecuteReader();
				while (oReader.Read())
					stbMain.AddKeyword((string)oReader[0], System.Drawing.Color.FromArgb(0, 174, 0), false, false);
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

		private void LoadFunctions()
		{
			/*string [] sFunctions = new string[] {
				"@@DATEFIRST", "@@OPTIONS", "@@DBTS", "@@REMSERVER", "@@LANGID",
				"@@SERVERNAME", "@@LANGUAGE", "@@SERVICENAME", "@@LOCK_TIMEOUT",
				"@@SPID", "@@MAX_CONNECTIONS", "@@TEXTSIZE", "@@MAX_PRECISION",
				"@@VERSION", "@@NESTLEVEL", "@@CURSOR_ROWS", "CURSOR_STATUS", "@@FETCH_STATUS",
				"DATEADD", "DATEDIFF", "DATENAME", "DATEPART", "DAY", "GETDATE",
				"GETUTCDATE", "MONTH", "YEAR",
				"ABS", "DEGREES", "RAND", "ACOS", "EXP", "ROUND", "ASIN", "FLOOR", "SIGN",
				"ATAN", "LOG", "SIN", "ATN2", "LOG10", "SQUARE", "CEILING", "PI", "SQRT",
				"COS", "POWER", "TAN", "COT", "RADIANS",
				"COL_LENGTH", "fn_listextendedproperty", "COL_NAME", "FULLTEXTCATALOGPROPERTY",
				"COLUMNPROPERTY", "FULLTEXTSERVICEPROPERTY", "DATABASEPROPERTY", "INDEX_COL",
				"DATABASEPROPERTYEX", "INDEXKEY_PROPERTY", "DB_ID", "INDEXPROPERTY",
				"DB_NAME", "OBJECT_ID", "FILE_ID", "OBJECT_NAME", "FILE_NAME", "OBJECTPROPERTY",
				"FILEGROUP_ID", "@@PROCID", "FILEGROUP_NAME", "SQL_VARIANT_PROPERTY",
				"FILEGROUPPROPERTY", "TYPEPROPERTY", "FILEPROPERTY",
				"fn_trace_geteventinfo", "IS_SRVROLEMEMBER", "fn_trace_getfilterinfo",
				"SUSER_SID", "fn_trace_getinfo", "SUSER_SNAME", "fn_trace_gettable", "USER_ID",
				"HAS_DBACCESS", "USER", "IS_MEMBER",
				"ASCII", "NCHAR", "SOUNDEX", "CHAR", "PATINDEX", "SPACE", "CHARINDEX",
				"REPLACE", "STR", "DIFFERENCE", "QUOTENAME", "STUFF", "LEFT", "REPLICATE",
				"SUBSTRING", "LEN", "REVERSE", "UNICODE", "LOWER", "RIGHT", "UPPER",
				"LTRIM", "RTRIM",
				"APP_NAME", "CASE", "CAST", "CONVERT", "COALESCE",
				"COLLATIONPROPERTY", "CURRENT_TIMESTAMP", "CURRENT_USER",
				"DATALENGTH", "@@ERROR", "fn_helpcollations", "fn_servershareddrives",
				"fn_virtualfilestats", "FORMATMESSAGE", "GETANSINULL",
				"HOST_ID", "HOST_NAME", "IDENT_CURRENT", "IDENT_INCR",
				"IDENT_SEED", "@@IDENTITY", "IDENTITY", "ISDATE", "ISNULL",
				"ISNUMERIC", "NEWID", "NULLIF", "PARSENAME", "PERMISSIONS",
				"@@ROWCOUNT", "ROWCOUNT_BIG", "SCOPE_IDENTITY", "SERVERPROPERTY",
				"SESSIONPROPERTY", "SESSION_USER", "STATS_DATE", "SYSTEM_USER",
				"@@TRANCOUNT", "USER_NAME",
				"@@CONNECTIONS", "@@PACK_RECEIVED", "@@CPU_BUSY", "@@PACK_SENT",
				"fn_virtualfilestats", "@@TIMETICKS", "@@IDLE", "@@TOTAL_ERRORS",
				"@@IO_BUSY", "@@TOTAL_READ", "@@PACKET_ERRORS", "@@TOTAL_WRITE",
				"PATINDEX", "TEXTPTR", "TEXTVALID"
				};
			foreach (string sFunction in sFunctions)
				stbMain.AddKeyword(sFunction, System.Drawing.Color.Red, false, false);*/
		}

		private void frmConnection_Closed(object sender, EventArgs e)
		{
			Globals.MainForm.SetActiveConnection(null);

			RegistryKey oKey = null;

			try
			{
				string sServer = m_oConn.DataSource.ToLower();
				sServer = sServer.Replace(SystemInformation.ComputerName.ToLower(), ".");
				oKey = Registry.CurrentUser.CreateSubKey(Globals.ksRegKeyRoot + @"\Databases");
				oKey.SetValue(sServer, m_oConn.Database);
			}
			finally
			{
				if (oKey != null)
					oKey.Close();
			}

			try
			{
				if (m_oConn != null)
				{
					m_oConn.Close();
					m_oConn = null;
				}
			}
			catch
			{
				// Ignore error.
			}
		}

		private void UpdateCaption()
		{
			string sCaption =
				"Query - " +
				m_oConn.DataSource + "." + m_oConn.Database + "." + m_sUsername + " - " +
				m_sFilename;
			if (stbMain.CanUndo)
				sCaption += "*";
			this.Text = sCaption;
		}

		private void stbMain_DocumentChanged(object sender, EventArgs e)
		{
			UpdateCaption();
		}

		public bool CanCloseConnection()
		{
			if (!this.IsModified)
				return true;

			this.Activate();

			DialogResult dr = MessageBox.Show(Globals.MainForm,
				"The text in " + m_sFilename + " has changed.\n\n" +
				"Do you want to save the changes?",
				Globals.ksAppTitle,
				MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
			if (dr == DialogResult.Cancel)
				return false;
			if (dr == DialogResult.Yes)
				return DoSave();

			return true;
		}

		private void frmConnection_Closing(object sender, CancelEventArgs e)
		{
			//if (!Globals.MainForm.IsAppClosing)
				e.Cancel = !CanCloseConnection();
		}
	}
}
