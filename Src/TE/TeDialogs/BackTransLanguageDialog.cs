// Copyright (c) 2005-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ChooseLanguageDialog.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using System.Collections.Generic;
using XCore;

namespace SIL.FieldWorks.TE
{
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for ChooseLanguageDialog.
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	public class BackTransLanguageDialog : Form, IFWDisposable
	{
		private SIL.FieldWorks.Common.Controls.OptionListBox m_olbWritingSystems;
		private CheckBox m_chkBxApplyToAllBtWs;
		private IHelpTopicProvider m_helpProvider;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="BackTransLanguageDialog"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public BackTransLanguageDialog()
		{
			InitializeComponent();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="BackTransLanguageDialog"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public BackTransLanguageDialog(FdoCache cache, int defaultWs,
			IHelpTopicProvider helpProvider) : this()
		{
			m_helpProvider = helpProvider;


			m_chkBxApplyToAllBtWs.Checked = false;

			foreach (CoreWritingSystemDefinition ws in cache.ServiceLocator.WritingSystems.AnalysisWritingSystems)
			{
				m_olbWritingSystems.Items.Add(ws);
				if (ws.Handle == defaultWs)
					m_olbWritingSystems.SelectedItem = ws;
			}
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

#if DEBUG
		/// <summary/>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			base.Dispose(disposing);
		}
#endif

		#region Windows Form Designer generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			System.Windows.Forms.Button m_btnOK;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BackTransLanguageDialog));
			System.Windows.Forms.Button m_btnHelp;
			System.Windows.Forms.Button m_btnCancel;
			this.m_chkBxApplyToAllBtWs = new System.Windows.Forms.CheckBox();
			this.m_olbWritingSystems = new SIL.FieldWorks.Common.Controls.OptionListBox();
			m_btnOK = new System.Windows.Forms.Button();
			m_btnHelp = new System.Windows.Forms.Button();
			m_btnCancel = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// m_btnOK
			//
			resources.ApplyResources(m_btnOK, "m_btnOK");
			m_btnOK.Name = "m_btnOK";
			m_btnOK.Click += new System.EventHandler(this.m_btnOK_Click);
			//
			// m_btnHelp
			//
			resources.ApplyResources(m_btnHelp, "m_btnHelp");
			m_btnHelp.Name = "m_btnHelp";
			m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// m_btnCancel
			//
			m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(m_btnCancel, "m_btnCancel");
			m_btnCancel.Name = "m_btnCancel";
			//
			// m_chkBxApplyToAllBtWs
			//
			resources.ApplyResources(this.m_chkBxApplyToAllBtWs, "m_chkBxApplyToAllBtWs");
			this.m_chkBxApplyToAllBtWs.Checked = true;
			this.m_chkBxApplyToAllBtWs.CheckState = System.Windows.Forms.CheckState.Checked;
			this.m_chkBxApplyToAllBtWs.Name = "m_chkBxApplyToAllBtWs";
			this.m_chkBxApplyToAllBtWs.UseVisualStyleBackColor = true;
			//
			// m_olbWritingSystems
			//
			this.m_olbWritingSystems.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
			resources.ApplyResources(this.m_olbWritingSystems, "m_olbWritingSystems");
			this.m_olbWritingSystems.Name = "m_olbWritingSystems";
			//
			// BackTransLanguageDialog
			//
			this.AcceptButton = m_btnOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = m_btnCancel;
			this.Controls.Add(this.m_chkBxApplyToAllBtWs);
			this.Controls.Add(this.m_olbWritingSystems);
			this.Controls.Add(m_btnCancel);
			this.Controls.Add(m_btnHelp);
			this.Controls.Add(m_btnOK);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "BackTransLanguageDialog";
			this.ShowInTaskbar = false;
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the WS that was selected
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int SelectedWS
		{
			get
			{
				CheckDisposed();
				return ((CoreWritingSystemDefinition) m_olbWritingSystems.SelectedItem).Handle;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not to change all back translation views to the
		/// selected writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ChangeAllBtWs
		{
			get
			{
				CheckDisposed();
				return m_chkBxApplyToAllBtWs.Checked;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_btnOK_Click(object sender, System.EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_btnHelp_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpProvider, "khtpBackTranslationWritingSystem");
		}
	}
}
