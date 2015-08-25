using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.LexText.Controls
{
	public class LinkEntryOrSenseDlg : EntryGoDlg
	{
		#region	Data members

		private readonly List<ILexSense> m_senses;

		#region	Designer data members

		private RadioButton m_rbEntry;
		private RadioButton m_rbSense;
		private FwComboBox m_fwcbSenses;
		protected GroupBox grplbl;
		protected GroupBox groupBox1;

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
				return new WindowParams
				{
					m_title = LexTextControls.ksChooseLexEntryOrSense,
					m_btnText = LexTextControls.ks_OK
				};
			}
		}

		protected override string PersistenceLabel
		{
			get { return "LinkEntryOrSense"; }
		}

		/// <summary>
		/// Gets the database id of the selected object.
		/// </summary>
		public override ICmObject SelectedObject
		{
			get
			{
				CheckDisposed();

				if (m_rbEntry.Checked)
					return m_selObject;
				if (m_fNewlyCreated)
					return m_newSense;
				if (m_fwcbSenses.SelectedIndex == -1)
				{
					if (m_selObject != null)
					{
						if (!(m_selObject is ILexEntry) || ((ILexEntry)m_selObject).SensesOS.Count == 0)
							return null; // We want a sense here, and there isn't one.
						// Just select the first sense, since the user used a similar entry in the Create dlg,
						// and the code doesn't then let them choose another sense, before closing.
						// Another option would be to populate the senses list, and let the user select one,
						// but the superclass code would need to handle that scenario,
						// or let this class override something to keep the dlg open.
						return ((ILexEntry)m_selObject).SensesOS[0];
					}

					// Don't crash here.  (See LT-9387.)  Let somebody else crash...  :-)
					return null;
				}

				return m_senses[m_fwcbSenses.SelectedIndex];
			}
		}

		#endregion Properties

		#region	Construction and Destruction

		public LinkEntryOrSenseDlg()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();
			ShowControlsBasedOnPanel1Position();	// used for sizing and display of some controls

			m_senses = new List<ILexSense>();

			m_btnHelp.Enabled = false; // Not until we know what kind of window it is

			// adjust btnInsert ("Create") and btnOK ("Set Relation"?)
			var ptOK = new Point(m_btnOK.Left, m_btnOK.Top);
			var ptInsert = new Point(m_btnOK.Left, m_btnOK.Top);
			ptOK.X -= m_btnInsert.Width + (m_btnClose.Left - m_btnOK.Right);
			m_btnOK.Location = ptOK;
			m_btnInsert.Location = ptInsert;
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
		/// <param name="propertyTable"></param>
		/// <param name="publisher"></param>
		/// <param name="startingEntry">Entry that cannot be used as a match in this dlg.</param>
		public void SetDlgInfo(FdoCache cache, IPropertyTable propertyTable, IPublisher publisher, ILexEntry startingEntry)
		{
			CheckDisposed();

			//Debug.Assert(startingEntry != null);
			m_startingEntry = startingEntry;

			SetDlgInfo(cache, null, propertyTable, publisher);
		}

		///  <summary>
		///
		///  </summary>
		///  <param name="cache"></param>
		///  <param name="wp"></param>
		/// <param name="propertyTable"></param>
		///  <param name="publisher"></param>
		public override void SetDlgInfo(FdoCache cache, WindowParams wp, IPropertyTable propertyTable, IPublisher publisher)
		{
			CheckDisposed();

			m_fwcbSenses.WritingSystemFactory = cache.LanguageWritingSystemFactoryAccessor;

			base.SetDlgInfo(cache, wp, propertyTable, publisher);
			// This is needed to make the replacement MatchingEntriesBrowser visible:
			Controls.SetChildIndex(m_matchingObjectsBrowser, 0);

			//Set the senses control so that it conforms to the size of the
			//DefaultAnalysisWritingSystem
			IWritingSystem defAnalWs = cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem;
			m_fwcbSenses.WritingSystemCode = defAnalWs.Handle;
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
						m_btnHelp.Enabled = true;
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
			m_senses.Clear();

			bool okBtnEnabled = false;
			bool senseControlsEnabled = false;
			if (m_selObject != null)
			{
				if (m_rbSense.Checked)
				{
					// Add new stuff to sense combo box.
					m_fwcbSenses.SuspendLayout();
					foreach (var sense in ((ILexEntry)m_selObject).AllSenses)
					{
						m_fwcbSenses.AddItem(sense.ChooserNameTS);
						m_senses.Add(sense);
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
						m_fwcbSenses.Text = LexText.Controls.LexTextControls.ksNoSensesInEntry;
					}
				}
				else
				{
					// User is selecting an entry, so enable OK button,
					// but not the sense controls.
					Debug.Assert(m_rbEntry.Checked);
					okBtnEnabled = true;
				}
			}
			// If no entry is selected, nothing new need happen here, since the relevant controls will
			// already be disabled. Indeed, the OK button will have been disabled in the m_tbForm_TextChanged
			// event handler, and this 'code' will never even be called.

			m_btnOK.Enabled = okBtnEnabled;
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
			this.m_panel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_tbForm)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_fwTextBoxBottomMsg)).BeginInit();
			this.grplbl.SuspendLayout();
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
			// m_panel1
			//
			resources.ApplyResources(this.m_panel1, "m_panel1");
			//
			// m_matchingObjectsBrowser
			//
			resources.ApplyResources(this.m_matchingObjectsBrowser, "m_matchingObjectsBrowser");
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
			resources.ApplyResources(this.m_wsLabel, "label1");
			//
			// m_fwTextBoxBottomMsg
			//
			this.m_fwTextBoxBottomMsg.BackColor = System.Drawing.SystemColors.InactiveCaptionText;
			resources.ApplyResources(this.m_fwTextBoxBottomMsg, "m_fwTextBoxBottomMsg");
			//
			// label2
			//
			resources.ApplyResources(this.m_objectsLabel, "label2");
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
			this.m_helpProvider.SetShowHelp(this.grplbl, ((bool)(resources.GetObject("grplbl.ShowHelp"))));
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
			this.m_helpProvider.SetHelpNavigator(this, ((System.Windows.Forms.HelpNavigator)(resources.GetObject("$this.HelpNavigator"))));
			this.Name = "LinkEntryOrSenseDlg";
			this.m_helpProvider.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			this.Controls.SetChildIndex(this.m_fwTextBoxBottomMsg, 0);
			this.Controls.SetChildIndex(this.m_btnInsert, 0);
			this.Controls.SetChildIndex(this.grplbl, 0);
			this.Controls.SetChildIndex(this.groupBox1, 0);
			this.Controls.SetChildIndex(this.m_btnClose, 0);
			this.Controls.SetChildIndex(this.m_btnOK, 0);
			this.Controls.SetChildIndex(this.m_btnHelp, 0);
			this.Controls.SetChildIndex(this.m_panel1, 0);
			this.Controls.SetChildIndex(this.m_matchingObjectsBrowser, 0);
			this.Controls.SetChildIndex(this.m_cbWritingSystems, 0);
			this.Controls.SetChildIndex(this.m_wsLabel, 0);
			this.Controls.SetChildIndex(this.m_objectsLabel, 0);
			this.m_panel1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_tbForm)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_fwTextBoxBottomMsg)).EndInit();
			this.grplbl.ResumeLayout(false);
			this.grplbl.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		#region Event handlers

		private void m_radioButtonClick(object sender, EventArgs e)
		{
			HandleMatchingSelectionChanged();
		}

		#endregion Event handlers

	}
}
