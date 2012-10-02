// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwDeleteProjectDlg.cs
// Responsibility: Edge
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;
using System.Data.SqlClient;

using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FwCoreDlgs
{
	#region FwDeleteProjectDlg class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for FwDeleteProjectDlg.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FwDeleteProjectDlg : Form, IFWDisposable
	{
		private System.Windows.Forms.ListBox m_lstProjects;
		private System.Windows.Forms.ListBox m_lstProjectsInUse;
		private static string m_helpFile = null;
		private IHelpTopicProvider m_helpTopicProvider;
		private Button m_btnDelete;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		#region Properties

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Gets and sets the help file.
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		public static string HelpFile
		{
			get
			{
				return m_helpFile;
			}
			set
			{
				m_helpFile = value;
			}
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FwDeleteProjectDlg"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwDeleteProjectDlg()
		{
			InitializeComponent();

			// Get information on all the projects in the current server.
			List<ProjectInfo> projectList = ProjectInfo.GetProjectInfo();

			// Fill in the list controls
			m_lstProjects.Items.Clear();
			m_lstProjectsInUse.Items.Clear();
			foreach (ProjectInfo info in projectList)
			{
				if (info.InUse)
					m_lstProjectsInUse.Items.Add(info);
				else
					m_lstProjects.Items.Add(info);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the dialog properties object for dialogs that are created.
		/// </summary>
		/// <param name="helpTopicProvider"></param>
		/// ------------------------------------------------------------------------------------
		public void SetDialogProperties(IHelpTopicProvider helpTopicProvider)
		{
			CheckDisposed();

			m_helpTopicProvider = helpTopicProvider;
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

		#region Windows Form Designer generated code
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			System.Windows.Forms.Button m_btnExit;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FwDeleteProjectDlg));
			System.Windows.Forms.Button m_btnHelp;
			System.Windows.Forms.Label label1;
			System.Windows.Forms.Label label2;
			System.Windows.Forms.HelpProvider m_helpProvider;
			this.m_lstProjects = new System.Windows.Forms.ListBox();
			this.m_btnDelete = new System.Windows.Forms.Button();
			this.m_lstProjectsInUse = new System.Windows.Forms.ListBox();
			m_btnExit = new System.Windows.Forms.Button();
			m_btnHelp = new System.Windows.Forms.Button();
			label1 = new System.Windows.Forms.Label();
			label2 = new System.Windows.Forms.Label();
			m_helpProvider = new System.Windows.Forms.HelpProvider();
			this.SuspendLayout();
			//
			// m_btnExit
			//
			m_btnExit.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(m_btnExit, "m_btnExit");
			m_btnExit.Name = "m_btnExit";
			m_helpProvider.SetShowHelp(m_btnExit, ((bool)(resources.GetObject("m_btnExit.ShowHelp"))));
			//
			// m_btnHelp
			//
			m_helpProvider.SetHelpString(m_btnHelp, resources.GetString("m_btnHelp.HelpString"));
			resources.ApplyResources(m_btnHelp, "m_btnHelp");
			m_btnHelp.Name = "m_btnHelp";
			m_helpProvider.SetShowHelp(m_btnHelp, ((bool)(resources.GetObject("m_btnHelp.ShowHelp"))));
			m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// label1
			//
			resources.ApplyResources(label1, "label1");
			label1.Name = "label1";
			m_helpProvider.SetShowHelp(label1, ((bool)(resources.GetObject("label1.ShowHelp"))));
			//
			// label2
			//
			resources.ApplyResources(label2, "label2");
			label2.Name = "label2";
			m_helpProvider.SetShowHelp(label2, ((bool)(resources.GetObject("label2.ShowHelp"))));
			//
			// m_lstProjects
			//
			resources.ApplyResources(this.m_lstProjects, "m_lstProjects");
			this.m_lstProjects.Name = "m_lstProjects";
			this.m_lstProjects.SelectionMode = System.Windows.Forms.SelectionMode.MultiSimple;
			m_helpProvider.SetShowHelp(this.m_lstProjects, ((bool)(resources.GetObject("m_lstProjects.ShowHelp"))));
			this.m_lstProjects.Sorted = true;
			this.m_lstProjects.SelectedIndexChanged += new System.EventHandler(this.m_lstProjects_SelectedIndexChanged);
			//
			// m_btnDelete
			//
			resources.ApplyResources(this.m_btnDelete, "m_btnDelete");
			this.m_btnDelete.Name = "m_btnDelete";
			m_helpProvider.SetShowHelp(this.m_btnDelete, ((bool)(resources.GetObject("m_btnDelete.ShowHelp"))));
			this.m_btnDelete.Click += new System.EventHandler(this.m_btnDelete_Click);
			//
			// m_lstProjectsInUse
			//
			resources.ApplyResources(this.m_lstProjectsInUse, "m_lstProjectsInUse");
			this.m_lstProjectsInUse.Name = "m_lstProjectsInUse";
			this.m_lstProjectsInUse.SelectionMode = System.Windows.Forms.SelectionMode.None;
			m_helpProvider.SetShowHelp(this.m_lstProjectsInUse, ((bool)(resources.GetObject("m_lstProjectsInUse.ShowHelp"))));
			this.m_lstProjectsInUse.Sorted = true;
			//
			// FwDeleteProjectDlg
			//
			resources.ApplyResources(this, "$this");
			this.CancelButton = m_btnExit;
			this.Controls.Add(label2);
			this.Controls.Add(label1);
			this.Controls.Add(this.m_lstProjectsInUse);
			this.Controls.Add(m_btnHelp);
			this.Controls.Add(m_btnExit);
			this.Controls.Add(this.m_btnDelete);
			this.Controls.Add(this.m_lstProjects);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FwDeleteProjectDlg";
			m_helpProvider.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			this.ShowInTaskbar = false;
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_lstProjects_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			m_btnDelete.Enabled = (((ListBox)sender).SelectedItems.Count > 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display help for this dialog box.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_btnHelp_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpDeleteProj");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a click on the Delete button - delete a project.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_btnDelete_Click(object sender, System.EventArgs e)
		{
			if (m_lstProjects.SelectedItems.Count == 0)
				return;

			DialogResult result =
				MessageBox.Show(this, ResourceHelper.GetResourceString("kstidDeleteProjWarning"),
					ResourceHelper.GetResourceString("kstidDeleteProjCaption"),
					MessageBoxButtons.YesNo);
			if (result == DialogResult.Yes)
			{
				SqlConnection connection = null;
				string databasename = string.Empty;
				try
				{
					string sSql = string.Format("Server={0}; Database=master; User ID=FWDeveloper;" +
						" Password=careful; Pooling=false;", MiscUtils.LocalServerName);
					connection = new SqlConnection(sSql);
					connection.Open();
					// Make a copy of the selected items so we can iterate through them, deleting
					// from the list box and maintaining the integrity of the collection being iterated.
					List<ProjectInfo> itemsToDelete = new List<ProjectInfo>();
					foreach (ProjectInfo info in m_lstProjects.SelectedItems)
					{
						itemsToDelete.Add(info);
					}
					foreach (ProjectInfo info in itemsToDelete)
					{
						databasename = info.DatabaseName;
						SqlCommand command = connection.CreateCommand();
						command.CommandText = string.Format("DROP DATABASE [{0}]", databasename);
						command.ExecuteNonQuery();
						m_lstProjects.Items.Remove(info);
					}
				}
				catch
				{
					MessageBox.Show(this,
						String.Format(ResourceHelper.GetResourceString("kstidDeleteProjError"), databasename),
						ResourceHelper.GetResourceString("kstidDeleteProjCaption"),
						MessageBoxButtons.OK);
				}
				finally
				{
					if (connection != null)
						connection.Close();
				}
			}
		}
	}
	#endregion
}
