using System.Diagnostics;
using System.Windows.Forms;

using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	public class LinkMSADlg : EntryGoDlg
	{
		#region Data members

		private Label label3;
		private FwComboBox m_fwcbFunctions;
		private GroupBox groupBox1;
		private GroupBox groupBox2;

		#endregion Data members

		#region Properties

		protected override WindowParams DefaultWindowParams
		{
			get
			{
				return new WindowParams
				{
					m_title = LexTextControls.ksChooseMorphAndGramInfo,
					m_btnText = LexTextControls.ks_OK
				};
			}
		}

		protected override string PersistenceLabel
		{
			get { return "LinkMSA"; }
		}

		/// <summary>
		/// Gets the database id of the selected object.
		/// </summary>
		public override ICmObject SelectedObject
		{
			get
			{
				CheckDisposed();
				return m_cache.ServiceLocator.GetInstance<IMoMorphSynAnalysisRepository>().GetObject(((LMsa)m_fwcbFunctions.SelectedItem).HVO);
			}
		}

		#endregion Properties

		#region	Construction and Destruction

		public LinkMSADlg()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();
			ShowControlsBasedOnPanel1Position();	// used for sizing and display of some controls

			m_btnInsert.Enabled = false;
			m_btnHelp.Enabled = true;

			SetHelpTopic("khtpInsertMorphemeChooseFunction");
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
			m_fwcbFunctions.WritingSystemFactory = cache.WritingSystemFactory;
			m_fwcbFunctions.WritingSystemCode = cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle;
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
			Controls.SetChildIndex(m_matchingObjectsBrowser, 0);
			// LT-6325 fix...
			SetComboWritingSystemFactory(cache);
		}
		#endregion	Construction and Destruction

		#region	Other methods

		protected override void HandleMatchingSelectionChanged()
		{
			m_fwcbFunctions.Items.Clear();
			if (m_selObject == null)
				return;
			m_fwcbFunctions.SuspendLayout();
			foreach (var msa in ((ILexEntry)m_selObject).MorphoSyntaxAnalysesOC)
				m_fwcbFunctions.Items.Add(new LMsa(msa));
			if (m_fwcbFunctions.Items.Count > 0)
				m_fwcbFunctions.SelectedItem = m_fwcbFunctions.Items[0];
			m_btnOK.Enabled = m_fwcbFunctions.Items.Count > 0;
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
			resources.ApplyResources(this.m_fwTextBoxBottomMsg, "m_fwTextBoxBottomMsg");
			//
			// label2
			//
			resources.ApplyResources(this.m_objectsLabel, "label2");
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
			this.m_helpProvider.SetHelpNavigator(this, ((System.Windows.Forms.HelpNavigator)(resources.GetObject("$this.HelpNavigator"))));
			this.Name = "LinkMSADlg";
			this.m_helpProvider.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			this.Controls.SetChildIndex(this.groupBox2, 0);
			this.Controls.SetChildIndex(this.groupBox1, 0);
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
			this.Controls.SetChildIndex(this.label3, 0);
			this.Controls.SetChildIndex(this.m_fwcbFunctions, 0);
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
