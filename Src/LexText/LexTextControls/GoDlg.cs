using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

namespace SIL.FieldWorks.LexText.Controls
{
	public class GoDlg : BaseEntryGoDlg
	{
		private System.ComponentModel.IContainer components = null;

		protected override string PersistenceLabel
		{
			get { return "Go"; }
		}

		public GoDlg()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();
			ShowControlsBasedOnPanel1Position();	// used for sizing and display of some controls
			m_useMinorEntries = true;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		// JohnT: This sets up a bottom message and then hides it! As far as I can determine, it is NEVER
		// made visible, so we can probably remove this method altogether. Leaving it for now in case there's
		// some reason I'm missing.
		protected override void SetBottomMessage()
		{
			int userWs = m_cache.LangProject.DefaultUserWritingSystem;

			m_fwTextBoxBottomMsg.Tss = m_tsf.MakeString(
				String.Format(LexText.Controls.LexTextControls.ksLeftButtonEnabledWhen, "\x2028"),
				userWs);
			m_fwTextBoxBottomMsg.Visible = false;
		}

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GoDlg));
			this.panel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_tbForm)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_fwTextBoxBottomMsg)).BeginInit();
			this.SuspendLayout();
			//
			// btnClose
			//
			resources.ApplyResources(this.btnClose, "btnClose");
			//
			// btnOK
			//
			resources.ApplyResources(this.btnOK, "btnOK");
			//
			// btnInsert
			//
			resources.ApplyResources(this.btnInsert, "btnInsert");
			//
			// btnHelp
			//
			resources.ApplyResources(this.btnHelp, "btnHelp");
			//
			// panel1
			//
			resources.ApplyResources(this.panel1, "panel1");
			//
			// matchingEntries
			//
			resources.ApplyResources(this.matchingEntries, "matchingEntries");
			//
			// m_formLabel
			//
			resources.ApplyResources(this.m_formLabel, "m_formLabel");
			//
			// m_tbForm
			//
			resources.ApplyResources(this.m_tbForm, "m_tbForm");
			//
			// m_fwTextBoxBottomMsg
			//
			resources.ApplyResources(this.m_fwTextBoxBottomMsg, "m_fwTextBoxBottomMsg");
			//
			// GoDlg
			//
			resources.ApplyResources(this, "$this");
			this.helpProvider.SetHelpNavigator(this, ((System.Windows.Forms.HelpNavigator)(resources.GetObject("$this.HelpNavigator"))));
			this.Name = "GoDlg";
			this.helpProvider.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			this.panel1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_tbForm)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_fwTextBoxBottomMsg)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}
