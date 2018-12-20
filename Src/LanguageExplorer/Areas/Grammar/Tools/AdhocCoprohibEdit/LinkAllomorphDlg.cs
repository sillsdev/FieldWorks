// Copyright (c) 2005-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.LexText;
using SIL.LCModel;

namespace LanguageExplorer.Areas.Grammar.Tools.AdhocCoprohibEdit
{
	public class LinkAllomorphDlg : EntryGoDlg
	{
		private Label label3;
		private FwComboBox m_fwcbAllomorphs;
		private GroupBox groupBox1;
		private GroupBox grplbl;

		#region Properties

		protected override WindowParams DefaultWindowParams => new WindowParams
		{
			m_title = LexTextControls.ksChooseAllomorph,
			m_btnText = LexTextControls.ks_OK
		};

		protected override string PersistenceLabel => "LinkAllomorph";

		/// <summary>
		/// Gets the database id of the selected object.
		/// </summary>
		public override ICmObject SelectedObject => m_cache.ServiceLocator.GetInstance<IMoFormRepository>().GetObject(((LAllomorph)m_fwcbAllomorphs.SelectedItem).HVO);
		#endregion Properties

		/// <summary>
		/// This method handles the 'form' text change so that when it goes back to a length of zero,
		/// the allomorphs combo box is made empty.
		/// (This is a refinement of the dlg to act 'more correctly'.)
		/// </summary>
		protected void Form_TextChanged(object sender, EventArgs e)
		{
			if (m_tbForm.Text.Length == 0)
			{
				m_fwcbAllomorphs.Text = "";     // clear the text box
				m_fwcbAllomorphs.Items.Clear(); // clear the drop down list box
			}
		}

		#region	Construction and Destruction

