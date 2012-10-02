// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: Form1.cs
// Responsibility: John Hatton
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
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;

using SIL.FieldWorks.WordWorks.Parser;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Ling;
using SIL.Utils;

namespace FwParserWatcher
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class Form1 : Form, IFWDisposable
	{
		internal System.Windows.Forms.ColumnHeader Machine;
		internal System.Windows.Forms.Button btnAddLow;
		internal System.Windows.Forms.GroupBox GroupBox1;
		internal System.Windows.Forms.Label Label6;
		internal System.Windows.Forms.Label Label5;
		internal System.Windows.Forms.Label Label2;
		internal System.Windows.Forms.ColumnHeader Language;
		internal System.Windows.Forms.ColumnHeader Status;
		internal System.Windows.Forms.Label Label1;
		internal System.Windows.Forms.ListView m_lvParsers;
		internal System.Windows.Forms.Button m_btnNewParser;
		internal System.Windows.Forms.Button btnAddMed;
		internal System.Windows.Forms.Button btnAddHigh;
		private System.ComponentModel.IContainer components;
		internal System.Windows.Forms.Label m_medQueueSize;
		internal System.Windows.Forms.Label m_highQueueSize;
		internal System.Windows.Forms.Label m_lowQueueSize;
		private System.Windows.Forms.Timer timerUpdateQueueSize;

		protected List<ParserConnection> m_parsers;
		private System.Windows.Forms.TextBox m_log;
		protected ParserConnection m_selectedParserConnection;
		protected FdoCache m_selectedFdoCache;
		protected TaskReport m_previousTask;
		private System.Windows.Forms.CheckBox m_btnVerbose;
		private System.Windows.Forms.CheckBox m_btnLog;
		private System.Windows.Forms.Button btnTrace;
		private System.Windows.Forms.Button m_btnPause;
		private System.Windows.Forms.Button btnTouchLexicon;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="Form1"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public Form1()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			AccessibleName = GetType().Name;
			m_parsers = new List<ParserConnection>();

			m_selectedFdoCache = FdoCache.Create("TestLangProj");

			// per KenZ, convention is for server to already include the \\SILFW,e.g. HATTON1\\SILFW
			//m_parsers.Add(new ParserConnection(".\\SILFW", "test", "test"));
			m_parsers.Add(new ParserConnection(".\\SILFW", "TestLangProj", "TestLangProj", true));
			m_selectedParserConnection = m_parsers[0];
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

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// -----------------------------------------------------------------------------------
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				foreach(ParserConnection connection in m_parsers)
					connection.Dispose();
				m_parsers.Clear();

				if (components != null)
					components.Dispose();

				m_selectedFdoCache.Dispose();
			}
			m_selectedFdoCache = null;
			m_parsers = null;

			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
			this.Machine = new System.Windows.Forms.ColumnHeader();
			this.btnAddLow = new System.Windows.Forms.Button();
			this.GroupBox1 = new System.Windows.Forms.GroupBox();
			this.Label6 = new System.Windows.Forms.Label();
			this.m_medQueueSize = new System.Windows.Forms.Label();
			this.m_highQueueSize = new System.Windows.Forms.Label();
			this.Label5 = new System.Windows.Forms.Label();
			this.m_lowQueueSize = new System.Windows.Forms.Label();
			this.Label2 = new System.Windows.Forms.Label();
			this.Language = new System.Windows.Forms.ColumnHeader();
			this.Status = new System.Windows.Forms.ColumnHeader();
			this.Label1 = new System.Windows.Forms.Label();
			this.m_lvParsers = new System.Windows.Forms.ListView();
			this.m_btnNewParser = new System.Windows.Forms.Button();
			this.btnAddMed = new System.Windows.Forms.Button();
			this.btnAddHigh = new System.Windows.Forms.Button();
			this.timerUpdateQueueSize = new System.Windows.Forms.Timer(this.components);
			this.m_log = new System.Windows.Forms.TextBox();
			this.btnTouchLexicon = new System.Windows.Forms.Button();
			this.m_btnVerbose = new System.Windows.Forms.CheckBox();
			this.m_btnLog = new System.Windows.Forms.CheckBox();
			this.btnTrace = new System.Windows.Forms.Button();
			this.m_btnPause = new System.Windows.Forms.Button();
			this.GroupBox1.SuspendLayout();
			this.SuspendLayout();
			//
			// Machine
			//
			resources.ApplyResources(this.Machine, "Machine");
			//
			// btnAddLow
			//
			resources.ApplyResources(this.btnAddLow, "btnAddLow");
			this.btnAddLow.Name = "btnAddLow";
			this.btnAddLow.Click += new System.EventHandler(this.btnAddLow_Click);
			//
			// GroupBox1
			//
			resources.ApplyResources(this.GroupBox1, "GroupBox1");
			this.GroupBox1.Controls.Add(this.Label6);
			this.GroupBox1.Controls.Add(this.m_medQueueSize);
			this.GroupBox1.Controls.Add(this.m_highQueueSize);
			this.GroupBox1.Controls.Add(this.Label5);
			this.GroupBox1.Controls.Add(this.m_lowQueueSize);
			this.GroupBox1.Controls.Add(this.Label2);
			this.GroupBox1.Name = "GroupBox1";
			this.GroupBox1.TabStop = false;
			//
			// Label6
			//
			resources.ApplyResources(this.Label6, "Label6");
			this.Label6.Name = "Label6";
			//
			// m_medQueueSize
			//
			resources.ApplyResources(this.m_medQueueSize, "m_medQueueSize");
			this.m_medQueueSize.Name = "m_medQueueSize";
			//
			// m_highQueueSize
			//
			resources.ApplyResources(this.m_highQueueSize, "m_highQueueSize");
			this.m_highQueueSize.Name = "m_highQueueSize";
			//
			// Label5
			//
			resources.ApplyResources(this.Label5, "Label5");
			this.Label5.Name = "Label5";
			//
			// m_lowQueueSize
			//
			resources.ApplyResources(this.m_lowQueueSize, "m_lowQueueSize");
			this.m_lowQueueSize.Name = "m_lowQueueSize";
			//
			// Label2
			//
			resources.ApplyResources(this.Label2, "Label2");
			this.Label2.Name = "Label2";
			//
			// Language
			//
			resources.ApplyResources(this.Language, "Language");
			//
			// Status
			//
			resources.ApplyResources(this.Status, "Status");
			//
			// Label1
			//
			resources.ApplyResources(this.Label1, "Label1");
			this.Label1.Name = "Label1";
			//
			// m_lvParsers
			//
			resources.ApplyResources(this.m_lvParsers, "m_lvParsers");
			this.m_lvParsers.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.Machine,
			this.Language,
			this.Status});
			this.m_lvParsers.Items.AddRange(new System.Windows.Forms.ListViewItem[] {
			((System.Windows.Forms.ListViewItem)(resources.GetObject("m_lvParsers.Items")))});
			this.m_lvParsers.Name = "m_lvParsers";
			this.m_lvParsers.UseCompatibleStateImageBehavior = false;
			this.m_lvParsers.View = System.Windows.Forms.View.Details;
			this.m_lvParsers.SelectedIndexChanged += new System.EventHandler(this.m_lvParsers_SelectedIndexChanged);
			//
			// m_btnNewParser
			//
			resources.ApplyResources(this.m_btnNewParser, "m_btnNewParser");
			this.m_btnNewParser.Name = "m_btnNewParser";
			//
			// btnAddMed
			//
			resources.ApplyResources(this.btnAddMed, "btnAddMed");
			this.btnAddMed.Name = "btnAddMed";
			this.btnAddMed.Click += new System.EventHandler(this.btnAddMed_Click);
			//
			// btnAddHigh
			//
			resources.ApplyResources(this.btnAddHigh, "btnAddHigh");
			this.btnAddHigh.Name = "btnAddHigh";
			this.btnAddHigh.Click += new System.EventHandler(this.btnAddHigh_Click);
			//
			// timerUpdateQueueSize
			//
			this.timerUpdateQueueSize.Enabled = true;
			this.timerUpdateQueueSize.Tick += new System.EventHandler(this.timerUpdateQueueSize_Tick);
			//
			// m_log
			//
			resources.ApplyResources(this.m_log, "m_log");
			this.m_log.Name = "m_log";
			//
			// btnTouchLexicon
			//
			resources.ApplyResources(this.btnTouchLexicon, "btnTouchLexicon");
			this.btnTouchLexicon.Name = "btnTouchLexicon";
			this.btnTouchLexicon.Click += new System.EventHandler(this.btnTouchLexicon_Click);
			//
			// m_btnVerbose
			//
			this.m_btnVerbose.Checked = true;
			this.m_btnVerbose.CheckState = System.Windows.Forms.CheckState.Checked;
			resources.ApplyResources(this.m_btnVerbose, "m_btnVerbose");
			this.m_btnVerbose.Name = "m_btnVerbose";
			this.m_btnVerbose.CheckedChanged += new System.EventHandler(this.btnVerbose_CheckedChanged);
			//
			// m_btnLog
			//
			this.m_btnLog.Checked = true;
			this.m_btnLog.CheckState = System.Windows.Forms.CheckState.Checked;
			resources.ApplyResources(this.m_btnLog, "m_btnLog");
			this.m_btnLog.Name = "m_btnLog";
			this.m_btnLog.CheckedChanged += new System.EventHandler(this.m_btnLog_CheckedChanged);
			//
			// btnTrace
			//
			resources.ApplyResources(this.btnTrace, "btnTrace");
			this.btnTrace.Name = "btnTrace";
			this.btnTrace.Click += new System.EventHandler(this.btnTrace_Click);
			//
			// m_btnPause
			//
			resources.ApplyResources(this.m_btnPause, "m_btnPause");
			this.m_btnPause.Name = "m_btnPause";
			this.m_btnPause.Click += new System.EventHandler(this.m_btnPause_Click);
			//
			// Form1
			//
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this.m_btnPause);
			this.Controls.Add(this.m_btnLog);
			this.Controls.Add(this.m_btnVerbose);
			this.Controls.Add(this.btnTouchLexicon);
			this.Controls.Add(this.m_log);
			this.Controls.Add(this.btnAddLow);
			this.Controls.Add(this.GroupBox1);
			this.Controls.Add(this.Label1);
			this.Controls.Add(this.m_lvParsers);
			this.Controls.Add(this.m_btnNewParser);
			this.Controls.Add(this.btnAddMed);
			this.Controls.Add(this.btnAddHigh);
			this.Controls.Add(this.btnTrace);
			this.Name = "Form1";
			this.Closing += new System.ComponentModel.CancelEventHandler(this.Form1_Closing);
			this.Load += new System.EventHandler(this.Form1_Load);
			this.GroupBox1.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[STAThread]
		static void Main()
		{
			Application.Run(new Form1());
		}

		private void Form1_Load(object sender, System.EventArgs e)
		{
			SubscribeToEvents();
		}

		private void timerUpdateQueueSize_Tick(object sender, System.EventArgs e)
		{
			if (null ==m_selectedParserConnection)
				return;
			m_lowQueueSize.Text = m_selectedParserConnection.Parser.GetQueueSize(ParserScheduler.Priority.eventually ).ToString();
			m_medQueueSize.Text = m_selectedParserConnection.Parser.GetQueueSize(ParserScheduler.Priority.soon).ToString();
			m_highQueueSize.Text = m_selectedParserConnection.Parser.GetQueueSize(ParserScheduler.Priority.ASAP).ToString();
		}

		private void btnAddLow_Click(object sender, System.EventArgs e)
		{
			AddWords(ParserScheduler.Priority.eventually);
		}

		private void btnAddHigh_Click(object sender, System.EventArgs e)
		{
			AddWords(ParserScheduler.Priority.ASAP);
		}

		private void btnAddMed_Click(object sender, System.EventArgs e)
		{
			AddWords(ParserScheduler.Priority.soon);
		}

		protected void AddWords(ParserScheduler.Priority priority)
		{
			FdoOwningCollection<IWfiWordform> words = m_selectedFdoCache.LangProject.WordformInventoryOA.WordformsOC;
			if (words.Count == 0)
			{
				MessageBox.Show("Can't do that, because there are no wordforms in this project.");
				return;
			}

			Random r = new Random();
			for(int i = 0; i < 10; i++)
			{
				int index = r.Next(0, words.Count - 1);
				int hvo = words.HvoArray[index];
				m_selectedParserConnection.Parser.ScheduleOneWordformForUpdate(hvo, priority);
			}
		}

		//this is invoked by the parser connection, on our own event handling thread.
		protected void ParserUpdateHandler(ParserScheduler parser, TaskReport task)
		{
			if (m_previousTask != task)
			{
				m_log.Text += "\r\n";
				Debug.WriteLine("");
			}

			string pad = "";
			for(int i= task.Depth; i>0;i--)
				pad += "    ";
			m_log.Text+=pad;

			switch(task.Phase)
			{
				case TaskReport.TaskPhase.started:
					m_log.Text += task.Description+" ";//+pad + "{\r\n";
					Debug.Write(task.Description+" ");
					break;
				case TaskReport.TaskPhase.finished:
					if (m_previousTask != task)
						m_log.Text +=" ^ ";
					m_log.Text += task.DurationSeconds.ToString()+ " seconds";
					Debug.Write(task.DurationSeconds.ToString()+ " seconds");
					//m_log.Text += "}\r\n";
					if (task.Details != null)
						m_log.Text += "Details:"+task.Details;

					break;
				default:
					m_log.Text +=  task.Description+"    " + task.PhaseDescription ;//+ "\r\n";
					Debug.Write(task.Description+"    " + task.PhaseDescription);
					break;
			}
			m_log.Select(m_log.Text.Length,0);
			m_log.ScrollToCaret();

			m_previousTask = task;
		}

		private void btnTouchGrammar_Click(object sender, System.EventArgs e)
		{
			MessageBox.Show("Not implemented");
			//m_selectedParserConnection.Parser.PhonyTouchGrammar();
		}

		private void btnTouchLexicon_Click(object sender, System.EventArgs e)
		{
			int hvo = m_selectedFdoCache.LangProject.LexDbOA.Entries.ToHvoArray()[0];
			ILexEntry le = LexEntry.CreateFromDBObject(m_selectedFdoCache, hvo);
			le.HomographNumber ++;
			m_selectedFdoCache.Save();
		}

		private void btnVerbose_CheckedChanged(object sender, System.EventArgs e)
		{
			SubscribeToEvents ();
		}

		protected void SubscribeToEvents()
		{
			if(m_selectedParserConnection!= null)
			{
				m_selectedParserConnection.UnsubscribeToParserEvents();
				if(m_btnLog.Checked)
					m_selectedParserConnection.SubscribeToParserEvents(m_btnVerbose.Checked, new ParserUpdateEventHandler(this.ParserUpdateHandler));

				UpdatePauseButton();
			}
		}

		private void m_lvParsers_SelectedIndexChanged(object sender, System.EventArgs e)
		{

		}

		private void m_btnLog_CheckedChanged(object sender, System.EventArgs e)
		{
			SubscribeToEvents ();
		}

		private void btnTrace_Click(object sender, System.EventArgs e)
		{
			FdoOwningCollection<IWfiWordform> words = m_selectedFdoCache.LangProject.WordformInventoryOA.WordformsOC;
			if (words.Count == 0)
			{
				MessageBox.Show("Can't do that, because there are no wordforms in this project.");
				return;
			}

			IWfiWordform word = WfiWordform.CreateFromDBObject(m_selectedFdoCache, words.HvoArray[0]);
			m_selectedParserConnection.TryAWordAsynchronously(word.Form.VernacularDefaultWritingSystem, true);
		}

		private void Form1_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
		}

		private void UpdatePauseButton()
		{
			if (m_selectedParserConnection == null)
			{
				m_btnPause.Enabled = false;
				m_btnPause.Text = "&Pause Parser";
			}
			else
			{
				m_btnPause.Enabled = true;
				if (m_selectedParserConnection.Parser.IsPaused)
				{
					m_btnPause.Text = "Un&Pause";
				}
				else
				{
					m_btnPause.Text = "&Pause Parser";
				}
			}
		}

		private void m_btnPause_Click(object sender, System.EventArgs e)
		{
			if (m_selectedParserConnection != null)
			{
				if (m_selectedParserConnection.Parser.IsPaused)
					m_selectedParserConnection.Parser.Resume();
				else
				{
					bool paused = m_selectedParserConnection.Parser.AttemptToPause();
				}

				UpdatePauseButton();
			}
		}
	}
}
