using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Framework;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	public class LinkEntryOrSenseDlg : BaseEntryGoDlg
	{
		#region	Data members

		List<int> m_senseIds;

		#region	Designer data members

		private System.Windows.Forms.RadioButton m_rbEntry;
		private System.Windows.Forms.RadioButton m_rbSense;
		private SIL.FieldWorks.Common.Widgets.FwComboBox m_fwcbSenses;
		protected GroupBox grplbl;
		protected GroupBox groupBox1;

		private System.ComponentModel.IContainer components = null;

		#endregion	Designer data members

		#endregion	Data members

		#region Properties

		public bool SelectSensesOnly
		{
			set
			{
				CheckDisposed();

				if (value)
				{
					m_rbSense.Checked = true;
					m_fwcbSenses.Enabled = true;
				}
				m_rbEntry.Enabled = !value;
				m_rbSense.Enabled = !value;
			}
		}

		protected override WindowParams DefaultWindowParams
		{
			get
			{
				WindowParams wp = new WindowParams();
				wp.m_title = LexText.Controls.LexTextControls.ksChooseLexEntryOrSense;
				wp.m_label = LexText.Controls.LexTextControls.ks_Find_;
				wp.m_btnText = LexText.Controls.LexTextControls.ks_OK;
				return wp;
			}
		}

		protected override string PersistenceLabel
		{
			get { return "LinkEntryOrSense"; }
		}

		/// <summary>
		/// Gets the database id of the selected object.
		/// </summary>
		public override int SelectedID
		{
			get
			{
				CheckDisposed();

				if (m_rbEntry.Checked)
					return m_selEntryID;
				else if (m_fNewlyCreated)
					return m_hvoNewSense;
				else
				{
					if (m_fwcbSenses.SelectedIndex == -1)
					{
						if (m_selEntryID != 0)
						{
							// Just select the first sense, since the user used a similar entry in the Create dlg,
							// and the code doesn't then let them choose another sense, before closing.
							// Another option would be to populate the senses list, and let the user select one,
							// but the superclass code would need to handle that scenario,
							// or let this class override something to keep the dlg open.
							ILexEntry le = LexEntry.CreateFromDBObject(m_cache, m_selEntryID);
							return le.SensesOS[0].Hvo;
						}
						else
						{
							// Don't crash here.  (See LT-9387.)  Let somebody else crash...  :-)
							return 0;
						}
					}
					else
					{
						return m_senseIds[m_fwcbSenses.SelectedIndex];
					}
				}
			}
		}

		#endregion Properties

		#region	Construction and Destruction

		public LinkEntryOrSenseDlg()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();
			ShowControlsBasedOnPanel1Position();	// used for sizing and display of some controls

			m_senseIds = new List<int>();

			btnHelp.Enabled = false; // Not until we know what kind of window it is

			// adjust btnInsert ("Create") and btnOK ("Set Relation"?)
			Point ptOK = new Point(btnOK.Left, btnOK.Top);
			Point ptInsert = new Point(btnOK.Left, btnOK.Top);
			ptOK.X -= btnInsert.Width + (btnClose.Left - btnOK.Right);
			btnOK.Location = ptOK;
			btnInsert.Location = ptInsert;
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

			//Debug.Assert(startingEntry != null);
			m_startingEntry = startingEntry;

			SetDlgInfo(cache, null, mediator);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="wp"></param>
		/// <param name="mediator"></param>
		public override void SetDlgInfo(FdoCache cache, WindowParams wp, Mediator mediator)
		{
			CheckDisposed();

			m_fwcbSenses.WritingSystemFactory = cache.LanguageWritingSystemFactoryAccessor;

			base.SetDlgInfo(cache, wp, mediator);
			// This is needed to make the replacement MatchingEntriesBrowser visible:
			this.Controls.SetChildIndex(this.matchingEntries, 0);

			//Set the senses control so that it conforms to the size of the
			//DefaultAnalysisWritingSystem
			m_fwcbSenses.WritingSystemCode = cache.LangProject.DefaultAnalysisWritingSystem;
			// the default font is set to size 100, so that when adding strings to the control
			// this becomes a limit.
			m_fwcbSenses.StyleSheet = m_tbForm.StyleSheet;
			m_fwcbSenses.AdjustForStyleSheet(this, grplbl, m_fwcbSenses.StyleSheet);

			if (wp != null)
			{
				switch (wp.m_title)
				{
					case "Identify sense":
						m_helpTopic = "khtpAddSenseToReversalEntry";
						btnHelp.Enabled = true;
						break;
				}
			}
		}

		#endregion	Construction and Destruction

		#region	Other methods

		protected override void HandleMatchingSelectionChanged()
		{
			// Clear out senses combo box, no matter how the dlg is being used.
			m_fwcbSenses.Items.Clear();
			m_fwcbSenses.Text = String.Empty;
			m_senseIds.Clear();

			bool okBtnEnabled = false;
			bool senseControlsEnabled = false;
			if (m_selEntryID > 0)
			{
				if (m_rbSense.Checked)
				{
					// Add new stuff to sense combo box.
					m_fwcbSenses.SuspendLayout();
					ILexEntry le = LexEntry.CreateFromDBObject(m_cache, m_selEntryID);
					foreach (ILexSense sense in le.AllSenses)
					{
						m_fwcbSenses.AddItem(sense.ChooserNameTS);
						m_senseIds.Add(sense.Hvo);
					}
					m_fwcbSenses.ResumeLayout();
					if (m_fwcbSenses.Items.Count > 0)
					{
						// Select first sense, and enable various controls.
						m_fwcbSenses.SelectedItem = m_fwcbSenses.Items[0];
						okBtnEnabled = true;
						senseControlsEnabled = true;
					}
					else
					{
						// Entry has no senses, so disable controls, and notify user.
						okBtnEnabled = false;
						senseControlsEnabled = false;
						m_fwcbSenses.Text = LexText.Controls.LexTextControls.ksNoSensesInEntry;
					}
				}
				else
				{
					// User is selecting an entry, so enable OK button,
					// but not the sense controls.
					Debug.Assert(m_rbEntry.Checked);
					okBtnEnabled = true;
					senseControlsEnabled = false;
				}
			}
			else
			{
				// No entry is selected.
				// Nothing new need happen here, since the relevant controls will already be disabled.
				// Indeed, the OK button will have been disabled in the m_tbForm_TextChanged event handler,
				// and this 'code' will never even be called.
			}
			btnOK.Enabled = okBtnEnabled;
			m_fwcbSenses.Enabled = senseControlsEnabled;
		}

		#endregion	Other methods

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LinkEntryOrSenseDlg));
			this.m_rbEntry = new System.Windows.Forms.RadioButton();
			this.m_rbSense = new System.Windows.Forms.RadioButton();
			this.m_fwcbSenses = new SIL.FieldWorks.Common.Widgets.FwComboBox();
			this.grplbl = new System.Windows.Forms.GroupBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.panel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_tbForm)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_fwTextBoxBottomMsg)).BeginInit();
			this.grplbl.SuspendLayout();
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
			// m_cbWritingSystems
			//
			resources.ApplyResources(this.m_cbWritingSystems, "m_cbWritingSystems");
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			//
			// m_fwTextBoxBottomMsg
			//
			this.m_fwTextBoxBottomMsg.BackColor = System.Drawing.SystemColors.InactiveCaptionText;
			resources.ApplyResources(this.m_fwTextBoxBottomMsg, "m_fwTextBoxBottomMsg");
			//
			// label2
			//
			resources.ApplyResources(this.label2, "label2");
			//
			// m_rbEntry
			//
			this.m_rbEntry.Checked = true;
			resources.ApplyResources(this.m_rbEntry, "m_rbEntry");
			this.m_rbEntry.Name = "m_rbEntry";
			this.m_rbEntry.TabStop = true;
			this.m_rbEntry.Click += new System.EventHandler(this.m_radioButtonClick);
			//
			// m_rbSense
			//
			resources.ApplyResources(this.m_rbSense, "m_rbSense");
			this.m_rbSense.Name = "m_rbSense";
			this.m_rbSense.Click += new System.EventHandler(this.m_radioButtonClick);
			//
			// m_fwcbSenses
			//
			this.m_fwcbSenses.AdjustStringHeight = true;
			resources.ApplyResources(this.m_fwcbSenses, "m_fwcbSenses");
			this.m_fwcbSenses.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDown;
			this.m_fwcbSenses.Name = "m_fwcbSenses";
			//
			// grplbl
			//
			resources.ApplyResources(this.grplbl, "grplbl");
			this.grplbl.BackColor = System.Drawing.SystemColors.Control;
			this.grplbl.Controls.Add(this.m_rbSense);
			this.grplbl.Controls.Add(this.m_rbEntry);
			this.grplbl.Controls.Add(this.m_fwcbSenses);
			this.grplbl.ForeColor = System.Drawing.SystemColors.ControlText;
			this.grplbl.Name = "grplbl";
			this.helpProvider.SetShowHelp(this.grplbl, ((bool)(resources.GetObject("grplbl.ShowHelp"))));
			this.grplbl.TabStop = false;
			//
			// groupBox1
			//
			resources.ApplyResources(this.groupBox1, "groupBox1");
			this.groupBox1.ForeColor = System.Drawing.SystemColors.ControlText;
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.TabStop = false;
			//
			// LinkEntryOrSenseDlg
			//
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.grplbl);
			this.helpProvider.SetHelpNavigator(this, ((System.Windows.Forms.HelpNavigator)(resources.GetObject("$this.HelpNavigator"))));
			this.Name = "LinkEntryOrSenseDlg";
			this.helpProvider.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			this.Controls.SetChildIndex(this.m_fwTextBoxBottomMsg, 0);
			this.Controls.SetChildIndex(this.btnInsert, 0);
			this.Controls.SetChildIndex(this.grplbl, 0);
			this.Controls.SetChildIndex(this.groupBox1, 0);
			this.Controls.SetChildIndex(this.btnClose, 0);
			this.Controls.SetChildIndex(this.btnOK, 0);
			this.Controls.SetChildIndex(this.btnHelp, 0);
			this.Controls.SetChildIndex(this.panel1, 0);
			this.Controls.SetChildIndex(this.matchingEntries, 0);
			this.Controls.SetChildIndex(this.m_cbWritingSystems, 0);
			this.Controls.SetChildIndex(this.label1, 0);
			this.Controls.SetChildIndex(this.label2, 0);
			this.panel1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_tbForm)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_fwTextBoxBottomMsg)).EndInit();
			this.grplbl.ResumeLayout(false);
			this.grplbl.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		#region Event handlers

		private void m_radioButtonClick(object sender, System.EventArgs e)
		{
			HandleMatchingSelectionChanged();
		}

		#endregion Event handlers

	}
}
