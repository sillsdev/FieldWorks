using System;
using System.Collections;
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
	public class LinkMSADlg : BaseEntryGoDlg
	{
		#region Data members

		private System.Windows.Forms.Label label3;
		private SIL.FieldWorks.Common.Widgets.FwComboBox m_fwcbFunctions;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.ComponentModel.IContainer components = null;

		#endregion Data members

		#region Properties

		protected override WindowParams DefaultWindowParams
		{
			get
			{
				WindowParams wp = new WindowParams();
				wp.m_title = LexText.Controls.LexTextControls.ksChooseMorphAndGramInfo;
				wp.m_label = LexText.Controls.LexTextControls.ks_Find_;
				wp.m_btnText = LexText.Controls.LexTextControls.ks_OK;
				return wp;
			}
		}

		protected override string PersistenceLabel
		{
			get { return "LinkMSA"; }
		}

		/// <summary>
		/// Gets the database id of the selected object.
		/// </summary>
		public override int SelectedID
		{
			get
			{
				CheckDisposed();
				return (m_fwcbFunctions.SelectedItem as LMsa).HVO;
			}
		}

		#endregion Properties

		#region	Construction and Destruction

		public LinkMSADlg()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();
			ShowControlsBasedOnPanel1Position();	// used for sizing and display of some controls

			btnInsert.Enabled = false;
			btnHelp.Enabled = true;

			this.SetHelpTopic("khtpInsertMorphemeChooseFunction");
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
			SetComboWritingSystemFactory(cache);
		}

		/// <summary>
		/// Common and needed code for the setup of the dlg
		/// </summary>
		/// <param name="cache"></param>
		private void SetComboWritingSystemFactory(FdoCache cache)
		{
			m_fwcbFunctions.WritingSystemFactory = cache.LanguageWritingSystemFactoryAccessor;
			m_fwcbFunctions.WritingSystemCode = cache.LangProject.DefaultAnalysisWritingSystem;
		}

		/// <summary>
		/// Set up the dlg in preparation to showing it.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="wp"></param>
		/// <param name="mediator"></param>
		public override void SetDlgInfo(FdoCache cache, WindowParams wp, Mediator mediator)
		{
			base.SetDlgInfo(cache, wp, mediator);
			// This is needed to make the replacement MatchingEntriesBrowser visible:
			this.Controls.SetChildIndex(this.matchingEntries, 0);
			// LT-6325 fix...
			SetComboWritingSystemFactory(cache);
		}
		#endregion	Construction and Destruction

		#region	Other methods

		protected override void HandleMatchingSelectionChanged()
		{
			m_fwcbFunctions.Items.Clear();
			if (m_selEntryID == 0)
				return;
			m_fwcbFunctions.SuspendLayout();
			ILexEntry le = LexEntry.CreateFromDBObject(m_cache, m_selEntryID);
			foreach (IMoMorphSynAnalysis msa in le.MorphoSyntaxAnalysesOC)
				m_fwcbFunctions.Items.Add(new LMsa(msa));
			if (m_fwcbFunctions.Items.Count > 0)
				m_fwcbFunctions.SelectedItem = m_fwcbFunctions.Items[0];
			btnOK.Enabled = m_fwcbFunctions.Items.Count > 0;
			m_fwcbFunctions.ResumeLayout();
		}

		#endregion	Other methods

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LinkMSADlg));
			this.m_fwcbFunctions = new SIL.FieldWorks.Common.Widgets.FwComboBox();
			this.label3 = new System.Windows.Forms.Label();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
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
			resources.ApplyResources(this.m_fwTextBoxBottomMsg, "m_fwTextBoxBottomMsg");
			//
			// label2
			//
			resources.ApplyResources(this.label2, "label2");
			//
			// m_fwcbFunctions
			//
			resources.ApplyResources(this.m_fwcbFunctions, "m_fwcbFunctions");
			this.m_fwcbFunctions.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_fwcbFunctions.DropDownWidth = 123;
			this.m_fwcbFunctions.DroppedDown = false;
			this.m_fwcbFunctions.Name = "m_fwcbFunctions";
			this.m_fwcbFunctions.PreviousTextBoxText = null;
			this.m_fwcbFunctions.SelectedIndex = -1;
			this.m_fwcbFunctions.SelectedItem = null;
			this.m_fwcbFunctions.StyleSheet = null;
			//
			// label3
			//
			resources.ApplyResources(this.label3, "label3");
			this.label3.Name = "label3";
			//
			// groupBox1
			//
			resources.ApplyResources(this.groupBox1, "groupBox1");
			this.groupBox1.ForeColor = System.Drawing.SystemColors.ActiveCaption;
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.TabStop = false;
			//
			// groupBox2
			//
			resources.ApplyResources(this.groupBox2, "groupBox2");
			this.groupBox2.ForeColor = System.Drawing.SystemColors.ActiveCaption;
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.TabStop = false;
			//
			// LinkMSADlg
			//
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this.m_fwcbFunctions);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.groupBox2);
			this.helpProvider.SetHelpNavigator(this, ((System.Windows.Forms.HelpNavigator)(resources.GetObject("$this.HelpNavigator"))));
			this.Name = "LinkMSADlg";
			this.helpProvider.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			this.Controls.SetChildIndex(this.groupBox2, 0);
			this.Controls.SetChildIndex(this.groupBox1, 0);
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
			this.Controls.SetChildIndex(this.label3, 0);
			this.Controls.SetChildIndex(this.m_fwcbFunctions, 0);
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
