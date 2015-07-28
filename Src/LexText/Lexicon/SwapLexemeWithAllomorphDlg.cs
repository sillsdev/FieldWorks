// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using XCore;
using SIL.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using System.Diagnostics.CodeAnalysis;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// Summary description for SwapLexemeWithAllomorphDlg.
	/// </summary>
	public class SwapLexemeWithAllomorphDlg : Form, IFWDisposable
	{
		private FwTextBox m_fwTextBoxBottomMsg;
		private FdoCache m_cache;
		private ILexEntry m_entry;
		private IMoForm m_allomorph;
		private ITsStrFactory m_tsf;
		private Mediator m_mediator;

		private Label label2;
		private PictureBox pictureBox1;
		private Button btnOK;
		private Button btnClose;
		private ListView m_lvAlloOptions;
		private ColumnHeader m_chItems;
		private Button buttonHelp;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		private string m_helpTopic;
		private HelpProvider helpProvider;

		public IMoForm SelectedAllomorph
		{
			get
			{
				CheckDisposed();
				return m_allomorph;
			}
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "infoIcon is a reference")]
		public SwapLexemeWithAllomorphDlg()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			AccessibleName = GetType().Name;

			SuspendLayout();
			m_fwTextBoxBottomMsg = new FwTextBox();
			((System.ComponentModel.ISupportInitialize)(m_fwTextBoxBottomMsg)).BeginInit();
			//
			// m_fwTextBoxBottomMsg
			//
			m_fwTextBoxBottomMsg.BackColor = SystemColors.Control;
			m_fwTextBoxBottomMsg.HasBorder = false;
			m_fwTextBoxBottomMsg.CausesValidation = false;
			m_fwTextBoxBottomMsg.controlID = null;
			m_fwTextBoxBottomMsg.Enabled = false;
			m_fwTextBoxBottomMsg.Location = new Point(46, 240);
			m_fwTextBoxBottomMsg.Name = "m_fwTextBoxBottomMsg";
			m_fwTextBoxBottomMsg.SelectionLength = 0;
			m_fwTextBoxBottomMsg.SelectionStart = 0;
			m_fwTextBoxBottomMsg.Size = new Size(386, 45);
			m_fwTextBoxBottomMsg.TabIndex = 1;
			// Can't do this yet as m_cache is not set until SetDlgInfo() is run.
			//m_fwTextBoxBottomMsg.WritingSystemFactory = m_cache.WritingSystemFactory;
			//m_fwTextBoxBottomMsg.WritingSystemCode = 1;
			Controls.Add(m_fwTextBoxBottomMsg);

			m_lvAlloOptions.TabIndex = 0;
			btnOK.TabIndex = 2;
			btnClose.TabIndex = 3;
			var infoIcon = SystemIcons.Information;
			pictureBox1.Image = infoIcon.ToBitmap();
			pictureBox1.Size = infoIcon.Size;
			ResumeLayout(false);
		}

		/// <summary>
		/// Set up the dlg in preparation to showing it.
		/// </summary>
		/// <param name="cache">FDO cache.</param>
		/// <param name="mediator"></param>
		/// <param name="entry">LexEntry</param>
		public void SetDlgInfo(FdoCache cache, Mediator mediator, ILexEntry entry)
		{
			CheckDisposed();

			Debug.Assert(cache != null);

			m_mediator = mediator;
			m_cache = cache;
			m_entry = entry;
			m_tsf = m_cache.TsStrFactory;
			m_fwTextBoxBottomMsg.WritingSystemFactory = m_cache.WritingSystemFactory;
			//m_fwTextBoxBottomMsg.WritingSystemCode = 1; // What!? Why? No longer makes ANY sense!
			IVwStylesheet stylesheet = FontHeightAdjuster.StyleSheetFromMediator(mediator);
			// We want to do this BEFORE the text gets set, to avoid overriding its height properties.
			// However, because of putting multiple lines in the box, we also need to do it AFTER we set the text
			// (in SetBottomMessage) so it adjusts to the resulting even greater height.
			m_fwTextBoxBottomMsg.AdjustForStyleSheet(this, null, stylesheet);
			Font f = FontHeightAdjuster.GetFontForNormalStyle(
				m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle, stylesheet, m_cache.LanguageWritingSystemFactoryAccessor);
			foreach (IMoForm allo in entry.AlternateFormsOS)
			{
				ListViewItem lvi = m_lvAlloOptions.Items.Add(allo.Form.VernacularDefaultWritingSystem.Text);
				lvi.Tag = allo;
				lvi.UseItemStyleForSubItems = true;
				lvi.Font = f;
			}
			m_lvAlloOptions.Font = f;
			// Get location to the stored values, if any.
			object locWnd = m_mediator.PropertyTable.GetValue("swapDlgLocation");
			// And when I do this, it works the first time, but later times the window is
			// too small and doesn't show all the controls. Give up on smart location for now.
			//object szWnd = this.Size;
			object szWnd = null; // suppresses the smart location stuff.
			//if (locWnd != null && szWnd != null)
			//{
			//    Rectangle rect = new Rectangle((Point)locWnd, (Size)szWnd);
			//    ScreenUtils.EnsureVisibleRect(ref rect);
			//    DesktopBounds = rect;
			//    StartPosition = FormStartPosition.Manual;
			//}
			m_lvAlloOptions.Items[0].Selected = true;
			Text = LexEdStrings.ksSwapLexWithAllo;
			label2.Text = LexEdStrings.ksAlternateForms;

			// Determine the help file to use, if any
			m_helpTopic = "khtpSwapLexemeWithAllomorph";

			if(m_mediator.HelpTopicProvider != null)
			{
				helpProvider = new HelpProvider();
				helpProvider.HelpNamespace = m_mediator.HelpTopicProvider.HelpFile;
				helpProvider.SetHelpKeyword(this, m_mediator.HelpTopicProvider.GetHelpString(m_helpTopic));
				helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			}
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				Controls.Remove(m_fwTextBoxBottomMsg);
				m_fwTextBoxBottomMsg.Dispose();
				if(components != null)
				{
					components.Dispose();
				}
			}
			m_fwTextBoxBottomMsg = null;
			m_mediator = null;
			if (m_tsf != null)
				System.Runtime.InteropServices.Marshal.ReleaseComObject(m_tsf);
			m_tsf = null;
			m_cache = null;

			base.Dispose( disposing );
		}

		#region	Other methods

		private void SetBottomMessage()
		{
			int userWs = m_cache.ServiceLocator.WritingSystemManager.UserWs;
			m_fwTextBoxBottomMsg.WritingSystemFactory = m_cache.LanguageWritingSystemFactoryAccessor;
			m_fwTextBoxBottomMsg.WritingSystemCode = userWs;
			string sLexVal = m_entry.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text;
			// Treat null value as empty string.  This fixes LT-5889, LT-5891, and LT-5914.
			if (sLexVal == null)
				sLexVal = "";
			ITsString tss;
			string sFmt = LexEdStrings.ksSwapXWithY;
			string sWithVal = m_allomorph.Form.VernacularDefaultWritingSystem.Text;
			if (sWithVal == null)
				sWithVal = "";
			tss = m_tsf.MakeString(String.Format(sFmt, sLexVal, sWithVal, StringUtils.kChHardLB), userWs);
			m_fwTextBoxBottomMsg.Tss = tss;
			// Do this AFTER setting the selected item, since that changes the text of the box and the needed size.
			//m_fwTextBoxBottomMsg.AdjustForStyleSheet(this, null, m_mediator);
		}
		#endregion Other methods

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SwapLexemeWithAllomorphDlg));
			this.label2 = new System.Windows.Forms.Label();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.btnOK = new System.Windows.Forms.Button();
			this.btnClose = new System.Windows.Forms.Button();
			this.m_lvAlloOptions = new System.Windows.Forms.ListView();
			this.m_chItems = new System.Windows.Forms.ColumnHeader();
			this.buttonHelp = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.SuspendLayout();
			//
			// label2
			//
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			//
			// pictureBox1
			//
			resources.ApplyResources(this.pictureBox1, "pictureBox1");
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.TabStop = false;
			//
			// btnOK
			//
			this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			resources.ApplyResources(this.btnOK, "btnOK");
			this.btnOK.Name = "btnOK";
			//
			// btnClose
			//
			this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.btnClose, "btnClose");
			this.btnClose.Name = "btnClose";
			//
			// m_lvAlloOptions
			//
			this.m_lvAlloOptions.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.m_chItems});
			this.m_lvAlloOptions.FullRowSelect = true;
			this.m_lvAlloOptions.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.m_lvAlloOptions.HideSelection = false;
			resources.ApplyResources(this.m_lvAlloOptions, "m_lvAlloOptions");
			this.m_lvAlloOptions.MultiSelect = false;
			this.m_lvAlloOptions.Name = "m_lvAlloOptions";
			this.m_lvAlloOptions.UseCompatibleStateImageBehavior = false;
			this.m_lvAlloOptions.View = System.Windows.Forms.View.Details;
			this.m_lvAlloOptions.SelectedIndexChanged += new System.EventHandler(this.m_lvAlloOptions_SelectedIndexChanged);
			this.m_lvAlloOptions.DoubleClick += new System.EventHandler(this.m_lvAlloOptions_DoubleClick);
			//
			// m_chItems
			//
			resources.ApplyResources(this.m_chItems, "m_chItems");
			//
			// buttonHelp
			//
			resources.ApplyResources(this.buttonHelp, "buttonHelp");
			this.buttonHelp.Name = "buttonHelp";
			this.buttonHelp.Click += new System.EventHandler(this.buttonHelp_Click);
			//
			// SwapLexemeWithAllomorphDlg
			//
			this.AcceptButton = this.btnOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.btnClose;
			this.CausesValidation = false;
			this.ControlBox = false;
			this.Controls.Add(this.buttonHelp);
			this.Controls.Add(this.m_lvAlloOptions);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.pictureBox1);
			this.Controls.Add(this.btnOK);
			this.Controls.Add(this.btnClose);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "SwapLexemeWithAllomorphDlg";
			this.ShowInTaskbar = false;
			this.Closed += new System.EventHandler(this.SwapLexemeWithAllomorphDlg_Closed);
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion

		private void m_lvAlloOptions_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			SetSelectedObject();
		}

		private void SetSelectedObject()
		{
			foreach (ListViewItem lvi in m_lvAlloOptions.Items)
			{
				if (lvi.Selected)
				{
					m_allomorph = lvi.Tag as IMoForm;
					SetBottomMessage();
				}
			}
			btnOK.Enabled = true;
		}

		private void m_lvAlloOptions_DoubleClick(object sender, System.EventArgs e)
		{
			SetSelectedObject();
			btnOK.PerformClick();
		}

		private void SwapLexemeWithAllomorphDlg_Closed(object sender, System.EventArgs e)
		{
			if (m_mediator != null)
			{
				m_mediator.PropertyTable.SetProperty("swapDlgLocation", Location);
			}
		}

		private void buttonHelp_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_mediator.HelpTopicProvider, m_helpTopic);
		}
	}
}
