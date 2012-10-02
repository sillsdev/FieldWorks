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

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// Summary description for LanguageSelectionDlg.
	/// </summary>
	public class LanguageSelectionDlg : Form, IFWDisposable
	{
		private SIL.FieldWorks.Common.Controls.LanguageSetup languageSetup1;
		private System.Windows.Forms.HelpProvider m_helpProvider;
		private IHelpTopicProvider m_helpTopicProvider;
		private Button m_btnOK;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="LanguageSelectionDlg"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public LanguageSelectionDlg()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
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
		/// Set the dialog properties object for dialogs that are created.
		/// </summary>
		/// <param name="helpTopicProvider"></param>
		/// ------------------------------------------------------------------------------------
		public void SetDialogProperties(IHelpTopicProvider helpTopicProvider)
		{
			CheckDisposed();

			m_helpTopicProvider = helpTopicProvider;
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
				if(components != null)
				{
					components.Dispose();
				}
			}
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LanguageSelectionDlg));
			System.Windows.Forms.Button m_btnCancel;
			System.Windows.Forms.Button m_btnHelp;
			this.languageSetup1 = new SIL.FieldWorks.Common.Controls.LanguageSetup();
			this.m_btnOK = new System.Windows.Forms.Button();
			this.m_helpProvider = new System.Windows.Forms.HelpProvider();
			m_btnCancel = new System.Windows.Forms.Button();
			m_btnHelp = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// languageSetup1
			//
			this.languageSetup1.EthnologueCode = "";
			this.languageSetup1.LanguageName = "";
			resources.ApplyResources(this.languageSetup1, "languageSetup1");
			this.languageSetup1.Name = "languageSetup1";
			this.m_helpProvider.SetShowHelp(this.languageSetup1, ((bool)(resources.GetObject("languageSetup1.ShowHelp"))));
			this.languageSetup1.StartedInModifyState = false;
			this.languageSetup1.LanguageNameChanged += new System.EventHandler(this.languageSetup1_LanguageNameChanged);
			//
			// m_btnCancel
			//
			m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(m_btnCancel, "m_btnCancel");
			m_btnCancel.Name = "m_btnCancel";
			this.m_helpProvider.SetShowHelp(m_btnCancel, ((bool)(resources.GetObject("m_btnCancel.ShowHelp"))));
			//
			// m_btnOK
			//
			this.m_btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			resources.ApplyResources(this.m_btnOK, "m_btnOK");
			this.m_btnOK.Name = "m_btnOK";
			this.m_helpProvider.SetShowHelp(this.m_btnOK, ((bool)(resources.GetObject("m_btnOK.ShowHelp"))));
			//
			// m_btnHelp
			//
			this.m_helpProvider.SetHelpString(m_btnHelp, resources.GetString("m_btnHelp.HelpString"));
			resources.ApplyResources(m_btnHelp, "m_btnHelp");
			m_btnHelp.Name = "m_btnHelp";
			this.m_helpProvider.SetShowHelp(m_btnHelp, ((bool)(resources.GetObject("m_btnHelp.ShowHelp"))));
			m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// LanguageSelectionDlg
			//
			this.AcceptButton = this.m_btnOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = m_btnCancel;
			this.Controls.Add(m_btnHelp);
			this.Controls.Add(this.m_btnOK);
			this.Controls.Add(m_btnCancel);
			this.Controls.Add(this.languageSetup1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "LanguageSelectionDlg";
			this.m_helpProvider.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			this.ShowInTaskbar = false;
			this.ResumeLayout(false);

		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void languageSetup1_LanguageNameChanged(object sender, System.EventArgs e)
		{
			m_btnOK.Enabled = (languageSetup1.LanguageName != string.Empty);
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
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpWsSelectLanguage");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// get or set the language name
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string LangName
		{
			get
			{
				CheckDisposed();
				return languageSetup1.LanguageName;
			}
			set
			{
				CheckDisposed();

				languageSetup1.LanguageName = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get or set the Eth code
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string EthCode
		{
			get
			{
				CheckDisposed();
				return languageSetup1.EthnologueCode;
			}
			set
			{
				CheckDisposed();

				languageSetup1.EthnologueCode = value;

			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the control property to specify if it's being started from a 'modify' button.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool StartedInModifyState
		{
			set
			{
				CheckDisposed();
				languageSetup1.StartedInModifyState = value;
			}
		}
	}
}