		public LinkAllomorphDlg()
		{
			InitializeComponent();
			SetHelpTopic("hktpInsertAllomorphChooseAllomorph");
			m_tbForm.TextChanged += Form_TextChanged;           // erase when needed
			ShowControlsBasedOnPanel1Position();    // make sure controls are all set properly
			m_btnInsert.Enabled = false;
			m_btnHelp.Enabled = true;
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
		public void SetDlgInfo(LcmCache cache, ILexEntry startingEntry)
		{
			Debug.Assert(startingEntry != null);
			m_startingEntry = startingEntry;

			SetDlgInfo(cache, (WindowParams)null);
		}

		/// <summary>
		/// Common init of the dialog, whether or not we have a starting lex entry.
		/// </summary>
		public override void SetDlgInfo(LcmCache cache, WindowParams wp)
		{
			base.SetDlgInfo(cache, wp);
			// This is needed to make the replacement MatchingEntriesBrowser visible:
			Controls.SetChildIndex(m_matchingObjectsBrowser, 0);

			m_fwcbAllomorphs.WritingSystemFactory = cache.WritingSystemFactory;
			m_fwcbAllomorphs.WritingSystemCode = cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle;
			// For a resizeable dialog, we don't want AdjustForStylesheet to really change its size,
			// because then it ends up growing every time it launches!
			var oldHeight = Height;
			m_fwcbAllomorphs.AdjustForStyleSheet(this, grplbl, PropertyTable);
			Height = oldHeight;
		}

		#endregion	Construction and Destruction

		#region	Other methods

		protected override void HandleMatchingSelectionChanged()
		{
			m_fwcbAllomorphs.Items.Clear();
			m_fwcbAllomorphs.Text = string.Empty;
			if (m_selObject == null)
			{
				return;
			}
			m_fwcbAllomorphs.SuspendLayout();
			/* NB: We remove abstract MoForms, because the adhoc allo coprohibiton object wants them removed.
			 * If any other client of this dlg comes along that wants them,
			 * we will need to feed in a parameter that tells us whether to exclude them or not.
			*/
			// Add the lexeme form, if it exists.
			var entry = (ILexEntry)m_selObject;
			var lf = entry.LexemeFormOA;
			if (lf != null && !lf.IsAbstract)
			{
				m_fwcbAllomorphs.Items.Add(new LAllomorph(entry.LexemeFormOA));
			}
			foreach (var allo in entry.AlternateFormsOS)
			{
				if (!allo.IsAbstract)
				{
					m_fwcbAllomorphs.Items.Add(new LAllomorph(allo));
				}
			}
			if (m_fwcbAllomorphs.Items.Count > 0)
			{
				m_fwcbAllomorphs.SelectedItem = m_fwcbAllomorphs.Items[0];
			}
			m_btnOK.Enabled = m_fwcbAllomorphs.Items.Count > 0;
			m_fwcbAllomorphs.ResumeLayout();
			// For a resizeable dialog, we don't want AdjustForStylesheet to really change its size,
			// because then it ends up growing every time it launches!
			var oldHeight = Height;
			m_fwcbAllomorphs.AdjustForStyleSheet(this, grplbl, PropertyTable);
			Height = oldHeight;
		}

		#endregion	Other methods

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LinkAllomorphDlg));
			this.label3 = new System.Windows.Forms.Label();
			this.m_fwcbAllomorphs = new LanguageExplorer.Controls.FwComboBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.grplbl = new System.Windows.Forms.GroupBox();
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
			// m_panel1
			//
			resources.ApplyResources(this.m_panel1, "m_panel1");
			//
			// m_matchingObjectsBrowser
			//
			resources.ApplyResources(this.m_matchingObjectsBrowser, "m_matchingObjectsBrowser");
			//
			// m_fwTextBoxBottomMsg
			//
			resources.ApplyResources(this.m_fwTextBoxBottomMsg, "m_fwTextBoxBottomMsg");
			//
			// label3
			//
			resources.ApplyResources(this.label3, "label3");
			this.label3.Name = "label3";
			//
			// m_fwcbAllomorphs
			//
			resources.ApplyResources(this.m_fwcbAllomorphs, "m_fwcbAllomorphs");
			this.m_fwcbAllomorphs.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_fwcbAllomorphs.DropDownWidth = 200;
			this.m_fwcbAllomorphs.DroppedDown = false;
			this.m_fwcbAllomorphs.Name = "m_fwcbAllomorphs";
			this.m_fwcbAllomorphs.PreviousTextBoxText = null;
			this.m_fwcbAllomorphs.SelectedIndex = -1;
			this.m_fwcbAllomorphs.SelectedItem = null;
			this.m_fwcbAllomorphs.StyleSheet = null;
			//
			// groupBox1
			//
			resources.ApplyResources(this.groupBox1, "groupBox1");
			this.groupBox1.ForeColor = System.Drawing.SystemColors.ActiveCaption;
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.TabStop = false;
			//
			// grplbl
			//
			resources.ApplyResources(this.grplbl, "grplbl");
			this.grplbl.ForeColor = System.Drawing.SystemColors.ActiveCaption;
			this.grplbl.Name = "grplbl";
			this.grplbl.TabStop = false;
			//
			// LinkAllomorphDlg
			//
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this.m_fwcbAllomorphs);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.grplbl);
			this.m_helpProvider.SetHelpNavigator(this, ((System.Windows.Forms.HelpNavigator)(resources.GetObject("$this.HelpNavigator"))));
			this.Name = "LinkAllomorphDlg";
			this.m_helpProvider.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			this.Controls.SetChildIndex(this.grplbl, 0);
			this.Controls.SetChildIndex(this.groupBox1, 0);
			this.Controls.SetChildIndex(this.label3, 0);
			this.Controls.SetChildIndex(this.m_fwcbAllomorphs, 0);
			this.Controls.SetChildIndex(this.m_btnClose, 0);
			this.Controls.SetChildIndex(this.m_btnOK, 0);
			this.Controls.SetChildIndex(this.m_btnInsert, 0);
			this.Controls.SetChildIndex(this.m_btnHelp, 0);
			this.Controls.SetChildIndex(this.m_panel1, 0);
			this.Controls.SetChildIndex(this.m_matchingObjectsBrowser, 0);
			this.Controls.SetChildIndex(this.m_cbWritingSystems, 0);
			this.Controls.SetChildIndex(this.m_wsLabel, 0);
			this.Controls.SetChildIndex(this.m_fwTextBoxBottomMsg, 0);
			this.Controls.SetChildIndex(this.m_objectsLabel, 0);
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