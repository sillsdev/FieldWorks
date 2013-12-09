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
// Responsibility: FW Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Data.SqlClient;

using SIL.Utils;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.Common.FwUtils;
using XCore;
using SIL.FieldWorks.FDO;
using System.IO;
using System.Diagnostics;

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
		private ListBox m_lstProjects;
		private ListBox m_lstProjectsInUse;
		private IHelpTopicProvider m_helpTopicProvider;
		private Button m_btnDelete;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FwDeleteProjectDlg"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwDeleteProjectDlg(ICollection<string> projectsOpen)
		{
			AccessibleName = GetType().Name;
			InitializeComponent();

			// Get information on all the projects in the current server.
			IEnumerable<ProjectInfo> projectList = GetLocalProjects(projectsOpen);

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

		private static IEnumerable<ProjectInfo> GetLocalProjects(ICollection<string> projectsOpen)
		{
			// ProjectInfo.AllProjects doesn't set the InUse flag, which is why we
			// pass a list of open projects to the dialog constructor.
			List<ProjectInfo> projectList = ProjectInfo.AllProjects;
			foreach (ProjectInfo info in projectList.Where(info => projectsOpen.Contains(info.DatabaseName)))
				info.InUse = true;
			return projectList;
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
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
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
			resources.ApplyResources(m_btnExit, "m_btnExit");
			m_btnExit.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			m_btnExit.Name = "m_btnExit";
			m_helpProvider.SetShowHelp(m_btnExit, ((bool)(resources.GetObject("m_btnExit.ShowHelp"))));
			//
			// m_btnHelp
			//
			resources.ApplyResources(m_btnHelp, "m_btnHelp");
			m_helpProvider.SetHelpString(m_btnHelp, resources.GetString("m_btnHelp.HelpString"));
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
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
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

			// Make a copy of the selected items so we can iterate through them, deleting
			// from the list box and maintaining the integrity of the collection being iterated.
			List<ProjectInfo> itemsToDelete = new List<ProjectInfo>();
			foreach (ProjectInfo info in m_lstProjects.SelectedItems)
				itemsToDelete.Add(info);
			foreach (ProjectInfo info in itemsToDelete)
			{
				string folder = Path.Combine(DirectoryFinder.ProjectsDirectory, info.DatabaseName);
				bool fExtraData = CheckForExtraData(info, folder);
				string msg;
				MessageBoxButtons buttons;
				if (fExtraData)
				{
					msg = ResourceHelper.FormatResourceString("kstidDeleteProjFolder", info.DatabaseName);
					buttons = MessageBoxButtons.YesNoCancel;
				}
				else
				{
					msg = ResourceHelper.FormatResourceString("kstidConfirmDeleteProject", info.DatabaseName);
					buttons = MessageBoxButtons.OKCancel;
				}
				DialogResult result = MessageBox.Show(msg,
					ResourceHelper.GetResourceString("kstidDeleteProjCaption"),
					buttons);
				if (result == DialogResult.Cancel)
					continue;
				try
				{
					if (result == DialogResult.Yes || result == DialogResult.OK)
					{
						Directory.Delete(folder, true);
					}
					else
					{
						string path = Path.Combine(folder, info.DatabaseName + FdoFileHelper.ksFwDataXmlFileExtension);
						if (File.Exists(path))
							File.Delete(path);
						path = Path.ChangeExtension(path, FdoFileHelper.ksFwDataDb4oFileExtension);
						if (File.Exists(path))
							File.Delete(path);
						path = Path.ChangeExtension(path, FdoFileHelper.ksFwDataFallbackFileExtension);
						if (File.Exists(path))
							File.Delete(path);
						path = Path.Combine(folder, DirectoryFinder.ksWritingSystemsDir);
						if (Directory.Exists(path))
							Directory.Delete(path, true);
						path = Path.Combine(folder, DirectoryFinder.ksBackupSettingsDir);
						if (Directory.Exists(path))
							Directory.Delete(path, true);
						path = Path.Combine(folder, DirectoryFinder.ksConfigurationSettingsDir);
						if (Directory.Exists(path))
							Directory.Delete(path, true);
						path = Path.Combine(folder, DirectoryFinder.ksSortSequenceTempDir);
						if (Directory.Exists(path))
							Directory.Delete(path, true);
						string[] folders = Directory.GetDirectories(folder);
						foreach (string dir in folders)
						{
							if (!FolderContainsFiles(dir))
								Directory.Delete(dir, true);
						}
					}
				}
				catch
				{
					MessageBox.Show(this,
						String.Format(ResourceHelper.GetResourceString("kstidDeleteProjError"), info.DatabaseName),
						ResourceHelper.GetResourceString("kstidDeleteProjCaption"),
						MessageBoxButtons.OK);
				}
				m_lstProjects.Items.Remove(info);
			}
		}

		private static bool CheckForExtraData(ProjectInfo info, string folder)
		{
			string[] folders = Directory.GetDirectories(folder);
			foreach (string dir in folders)
			{
				string name = Path.GetFileName(dir);
				if (name == DirectoryFinder.ksWritingSystemsDir ||
					name == DirectoryFinder.ksBackupSettingsDir ||
					name == DirectoryFinder.ksConfigurationSettingsDir ||
					name == DirectoryFinder.ksSortSequenceTempDir)
				{
					continue;
				}
				if (FolderContainsFiles(dir))
					return true;
			}
			string[] files = Directory.GetFiles(folder);
			if (files.Length > 3)
				return true;
			foreach (string filepath in files)
			{
				string file = Path.GetFileName(filepath);
				if (file != info.DatabaseName + FdoFileHelper.ksFwDataXmlFileExtension &&
					file != info.DatabaseName + FdoFileHelper.ksFwDataDb4oFileExtension &&
					file != info.DatabaseName + FdoFileHelper.ksFwDataFallbackFileExtension)
				{
					return true;
				}
			}
			return false;
		}

		private static bool FolderContainsFiles(string folder)
		{
			string[] files = Directory.GetFiles(folder);
			if (files.Length > 0)
				return true;
			string[] folders = Directory.GetDirectories(folder);
			foreach (string dir in folders)
			{
				if (FolderContainsFiles(dir))
					return true;
			}
			return false;
		}
	}
	#endregion
}
