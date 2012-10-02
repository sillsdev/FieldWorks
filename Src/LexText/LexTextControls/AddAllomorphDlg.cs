using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.COMInterfaces;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	public class AddAllomorphDlg : BaseEntryGoDlg
	{
		#region	Data members

		private string m_formOrig;
		private int m_hvoType;
		protected bool m_fInconsistentType = false; // see comment on property.
		protected bool m_fMatchingForm = false;

		#region	Designer data members

		private System.ComponentModel.IContainer components = null;

		#endregion	Designer data members

		#endregion	Data members

		#region Properties

		protected override WindowParams DefaultWindowParams
		{
			get
			{
				WindowParams wp = new WindowParams();
				wp.m_title = SIL.FieldWorks.LexText.Controls.LexTextControls.ksFindEntryToAddAllomorph;
				wp.m_label = SIL.FieldWorks.LexText.Controls.LexTextControls.ksGo_To_;
				wp.m_btnText = SIL.FieldWorks.LexText.Controls.LexTextControls.ksAddAllomorph_;
				return wp;
			}
		}

		protected override string PersistenceLabel
		{
			get { return "AddAllomorph"; }
		}

		/// <summary>
		/// This flag is set when the dialog is used to add an allomorph and the
		/// type of morpheme deduced from the punctuation of the new form doesn't
		/// match the type of any exising MoMorph. This used to disable the OK
		/// button, but now it's just a warning.
		/// </summary>
		public bool InconsistentType
		{
			get
			{
				CheckDisposed();
				return m_fInconsistentType;
			}
		}

		/// <summary>
		/// This flag is set when the dialog is used to add an allomorph and the
		/// allomorph chosen already exists.
		/// </summary>
		public bool MatchingForm
		{
			get
			{
				CheckDisposed();
				return m_fMatchingForm;
			}
		}

		#endregion Properties

		#region	Construction and Destruction

		public AddAllomorphDlg()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();

			this.SetHelpTopic("khtpFindEntryToAddAllomorph");

			ShowControlsBasedOnPanel1Position();

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

		/// <summary>
		/// Set up the dlg in preparation to showing it.
		/// </summary>
		/// <param name="cache">FDO cache.</param>
		/// <param name="wp">Strings used for various items in this dialog.</param>
		public void SetDlgInfo(FdoCache cache, WindowParams wp, Mediator mediator, ITsString tssform, int hvoType)
		{
			CheckDisposed();

			SetDlgInfo(cache, wp, mediator, tssform);

			m_formOrig = m_tbForm.Text;
			m_hvoType = hvoType;

			// JohnT: a prior call to SetForm should have established whether this button
			// is enabled...and it should NOT be, if there are no entries selected,
			// typically because none at all match the form.
			//btnOK.Enabled = true;
			btnOK.Width += 30;
			btnOK.Left += 90;
			btnClose.Width += 30;		// for balance...

			ShowControlsBasedOnPanel1Position();

		}

		#endregion	Construction and Destruction

		#region	Other methods

		protected override void HandleMatchingSelectionChanged()
		{
			if (m_selEntryID > 0)
			{
				// Make sure that none of the allomorphs of this entry match the original
				// form before we can enable btnOK.
				ILexEntry le = LexEntry.CreateFromDBObject(m_cache, m_selEntryID);
				// true if current list of MoForms contains one with matching form text.
				bool fMatchingForm = false;
				// true if current list of MoForms contains one of correct type
				bool fMatchingType = false;
				// If we don't know a morpheme type, don't restrict by type.
				IMoMorphType mtOrig = null;
				if (m_hvoType == 0)
					fMatchingType = true;
				else
					mtOrig = MoMorphType.CreateFromDBObject(m_cache, m_hvoType);
				foreach (IMoForm mf in le.AllAllomorphs)
				{
					if (mf.Form.VernacularDefaultWritingSystem == m_formOrig)
					{
						fMatchingForm = true;
						fMatchingType = false;
					}
					// To prevent confusion, allow any type that is ambiguous with the
					// current type (or, of course, if it is the SAME type, which for some
					// reason is NOT considered ambiguous).
					if (mtOrig != null && mf.MorphTypeRA != null &&
						(m_hvoType == mf.MorphTypeRA.Hvo ||
						 mtOrig.IsAmbiguousWith(m_cache, m_types, mtOrig, mf.MorphTypeRA)))
					{
						fMatchingType = true;
					}
					if (fMatchingForm)
						break;
				}
				m_fInconsistentType = !fMatchingType;
				m_fMatchingForm = fMatchingForm;
				if (fMatchingForm && fMatchingType)
					btnOK.Text = SIL.FieldWorks.LexText.Controls.LexTextControls.ksUseAllomorph;
				else
					btnOK.Text = SIL.FieldWorks.LexText.Controls.LexTextControls.ksAddAllomorph_;
				btnOK.Enabled = true;
			}
			else
			{
				base.HandleMatchingSelectionChanged();
			}
		}

		#endregion	Other methods

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AddAllomorphDlg));
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
			// matchingEntries
			//
			resources.ApplyResources(this.matchingEntries, "matchingEntries");
			//
			// m_fwTextBoxBottomMsg
			//
			resources.ApplyResources(this.m_fwTextBoxBottomMsg, "m_fwTextBoxBottomMsg");
			//
			// AddAllomorphDlg
			//
			resources.ApplyResources(this, "$this");
			this.helpProvider.SetHelpNavigator(this, ((System.Windows.Forms.HelpNavigator)(resources.GetObject("$this.HelpNavigator"))));
			this.Name = "AddAllomorphDlg";
			this.helpProvider.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_tbForm)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_fwTextBoxBottomMsg)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}
