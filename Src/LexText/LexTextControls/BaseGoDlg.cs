// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2003' to='2004' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: BaseGoDlg.cs
// Responsibility: Randy Regnier
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Drawing;
using System.ComponentModel;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Data.SqlClient;
using System.Diagnostics;
using Microsoft.Win32;
using System.Xml;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using XCore;
using SIL.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Widgets;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Summary description for BaseGoDlg.
	/// </summary>
	public class BaseGoDlg : Form, IFWDisposable
	{
		#region	Data members

		protected FdoCache m_cache;
		protected int m_selEntryID;
		protected Set<int> m_vernHvos;
		protected Set<int> m_analHvos;
		protected ITsStrFactory m_tsf;
		/// <summary>
		/// </summary>
		protected Mediator m_mediator;
		/// <summary>
		/// Optional configuration parameters.
		/// </summary>
		//protected XmlNode m_configurationParameters;
		protected bool m_skipCheck = false;
		protected bool m_hasBeenActivated = false;
		protected string m_oldSearchKey;

		protected System.Windows.Forms.HelpProvider helpProvider;
		protected string m_helpTopic = ""; // Default help topic ID

		protected SIL.FieldWorks.Resources.SearchingAnimation m_searchAnimtation;
		/// <summary>
		/// Remember how much we adjusted the height for the lexical form text box.
		/// </summary>
		private int m_delta = 0;

		#region	Designer data members

		private System.ComponentModel.Container components = null;
		protected System.Windows.Forms.Button btnClose;
		protected System.Windows.Forms.Button btnOK;
		protected System.Windows.Forms.Button btnInsert;
		protected System.Windows.Forms.Button btnHelp;
		protected System.Windows.Forms.Panel panel1;
		protected UserControl matchingEntries;
		protected System.Windows.Forms.Label m_formLabel;
		protected SIL.FieldWorks.Common.Widgets.FwTextBox m_tbForm;
		protected FwOverrideComboBox m_cbWritingSystems;
		protected System.Windows.Forms.Label label1;
		protected SIL.FieldWorks.Common.Widgets.FwTextBox m_fwTextBoxBottomMsg;
		protected System.Windows.Forms.Label label2;

		#endregion	// Designer data members

		#endregion	// Data members

		#region Properties

		protected virtual WindowParams DefaultWindowParams
		{
			get
			{
				WindowParams wp = new WindowParams();
				wp.m_label = SIL.FieldWorks.LexText.Controls.LexTextControls.ks_Find_;
				wp.m_btnText = SIL.FieldWorks.LexText.Controls.LexTextControls.ks_GoTo;
				return wp;
			}
		}

		protected virtual string PersistenceLabel
		{
			get { return null; }
		}

		protected virtual string Form
		{
			get { return m_tbForm.Text; }
			set
			{
				if (value == null)
					m_tbForm.Text = "";
				else
					m_tbForm.Text = value;
				m_tbForm.SelectAll();
			}
		}

		protected string UnselectedText
		{
			get
			{
				if (string.IsNullOrEmpty(m_tbForm.Text))
					return string.Empty;

				if (m_tbForm.SelectionLength > 0)
					return m_tbForm.Text.Substring(0, m_tbForm.SelectionStart);
				return m_tbForm.Text;
			}
		}

		/// <summary>
		/// Answer whether the OK button is currently enabled.
		/// </summary>
		public bool IsOkEnabled
		{
			get
			{
				CheckDisposed();
				return btnOK.Enabled;
			}
		}

		/// <summary>
		/// Gets the database id of the selected object.
		/// </summary>
		public virtual int SelectedID
		{
			get
			{
				CheckDisposed();
				return m_selEntryID;
			}
		}

		#endregion Properties

		#region	Construction and Destruction

		/// <summary>
		/// Constructor.
		/// </summary>
		public BaseGoDlg()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			this.helpProvider = new System.Windows.Forms.HelpProvider();
			if (FwApp.App != null)
				this.helpProvider.HelpNamespace = FwApp.App.HelpFile;
			this.helpProvider.SetHelpNavigator(this, System.Windows.Forms.HelpNavigator.Topic);
			this.helpProvider.SetShowHelp(this, true);

			m_vernHvos = new Set<int>();
			m_analHvos = new Set<int>();

			// NB: Don't set this here, because the fake writing system factory
			// will cause an assert down in VwPropertyStore.
			//m_tbForm.Text = "";
			m_tbForm.TextChanged += new EventHandler(m_tbForm_TextChanged);

			// Reset Tab indices of direct child controls of the form.
			ResetTabOrder();

			// If called indirectly from Data Notebook (C++ code), disable the Help since it
			// depends on FwApp.App and FwApp.App.HelpFile.
			// (At one point we also hid the OK button, but it is useful in case the user
			// types something which does have matches and wants to see it.)
			if (FwApp.App == null)
			{
				btnHelp.Enabled = false;
			}

			m_tbForm.KeyDown += new KeyEventHandler(m_tbForm_KeyDown);
			// Now position the searching animation just above the list
			m_searchAnimtation = new SIL.FieldWorks.Resources.SearchingAnimation();
			m_searchAnimtation.Top = matchingEntries.Top - m_searchAnimtation.Height - 5;
			m_searchAnimtation.Left = matchingEntries.Right - m_searchAnimtation.Width - 10;

			// The standard localization code doesn't work, so set these explicitly.
			btnClose.Text = SIL.FieldWorks.LexText.Controls.LexTextControls.ksCancel;
			btnHelp.Text = SIL.FieldWorks.LexText.Controls.LexTextControls.ks_Help_;
			btnInsert.Text = SIL.FieldWorks.LexText.Controls.LexTextControls.ks_Create_;
			label1.Text = SIL.FieldWorks.LexText.Controls.LexTextControls.ks_WritingSystem_;
			label2.Text = SIL.FieldWorks.LexText.Controls.LexTextControls.ksLexicalEntries;

			// remove our initial matching items control, so our clients can add their own.
			this.RemoveInitialMatchingItemsControl();
		}

		protected virtual void ResetTabOrder()
		{
			panel1.TabIndex = 0;
			label1.TabIndex = 1;
			m_cbWritingSystems.TabIndex = 2;
			matchingEntries.TabIndex = 3;
			m_fwTextBoxBottomMsg.TabIndex = 4;
			btnOK.TabIndex = 5;
			btnInsert.TabIndex = 6;
			btnClose.TabIndex = 7;
			btnHelp.TabIndex = 8;
		}
		/// <summary>
		/// translate up and down arrow keys in the Find textbox into moving the selection in
		/// the matching entries list view.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected virtual void m_tbForm_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.KeyCode)
			{
				case Keys.Up:
					e.Handled = true;
					m_tbForm.Select();
					break;
				case Keys.Down:
					e.Handled = true;
					m_tbForm.Select();
					break;
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
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			m_cache = null;
			m_tsf = null;

			base.Dispose( disposing );
		}

		/// <summary>
		/// Set up the dlg in preparation to showing it.
		/// </summary>
		/// <param name="cache">FDO cache.</param>
		/// <param name="wp">Strings used for various items in this dialog.</param>
		public virtual void SetDlgInfo(FdoCache cache, WindowParams wp, Mediator mediator)
		{
			SetDlgInfo(cache, wp, mediator, cache.DefaultVernWs);
		}

		protected virtual void SetDlgInfo(FdoCache cache, WindowParams wp, Mediator mediator, int wsVern)
		{
			CheckDisposed();

			Debug.Assert(cache != null);
			m_cache = cache;

			m_mediator = mediator;

			if (m_mediator != null)
			{
				ReplaceMatchingItemsControl();

				// Reset window location.
				// Get location to the stored values, if any.
				object locWnd = m_mediator.PropertyTable.GetValue(PersistenceLabel + "DlgLocation");
				object szWnd = m_mediator.PropertyTable.GetValue(PersistenceLabel + "DlgSize");
				if (locWnd != null && szWnd != null)
				{
					Rectangle rect = new Rectangle((Point)locWnd, (Size)szWnd);

					//grow it if it's too small.  This will happen when we add new controls to the dialog box.
					if(rect.Width < btnHelp.Left + btnHelp.Width + 30)
						rect.Width = btnHelp.Left + btnHelp.Width + 30;

					if(rect.Height < btnHelp.Top + btnHelp.Height + 50)
						rect.Height = btnHelp.Top + btnHelp.Height + 50;

					//rect.Height = 600;

					ScreenUtils.EnsureVisibleRect(ref rect);
					DesktopBounds = rect;
					StartPosition = FormStartPosition.Manual;
				}
			}

			SetupBasicTextProperties(wp);

			IVwStylesheet stylesheet = FontHeightAdjuster.StyleSheetFromMediator(mediator);
			InitializeMatchingEntries(cache, mediator);
			int hvoWs = wsVern;
			// Set font, writing system factory, and writing system code for the Lexical Form
			// edit box.  Also set an empty string with the proper writing system.
			m_tbForm.Font =
				new Font(cache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(wsVern).DefaultSerif, 10);
			m_tbForm.WritingSystemFactory = cache.LanguageWritingSystemFactoryAccessor;
			m_tbForm.WritingSystemCode = hvoWs;
			m_tsf = TsStrFactoryClass.Create();
			m_tbForm.AdjustStringHeight = false;
			m_tbForm.Tss = m_tsf.MakeString("", hvoWs);
			m_tbForm.StyleSheet = stylesheet;

			// Setup the fancy message text box.
			// Note: at 120DPI (only), it seems to be essential to set at least the WSF of the
			// bottom message even if not using it.
			SetupBottomMsg();
			SetBottomMessage();
			m_fwTextBoxBottomMsg.BorderStyle = BorderStyle.None;

			m_analHvos.AddRange(cache.LangProject.CurAnalysisWssRS.HvoArray);
			List<int> vernList = new List<int>(cache.LangProject.CurVernWssRS.HvoArray);
			m_vernHvos.AddRange(vernList);
			LoadWritingSystemCombo();
			int iWs = vernList.IndexOf(hvoWs);
			ILgWritingSystem lgwsCurrent;
			if (iWs < 0)
			{
				List<int> analList = new List<int>(cache.LangProject.CurAnalysisWssRS.HvoArray);
				iWs = analList.IndexOf(hvoWs);
				if (iWs < 0)
				{
					lgwsCurrent = LgWritingSystem.CreateFromDBObject(cache, hvoWs);
					m_cbWritingSystems.Items.Add(lgwsCurrent);
				}
				else
				{
					lgwsCurrent = cache.LangProject.CurAnalysisWssRS[iWs];
				}
			}
			else
			{
				lgwsCurrent = cache.LangProject.CurVernWssRS[iWs];
			}
			Debug.Assert(lgwsCurrent != null && lgwsCurrent.Hvo == hvoWs);

			m_skipCheck = true;
			m_cbWritingSystems.SelectedItem = lgwsCurrent;
			m_skipCheck = false;
			// Don't hook this up until AFTER we've initialized it; otherwise, it can
			// modify the contents of the form as a side effect of initialization.
			// Also, doing that triggers laying out the dialog prematurely, before
			// we've set WSF on all the controls.
			m_cbWritingSystems.SelectedIndexChanged += new System.EventHandler(this.m_cbWritingSystems_SelectedIndexChanged);


			// Adjust things if the form box needs to grow to accommodate its style.
			int oldHeight = m_tbForm.Height;
			int newHeight = Math.Max(oldHeight, m_tbForm.PreferredHeight);
			int delta = newHeight - oldHeight;
			if (delta != 0)
			{
				m_tbForm.Height = newHeight;
				panel1.Height += delta;
				GrowDialogAndAdjustControls(delta, panel1);
			}
		}

		private void SetupBasicTextProperties(WindowParams wp)
		{
			if (wp == null)
			{
				// NOTE: ideally we'd take visual studio designer's settings for defaults.
				// for now, just have the subclass dialogs override DefaultWindowParams
				// to return 'null' in order to take visual studio designer's
				wp = DefaultWindowParams;
				if (wp == null)
				{
					AdjustControlsToTextResize();
					ShowControlsBasedOnPanel1Position();
					return;	// use visual studio designer's settings.
				}
			}
			Text = wp.m_title;
			m_formLabel.Text = wp.m_label;
			btnOK.Text = wp.m_btnText;
			// The text may be too wide for the button.  See LT-6215.
			if (btnOK.PreferredSize.Width > btnOK.Size.Width)
			{
				int delta = btnOK.PreferredSize.Width - btnOK.Size.Width;
				btnOK.Location = new Point(btnOK.Location.X - delta, btnOK.Location.Y);
				btnOK.Width += delta;
			}

			AdjustControlsToTextResize();
			ShowControlsBasedOnPanel1Position();
		}

		// Changing the text of the label can, unbelievably, move the text box.
		// Adjust things to line up.
		private void AdjustControlsToTextResize()
		{
			int align = Math.Max(m_cbWritingSystems.Left, panel1.Left + m_tbForm.Left);
			if (m_cbWritingSystems.Left != align)
				m_cbWritingSystems.Left = align;
			if (panel1.Left + m_tbForm.Left != align)
				panel1.Left = align - m_tbForm.Left;
			if (label1.Right != panel1.Left + m_formLabel.Right)
			{
				int width = label1.Width;
				int right = panel1.Left + m_formLabel.Right;
				int left = right - width;
				label1.Left = left;
			}
		}

		protected virtual void LoadWritingSystemCombo()
		{
			foreach (ILgWritingSystem ws in m_cache.LangProject.CurAnalysisWssRS)
			{
				m_cbWritingSystems.Items.Add(ws);
			}
			foreach (ILgWritingSystem ws in m_cache.LangProject.CurVernWssRS)
			{
				if (!m_cbWritingSystems.Items.Contains(ws.Hvo))
				{
					m_cbWritingSystems.Items.Add(ws);
				}
			}
		}

		/// <summary>
		/// remove the first matching entries control we setup in BaseGoDlg as a placeholder.
		/// </summary>
		protected void RemoveInitialMatchingItemsControl()
		{
			if (matchingEntries != null && Controls.Contains(matchingEntries))
			{
				// we're going to replace this control.
				Controls.Remove(matchingEntries);
				matchingEntries.Dispose();
				matchingEntries = null;
			}
		}

		protected virtual void InitializeMatchingEntries(FdoCache cache, Mediator mediator)
		{
			// override.
		}

		protected virtual void ReplaceMatchingItemsControl()
		{
			// override.
		}

		protected static void CopyBasicControlInfo(UserControl src, UserControl target)
		{
			target.Location = src.Location;
			target.Size = src.Size;
			target.Name = src.Name;
			target.AccessibleName = src.AccessibleName;
			target.TabStop = src.TabStop;
			target.TabIndex = src.TabIndex;
			target.Anchor = src.Anchor;
		}

		// Grow the dialog's height by delta.
		// Adjust any controls that need it.
		// (Duplicated in BaseGoDlg...)
		private void GrowDialogAndAdjustControls(int delta, Control grower)
		{
			if (delta == 0)
				return;
			m_delta += delta;
			FontHeightAdjuster.GrowDialogAndAdjustControls(this, delta, grower);
		}

		/// <summary>
		/// Overridden to defeat the standard .NET behavior of adjusting size by
		/// screen resolution. That is bad for this dialog because we remember the size,
		/// and if we remember the enlarged size, it just keeps growing.
		/// If we defeat it, it may look a bit small the first time at high resolution,
		/// but at least it will stay the size the user sets.
		/// This also ensures that m_tbForm has the focus when the dialog box first
		/// comes up, even for 120dpi fonts.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnLoad(EventArgs e)
		{
			Size size = this.Size;
			base.OnLoad(e);
			if (this.Size != size)
				this.Size = size;
			// The following is needed for 120dpi fonts, which cause BaseGoDlg_Activated
			// to be called while executing OnLoad() instead of after OnLoad() finishes.
			if (m_hasBeenActivated)
				m_tbForm.Select();
		}

		/// <summary>
		/// Set up the dlg in preparation to showing it.
		/// </summary>
		/// <param name="cache">FDO cache.</param>
		/// <param name="wp">Strings used for various items in this dialog.</param>
		/// <param name="form">Form to use in main text edit box.</param>
		public void SetDlgInfo(FdoCache cache, WindowParams wp, Mediator mediator, string form)
		{
			CheckDisposed();
			SetDlgInfo(cache, wp, mediator, form, cache.DefaultVernWs);

		}

		protected void SetDlgInfo(FdoCache cache, WindowParams wp, Mediator mediator, string form, int vernWs)
		{
			SetDlgInfo(cache, wp, mediator, vernWs);
			Form = form;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="wp"></param>
		/// <param name="mediator"></param>
		/// <param name="tssform">establishes the ws of the dialog.</param>
		public void SetDlgInfo(FdoCache cache, WindowParams wp, Mediator mediator, ITsString tssform)
		{
			CheckDisposed();
			SetDlgInfo(cache, wp, mediator, tssform.Text, StringUtils.GetWsAtOffset(tssform, 0));
		}

		#endregion Construction and Destruction

		#region	Other methods

		/// <summary>
		/// Subclasses should override this, if they have special behavior for messages.
		/// </summary>
		protected virtual void SetBottomMessage()
		{
			SetupBottomMsg();
		}

		/// <summary>
		/// Sets various proerties on the m_fwTextBoxBottomMsg control.
		/// </summary>
		/// <returns>DefaultUserWritingSystem integer</returns>
		protected int SetupBottomMsg()
		{
			int userWs = m_cache.LangProject.DefaultUserWritingSystem;
			m_fwTextBoxBottomMsg.Font = new Font(m_cache.LangProject.DefaultUserWritingSystemFont, 10);
			m_fwTextBoxBottomMsg.WritingSystemFactory = m_cache.LanguageWritingSystemFactoryAccessor;
			m_fwTextBoxBottomMsg.WritingSystemCode = userWs;
			return userWs;
		}

		/// <summary>
		/// Sets the help topic ID for the window.  This is used in both the Help button and when the user hits F1
		/// </summary>
		public void SetHelpTopic(string helpTopic)
		{
			CheckDisposed();

			m_helpTopic = helpTopic;
			if (FwApp.App != null)
			{
				helpProvider.SetHelpKeyword(this, FwApp.App.GetHelpString(m_helpTopic, 0));
				btnHelp.Enabled = true;
			}
		}

		/// <summary>
		/// Reset the list of matching items.
		/// </summary>
		/// <param name="searchKey"></param>
		protected virtual void ResetMatches(string searchKey)
		{
			// override
		}

		/// <summary>
		/// Controls appearance of animation window.
		/// </summary>
		internal bool Searching
		{
			get
			{
				CheckDisposed();
				return Controls.Contains(m_searchAnimtation);
			}
			set
			{
				CheckDisposed();

				if (value && !Controls.Contains(m_searchAnimtation))
				{
					Controls.Add(m_searchAnimtation);
					m_searchAnimtation.BringToFront();
				}
				else if (!value && Controls.Contains(m_searchAnimtation))
					Controls.Remove(m_searchAnimtation);
			}
		}

		protected void ResetForm()
		{
			m_tbForm.Tss = m_tsf.MakeString("", m_tbForm.WritingSystemCode);
			m_tbForm.Focus();
		}

		protected virtual void HandleMatchingSelectionChanged(FwObjectSelectionEventArgs e)
		{
			HandleMatchingSelectionChanged();
		}

		protected virtual void HandleMatchingSelectionChanged()
		{
			btnOK.Enabled = (m_selEntryID > 0);
		}

		protected void ShowControlsBasedOnPanel1Position()
		{
			// Adjust the controls in the panel1 control if needed (make sure they don't overlap)
			if (m_formLabel.Right >= m_tbForm.Left)
				m_tbForm.Left = m_formLabel.Right + 1;	// seperate the controls by at least 1 empty pixel

			//
			// m_cbWritingSystems
			//
			int ypos = panel1.Bottom + 5;
			int xpos = this.matchingEntries.Left;

			m_cbWritingSystems.Location = new System.Drawing.Point(panel1.Left + m_tbForm.Left, ypos);
			label1.Location = new System.Drawing.Point(label1.Left, ypos);

			ypos = this.matchingEntries.Top - label2.Size.Height - 5;
			this.label2.Location = new System.Drawing.Point(xpos, ypos);
		}

		#endregion	// Other methods

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BaseGoDlg));
			this.btnClose = new System.Windows.Forms.Button();
			this.btnOK = new System.Windows.Forms.Button();
			this.btnInsert = new System.Windows.Forms.Button();
			this.btnHelp = new System.Windows.Forms.Button();
			this.panel1 = new System.Windows.Forms.Panel();
			this.m_tbForm = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.m_formLabel = new System.Windows.Forms.Label();
			this.m_cbWritingSystems = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.label1 = new System.Windows.Forms.Label();
			this.m_fwTextBoxBottomMsg = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.matchingEntries = new System.Windows.Forms.UserControl();
			this.panel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_tbForm)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_fwTextBoxBottomMsg)).BeginInit();
			this.SuspendLayout();
			//
			// btnClose
			//
			resources.ApplyResources(this.btnClose, "btnClose");
			this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnClose.Name = "btnClose";
			//
			// btnOK
			//
			resources.ApplyResources(this.btnOK, "btnOK");
			this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOK.Name = "btnOK";
			//
			// btnInsert
			//
			resources.ApplyResources(this.btnInsert, "btnInsert");
			this.btnInsert.Name = "btnInsert";
			this.btnInsert.Click += new System.EventHandler(this.btnInsert_Click);
			//
			// btnHelp
			//
			resources.ApplyResources(this.btnHelp, "btnHelp");
			this.btnHelp.Name = "btnHelp";
			this.btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
			//
			// panel1
			//
			this.panel1.Controls.Add(this.m_tbForm);
			this.panel1.Controls.Add(this.m_formLabel);
			resources.ApplyResources(this.panel1, "panel1");
			this.panel1.Name = "panel1";
			//
			// m_tbForm
			//
			this.m_tbForm.AdjustStringHeight = true;
			this.m_tbForm.AllowMultipleLines = false;
			this.m_tbForm.BackColor = System.Drawing.SystemColors.Window;
			this.m_tbForm.controlID = null;
			resources.ApplyResources(this.m_tbForm, "m_tbForm");
			this.m_tbForm.Name = "m_tbForm";
			this.m_tbForm.SelectionLength = 0;
			this.m_tbForm.SelectionStart = 0;
			//
			// m_formLabel
			//
			resources.ApplyResources(this.m_formLabel, "m_formLabel");
			this.m_formLabel.Name = "m_formLabel";
			//
			// m_cbWritingSystems
			//
			this.m_cbWritingSystems.AllowSpaceInEditBox = false;
			this.m_cbWritingSystems.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.m_cbWritingSystems, "m_cbWritingSystems");
			this.m_cbWritingSystems.Name = "m_cbWritingSystems";
			this.m_cbWritingSystems.Sorted = true;
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			//
			// m_fwTextBoxBottomMsg
			//
			this.m_fwTextBoxBottomMsg.AdjustStringHeight = true;
			this.m_fwTextBoxBottomMsg.AllowMultipleLines = false;
			this.m_fwTextBoxBottomMsg.HasBorder = false;
			resources.ApplyResources(this.m_fwTextBoxBottomMsg, "m_fwTextBoxBottomMsg");
			this.m_fwTextBoxBottomMsg.BackColor = System.Drawing.SystemColors.Control;
			this.m_fwTextBoxBottomMsg.CausesValidation = false;
			this.m_fwTextBoxBottomMsg.controlID = null;
			this.m_fwTextBoxBottomMsg.Name = "m_fwTextBoxBottomMsg";
			this.m_fwTextBoxBottomMsg.SelectionLength = 0;
			this.m_fwTextBoxBottomMsg.SelectionStart = 0;
			//
			// label2
			//
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			//
			// matchingEntries
			//
			resources.ApplyResources(this.matchingEntries, "matchingEntries");
			this.matchingEntries.Name = "matchingEntries";
			this.matchingEntries.TabStop = false;
			//
			// BaseGoDlg
			//
			this.AcceptButton = this.btnOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.btnClose;
			this.Controls.Add(this.matchingEntries);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.m_fwTextBoxBottomMsg);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.m_cbWritingSystems);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.btnHelp);
			this.Controls.Add(this.btnInsert);
			this.Controls.Add(this.btnOK);
			this.Controls.Add(this.btnClose);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "BaseGoDlg";
			this.ShowInTaskbar = false;
			this.Closed += new System.EventHandler(this.BaseGoDlg_Closed);
			this.Activated += new System.EventHandler(this.BaseGoDlg_Activated);
			this.panel1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_tbForm)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_fwTextBoxBottomMsg)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		/// <summary>
		/// Override this to apply BaseGoDlg resource information to a subclass.
		/// </summary>
		protected virtual void InitializeComponentsFromBaseGoDlg()
		{
			//System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BaseGoDlg));
		}
		#endregion

		#region	Event handlers

		protected void m_tbForm_TextChanged(object sender, System.EventArgs e)
		{
			if (m_skipCheck)
				return;

			bool fWantSelect = true;
			string unSelText = AdjustUnselectedText(out fWantSelect);
			int selLocation = unSelText.Length;
			ResetMatches(unSelText);
			// Unnecessary focus changes can cause loss of characters with Yi and Indic languages.
			if (fWantSelect && (m_tbForm.SelectionStart != selLocation || m_tbForm.SelectionLength != 0))
				m_tbForm.Select(selLocation, 0);
		}

		protected virtual string AdjustUnselectedText(out bool fWantSelect)
		{
			// TODO: For each keystroke:
			//		1. If it is a reserved character, then...
			//			(e.g., '-' for prefixes or suffixes).
			//		2. If it is not a wordforming character, then...?
			//		3. If it is a wordforming character, then modify the 'matching entries'
			//			list box, and select the first item in the list.
			string unSelText = UnselectedText.Trim(); ;
			fWantSelect = true;
			if (unSelText != UnselectedText)
			{
				// Note: Yi and Chinese use \x3000 for this.
				if (unSelText + " " == UnselectedText || unSelText + "\x3000" == UnselectedText)
				{
					// It's important (see LT-3770) to allow the user to type a space.
					// So if the only difference is a trailing space, don't adjust the string,
					// and also don't adjust the selection...that produces a stack overflow!
					fWantSelect = false;
				}
				else
				{
					m_skipCheck = true;
					m_tbForm.Text = unSelText;
					m_skipCheck = false;
				}
			}
			return unSelText;
		}

		protected virtual void btnInsert_Click(object sender, System.EventArgs e)
		{
			// override
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display help for this dialog.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void btnHelp_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(FwApp.App, m_helpTopic);
		}

		private void BaseGoDlg_Closed(object sender, System.EventArgs e)
		{
			// Save location.
			if (m_mediator != null)
			{
				m_mediator.PropertyTable.SetProperty(PersistenceLabel + "DlgLocation", Location);
				Size sz = new Size(0, m_delta);
				m_mediator.PropertyTable.SetProperty(PersistenceLabel + "DlgSize", Size - sz);
			}
		}

		protected void matchingEntries_SelectionChanged(object sender,
			SIL.FieldWorks.Common.Utils.FwObjectSelectionEventArgs e)
		{
			if (m_skipCheck)
				return;

			m_selEntryID = e.Hvo;

			HandleMatchingSelectionChanged(e);
		}

		private void BaseGoDlg_Activated(object sender, System.EventArgs e)
		{
			if (m_hasBeenActivated)
				return; // Only do this once.

			m_tbForm.Focus();
			string form = Form.Trim();
			if (form != null && form.Length > 0)
			{
				m_tbForm.Select(form.Length, 0);
				ResetMatches(form);
			}
			else
			{
				m_tbForm.Select(0, 0);
			}
			m_hasBeenActivated = true;
		}

		private void m_cbWritingSystems_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			int start = m_tbForm.SelectionStart;
			int length = m_tbForm.SelectionLength;
			m_tbForm.WritingSystemCode =
				(m_cbWritingSystems.SelectedItem as ILgWritingSystem).Hvo;
			// Change the writing system inside the ITsString.
			ITsStrBldr tsb = m_tbForm.Tss.GetBldr();
			int cch = tsb.Length;
			tsb.SetIntPropValues(0, cch, (int)FwTextPropType.ktptWs, 0,
				m_tbForm.WritingSystemCode);
			m_tbForm.Tss = tsb.GetString();
			//we need to adjust the size of the box based on the changed writing system
			m_tbForm.AdjustForStyleSheet(this, panel1, m_tbForm.StyleSheet);
			// Restore the selection, whether IP or range.
			m_tbForm.Select(start, length);
			m_oldSearchKey = string.Empty;
			if (m_tbForm != null)
				ResetMatches(m_tbForm.Text);
			m_tbForm.Focus();
		}

		protected void matchingEntries_RestoreFocus(object sender, EventArgs e)
		{
			// Set the focus on m_tbForm.
			// Note: due to Keyman/TSF interactions in Indic scripts, do not set focus
			// if it is already set, or we can lose typed characters (e.g., typing poM in
			// Kannada Keyman script causes everything to disappear on M)
			if (!m_tbForm.Focused)
				m_tbForm.Focus();
		}

		#endregion	// Event handlers
	}

	public class BaseEntryGoDlg : BaseGoDlg
	{
		#region	Data members

		protected bool m_fNewlyCreated;
		protected int m_hvoNewSense;
		protected ILexEntry m_startingEntry;
		protected MoMorphTypeCollection m_types;
		protected bool m_useMinorEntries = false;

		#region	Designer data members

		private System.ComponentModel.Container components = null;

		#endregion	// Designer data members

		#endregion	// Data members

		#region Properties

		protected override WindowParams DefaultWindowParams
		{
			get
			{
				WindowParams wp = base.DefaultWindowParams;
				wp.m_title = SIL.FieldWorks.LexText.Controls.LexTextControls.ksFindLexEntry;
				return wp;
			}
		}

		/// <summary>
		/// Get/Set the starting entry object.  This will not be displayed in the list of
		/// matching entries.
		/// </summary>
		public ILexEntry StartingEntry
		{
			get
			{
				CheckDisposed();
				return m_startingEntry;
			}
			set
			{
				CheckDisposed();
				m_startingEntry = value;
			}
		}

		/// <summary>
		/// Override to add any entries that should not be included in the matches list.
		/// </summary>
		protected virtual List<ExtantEntryInfo> FilteredEntries
		{
			get
			{
				List<ExtantEntryInfo> filters = new List<ExtantEntryInfo>();
				if (m_startingEntry != null)
				{
					// Make sure we don't try to merge into the same entry.
					ExtantEntryInfo eei = new ExtantEntryInfo();
					eei.ID = m_startingEntry.Hvo;
					filters.Add(eei);
				}
				return filters;
			}
		}

		protected override string Form
		{
			set
			{
				base.Form = MoForm.EnsureNoMarkers(value, m_cache);
			}
		}

		/// <summary>
		/// Gets or sets control for including minor entries in matched results, or not.
		/// </summary>
		public bool IncludeMinorEntries
		{
			get
			{
				CheckDisposed();
				return m_useMinorEntries;
			}
			set
			{
				CheckDisposed();
				m_useMinorEntries = value;
			}
		}

		#endregion Properties

		#region	Construction and Destruction

		/// <summary>
		/// Constructor.
		/// </summary>
		public BaseEntryGoDlg() : base()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			InitializeComponentsFromBaseGoDlg();
			SetHelpTopic("khtpFindInDictionary"); // Default help topic ID
			m_startingEntry = null;

			// We will add it to controls when we want to show it.
			(matchingEntries as MatchingEntries).SearchingChanged += new EventHandler(matchingEntries_SearchingChanged);

		}

		/// <summary>
		/// translate up and down arrow keys in the Find textbox into moving the selection in
		/// the matching entries list view.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected override void m_tbForm_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.KeyCode)
			{
				case Keys.Up:
					(matchingEntries as MatchingEntries).SelectPrevious();
					break;
				case Keys.Down:
					(matchingEntries as MatchingEntries).SelectNext();
					break;
			}
			base.m_tbForm_KeyDown(sender, e);
		}
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (components != null)
				{
					components.Dispose();
				}
				if (matchingEntries != null)
				{
					if (matchingEntries is MatchingEntries)
					{
						(matchingEntries as MatchingEntries).SearchingChanged -= new EventHandler(matchingEntries_SearchingChanged);
						(matchingEntries as MatchingEntries).RestoreFocus -= new EventHandler(matchingEntries_RestoreFocus);
						(matchingEntries as MatchingEntries).SelectionChanged -= new SIL.FieldWorks.Common.Utils.FwSelectionChangedEventHandler(this.matchingEntries_SelectionChanged);
					}
					matchingEntries.Dispose();
				}

			}
			m_types = null;
			matchingEntries = null;
			m_startingEntry = null;

			base.Dispose(disposing);
		}

		protected override void SetDlgInfo(FdoCache cache, WindowParams wp, Mediator mediator, int wsVern)
		{
			CheckDisposed();
			m_types = new MoMorphTypeCollection(cache);
			base.SetDlgInfo(cache, wp, mediator, wsVern);
		}

		protected override void InitializeMatchingEntries(FdoCache cache, Mediator mediator)
		{
			(matchingEntries as MatchingEntries).Initialize(cache, FontHeightAdjuster.StyleSheetFromMediator(mediator), mediator);
		}

		protected override void ReplaceMatchingItemsControl()
		{
			if (m_mediator == null)
				return;
			XmlNode xnWindow = (XmlNode)m_mediator.PropertyTable.GetValue("WindowConfiguration");
			if (xnWindow == null)
				return;
			XmlNode xnControl = xnWindow.SelectSingleNode("controls/parameters/guicontrol[@id=\"matchingEntries\"]");
			if (xnControl == null)
				return;
			// Replace the current matchingEntries object with the one specified in the XML.
			MatchingEntries newME = DynamicLoader.CreateObject(xnControl) as MatchingEntries;
			if (newME != null)
			{
				CopyBasicControlInfo(matchingEntries, newME);
				this.Controls.Remove(matchingEntries);
				bool fAddSearchingChanged = false;
				if (matchingEntries is MatchingEntries)
				{
					(matchingEntries as MatchingEntries).SelectionChanged -= new SIL.FieldWorks.Common.Utils.FwSelectionChangedEventHandler(this.matchingEntries_SelectionChanged);
					if ((matchingEntries as MatchingEntries).HasSearchingChanged)
					{
						fAddSearchingChanged = true;
						(matchingEntries as MatchingEntries).SearchingChanged -= new EventHandler(matchingEntries_SearchingChanged);
					}
					(matchingEntries as MatchingEntries).RestoreFocus -= new EventHandler(matchingEntries_RestoreFocus);
				}
				matchingEntries.Dispose();
				matchingEntries = newME;
				this.Controls.Add(matchingEntries);
				(matchingEntries as MatchingEntries).SelectionChanged += new SIL.FieldWorks.Common.Utils.FwSelectionChangedEventHandler(this.matchingEntries_SelectionChanged);
				if (fAddSearchingChanged)
					(matchingEntries as MatchingEntries).SearchingChanged += new EventHandler(matchingEntries_SearchingChanged);
				(matchingEntries as MatchingEntries).RestoreFocus += new EventHandler(matchingEntries_RestoreFocus);
				// Reset Tab indices of direct child controls of the form.
				ResetTabOrder();
			}
		}

		#endregion Construction and Destruction

		#region	Other methods

		/// <summary>
		/// Reset the list of matching items.
		/// </summary>
		/// <param name="searchKey"></param>
		protected override void ResetMatches(string searchKey)
		{
			try
			{
				Cursor = Cursors.WaitCursor;
				string sAdjusted;
				IMoMorphType mmt = m_types.GetTypeIfMatchesPrefix(searchKey, out sAdjusted);
				if (mmt != null)
				{
					searchKey = String.Empty;
					btnInsert.Enabled = false;
				}
				else if (searchKey.Length > 0)
				{
					int clsidForm;
					// NB: This method strips off reserved characters for searchKey,
					// which is a good thing.  (fixes LT-802?)
					try
					{
						mmt = MoMorphType.FindMorphType(m_cache, m_types, ref searchKey, out clsidForm);
						btnInsert.Enabled = searchKey.Length > 0;
					}
					catch (Exception ex)
					{
						Cursor = Cursors.Default;
						MessageBox.Show(ex.Message, LexText.Controls.LexTextControls.ksInvalidForm,
							MessageBoxButtons.OK);
						btnInsert.Enabled = false;
						return;
					}
				}
				else
				{
					btnInsert.Enabled = false;
				}

				if (m_oldSearchKey == searchKey)
				{
					Cursor = Cursors.Default;
					return; // Nothing new to do, so skip it.
				}
				else
				{
					// disable Go button until we rebuild our match list.
					btnOK.Enabled = false;
				}
				m_oldSearchKey = searchKey;

				string form = "";
				string gloss = "";
				int wsSelHvo = (m_cbWritingSystems.SelectedItem as ILgWritingSystem).Hvo;
				int vernWs = StringUtils.GetWsAtOffset(m_tbForm.Tss, 0);
				int analWs = m_cache.DefaultAnalWs;
				bool isVernWS = m_vernHvos.Contains(wsSelHvo);
				if (isVernWS && m_analHvos.Contains(wsSelHvo))
				{
					// Ambiguous, so search both.
					vernWs = wsSelHvo;
					analWs = wsSelHvo;
					form = searchKey;
					gloss = searchKey;
				}
				else if (isVernWS)
				{
					vernWs = wsSelHvo;
					form = searchKey;
				}
				else
				{
					vernWs = m_cache.DefaultVernWs;
					analWs = wsSelHvo;
					gloss = searchKey;
				}
				List<ExtantEntryInfo> filters = FilteredEntries;
				(matchingEntries as MatchingEntries).ResetSearch(m_cache,
					0, // m_selEntryID. Use 0 here, or you will have to fix LT-1577 some other way. :-)
					false,
					vernWs, form, form, form,
					analWs, gloss,
					filters);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		#endregion	// Other methods

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
		}

		protected override void InitializeComponentsFromBaseGoDlg()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BaseGoDlg));
			this.matchingEntries = new SIL.FieldWorks.LexText.Controls.MatchingEntries();
			this.SuspendLayout();
			//
			// matchingEntries
			//
			resources.ApplyResources(this.matchingEntries, "matchingEntries");
			this.matchingEntries.Name = "matchingEntries";
			this.matchingEntries.TabStop = false;
			(matchingEntries as MatchingEntries).SelectionChanged += new SIL.FieldWorks.Common.Utils.FwSelectionChangedEventHandler(this.matchingEntries_SelectionChanged);
			this.Controls.Add(this.matchingEntries);
			this.Name = "BaseEntryGoDlg";
			this.ResumeLayout(false);
			this.PerformLayout();
		}
		#endregion

		#region	Event handlers

		private void matchingEntries_SearchingChanged(object sender, EventArgs e)
		{
			this.Searching = (matchingEntries as MatchingEntries).Searching;
		}

		protected override string AdjustUnselectedText(out bool fWantSelect)
		{
			string unSelText = base.AdjustUnselectedText(out fWantSelect);
			// Check whether we need to handle partial marking of a morphtype (suprafix in the
			// default case: see LT-6082).
			string sAdjusted;
			IMoMorphType mmt = m_types.GetTypeIfMatchesPrefix(unSelText, out sAdjusted);
			if (mmt != null && unSelText != sAdjusted)
			{
				m_skipCheck = true;
				m_tbForm.Text = sAdjusted;
				m_skipCheck = false;
			}
			return unSelText;
		}

		/// <summary>
		/// indicate whether SelectedID has been newly created.
		/// </summary>
		public bool SelectedIDNewlyCreated
		{
			get { return m_fNewlyCreated; }
		}

		protected override void btnInsert_Click(object sender, System.EventArgs e)
		{
			using (InsertEntryDlg dlg = new InsertEntryDlg())
			{
				string form = m_tbForm.Text.Trim();
				ITsString tssFormTrimmed = StringUtils.MakeTss(form, StringUtils.GetWsAtOffset(m_tbForm.Tss, 0));
				dlg.SetDlgInfo(m_cache, tssFormTrimmed, m_mediator);
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					dlg.GetDialogInfo(out m_selEntryID, out m_fNewlyCreated);
					if (m_fNewlyCreated)
						m_hvoNewSense = dlg.NewSenseId;
					// If we ever decide not to simulate the btnOK click at this point, then
					// the new sense id will need to be handled by a subclass differently (ie,
					// being added to the list of senses maintained by LinkEntryOrSenseDlg,
					// the selected index into that list also being changed).
					HandleMatchingSelectionChanged();
					if (btnOK.Enabled)
						btnOK.PerformClick();
				}
			}
		}

		#endregion	// Event handlers
	}
}
