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
// File: LanguageSelectionDlg.cs
// Responsibility:
// --------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.Controls;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;
using XCore;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// Summary description for LanguageSelectionDlg.
	/// </summary>
	public class LanguageSelectionDlg : Form, IFWDisposable
	{
		private readonly IHelpTopicProvider m_helpTopicProvider;
		private LanguageSetup m_languageSetup;
		private HelpProvider m_helpProvider;
		private Button m_btnOK;

		/// <summary>
		/// Initializes a new instance of the <see cref="LanguageSelectionDlg"/> class.
		/// </summary>
		private LanguageSelectionDlg()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			AccessibleName = GetType().Name;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="LanguageSelectionDlg"/> class.
		/// </summary>
		/// <param name="wsManager">The writing system manager.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// ------------------------------------------------------------------------------------
		public LanguageSelectionDlg(IWritingSystemManager wsManager,
			IHelpTopicProvider helpTopicProvider) : this()
		{
			m_helpTopicProvider = helpTopicProvider;
			m_languageSetup.WritingSystemManager = wsManager;
		}

		/// <summary>
		/// Set the initial target for searching for a language by name.
		/// </summary>
		public string DefaultLanguageName
		{
			set { m_languageSetup.PerformInitialSearch(value); }
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

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.Windows.Forms.Button m_btnCancel;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LanguageSelectionDlg));
			System.Windows.Forms.Button m_btnHelp;
			this.m_languageSetup = new SIL.FieldWorks.Common.Controls.LanguageSetup();
			this.m_btnOK = new System.Windows.Forms.Button();
			this.m_helpProvider = new System.Windows.Forms.HelpProvider();
			m_btnCancel = new System.Windows.Forms.Button();
			m_btnHelp = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// m_btnCancel
			//
			m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(m_btnCancel, "m_btnCancel");
			m_btnCancel.Name = "m_btnCancel";
			this.m_helpProvider.SetShowHelp(m_btnCancel, ((bool)(resources.GetObject("m_btnCancel.ShowHelp"))));
			//
			// m_btnHelp
			//
			this.m_helpProvider.SetHelpString(m_btnHelp, resources.GetString("m_btnHelp.HelpString"));
			resources.ApplyResources(m_btnHelp, "m_btnHelp");
			m_btnHelp.Name = "m_btnHelp";
			this.m_helpProvider.SetShowHelp(m_btnHelp, ((bool)(resources.GetObject("m_btnHelp.ShowHelp"))));
			m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// m_languageSetup
			//
			this.m_languageSetup.EthnologueCode = global::SIL.FieldWorks.FwCoreDlgs.FwCoreDlgs.kstidOpen;
			this.m_languageSetup.LanguageName = "";
			resources.ApplyResources(this.m_languageSetup, "m_languageSetup");
			this.m_languageSetup.Name = "m_languageSetup";
			this.m_helpProvider.SetShowHelp(this.m_languageSetup, ((bool)(resources.GetObject("m_languageSetup.ShowHelp"))));
			this.m_languageSetup.StartedInModifyState = false;
			this.m_languageSetup.LanguageNameChanged += new System.EventHandler(this.m_languageSetup_LanguageNameChanged);
			//
			// m_btnOK
			//
			this.m_btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			resources.ApplyResources(this.m_btnOK, "m_btnOK");
			this.m_btnOK.Name = "m_btnOK";
			this.m_helpProvider.SetShowHelp(this.m_btnOK, ((bool)(resources.GetObject("m_btnOK.ShowHelp"))));
			//
			// LanguageSelectionDlg
			//
			this.AcceptButton = this.m_btnOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = m_btnCancel;
			this.Controls.Add(m_btnHelp);
			this.Controls.Add(this.m_btnOK);
			this.Controls.Add(m_btnCancel);
			this.Controls.Add(this.m_languageSetup);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "LanguageSelectionDlg";
			this.m_helpProvider.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			this.ShowInTaskbar = false;
			this.ResumeLayout(false);

		}
		#endregion


		private void m_languageSetup_LanguageNameChanged(object sender, EventArgs e)
		{
			m_btnOK.Enabled = !string.IsNullOrEmpty(m_languageSetup.LanguageName);
		}


		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpWsSelectLanguage");
		}

		/// <summary>
		/// Gets or sets the name of the language.
		/// </summary>
		/// <value>The name of the language.</value>
		public string LanguageName
		{
			get
			{
				CheckDisposed();
				return m_languageSetup.LanguageName;
			}
			set
			{
				CheckDisposed();

				m_languageSetup.LanguageName = value;
			}
		}

		/// <summary>
		/// Gets or sets the ethnologue code.
		/// </summary>
		/// <value>The ethnologue code.</value>
		public string EthnologueCode
		{
			get
			{
				CheckDisposed();
				return m_languageSetup.EthnologueCode;
			}
			set
			{
				CheckDisposed();

				m_languageSetup.EthnologueCode = value;

			}
		}

		/// <summary>
		/// Gets or sets the language subtag.
		/// </summary>
		/// <value>The language subtag.</value>
		public LanguageSubtag LanguageSubtag
		{
			get
			{
				CheckDisposed();

				return m_languageSetup.LanguageSubtag;
			}

			set
			{
				CheckDisposed();

				m_languageSetup.LanguageSubtag = value;
			}
		}

		/// <summary>
		/// Set the control property to specify if it's being started from a 'modify' button.
		/// </summary>
		public bool StartedInModifyState
		{
			set
			{
				CheckDisposed();
				m_languageSetup.StartedInModifyState = value;
			}
		}
	}
}
