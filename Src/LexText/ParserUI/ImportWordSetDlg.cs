// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ImportWordSetDlg.cs
// Responsibility: Andy Black
// Last reviewed:
//
// <remarks>
// Implementation of:
//		ImportWordSetDlg - Dialog for editing XML representation of parser parameters
//                            (MoMorphData : ParserParameters)
// </remarks>
using System;
using System.Windows.Forms;
using System.Text;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Controls;
using SIL.Utils;
using SIL.Utils.FileDialog;
using SIL.FieldWorks.Common.FwUtils;
using XCore;
using SIL.FieldWorks.Resources;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Summary description for ImportWordSetDlg.
	/// </summary>
	public class ImportWordSetDlg : Form, IFWDisposable
	{
		#region Data members

		/// <summary>
		/// xCore Mediator.
		/// </summary>
		protected Mediator m_mediator;
		/// <summary>
		///
		/// </summary>
		protected IHelpTopicProvider m_helpTopicProvider;
		protected FdoCache m_cache;
		protected string[] m_paths;

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button btnCancel;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Button btnChooseFiles;

		private System.Windows.Forms.TextBox tbFileNames;
		private System.Windows.Forms.TextBox tbName;
		private System.Windows.Forms.Button buttonHelp;
		private System.Windows.Forms.Button btnImport;

		private const string s_helpTopic = "khtpImportWordSet";
		private System.Windows.Forms.HelpProvider helpProvider;

		#endregion Data members

		private ImportWordSetDlg()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			AccessibleName = GetType().Name;
		}

		public ImportWordSetDlg(Mediator mediator, PropertyTable propertyTable)
			: this()
		{
			//InitializeComponent();
			m_mediator = mediator;
			m_cache = propertyTable.GetValue<FdoCache>("cache");

			m_helpTopicProvider = propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider");
			helpProvider = new HelpProvider();
			helpProvider.HelpNamespace = m_helpTopicProvider.HelpFile;
			helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(s_helpTopic));
			helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
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

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
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
			m_mediator = null;
			m_cache = null;

			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ImportWordSetDlg));
			this.label1 = new System.Windows.Forms.Label();
			this.btnImport = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.tbName = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.btnChooseFiles = new System.Windows.Forms.Button();
			this.tbFileNames = new System.Windows.Forms.TextBox();
			this.buttonHelp = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			//
			// btnImport
			//
			resources.ApplyResources(this.btnImport, "btnImport");
			this.btnImport.Name = "btnImport";
			this.btnImport.Click += new System.EventHandler(this.btnImport_Click);
			//
			// btnCancel
			//
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.btnCancel, "btnCancel");
			this.btnCancel.Name = "btnCancel";
			//
			// tbName
			//
			resources.ApplyResources(this.tbName, "tbName");
			this.tbName.Name = "tbName";
			//
			// label2
			//
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			//
			// btnChooseFiles
			//
			resources.ApplyResources(this.btnChooseFiles, "btnChooseFiles");
			this.btnChooseFiles.Name = "btnChooseFiles";
			this.btnChooseFiles.Click += new System.EventHandler(this.btnChooseFiles_Click);
			//
			// tbFileNames
			//
			resources.ApplyResources(this.tbFileNames, "tbFileNames");
			this.tbFileNames.Name = "tbFileNames";
			this.tbFileNames.ReadOnly = true;
			//
			// buttonHelp
			//
			resources.ApplyResources(this.buttonHelp, "buttonHelp");
			this.buttonHelp.Name = "buttonHelp";
			this.buttonHelp.Click += new System.EventHandler(this.buttonHelp_Click);
			//
			// ImportWordSetDlg
			//
			this.AcceptButton = this.btnImport;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.btnCancel;
			this.Controls.Add(this.buttonHelp);
			this.Controls.Add(this.tbFileNames);
			this.Controls.Add(this.tbName);
			this.Controls.Add(this.btnChooseFiles);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnImport);
			this.Controls.Add(this.label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "ImportWordSetDlg";
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		private void btnImport_Click(object sender, System.EventArgs e)
		{
			if (m_paths == null)
			{
				MessageBox.Show(ParserUIStrings.ksNoFilesToImport, ParserUIStrings.ksNoFiles,
					MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}

			m_mediator.SendMessage("StopParser", null);

			CreateWordsetFromFiles(m_paths);

			//starting up the parser without the user asking for that pain is a bit over ambitious at the moment:
			//m_mediator.SendMessage("StartParser", null);

			m_mediator.SendMessage("FilterListChanged", null); // let record clerk know the list of filters has changed.
			DialogResult = DialogResult.OK;
		}

		private void btnChooseFiles_Click(object sender, System.EventArgs e)
		{
			using (var dlg = new OpenFileDialogAdapter())
			{
				dlg.Multiselect = true;
				dlg.Filter = ResourceHelper.FileFilter(FileFilterType.Text);
				if (DialogResult.OK == dlg.ShowDialog(this))
				{
					tbFileNames.Lines = dlg.FileNames;
					m_paths = dlg.FileNames;
				}
			}
		}
		/// <summary>
		/// Parse the given lists of files and create a wordset from them.
		/// </summary>
		/// <param name="paths"></param>
		/// <remarks>This is marked internal so that unit tests can call it</remarks>
		internal void CreateWordsetFromFiles(string[] paths)
		{
			CheckDisposed();

			using (ProgressDialogWorkingOn dlg = new ProgressDialogWorkingOn())
			{
				m_cache.DomainDataByFlid.BeginUndoTask("Import Word Set", "Import Word Set");
				string sWordSetName = GetWordSetName(paths);
				var wordSet = m_cache.ServiceLocator.GetInstance<IWfiWordSetFactory>().Create();
				m_cache.LangProject.MorphologicalDataOA.TestSetsOC.Add(wordSet);
				wordSet.Name.SetAnalysisDefaultWritingSystem(sWordSetName);
				wordSet.Description.SetAnalysisDefaultWritingSystem(GetWordSetDescription(paths));
				dlg.Owner = FindForm();
				dlg.Icon = dlg.Owner.Icon;
				dlg.Minimum = 0;
				dlg.Maximum = paths.Length;
				dlg.Text = String.Format(ParserUIStrings.ksLoadingFilesForWordSetX, sWordSetName);
				dlg.Show();
				dlg.BringToFront();
				WordImporter importer = new WordImporter(m_cache);
				foreach (string path in paths)
				{
					UpdateProgress(path, dlg);
					importer.PopulateWordset(path, wordSet);
				}
				m_cache.DomainDataByFlid.EndUndoTask();
				dlg.Close();
			}
		}

		private void UpdateProgress(string sMessage, ProgressDialogWorkingOn dlg)
		{
			dlg.WorkingOnText = sMessage;
			dlg.PerformStep();
			dlg.Refresh();
		}

		private string GetWordSetName(string[] paths)
		{
			string sWordSetName = tbName.Text;
			if (sWordSetName.Length == 0)
				sWordSetName = System.IO.Path.GetFileName(paths[0]); // use first file name if user doesn't give one
			return sWordSetName;
		}

		private string GetWordSetDescription(string[] paths)
		{
			// REVIEW: SHOULD THE LIST BUILDING BE LOCALIZED SOMEHOW?
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < paths.Length; i++)
			{
				if (i > 0)
					sb.Append(", ");
				sb.Append(paths[i]);
			}
			if (paths.Length > 1)
				return String.Format(ParserUIStrings.ksImportedFromFilesX, sb.ToString());
			else
				return String.Format(ParserUIStrings.ksImportedFromFileX, sb.ToString());
		}

		/* Not needed now that we use a real listener.
		protected override void OnClosed(EventArgs ea)
		{
			base.OnClosed(ea);
			tbName.Text = "";
			tbFileNames.Text = "";
			m_paths = null;
		}
		*/

		protected override void OnActivated(EventArgs ea)
		{
			base.OnActivated(ea);
			tbName.Focus();
		}

		private void buttonHelp_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, s_helpTopic);
		}
	}
}
