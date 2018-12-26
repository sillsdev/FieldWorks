// Copyright (c) 2005-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using LanguageExplorer.Controls;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	public class AddAllomorphDlg : EntryGoDlg
	{
		private string m_formOrig;
		private int m_hvoType;

		#region Properties

		protected override WindowParams DefaultWindowParams
		{
			get
			{
				var wp = new WindowParams
				{
					m_title = LanguageExplorerControls.ksFindEntryToAddAllomorph,
					m_label = LanguageExplorerControls.ksGo_To_,
					m_btnText = LanguageExplorerControls.ksAddAllomorph_
				};
				return wp;
			}
		}

		protected override string PersistenceLabel => "AddAllomorph";

		/// <summary>
		/// This flag is set when the dialog is used to add an allomorph and the
		/// type of morpheme deduced from the punctuation of the new form doesn't
		/// match the type of any exising MoMorph. This used to disable the OK
		/// button, but now it's just a warning.
		/// </summary>
		public bool InconsistentType { get; protected set; }

		/// <summary>
		/// This flag is set when the dialog is used to add an allomorph and the
		/// allomorph chosen already exists.
		/// </summary>
		public bool MatchingForm { get; protected set; }

		#endregion Properties

		#region	Construction and Destruction

		public AddAllomorphDlg()
		{
			InitializeComponent();
			SetHelpTopic("khtpFindEntryToAddAllomorph");
			ShowControlsBasedOnPanel1Position();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// Set up the dlg in preparation to showing it.
		/// </summary>
		public void SetDlgInfo(LcmCache cache, WindowParams wp, ITsString tssform, int hvoType)
		{
			SetDlgInfo(cache, wp, tssform);
			m_formOrig = m_tbForm.Text;
			m_hvoType = hvoType;
			// "m_btnOK" enabling is handled elsewhere.
			m_btnOK.Width += 30;
			m_btnOK.Left += 90;
			m_btnClose.Width += 30;     // for balance...

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
				var fMatchingForm = false;
				// true if current list of MoForms contains one of correct type
				var fMatchingType = false;
				// If we don't know a morpheme type, don't restrict by type.
				IMoMorphType mtOrig = null;
				if (m_hvoType == 0)
				{
					fMatchingType = true;
				}
				else
				{
					mtOrig = m_cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(m_hvoType);
				}
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
					if (mtOrig != null && mf.MorphTypeRA != null && (m_hvoType == mf.MorphTypeRA.Hvo || mtOrig.IsAmbiguousWith(mf.MorphTypeRA)))
					{
						fMatchingType = true;
					}
					if (fMatchingForm)
					{
						break;
					}
				}
				InconsistentType = !fMatchingType;
				MatchingForm = fMatchingForm;
				if (fMatchingForm && fMatchingType)
				{
					m_btnOK.Text = LanguageExplorerControls.ksUseAllomorph;
				}
				else
				{
					m_btnOK.Text = LanguageExplorerControls.ksAddAllomorph_;
				}
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