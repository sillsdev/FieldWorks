using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.LexText.Controls;
using SIL.FieldWorks.Common.COMInterfaces;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	public class MergeEntryDlg : BaseEntryGoDlg
	{
		#region Data members

		private System.Windows.Forms.PictureBox m_pictureBox;
		private System.ComponentModel.IContainer components = null;

		#endregion Data members

		#region Properties

		protected override WindowParams DefaultWindowParams
		{
			get
			{
				WindowParams wp = new WindowParams();
				wp.m_title = LexText.Controls.LexTextControls.ksMergeEntry;
				wp.m_label = LexText.Controls.LexTextControls.ks_Find_;
				wp.m_btnText = LexText.Controls.LexTextControls.ks_Merge;
				return wp;
			}
		}

		protected override string PersistenceLabel
		{
			get { return "MergeEntry"; }
		}

		#endregion Properties

		#region	Construction and Destruction

		public MergeEntryDlg()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();
			ShowControlsBasedOnPanel1Position();	// used for sizing and display of some controls

			m_useMinorEntries = true;
			Icon infoIcon = System.Drawing.SystemIcons.Information;
			m_pictureBox.Image = infoIcon.ToBitmap();
			m_pictureBox.Size = infoIcon.Size;
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
		/// <param name="mediator">Mediator used to restore saved siz and location info.</param>
		/// <param name="startingEntry">Entry that cannot be used as a match in this dlg.</param>
		public void SetDlgInfo(FdoCache cache, Mediator mediator, ILexEntry startingEntry)
		{
			CheckDisposed();

			Debug.Assert(startingEntry != null);
			m_startingEntry = startingEntry;

			SetDlgInfo(cache, null, mediator);

			// Relocate remaining three buttons.
			Point pt = btnHelp.Location;
			// Make the Help btn 20 off the right edge of the dlg
			pt.X = Width - btnHelp.Width - 20;
			btnHelp.Location = pt;
			// Make the Cancel btn 10 from the left of the Help btn
			pt.X -= (btnClose.Width + 10);
			btnClose.Location = pt;
			// Make the Merge Entry btn 10 from the left of the Cancel btn.
			pt.X -= (btnOK.Width + 10);
			btnOK.Location = pt;
			int userWs = m_cache.LangProject.DefaultUserWritingSystem;
			SetBottomMessage();

			SetHelpTopic("khtpMergeEntry");

			//LT-3017 Launch the dialog with the Lexeme that is currently selected.
			Form = m_startingEntry.HomographForm;
		}

		#endregion	Construction and Destruction

		#region	Other methods

		protected override void HandleMatchingSelectionChanged()
		{
			SetBottomMessage();
			base.HandleMatchingSelectionChanged();
		}

		protected override void SetBottomMessage()
	{
			int userWs = m_cache.LangProject.DefaultUserWritingSystem;
			int vernWs = m_cache.LangProject.DefaultVernacularWritingSystem;
			string sBase;
			if (m_selEntryID > 0)
				sBase = LexText.Controls.LexTextControls.ksEntryXMergedIntoY;
			else
				sBase = LexText.Controls.LexTextControls.ksEntryXMergedIntoSel;
			ITsStrBldr tsb = TsStrBldrClass.Create();
			tsb.ReplaceTsString(0, tsb.Length, m_tsf.MakeString(sBase, userWs));
			// Replace every "{0}" with the headword we'll be merging, and make it bold.
			ITsString tssFrom = m_startingEntry.HeadWord;
			string sTmp = tsb.Text;
			int ich = sTmp.IndexOf("{0}");
			int cch = tssFrom.Length;
			while (ich >= 0 && cch > 0)
			{
				tsb.ReplaceTsString(ich, ich + 3, tssFrom);
				tsb.SetIntPropValues(ich, ich + cch,
					(int)FwTextPropType.ktptBold,
					(int)FwTextPropVar.ktpvEnum,
					(int)FwTextToggleVal.kttvForceOn);
				sTmp = tsb.Text;
				ich = sTmp.IndexOf("{0}");	// in case localization needs more than one.
			}
			if (m_selEntryID > 0)
			{
				// Replace every "{1}" with the headword we'll be merging into.
				ILexEntry le = LexEntry.CreateFromDBObject(m_cache, m_selEntryID);
				ITsString tssTo = le.HeadWord;
				ich = sTmp.IndexOf("{1}");
				cch = tssTo.Length;
				while (ich >= 0 && cch > 0)
				{
					tsb.ReplaceTsString(ich, ich + 3, tssTo);
					tsb.SetIntPropValues(ich, ich + cch,
						(int)FwTextPropType.ktptBold,
						(int)FwTextPropVar.ktpvEnum,
						(int)FwTextToggleVal.kttvForceOn);
					sTmp = tsb.Text;
					ich = sTmp.IndexOf("{0}");
				}
				// Replace every "{2}" with a newline character.
				ich = sTmp.IndexOf("{2}");
				while (ich >= 0)
				{
					tsb.ReplaceTsString(ich, ich + 3, m_tsf.MakeString("\x2028", userWs));
					sTmp = tsb.Text;
					ich = sTmp.IndexOf("{2}");
				}
			}
			else
			{
				// Replace every "{1}" with a newline character.
				ich = sTmp.IndexOf("{1}");
				while (ich >= 0)
				{
					tsb.ReplaceTsString(ich, ich + 3, m_tsf.MakeString("\x2028", userWs));
					sTmp = tsb.Text;
					ich = sTmp.IndexOf("{1}");
				}
			}
			m_fwTextBoxBottomMsg.Tss = tsb.GetString();
		}

		#endregion	Other methods

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MergeEntryDlg));
			this.m_pictureBox = new System.Windows.Forms.PictureBox();
			this.panel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_tbForm)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_fwTextBoxBottomMsg)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_pictureBox)).BeginInit();
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
			// m_pictureBox
			//
			resources.ApplyResources(this.m_pictureBox, "m_pictureBox");
			this.m_pictureBox.Name = "m_pictureBox";
			this.m_pictureBox.TabStop = false;
			//
			// MergeEntryDlg
			//
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this.m_pictureBox);
			this.helpProvider.SetHelpNavigator(this, ((System.Windows.Forms.HelpNavigator)(resources.GetObject("$this.HelpNavigator"))));
			this.Name = "MergeEntryDlg";
			this.helpProvider.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			this.Controls.SetChildIndex(this.btnClose, 0);
			this.Controls.SetChildIndex(this.btnOK, 0);
			this.Controls.SetChildIndex(this.btnInsert, 0);
			this.Controls.SetChildIndex(this.btnHelp, 0);
			this.Controls.SetChildIndex(this.panel1, 0);
			this.Controls.SetChildIndex(this.matchingEntries, 0);
			this.Controls.SetChildIndex(this.m_cbWritingSystems, 0);
			this.Controls.SetChildIndex(this.label1, 0);
			this.Controls.SetChildIndex(this.m_fwTextBoxBottomMsg, 0);
			this.Controls.SetChildIndex(this.label2, 0);
			this.Controls.SetChildIndex(this.m_pictureBox, 0);
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_tbForm)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_fwTextBoxBottomMsg)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_pictureBox)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}
