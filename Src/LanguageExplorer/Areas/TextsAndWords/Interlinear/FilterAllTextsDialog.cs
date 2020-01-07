// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using SIL.Code;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.LCModel;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// Abstract base dialog for displaying a list of texts including Scripture portions (books, sections, etc.)
	/// and allowing the user to choose which ones to include
	/// </summary>
	public abstract class FilterAllTextsDialog : Form
	{
		#region Data Members
		/// <summary>List of Scripture objects</summary>
		protected IStText[] m_objList;
		/// <summary>LCM cache</summary>
		protected LcmCache m_cache;
		/// <summary>Help Provider</summary>
		protected HelpProvider m_helpProvider;
		private IHelpTopicProvider m_helpTopicProvider;
		/// <summary>Help Topic Id</summary>
		protected string m_helpTopicId = "";
		/// <summary>The text tree with scripture, genres and unassigned texts</summary>
		protected TextsTriStateTreeView m_treeTexts;
		/// <summary>Label for the tree view.</summary>
		protected Label m_treeViewLabel;
		/// <remarks>protected because of testing</remarks>
		protected Button m_btnOK;
		private IContainer components;
		#endregion

		#region Constructor/Destructor

		/// <summary />
		protected FilterAllTextsDialog()
		{
			InitializeComponent();
		}

		/// <summary />
		protected FilterAllTextsDialog(IApp app, LcmCache cache, IStText[] objList, IHelpTopicProvider helpTopicProvider) : this()
		{
			Guard.AgainstNull(app, nameof(app));
			Guard.AgainstNull(cache, nameof(cache));
			Guard.AgainstNull(objList, nameof(objList));

			m_treeTexts.App = app;
			m_treeTexts.Cache = cache;
			m_cache = cache;
			m_objList = objList;
			m_helpTopicProvider = helpTopicProvider;
			m_treeTexts.AfterCheck += OnCheckedChanged;
			AccessibleName = "FilterAllTextsDialog";
		}

		/// <summary/>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". ******");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				components?.Dispose();
			}
			base.Dispose(disposing);
		}
		#endregion

		/// <summary>
		/// Load whatever texts this control is managing
		/// </summary>
		protected abstract void LoadTexts();

		/// <summary>
		/// Load settings for the dialog
		/// </summary>
		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);

			// Add all of the Scripture book names to the book list
			m_treeTexts.BeginUpdate();
			LoadTexts();
			m_treeTexts.ExpandToBooks();
			m_treeTexts.EndUpdate();
			if (m_btnOK == null || m_objList == null)
			{
				return;
			}
			var prevSeqCount = m_cache.ActionHandlerAccessor.UndoableSequenceCount;
			foreach (var obj in m_objList)
			{
				m_treeTexts.CheckNodeByTag(obj, TriStateTreeViewCheckState.Checked);
			}
			if (prevSeqCount != m_cache.ActionHandlerAccessor.UndoableSequenceCount)
			{
				// Selecting node(s) changed something, so save it so that the UI doesn't become
				// unresponsive
				using (var progressDlg = new ProgressDialogWithTask(this))
				{
					progressDlg.IsIndeterminate = true;
					progressDlg.Title = Text;
					progressDlg.Message = ResourceHelper.GetResourceString("kstidSavingChanges");
					progressDlg.RunTask((progDlg, parms) =>
					{
						m_cache.ServiceLocator.GetInstance<IUndoStackManager>().Save();
						return null;
					});
				}
			}
			UpdateButtonState();
		}

		/// <summary>
		/// Return an array of all of the included objects of the filter type.
		/// </summary>
		public IStText[] GetListOfIncludedTexts()
		{
			return m_treeTexts.GetCheckedTagData().OfType<IStText>().ToArray();
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Windows.Forms.Button m_btnCancel;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FilterAllTextsDialog));
			System.Windows.Forms.Button m_btnHelp;
			this.m_treeViewLabel = new System.Windows.Forms.Label();
			this.m_btnOK = new System.Windows.Forms.Button();
			this.m_treeTexts = new LanguageExplorer.Areas.TextsAndWords.Interlinear.TextsTriStateTreeView();
			this.m_helpProvider = new System.Windows.Forms.HelpProvider();
			m_btnCancel = new System.Windows.Forms.Button();
			m_btnHelp = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// m_btnCancel
			//
			resources.ApplyResources(m_btnCancel, "m_btnCancel");
			m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			m_btnCancel.Name = "m_btnCancel";
			this.m_helpProvider.SetShowHelp(m_btnCancel, ((bool)(resources.GetObject("m_btnCancel.ShowHelp"))));
			//
			// m_btnHelp
			//
			resources.ApplyResources(m_btnHelp, "m_btnHelp");
			m_btnHelp.Name = "m_btnHelp";
			this.m_helpProvider.SetShowHelp(m_btnHelp, ((bool)(resources.GetObject("m_btnHelp.ShowHelp"))));
			m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// m_treeViewLabel
			//
			resources.ApplyResources(this.m_treeViewLabel, "m_treeViewLabel");
			this.m_treeViewLabel.Name = "m_treeViewLabel";
			this.m_helpProvider.SetShowHelp(this.m_treeViewLabel, ((bool)(resources.GetObject("m_treeViewLabel.ShowHelp"))));
			//
			// m_btnOK
			//
			resources.ApplyResources(this.m_btnOK, "m_btnOK");
			this.m_btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btnOK.Name = "m_btnOK";
			this.m_helpProvider.SetShowHelp(this.m_btnOK, ((bool)(resources.GetObject("m_btnOK.ShowHelp"))));
			//
			// m_treeTexts
			//
			resources.ApplyResources(this.m_treeTexts, "m_treeTexts");
			this.m_treeTexts.ItemHeight = 16;
			this.m_treeTexts.Name = "m_treeTexts";
			this.m_helpProvider.SetShowHelp(this.m_treeTexts, ((bool)(resources.GetObject("m_treeTexts.ShowHelp"))));
			//
			// FilterAllTextsDialog
			//
			this.AcceptButton = this.m_btnOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = m_btnCancel;
			this.Controls.Add(this.m_treeTexts);
			this.Controls.Add(this.m_treeViewLabel);
			this.Controls.Add(m_btnHelp);
			this.Controls.Add(m_btnCancel);
			this.Controls.Add(this.m_btnOK);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FilterAllTextsDialog";
			this.m_helpProvider.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			this.ShowInTaskbar = false;
			this.ResumeLayout(false);

		}
		#endregion

		#region Event Handlers

		/// <summary>
		/// Open the help window when the help button is pressed.
		/// </summary>
		protected void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, m_helpTopicId);
		}

		/// <summary>
		/// Called after the box is checked or unchecked
		/// </summary>
		protected void OnCheckedChanged(object sender, TreeViewEventArgs e)
		{
			UpdateButtonState();
		}

		/// <summary>
		/// controls the logic for enabling/disabling buttons on this dialog.
		/// </summary>
		protected virtual void UpdateButtonState()
		{
			m_btnOK.Enabled = (m_treeTexts.GetCheckedTagData().Count > 0);
		}
		#endregion
	}
}