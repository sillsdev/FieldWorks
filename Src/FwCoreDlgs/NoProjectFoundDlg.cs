// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: NoProjectFoundDlg.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices; // needed for Marshal

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Utils;
using SIL.Utils;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for NoProjectFoundDlg.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class NoProjectFoundDlg : Form, IFWDisposable
	{
		private const string s_helpTopic = "khtpNoProjectFound";
		private System.Windows.Forms.HelpProvider helpProvider;
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public enum ButtonPress
		{
			/// <summary></summary>
			Open,
			/// <summary></summary>
			New,
			/// <summary></summary>
			Restore,
			/// <summary></summary>
			Exit,
		}

		#region Data members
		private ButtonPress m_dlgResult = ButtonPress.Exit;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		IHelpTopicProvider m_helpTopicProvider = null;
		#endregion

		#region Construction, Initialization and Deconstruction
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="NoProjectFoundDlg"/> class.
		/// </summary>
		/// <param name="helpTopicProvider"></param>
		/// ------------------------------------------------------------------------------------
		public NoProjectFoundDlg(IHelpTopicProvider helpTopicProvider)
		{
			InitializeComponent();
			//
			// helpProvider
			//
			m_helpTopicProvider = helpTopicProvider;
			this.helpProvider = new System.Windows.Forms.HelpProvider();
			this.helpProvider.HelpNamespace = DirectoryFinder.FWCodeDirectory + m_helpTopicProvider.GetHelpString("UserHelpFile", 0);
			this.helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(s_helpTopic, 0));
			this.helpProvider.SetHelpNavigator(this, System.Windows.Forms.HelpNavigator.Topic);

			// make sure no open ICU files -dlh
			IIcuCleanupManager icm = IcuCleanupManagerClass.Create();
			icm.Cleanup();
			Marshal.ReleaseComObject(icm);
			icm = null;
			Logger.WriteEvent("Opening 'Welcome to FieldWorks' dialog");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
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
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the button that was pressed
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ButtonPress DlgResult
		{
			get
			{
				CheckDisposed();
				return m_dlgResult;
			}
		}
		#endregion

		#region Windows Form Designer generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			System.Windows.Forms.Button m_btnOpen;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NoProjectFoundDlg));
			System.Windows.Forms.Button m_btnNew;
			System.Windows.Forms.Button m_btnRestore;
			System.Windows.Forms.Button m_btnExit;
			System.Windows.Forms.Label label3;
			System.Windows.Forms.Label label5;
			System.Windows.Forms.Label label7;
			System.Windows.Forms.Label label6;
			System.Windows.Forms.Label label8;
			System.Windows.Forms.Button m_btnHelp;
			System.Windows.Forms.Label label1;
			m_btnOpen = new System.Windows.Forms.Button();
			m_btnNew = new System.Windows.Forms.Button();
			m_btnRestore = new System.Windows.Forms.Button();
			m_btnExit = new System.Windows.Forms.Button();
			label3 = new System.Windows.Forms.Label();
			label5 = new System.Windows.Forms.Label();
			label7 = new System.Windows.Forms.Label();
			label6 = new System.Windows.Forms.Label();
			label8 = new System.Windows.Forms.Label();
			m_btnHelp = new System.Windows.Forms.Button();
			label1 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			//
			// m_btnOpen
			//
			resources.ApplyResources(m_btnOpen, "m_btnOpen");
			m_btnOpen.Name = "m_btnOpen";
			m_btnOpen.Click += new System.EventHandler(this.m_btnOpen_Click);
			//
			// m_btnNew
			//
			resources.ApplyResources(m_btnNew, "m_btnNew");
			m_btnNew.Name = "m_btnNew";
			m_btnNew.Click += new System.EventHandler(this.m_btnNew_Click);
			//
			// m_btnRestore
			//
			resources.ApplyResources(m_btnRestore, "m_btnRestore");
			m_btnRestore.Name = "m_btnRestore";
			m_btnRestore.Click += new System.EventHandler(this.m_btnRestore_Click);
			//
			// m_btnExit
			//
			m_btnExit.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(m_btnExit, "m_btnExit");
			m_btnExit.Name = "m_btnExit";
			m_btnExit.Click += new System.EventHandler(this.m_btnExit_Click);
			//
			// label3
			//
			resources.ApplyResources(label3, "label3");
			label3.Name = "label3";
			//
			// label5
			//
			resources.ApplyResources(label5, "label5");
			label5.Name = "label5";
			//
			// label7
			//
			resources.ApplyResources(label7, "label7");
			label7.Name = "label7";
			//
			// label6
			//
			resources.ApplyResources(label6, "label6");
			label6.Name = "label6";
			//
			// label8
			//
			resources.ApplyResources(label8, "label8");
			label8.Name = "label8";
			//
			// m_btnHelp
			//
			resources.ApplyResources(m_btnHelp, "m_btnHelp");
			m_btnHelp.Name = "m_btnHelp";
			m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// label1
			//
			resources.ApplyResources(label1, "label1");
			label1.Name = "label1";
			//
			// NoProjectFoundDlg
			//
			resources.ApplyResources(this, "$this");
			this.CancelButton = m_btnExit;
			this.Controls.Add(label1);
			this.Controls.Add(label8);
			this.Controls.Add(label6);
			this.Controls.Add(label7);
			this.Controls.Add(label5);
			this.Controls.Add(label3);
			this.Controls.Add(m_btnHelp);
			this.Controls.Add(m_btnExit);
			this.Controls.Add(m_btnRestore);
			this.Controls.Add(m_btnNew);
			this.Controls.Add(m_btnOpen);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "NoProjectFoundDlg";
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		#region Overriden methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Log the dialog result
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnClosing(CancelEventArgs e)
		{
			Logger.WriteEvent("Closing dialog: " + m_dlgResult.ToString());
			base.OnClosing (e);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.VisibleChanged"></see> event.
		/// </summary>
		/// <param name="e">The <see cref="T:System.EventArgs"></see> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnVisibleChanged(EventArgs e)
		{
			base.OnVisibleChanged(e);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			base.OnFormClosing(e);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		protected override void OnHandleDestroyed(EventArgs e)
		{
			base.OnHandleDestroyed(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When the dialog is loaded, make sure it gets focused.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
			// This dialog may be created when no other forms are active. Calling Activate will
			// make sure that the dialog comes up visible and activated.
			Activate();
		}
		#endregion

		#region Button click handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_btnOpen_Click(object sender, System.EventArgs e)
		{
			m_dlgResult = ButtonPress.Open;
			Hide();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_btnNew_Click(object sender, System.EventArgs e)
		{
			m_dlgResult = ButtonPress.New;
			Hide();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_btnRestore_Click(object sender, System.EventArgs e)
		{
			m_dlgResult = ButtonPress.Restore;
			Hide();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_btnExit_Click(object sender, System.EventArgs e)
		{
			m_dlgResult = ButtonPress.Exit;
			Hide();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Open the context-sensitive help for this dialog.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_btnHelp_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, s_helpTopic);
		}

		#endregion
	}
}
