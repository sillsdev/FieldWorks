// Copyright (c) 2003-2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: BaseGoDlg.cs
// Responsibility: Randy Regnier

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Resources;
using SIL.Utils;
using XCore;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Widgets;
using SIL.CoreImpl;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary/>
	public class BaseGoDlg : Form, IFWDisposable
	{
		#region	Data members

		protected FdoCache m_cache;
		protected IHelpTopicProvider m_helpTopicProvider;
		protected ICmObject m_selObject;
		protected HashSet<int> m_vernHvos;
		protected HashSet<int> m_analHvos;
		protected ITsStrFactory m_tsf;
		/// <summary>
		/// </summary>
		protected Mediator m_mediator;
		/// <summary>
		/// Optional configuration parameters.
		/// </summary>
		//protected XmlNode m_configurationParameters;
		protected bool m_skipCheck;
		protected bool m_hasBeenActivated;
		protected string m_oldSearchKey;

		protected HelpProvider m_helpProvider;
		protected string m_helpTopic = ""; // Default help topic ID

		protected SearchingAnimation m_searchAnimation;
		/// <summary>
		/// Remember how much we adjusted the height for the lexical form text box.
		/// </summary>
		private int m_delta;

		#region	Designer data members

		protected Button m_btnClose;
		protected Button m_btnOK;
		protected Button m_btnInsert;
		protected Button m_btnHelp;
		protected Panel m_panel1;
		protected MatchingObjectsBrowser m_matchingObjectsBrowser;
		protected Label m_formLabel;
		protected FwTextBox m_tbForm;
		protected FwOverrideComboBox m_cbWritingSystems;
		protected Label m_wsLabel;
		protected FwTextBox m_fwTextBoxBottomMsg;
		protected Label m_objectsLabel;

		#endregion	// Designer data members

		#endregion	// Data members

		#region Properties

		protected virtual WindowParams DefaultWindowParams
		{
			get
			{
				return null;
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
				m_tbForm.Text = value ?? "";
				m_tbForm.SelectAll();
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
				return m_btnOK.Enabled;
			}
		}

		/// <summary>
		/// Gets the database id of the selected object.
		/// </summary>
		public virtual ICmObject SelectedObject
		{
			get
			{
				CheckDisposed();
				return m_selObject;
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
			AccessibleName = GetType().Name;

			m_helpProvider = new HelpProvider();
			m_helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			m_helpProvider.SetShowHelp(this, true);

			m_vernHvos = new HashSet<int>();
			m_analHvos = new HashSet<int>();

			// NB: Don't set this here, because the fake writing system factory
			// will cause an assert down in VwPropertyStore.
			//m_tbForm.Text = "";
			m_tbForm.TextChanged += m_tbForm_TextChanged;

			m_tbForm.KeyDown += m_tbForm_KeyDown;
			// Now position the searching animation just above the list
			m_searchAnimation = new SearchingAnimation();
			m_searchAnimation.Top = m_matchingObjectsBrowser.Top - m_searchAnimation.Height - 5;
			m_searchAnimation.Left = m_matchingObjectsBrowser.Right - m_searchAnimation.Width - 10;

			// The standard localization code doesn't work, so set these explicitly.
			m_btnClose.Text = LexTextControls.ksCancel;
			m_btnHelp.Text = LexTextControls.ks_Help_;
			m_btnInsert.Text = LexTextControls.ks_Create_;
			m_wsLabel.Text = LexTextControls.ks_WritingSystem_;
			m_objectsLabel.Text = LexTextControls.ksLexicalEntries;
			m_oldSearchKey = string.Empty;
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
					m_matchingObjectsBrowser.SelectPrevious();
					e.Handled = true;
					m_tbForm.Select();
					break;
				case Keys.Down:
					m_matchingObjectsBrowser.SelectNext();
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
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
			}
			m_cache = null;
			m_tsf = null;

			base.Dispose(disposing);
		}

		/// <summary>
		/// Set up the dlg in preparation to showing it.
		/// </summary>
		/// <param name="cache">FDO cache.</param>
		/// <param name="wp">Strings used for various items in this dialog.</param>
		/// <param name="mediator"></param>
		public virtual void SetDlgInfo(FdoCache cache, WindowParams wp, Mediator mediator)
		{
			SetDlgInfo(cache, wp, mediator, cache.DefaultVernWs);
		}

		protected virtual void SetDlgInfo(FdoCache cache, WindowParams wp, Mediator mediator, int ws)
		{
			CheckDisposed();

			Debug.Assert(cache != null);
			m_cache = cache;
			m_tsf = cache.TsStrFactory; // do this very early, other initializers may depend on it.

			m_mediator = mediator;

			if (m_mediator != null)
			{
				// Reset window location.
				// Get location to the stored values, if any.
				object locWnd = m_mediator.PropertyTable.GetValue(PersistenceLabel + "DlgLocation");
				object szWnd = m_mediator.PropertyTable.GetValue(PersistenceLabel + "DlgSize");
				if (locWnd != null && szWnd != null)
				{
					var rect = new Rectangle((Point)locWnd, (Size)szWnd);

					//grow it if it's too small.  This will happen when we add new controls to the dialog box.
					if (rect.Width < m_btnHelp.Left + m_btnHelp.Width + 30)
						rect.Width = m_btnHelp.Left + m_btnHelp.Width + 30;

					if (rect.Height < m_btnHelp.Top + m_btnHelp.Height + 50)
						rect.Height = m_btnHelp.Top + m_btnHelp.Height + 50;

					//rect.Height = 600;

					ScreenUtils.EnsureVisibleRect(ref rect);
					DesktopBounds = rect;
					StartPosition = FormStartPosition.Manual;
				}

				m_helpTopicProvider = m_mediator.HelpTopicProvider;
				if (m_helpTopicProvider != null)
				{
					m_helpProvider.HelpNamespace = m_helpTopicProvider.HelpFile;
					SetHelpButtonEnabled();
				}

			}

			SetupBasicTextProperties(wp);

			IVwStylesheet stylesheet = FontHeightAdjuster.StyleSheetFromMediator(mediator);
			// Set font, writing system factory, and writing system code for the Lexical Form
			// edit box.  Also set an empty string with the proper writing system.
			m_tbForm.Font = new Font(cache.ServiceLocator.WritingSystemManager.Get(ws).DefaultFontName, 10);
			m_tbForm.WritingSystemFactory = cache.WritingSystemFactory;
			m_tbForm.WritingSystemCode = ws;
			m_tbForm.AdjustStringHeight = false;
			m_tbForm.Tss = m_tsf.MakeString("", ws);
			m_tbForm.StyleSheet = stylesheet;

			// Setup the fancy message text box.
			// Note: at 120DPI (only), it seems to be essential to set at least the WSF of the
			// bottom message even if not using it.
			SetupBottomMsg();
			SetBottomMessage();
			m_fwTextBoxBottomMsg.BorderStyle = BorderStyle.None;

			m_analHvos.UnionWith(cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Select(wsObj => wsObj.Handle));
			List<int> vernList = cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Select(wsObj => wsObj.Handle).ToList();
			m_vernHvos.UnionWith(vernList);
			LoadWritingSystemCombo();
			int iWs = vernList.IndexOf(ws);
			IWritingSystem currentWs;
			if (iWs < 0)
			{
				List<int> analList = cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Select(wsObj => wsObj.Handle).ToList();
				iWs = analList.IndexOf(ws);
				if (iWs < 0)
				{
					currentWs = cache.ServiceLocator.WritingSystemManager.Get(ws);
					m_cbWritingSystems.Items.Add(currentWs);
					SetCbWritingSystemsSize();
				}
				else
				{
					currentWs = cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems[iWs];
				}
			}
			else
			{
				currentWs = cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems[iWs];
			}
			Debug.Assert(currentWs != null && currentWs.Handle == ws);

			m_skipCheck = true;
			m_cbWritingSystems.SelectedItem = currentWs;
			m_skipCheck = false;
			// Don't hook this up until AFTER we've initialized it; otherwise, it can
			// modify the contents of the form as a side effect of initialization.
			// Also, doing that triggers laying out the dialog prematurely, before
			// we've set WSF on all the controls.
			m_cbWritingSystems.SelectedIndexChanged += m_cbWritingSystems_SelectedIndexChanged;

			InitializeMatchingObjects(cache, mediator);

			// Adjust things if the form box needs to grow to accommodate its style.
			int oldHeight = m_tbForm.Height;
			int newHeight = Math.Max(oldHeight, m_tbForm.PreferredHeight);
			int delta = newHeight - oldHeight;
			if (delta != 0)
			{
				m_tbForm.Height = newHeight;
				m_panel1.Height += delta;
				GrowDialogAndAdjustControls(delta, m_panel1);
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
			if (wp.m_title != null)
				Text = wp.m_title;
			if (wp.m_label != null)
				m_formLabel.Text = wp.m_label;
			if (wp.m_btnText != null)
				m_btnOK.Text = wp.m_btnText;
			// The text may be too wide for the button.  See LT-6215.
			if (m_btnOK.PreferredSize.Width > m_btnOK.Size.Width)
			{
				int delta = m_btnOK.PreferredSize.Width - m_btnOK.Size.Width;
				m_btnOK.Location = new Point(m_btnOK.Location.X - delta, m_btnOK.Location.Y);
				m_btnOK.Width += delta;
			}

			AdjustControlsToTextResize();
			ShowControlsBasedOnPanel1Position();
		}

		// Changing the text of the label can, unbelievably, move the text box.
		// Adjust things to line up.
		private void AdjustControlsToTextResize()
		{
			int align = Math.Max(m_cbWritingSystems.Left, m_panel1.Left + m_tbForm.Left);
			m_cbWritingSystems.Left = align;
			if (m_panel1.Left + m_tbForm.Left != align)
				m_panel1.Left = align - m_tbForm.Left;
			if (m_wsLabel.Right != m_panel1.Left + m_formLabel.Right)
			{
				int width = m_wsLabel.Width;
				int right = m_panel1.Left + m_formLabel.Right;
				int left = right - width;
				m_wsLabel.Left = left;
			}
		}

		/// <summary>
		/// Set the text for the OK button.
		/// </summary>
		public void SetOkButtonText(string text)
		{
			m_btnOK.Text = text;
		}

		protected virtual void LoadWritingSystemCombo()
		{
			foreach (IWritingSystem ws in m_cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems)
				m_cbWritingSystems.Items.Add(ws);

			foreach (IWritingSystem ws in m_cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems)
			{
				if (!m_cbWritingSystems.Items.Contains(ws))
					m_cbWritingSystems.Items.Add(ws);
			}
			SetCbWritingSystemsSize();
		}

		/// <summary>
		/// Increase the width of the writing systems combobox if needed to display the names.
		/// (This fixes FWNX-795.)
		/// </summary>
		protected virtual void SetCbWritingSystemsSize()
		{
			int requiredWidth = m_cbWritingSystems.Width;
			int approxDropdownArrowWidth = m_cbWritingSystems.Height;
			using (Graphics g = Graphics.FromHwnd(m_cbWritingSystems.Handle))
			{
				foreach (var item in m_cbWritingSystems.Items)
				{
					var stringSize = g.MeasureString(item.ToString(), m_cbWritingSystems.Font);
					int textwidth = (int)stringSize.Width;
					if (requiredWidth < textwidth + approxDropdownArrowWidth)
						requiredWidth = textwidth + approxDropdownArrowWidth;
				}
				// Allow at most one extra inch beyond m_tbForms's width.  This keeps the new
				// width within reasonable bounds, and should be ample to display a unique
				// name even with breaking between words like Mono's implementation does.
				int width = Math.Min(requiredWidth, m_tbForm.Width + (int)g.DpiX);
				if (width != m_cbWritingSystems.Width)
					m_cbWritingSystems.Width = width;
			}
		}

		protected virtual void InitializeMatchingObjects(FdoCache cache, Mediator mediator)
		{
			// override.
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
			Size size = Size;
			base.OnLoad(e);
			Size = size;
			// The following is needed for 120dpi fonts, which cause BaseGoDlg_Activated
			// to be called while executing OnLoad() instead of after OnLoad() finishes.
			FocusTbFormTheFirstTime();
		}

		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
			FocusTbFormTheFirstTime();
		}

		/// <summary>
		/// Set up the dlg in preparation to showing it.
		/// </summary>
		/// <param name="cache">FDO cache.</param>
		/// <param name="wp">Strings used for various items in this dialog.</param>
		/// <param name="mediator"></param>
		/// <param name="form">Form to use in main text edit box.</param>
		public virtual void SetDlgInfo(FdoCache cache, WindowParams wp, Mediator mediator, string form)
		{
			CheckDisposed();
			SetDlgInfo(cache, wp, mediator, form, cache.DefaultVernWs);
		}

		protected void SetDlgInfo(FdoCache cache, WindowParams wp, Mediator mediator, string form, int ws)
		{
			SetDlgInfo(cache, wp, mediator, ws);
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
			SetDlgInfo(cache, wp, mediator, tssform.Text, TsStringUtils.GetWsAtOffset(tssform, 0));
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
			IWritingSystem userWs = m_cache.ServiceLocator.WritingSystemManager.UserWritingSystem;
			m_fwTextBoxBottomMsg.Font = new Font(userWs.DefaultFontName, 10);
			m_fwTextBoxBottomMsg.WritingSystemFactory = m_cache.WritingSystemFactory;
			m_fwTextBoxBottomMsg.WritingSystemCode = userWs.Handle;
			return userWs.Handle;
		}

		/// <summary>
		/// Sets the help topic ID for the window.  This is used in both the Help button and when the user hits F1
		/// </summary>
		public void SetHelpTopic(string helpTopic)
		{
			CheckDisposed();

			m_helpTopic = helpTopic;
			if (m_helpTopicProvider != null)
			{
				SetHelpButtonEnabled();
			}
		}

		private void SetHelpButtonEnabled()
		{
			m_helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(m_helpTopic));
			m_btnHelp.Enabled = !string.IsNullOrEmpty(m_helpTopic);
		}

		/// <summary>
		/// Reset the list of matching items.
		/// This is not abstract so that this form can be opened in the Windows Forms Designer.
		/// </summary>
		protected virtual void ResetMatches(string searchKey)
		{
			// override
		}

		protected void StartSearchAnimation()
		{
			if (!Controls.Contains(m_searchAnimation))
			{
				Controls.Add(m_searchAnimation);
				m_searchAnimation.BringToFront();
			}
		}

		protected void ResetForm()
		{
			m_tbForm.Tss = m_tsf.MakeString("", m_tbForm.WritingSystemCode);
			m_tbForm.Select();
		}

		protected virtual void HandleMatchingSelectionChanged(FwObjectSelectionEventArgs e)
		{
			HandleMatchingSelectionChanged();
		}

		protected virtual void HandleMatchingSelectionChanged()
		{
			m_btnOK.Enabled = (m_selObject != null);
		}

		protected void ShowControlsBasedOnPanel1Position()
		{
			// Adjust the controls in the panel1 control if needed (make sure they don't overlap)
			if (m_formLabel.Right >= m_tbForm.Left)
				m_tbForm.Left = m_formLabel.Right + 1;	// seperate the controls by at least 1 empty pixel

			//
			// m_cbWritingSystems
			//
			int ypos = m_panel1.Bottom + 5;
			int xpos = m_matchingObjectsBrowser.Left;

			m_cbWritingSystems.Location = new Point(m_panel1.Left + m_tbForm.Left, ypos);
			m_wsLabel.Location = new Point(m_wsLabel.Left, ypos);

			ypos = m_matchingObjectsBrowser.Top - m_objectsLabel.Size.Height - 5;
			m_objectsLabel.Location = new Point(xpos, ypos);
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
			this.m_btnClose = new System.Windows.Forms.Button();
			this.m_btnOK = new System.Windows.Forms.Button();
			this.m_btnInsert = new System.Windows.Forms.Button();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.m_panel1 = new System.Windows.Forms.Panel();
			this.m_tbForm = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.m_formLabel = new System.Windows.Forms.Label();
			this.m_cbWritingSystems = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.m_wsLabel = new System.Windows.Forms.Label();
			this.m_fwTextBoxBottomMsg = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.m_objectsLabel = new System.Windows.Forms.Label();
			this.m_matchingObjectsBrowser = new SIL.FieldWorks.Common.Controls.MatchingObjectsBrowser();
			this.m_panel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_tbForm)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_fwTextBoxBottomMsg)).BeginInit();
			this.SuspendLayout();
			//
			// m_btnClose
			//
			resources.ApplyResources(this.m_btnClose, "m_btnClose");
			this.m_btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btnClose.Name = "m_btnClose";
			//
			// m_btnOK
			//
			resources.ApplyResources(this.m_btnOK, "m_btnOK");
			this.m_btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btnOK.Name = "m_btnOK";
			//
			// m_btnInsert
			//
			resources.ApplyResources(this.m_btnInsert, "m_btnInsert");
			this.m_btnInsert.Name = "m_btnInsert";
			this.m_btnInsert.Click += new System.EventHandler(this.m_btnInsert_Click);
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// m_panel1
			//
			this.m_panel1.Controls.Add(this.m_tbForm);
			this.m_panel1.Controls.Add(this.m_formLabel);
			resources.ApplyResources(this.m_panel1, "m_panel1");
			this.m_panel1.Name = "m_panel1";
			//
			// m_tbForm
			//
			this.m_tbForm.AdjustStringHeight = true;
			this.m_tbForm.BackColor = System.Drawing.SystemColors.Window;
			this.m_tbForm.controlID = null;
			resources.ApplyResources(this.m_tbForm, "m_tbForm");
			this.m_tbForm.HasBorder = true;
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
			// m_wsLabel
			//
			resources.ApplyResources(this.m_wsLabel, "m_wsLabel");
			this.m_wsLabel.Name = "m_wsLabel";
			//
			// m_fwTextBoxBottomMsg
			//
			this.m_fwTextBoxBottomMsg.AdjustStringHeight = true;
			resources.ApplyResources(this.m_fwTextBoxBottomMsg, "m_fwTextBoxBottomMsg");
			this.m_fwTextBoxBottomMsg.BackColor = System.Drawing.SystemColors.Control;
			this.m_fwTextBoxBottomMsg.CausesValidation = false;
			this.m_fwTextBoxBottomMsg.controlID = null;
			this.m_fwTextBoxBottomMsg.HasBorder = false;
			this.m_fwTextBoxBottomMsg.Name = "m_fwTextBoxBottomMsg";
			this.m_fwTextBoxBottomMsg.SelectionLength = 0;
			this.m_fwTextBoxBottomMsg.SelectionStart = 0;
			//
			// m_objectsLabel
			//
			resources.ApplyResources(this.m_objectsLabel, "m_objectsLabel");
			this.m_objectsLabel.Name = "m_objectsLabel";
			//
			// m_matchingObjectsBrowser
			//
			resources.ApplyResources(this.m_matchingObjectsBrowser, "m_matchingObjectsBrowser");
			this.m_matchingObjectsBrowser.Name = "m_matchingObjectsBrowser";
			this.m_matchingObjectsBrowser.TabStop = false;
			this.m_matchingObjectsBrowser.SelectionChanged += new FwSelectionChangedEventHandler(this.m_matchingObjects_SelectionChanged);
			this.m_matchingObjectsBrowser.SelectionMade += new FwSelectionChangedEventHandler(this.m_matchingObjectsBrowser_SelectionMade);
			this.m_matchingObjectsBrowser.SearchCompleted += new EventHandler(this.m_matchingObjectsBrowser_SearchCompleted);
			//
			// BaseGoDlg
			//
			this.AcceptButton = this.m_btnOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.m_btnClose;
			this.Controls.Add(this.m_matchingObjectsBrowser);
			this.Controls.Add(this.m_objectsLabel);
			this.Controls.Add(this.m_fwTextBoxBottomMsg);
			this.Controls.Add(this.m_wsLabel);
			this.Controls.Add(this.m_cbWritingSystems);
			this.Controls.Add(this.m_panel1);
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.m_btnInsert);
			this.Controls.Add(this.m_btnOK);
			this.Controls.Add(this.m_btnClose);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "BaseGoDlg";
			this.ShowInTaskbar = false;
			this.Closed += new System.EventHandler(this.BaseGoDlg_Closed);
			this.Activated += new System.EventHandler(this.BaseGoDlg_Activated);
			this.m_panel1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_tbForm)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_fwTextBoxBottomMsg)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		#region	Event handlers

		protected void m_tbForm_TextChanged(object sender, EventArgs e)
		{
			if (m_skipCheck)
				return;
			int selStart = m_tbForm.SelectionStart;
			int selLen = m_tbForm.SelectionLength;
			int addToSelection;
			string fixedText = AdjustText(out addToSelection);

			ResetMatches(fixedText);
			// Even if AdjustText didn't move the selection, it may have changed the text,
			// which has a side effect in a text box of selecting all of it. We don't want that here,
			// so reset the selection to what it ought to be.
			selStart = Math.Min(Math.Max(selStart + addToSelection, 0), fixedText.Length);
			if (selLen + selStart > fixedText.Length)
				selLen = fixedText.Length - selStart;
			if (m_tbForm.SelectionStart != selStart || m_tbForm.SelectionLength != selLen)
				m_tbForm.Select(selStart, selLen);
		}

		protected virtual string AdjustText(out int addToSelection)
		{
			// TODO: For each keystroke:
			//		1. If it is a reserved character, then...
			//			(e.g., '-' for prefixes or suffixes).
			//		2. If it is not a wordforming character, then...?
			//		3. If it is a wordforming character, then modify the 'matching entries'
			//			list box, and select the first item in the list.
			var oldText = m_tbForm.Text;
			string fixedText = oldText.Trim();
			addToSelection = 0;
			if (fixedText != oldText)
			{
				// It's important (see LT-3770) to allow the user to type a space.
				// So if the only difference is a trailing space, don't adjust the string!
				// (But a single space as the first thing typed is not allowed.)
				// Note: Yi and Chinese use \x3000 for this.
				if (fixedText != "" && (fixedText + " " == oldText || fixedText + "\x3000" == oldText))
				{
					return oldText;
				}
				m_skipCheck = true;
				m_tbForm.Text = fixedText;
				m_skipCheck = false;
				int loc = oldText.IndexOf(fixedText);
				Debug.Assert(loc >= 0);
				addToSelection = -loc; // move selection back by the amount we removed at the start.
			}
			return fixedText;
		}

		protected virtual void m_btnInsert_Click(object sender, EventArgs e)
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
		protected void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, m_helpTopic);
		}

		private void BaseGoDlg_Closed(object sender, EventArgs e)
		{
			// Save location.
			if (m_mediator != null)
			{
				m_mediator.PropertyTable.SetProperty(PersistenceLabel + "DlgLocation", Location);
				var sz = new Size(0, m_delta);
				m_mediator.PropertyTable.SetProperty(PersistenceLabel + "DlgSize", Size - sz);
			}
		}

		private void m_matchingObjects_SelectionChanged(object sender, FwObjectSelectionEventArgs e)
		{
			if (m_skipCheck)
				return;

			m_selObject = m_cache.ServiceLocator.GetObject(e.Hvo);

			HandleMatchingSelectionChanged(e);
		}

		private void m_matchingObjectsBrowser_SelectionMade(object sender, FwObjectSelectionEventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
		}

		private void m_matchingObjectsBrowser_SearchCompleted(object sender, EventArgs e)
		{
			if (Controls.Contains(m_searchAnimation))
				Controls.Remove(m_searchAnimation);
		}

		private bool m_fTbFormHasBeenFocused;
		/// <summary>
		/// The first time it becomes possible, focus m_tbForm.
		/// Depending for some obscure reason on the DPI, events occur in different orders,
		/// and it may not be possible to focus this control at various points where
		/// we would like to. Do it (once) as soon as we can. Not more than that, lest
		/// we move the focus back here from somewhere else the user put it.
		/// </summary>
		void FocusTbFormTheFirstTime()
		{
			if (m_fTbFormHasBeenFocused || ! m_tbForm.CanFocus)
				return;
			m_tbForm.Select();
			m_fTbFormHasBeenFocused = true;
		}

		private void BaseGoDlg_Activated(object sender, EventArgs e)
		{
			FocusTbFormTheFirstTime();
			if (m_hasBeenActivated)
				return; // Only do this once.

			string form = Form.Trim();
			if (!string.IsNullOrEmpty(form))
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

		protected virtual void m_cbWritingSystems_SelectedIndexChanged(object sender, EventArgs e)
		{
			int start = m_tbForm.SelectionStart;
			int length = m_tbForm.SelectionLength;
			m_tbForm.WritingSystemCode = ((ILgWritingSystem)m_cbWritingSystems.SelectedItem).Handle;
			// Change the writing system inside the ITsString.
			ITsStrBldr tsb = m_tbForm.Tss.GetBldr();
			int cch = tsb.Length;
			tsb.SetIntPropValues(0, cch, (int)FwTextPropType.ktptWs, 0,
				m_tbForm.WritingSystemCode);
			m_tbForm.Tss = tsb.GetString();
			//we need to adjust the size of the box based on the changed writing system
			m_tbForm.AdjustForStyleSheet(this, m_panel1, m_tbForm.StyleSheet);
			// Restore the selection, whether IP or range.
			m_tbForm.Select(start, length);
			m_oldSearchKey = string.Empty;
			ResetMatches(m_tbForm.Text);
			m_tbForm.Select();
		}

		#endregion	// Event handlers
	}
}