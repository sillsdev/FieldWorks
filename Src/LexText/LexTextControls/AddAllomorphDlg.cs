using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.LexText.Controls
{
	public class AddAllomorphDlg : EntryGoDlg
	{
		#region	Data members

		private string m_formOrig;
		private int m_hvoType;
		protected bool m_fInconsistentType; // see comment on property.
		protected bool m_fMatchingForm;

		#region	Designer data members

		#endregion	Designer data members

		#endregion	Data members

		#region Properties

		protected override WindowParams DefaultWindowParams
		{
			get
			{
				var wp = new WindowParams
							{
								m_title = LexTextControls.ksFindEntryToAddAllomorph,
								m_label = LexTextControls.ksGo_To_,
								m_btnText = LexTextControls.ksAddAllomorph_
							};
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

			SetHelpTopic("khtpFindEntryToAddAllomorph");

			ShowControlsBasedOnPanel1Position();
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
			}
			base.Dispose( disposing );
		}

		/// <summary>
		/// Set up the dlg in preparation to showing it.
		/// </summary>
		/// <param name="cache">FDO cache.</param>
		/// <param name="wp">Strings used for various items in this dialog.</param>
		/// <param name="propertyTable"></param>
		/// <param name="publisher"></param>
		/// <param name="tssform">The form.</param>
		/// <param name="hvoType">The HVO of the type.</param>
		public void SetDlgInfo(FdoCache cache, WindowParams wp, IPropertyTable propertyTable, IPublisher publisher, ITsString tssform, int hvoType)
		{
			CheckDisposed();

			SetDlgInfo(cache, wp, propertyTable, publisher, tssform);

			m_formOrig = m_tbForm.Text;
			m_hvoType = hvoType;

			// JohnT: a prior call to SetForm should have established whether this button
			// is enabled...and it should NOT be, if there are no entries selected,
			// typically because none at all match the form.
			//btnOK.Enabled = true;
			m_btnOK.Width += 30;
			m_btnOK.Left += 90;
			m_btnClose.Width += 30;		// for balance...

			ShowControlsBasedOnPanel1Position();

		}

		#endregion	Construction and Destruction

		#region	Other methods

		protected override void HandleMatchingSelectionChanged()
		{
			if (m_selObject != null)
			{
				// Make sure that none of the allomorphs of this entry match the original
				// form before we can enable btnOK.
				// true if current list of MoForms contains one with matching form text.
				bool fMatchingForm = false;
				// true if current list of MoForms contains one of correct type
				bool fMatchingType = false;
				// If we don't know a morpheme type, don't restrict by type.
				IMoMorphType mtOrig = null;
				if (m_hvoType == 0)
					fMatchingType = true;
				else
					mtOrig = m_cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(m_hvoType);
				foreach (var mf in ((ILexEntry)m_selObject).AllAllomorphs)
				{
					if (mf.Form.VernacularDefaultWritingSystem.Text == m_formOrig)
					{
						fMatchingForm = true;
						fMatchingType = false;
					}
					// To prevent confusion, allow any type that is ambiguous with the
					// current type (or, of course, if it is the SAME type, which for some
					// reason is NOT considered ambiguous).
					if (mtOrig != null && mf.MorphTypeRA != null &&
						(m_hvoType == mf.MorphTypeRA.Hvo ||
						 mtOrig.IsAmbiguousWith(mf.MorphTypeRA)))
					{
						fMatchingType = true;
					}
					if (fMatchingForm)
						break;
				}
				m_fInconsistentType = !fMatchingType;
				m_fMatchingForm = fMatchingForm;
				if (fMatchingForm && fMatchingType)
					m_btnOK.Text = LexTextControls.ksUseAllomorph;
				else
					m_btnOK.Text = LexTextControls.ksAddAllomorph_;
				m_btnOK.Enabled = true;
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
			this.m_panel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_tbForm)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_fwTextBoxBottomMsg)).BeginInit();
			this.SuspendLayout();
			//
			// m_btnClose
			//
			resources.ApplyResources(this.m_btnClose, "m_btnClose");
			//
			// m_btnOK
			//
			resources.ApplyResources(this.m_btnOK, "m_btnOK");
			//
			// m_btnInsert
			//
			resources.ApplyResources(this.m_btnInsert, "m_btnInsert");
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			//
			// m_matchingObjectsBrowser
			//
			resources.ApplyResources(this.m_matchingObjectsBrowser, "m_matchingObjectsBrowser");
			//
			// m_fwTextBoxBottomMsg
			//
			resources.ApplyResources(this.m_fwTextBoxBottomMsg, "m_fwTextBoxBottomMsg");
			//
			// AddAllomorphDlg
			//
			resources.ApplyResources(this, "$this");
			this.m_helpProvider.SetHelpNavigator(this, ((System.Windows.Forms.HelpNavigator)(resources.GetObject("$this.HelpNavigator"))));
			this.Name = "AddAllomorphDlg";
			this.m_helpProvider.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			this.m_panel1.ResumeLayout(false);
			this.m_panel1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_tbForm)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_fwTextBoxBottomMsg)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}
