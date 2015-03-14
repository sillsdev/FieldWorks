// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FwFindReplaceDlg.cs
// Responsibility: TE Team
using System;
using System.Diagnostics;
using System.Drawing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.Common.Drawing;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.Filters;
using XCore;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Find/Replace dialog
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FwFindReplaceDlg : Form, IFWDisposable, IMessageFilter
	{
		#region Constants
		private const string kPersistenceLabel = "FindReplace_";

		#endregion

		#region Events
		/// <summary>Handler for MatchNotFound events.</summary>
		public delegate bool MatchNotFoundHandler(object sender, string defaultMsg,
			MatchType type);

		/// <summary>Fired when a match is not found.</summary>
		public event MatchNotFoundHandler MatchNotFound;
		#endregion

		#region Enumerations
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Status of matches during find/replace
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public enum MatchType
		{
			/// <summary></summary>
			NotSet,
			/// <summary>no match found after previous match</summary>
			NoMoreMatchesFound,
			/// <summary>no match found in whole document</summary>
			NoMatchFound,
			/// <summary>A replace all is done and it made replacements</summary>
			ReplaceAllFinished,
		}
		#endregion

		#region Data members
		/// <summary>all the search settings</summary>
		protected IVwPattern m_vwPattern;
		/// <summary>Environment that keeps track of where we're finding</summary>
		protected FindCollectorEnv m_findEnvironment;
		/// <summary>The rootsite where the find operation will be performed</summary>
		protected IVwRootSite m_vwRootsite;
		/// <summary></summary>
		protected FdoCache m_cache;
		private IApp m_app;
		private bool m_cacheMadeLocally = false;
		/// <summary></summary>
		protected IVwSelection m_vwselPattern;
		private ITsString m_resultReplaceText; // saves replace text for reading after dlg closes.
		/// <summary></summary>
		protected ITsString m_prevSearchText = null;
		/// <summary></summary>
		protected SearchKiller m_searchKiller = new SearchKiller();
		private bool m_messageFilterInstalled = false;

		private string m_sMoreButtonText;
		private int m_heightDlgMore;
		private int m_heightTabControlMore;
		private int m_heightDlgLess;
		private int m_heightTabControlLess;

		private bool m_fLastDirectionForward;

		private System.Windows.Forms.TabPage tabFind;
		private System.Windows.Forms.TabPage tabReplace;
		private System.Windows.Forms.TabControl tabControls;
		/// <summary></summary>
		protected System.Windows.Forms.CheckBox chkMatchDiacritics;
		/// <summary></summary>
		protected System.Windows.Forms.CheckBox chkMatchWS;
		/// <summary></summary>
		protected System.Windows.Forms.CheckBox chkMatchCase;
		/// <summary></summary>
		protected System.Windows.Forms.CheckBox chkMatchWholeWord;
		/// <summary>Panel containing advanced controls</summary>
		protected System.Windows.Forms.Panel panelSearchOptions;
		/// <summary></summary>
		protected SIL.FieldWorks.Common.Widgets.FwTextBox fweditFindText;
		/// <summary></summary>
		protected SIL.FieldWorks.Common.Widgets.FwTextBox fweditReplaceText;
		private System.ComponentModel.IContainer components = null;
		/// <summary></summary>
		protected System.Windows.Forms.Label lblFindFormatText;
		/// <summary></summary>
		protected System.Windows.Forms.Label lblReplaceFormatText;

		private FwTextBox m_lastTextBoxInFocus;
		private bool m_initialActivate;
		private bool m_inReplace = false;
		private bool m_inFind = false;
		private bool m_inGetSpecs = false;
		/// <summary>The OK button is usually hidden. It is visible in Flex after clicking the
		/// Setup button of the Find/Replace tab of the Bulk Edit bar.</summary>
		private System.Windows.Forms.Button m_okButton;
		/// <summary></summary>
		protected System.Windows.Forms.CheckBox chkUseRegularExpressions;

		private IHelpTopicProvider m_helpTopicProvider;

		private bool m_fDisableReplacePatternMatching;

		private Mediator m_mediator; // optional, used for persistence.

		private string s_helpTopic;
		private System.Windows.Forms.HelpProvider helpProvider;

		// Used by EnableControls to remember what was enabled when things were disabled for
		// the duration of an operation, in order to put them right afterwards.
		private Dictionary<Control, bool> m_enableStates;
		private System.Windows.Forms.Button btnRegexMenuFind;
		private System.Windows.Forms.Button btnRegexMenuReplace;

		private RegexHelperMenu regexContextMenuFind;
		/// <summary></summary>
		protected MenuItem mnuWritingSystem;
		/// <summary></summary>
		protected MenuItem mnuStyle;
		private Button btnFormat;
		/// <summary>The close button</summary>
		/// <remarks>TE-4839: Changed the text from Cancel to Close according to TE Analyst (2007-06-22).</remarks>
		protected Button btnClose;
		private Button btnFindNext;
		private Button btnMore;
		private Button btnReplace;
		private Button btnReplaceAll;
		private Panel panelBasic;
		/// <summary></summary>
		protected Label lblReplaceFormat;
		private Label lblReplaceText;
		/// <summary></summary>
		protected ContextMenu mnuFormat;
		private Label lblSearchOptions;
		/// <summary></summary>
		protected Label lblFindFormat;
		private RegexHelperMenu regexContextMenuReplace;

		#endregion

		#region Construction, initialization, destruction
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FwFindReplaceDlg"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public FwFindReplaceDlg()
		{
			AccessibleName = GetType().Name;
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			this.helpProvider = new HelpProvider();
			this.helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);

			m_lastTextBoxInFocus = fweditFindText;
			// Init of member variables related to dialog height removed from here and moved
			// to OnLayout. This allows them to correctly remember the adjusted dialog size
			// that occurs when screen resolution is not set to 96dpi
			m_sMoreButtonText = btnMore.Text;
			btnMore.Image = ResourceHelper.MoreButtonDoubleArrowIcon;
			btnFormat.Image = ResourceHelper.ButtonMenuArrowIcon;
			panelSearchOptions.Visible = false;
			fweditFindText.TextChanged +=new EventHandler(HandleTextChanged);
			fweditReplaceText.TextChanged +=new EventHandler(HandleTextChanged);

			m_searchKiller.Control = this;	// used for redrawing
			m_searchKiller.StopControl = this.btnClose;	// need to know the stop button
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the initial values for the dialog controls, assuming that the find and replace
		/// edit boxes use the default vernacular writing system.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="vwPattern">Find/replace values</param>
		/// <param name="rootSite">view</param>
		/// <param name="fReplace"><c>true</c> to initially display replace dialog page</param>
		/// <param name="fOverlays">ignored for now</param>
		/// <param name="owner">The main window that owns the rootsite</param>
		/// <param name="helpTopicProvider">help topic provider allows the dialog box class
		/// to specify the appropriate help topic path for this dialog</param>
		/// <param name="app">The application</param>
		/// <returns>
		/// true if the dialog was initialized properly, otherwise false.
		/// False indicates some problem and the find/replace dialog should not be
		/// shown at this time.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public bool SetDialogValues(FdoCache cache, IVwPattern vwPattern, IVwRootSite rootSite,
			bool fReplace, bool fOverlays, Form owner, IHelpTopicProvider helpTopicProvider, IApp app)
		{
			return SetDialogValues(cache, vwPattern, rootSite, fReplace, fOverlays,
				owner, helpTopicProvider, app,
				cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the initial values for the dialog controls, prior to displaying the dialog.
		/// This method should be called after creating, but prior to calling DoModeless. This
		/// overload is meant to be called from managed code.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="vwPattern">Find/replace values</param>
		/// <param name="rootSite">view</param>
		/// <param name="fReplace"><c>true</c> to initially display replace dialog page</param>
		/// <param name="fOverlays">ignored for now</param>
		/// <param name="owner">The main window that owns the rootsite</param>
		/// <param name="helpTopicProvider">help topic provider allows the dialog box class
		/// to specify the appropriate help topic path for this dialog</param>
		/// <param name="app">The application</param>
		/// <param name="wsEdit">writing system for the find and replace edit boxes</param>
		/// <returns>
		/// true if the dialog was initialized properly, otherwise false.
		/// False indicates some problem and the find/replace dialog should not be
		/// shown at this time.
		/// </returns>
		/// <remarks>ENHANCE JohnT: it may need more arguments, for example, the name of the
		/// kind of object we can restrict the search to, a list of fields.</remarks>
		/// ------------------------------------------------------------------------------------
		public bool SetDialogValues(FdoCache cache, IVwPattern vwPattern, IVwRootSite rootSite,
			bool fReplace, bool fOverlays, Form owner, IHelpTopicProvider helpTopicProvider,
			IApp app, int wsEdit)
		{
			CheckDisposed();

			fweditFindText.controlID = "Find";
			fweditReplaceText.controlID = "Replace";
			// save the pattern and put the text into the find edit box.
			if (vwPattern == null)
				throw new ArgumentNullException("vwPattern");
			if (cache == null)
				throw new ArgumentNullException("cache");
			m_vwPattern = vwPattern;
			m_cache = cache;
			m_helpTopicProvider = helpTopicProvider;
			m_app = app;

			SetOwner(rootSite, owner, vwPattern, wsEdit);
			bool readOnly = rootSite is SimpleRootSite && ((SimpleRootSite)rootSite).ReadOnlyView;
			if (readOnly)
			{
				if (tabControls.Controls.Contains(tabReplace))
					tabControls.Controls.Remove(tabReplace);
			}
			else
			{
				if (!tabControls.Controls.Contains(tabReplace))
					tabControls.Controls.Add(tabReplace);
			}

			ILgWritingSystemFactory wsf = m_cache.WritingSystemFactory;
			fweditFindText.WritingSystemFactory = fweditReplaceText.WritingSystemFactory = wsf;
			ITsStrFactory strFact = m_cache.TsStrFactory;
			CoreWritingSystemDefinition defVernWs = m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;
			FindText = strFact.MakeString(string.Empty, defVernWs.Handle);
			ReplaceText = strFact.MakeString(string.Empty, defVernWs.Handle);
			// Make sure each of the edit boxes has a reasonable writing system assigned.
			// (See LT-5130 for what can happen otherwise.)
			fweditFindText.WritingSystemCode = wsEdit;
			fweditReplaceText.WritingSystemCode = wsEdit;
			FindText = EnsureValidWs(wsEdit, vwPattern.Pattern);
			ReplaceText = EnsureValidWs(wsEdit, vwPattern.ReplaceWith);

			tabControls.SelectedTab = fReplace ? tabReplace : tabFind;
			tabControls_SelectedIndexChanged(null, new EventArgs());

			if (m_helpTopicProvider != null) // Will be null when running tests
			{
				helpProvider.HelpNamespace = FwDirectoryFinder.CodeDirectory +
					m_helpTopicProvider.GetHelpString("UserHelpFile");
			}

			SetCheckboxStates(vwPattern);

			if (regexContextMenuFind != null)
				regexContextMenuFind.Dispose();
			regexContextMenuFind = new RegexHelperMenu(fweditFindText, m_helpTopicProvider);
			if (regexContextMenuReplace != null)
				regexContextMenuReplace.Dispose();
			regexContextMenuReplace = new RegexHelperMenu(fweditReplaceText, m_helpTopicProvider, false);

			EnableRegexMenuReplaceButton();

			// get the current selection text (if available) to fill in the find pattern.
			IVwSelection sel = null;
			if (rootSite != null && rootSite.RootBox != null)
				sel = rootSite.RootBox.Selection;
			if (sel == null)
			{
				// Set the TSS of the edit box to an empty string if it isn't set.
				if (FindText == null)
				{
					FindText = m_cache.TsStrFactory.MakeString(
						string.Empty,
						m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle);
				}
			}
			else
			{
				// Get the selected text as the initial contents of the find box. Make a new TS String without
				// any character style so the character style from the selection will not be used. Also, if the
				// selection ends with a paragraph end sequence (CR/LF) then remove it.
				ITsString tssSel;
				bool fGotItAll;
				sel.GetFirstParaString(out tssSel, " ", out fGotItAll);
				if (tssSel == null)
				{
					// Not able to get ITsString from selection (e.g. if it is a picture)...
					SetFormatLabels();
					return true;
				}
				ITsStrBldr bldr = tssSel.GetBldr();
				bldr.SetStrPropValue(0, bldr.Length, (int)FwTextPropType.ktptNamedStyle, null);
				// Unfortunately, the TsString returned by sel.GetFirstParaString() can have an
				// empty TsTextProps for at least the first run.  If that happens, we blow up
				// when we try to get the string later.
				for (int irun = 0; irun < bldr.RunCount; ++irun)
				{
					ITsTextProps ttp = bldr.get_Properties(irun);
					int var;
					if (ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out var) <= 0)
					{
						TsRunInfo tri;
						bldr.FetchRunInfo(irun, out tri);
						bldr.SetIntPropValues(tri.ichMin, tri.ichLim,
							(int)FwTextPropType.ktptWs, 0, m_cache.DefaultAnalWs);
					}
				}
				RemoveEndOfPara(bldr);
				// We don't want to copy a multi-line selection into the find box.
				// Currently treating it as don't copy anything; another plausible option would be to copy the first line.
				var text = bldr.Text;
				if (text != null && (text.IndexOf('\r') >= 0 || text.IndexOf('\n') > 0))
				{
					bldr.Replace(0, bldr.Length, "", null);
				}
				// Set the TSS of the edit box if there is any text to set, or if there is no
				// TSS for the box, or if there is no text in the find box AND the selection is not a user prompt.
				// If the current selection is an IP AND we have a previous find text, we want to use that
				// instead of the current selection (TE-5127 and TE-5126).
				int nVar; //dummy for out params
				if (bldr.Length == 0 && vwPattern != null && vwPattern.Pattern != null)
				{
					FindText = vwPattern.Pattern;
				}
				else if ((bldr.Length != 0 || FindText == null || FindText.Length == 0)
					&& tssSel.get_Properties(0).GetIntPropValues(SimpleRootSite.ktptUserPrompt, out nVar) != 1)
				{
					FindText = bldr.GetString();
				}
				if (FindText != null)
				{
					// Set the replace text box properties to be the same as the find text box.
					// The best we can do is take the properties of the first run which should
					// be fine for most cases.
					ITsTextProps props = FindText.get_Properties(0);
					ITsStrBldr replaceBldr = TsStrBldrClass.Create();
					replaceBldr.Replace(0, 0, "", props);
					ReplaceText = replaceBldr.GetString();
				}
			}

			SetFormatLabels();
			return true;
		}

		private void EnableRegexMenuReplaceButton()
		{
			btnRegexMenuReplace.Visible = (tabControls.SelectedTab != tabFind && !DisableReplacePatternMatching);
			btnRegexMenuReplace.Enabled = chkUseRegularExpressions.Checked;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set initial values, assuming default vernacular writing system for the find
		/// and replace edit boxes.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="vwPattern">Find/replace values</param>
		/// <param name="stylesheet">to use in text boxes</param>
		/// <param name="owner">The main window that owns the rootsite</param>
		/// <param name="helpTopicProvider">help topic provider allows the dialog box class
		/// to specify the appropriate help topic path for this dialog</param>
		/// <param name="app">The application</param>
		/// <returns>
		/// true if the dialog was initialized properly, otherwise false.
		/// False indicates some problem and the find/replace dialog should not be
		/// shown at this time.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public bool SetDialogValues(FdoCache cache, IVwPattern vwPattern,
			IVwStylesheet stylesheet, Form owner, IHelpTopicProvider helpTopicProvider, IApp app)
		{
			return SetDialogValues(cache, vwPattern, stylesheet, owner, helpTopicProvider, app,
				cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the initial values for the dialog controls, prior to displaying the dialog.
		/// This method should be called after creating, but prior to calling DoModal. This
		/// overload is meant to be called from the Setup button of the Find/Replace tab
		/// of the Bulk Edit bar. Instead of having a root site and controls that allow the
		/// find/replace to be actually done, it just serves to edit the pattern.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="vwPattern">Find/replace values</param>
		/// <param name="stylesheet">to use in text boxes</param>
		/// <param name="owner">The main window that owns the rootsite</param>
		/// <param name="helpTopicProvider">help topic provider allows the dialog box class
		/// to specify the appropriate help topic path for this dialog</param>
		/// <param name="app">The application</param>
		/// <param name="wsEdit">writing system used in the find/replace text boxes</param>
		/// <returns>
		/// true if the dialog was initialized properly, otherwise false.
		/// False indicates some problem and the find/replace dialog should not be
		/// shown at this time.
		/// </returns>
		/// <remarks>ENHANCE JohnT: it may need more arguments, for example, the name of the
		/// kind of object we can restrict the search to, a list of fields.</remarks>
		/// ------------------------------------------------------------------------------------
		public bool SetDialogValues(FdoCache cache, IVwPattern vwPattern,
			IVwStylesheet stylesheet, Form owner, IHelpTopicProvider helpTopicProvider,
			IApp app, int wsEdit)
		{
			CheckDisposed();

			// Must set the stylesheet for the FwEdit boxes before calling SetDialogValues since
			// that call can reset the text in those boxes.
			fweditFindText.StyleSheet = fweditReplaceText.StyleSheet = stylesheet;

			// For now pass a null writing system string since it isn't used at all.
			if (!SetDialogValues(cache, vwPattern, null, true, false, owner, helpTopicProvider, app, wsEdit))
				return false;

			FindText = vwPattern.Pattern;
			// Reconfigure the dialog for this special purpose. The Find/Replace buttons go away,
			// we have an OK button which is the default.
			btnReplace.Hide();
			btnFindNext.Hide();
			btnReplaceAll.Hide();
			m_okButton.Show();
			m_inGetSpecs = true; // disables showing Replace buttons
			//m_inReplace = true; // disables switch to Find tab.
			tabControls.TabPages.Remove(tabFind);
			this.AcceptButton = m_okButton;
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the checkbox states.
		/// </summary>
		/// <param name="vwPattern">The vw pattern.</param>
		/// ------------------------------------------------------------------------------------
		private void SetCheckboxStates(IVwPattern vwPattern)
		{
			// Set initial checkbox states
			chkMatchWS.Checked = vwPattern.MatchOldWritingSystem;
			chkMatchDiacritics.Checked = vwPattern.MatchDiacritics;
			chkMatchCase.Checked = vwPattern.MatchCase;
			chkMatchWholeWord.Checked = vwPattern.MatchWholeWord;
			if (chkUseRegularExpressions.Enabled)
				chkUseRegularExpressions.Checked = vwPattern.UseRegularExpressions;
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

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// -----------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			m_lastTextBoxInFocus = null;

			if (disposing)
			{
				if (components != null)
					components.Dispose();
				if (m_cacheMadeLocally && m_cache != null)
					m_cache.Dispose();
				if (regexContextMenuFind != null)
					regexContextMenuFind.Dispose();
				if (regexContextMenuReplace != null)
					regexContextMenuReplace.Dispose();
				//if (m_helpTopicProvider != null && (m_helpTopicProvider is IDisposable)) // No, since the client provides it.
				//	(m_helpTopicProvider as IDisposable).Dispose();
				if (m_messageFilterInstalled)
				{
					Application.RemoveMessageFilter(this);
					m_messageFilterInstalled = false;
				}
			}
			m_helpTopicProvider = null;
			m_searchKiller = null;
			m_prevSearchText = null;
			m_vwRootsite = null;
			m_vwPattern = null;
			m_cache = null;
			regexContextMenuReplace = null;
			regexContextMenuFind = null;
			base.Dispose(disposing);
		}
		#endregion // Construction, initialization, destruction

		#region Other methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes any end of paragraph marker from the string builder.
		/// </summary>
		/// <param name="bldr">The string builder.</param>
		/// ------------------------------------------------------------------------------------
		private void RemoveEndOfPara(ITsStrBldr bldr)
		{
			string endOfPara = Environment.NewLine;
			// Remove any end of paragraph marker from the string builder.
			if (bldr.Length < endOfPara.Length)
				return;

			if (bldr.Text.EndsWith(endOfPara))
				bldr.Replace(bldr.Length - endOfPara.Length, bldr.Length, null, null);
		}

		/// <summary>
		/// Call this after SetDialogValues on startup to restore settings and have them
		/// saved on close.
		/// </summary>
		/// <param name="mediator"></param>
		public void RestoreAndPersistSettingsIn(Mediator mediator)
		{
			CheckDisposed();

			if (mediator == null)
				return; // for robustness against client lacking one.
			m_mediator = mediator;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If this is set true, the 'use regular expression' check is disabled in the Replace tab.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool DisableReplacePatternMatching
		{
			get
			{
				CheckDisposed();
				return m_fDisableReplacePatternMatching;
			}
			set
			{
				CheckDisposed();
				m_fDisableReplacePatternMatching = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="levent"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnLayout(LayoutEventArgs levent)
		{
			// We want to save and adjust these sizes AFTER our handle is created
			// (which is one way to ensure that it is AFTER .NET has adjusted the
			// dialog size if our screen resolution is not 96 DPI), but only ONCE,
			// because after the first time we have changed the values we are basing
			// things on.
			if (this.IsHandleCreated && m_heightDlgLess == 0)
			{
				m_heightDlgMore = Height;
				m_heightTabControlMore = tabControls.Height;
				m_heightDlgLess = Height - panelSearchOptions.Height;
				m_heightTabControlLess = tabControls.Height - panelSearchOptions.Height;
				Height = m_heightDlgLess;
				tabControls.Height = m_heightTabControlLess;
				if (m_mediator != null)
				{
					// Now we have our natural size, we can properly adjust our location etc.
					object locWnd = m_mediator.PropertyTable.GetValue(kPersistenceLabel + "DlgLocation");
					object showMore = m_mediator.PropertyTable.GetValue(kPersistenceLabel + "ShowMore");
					if (showMore != null && "true" == (string)showMore)
						btnMore_Click(this, new EventArgs());
					if (locWnd != null)
					{
						Rectangle rect = new Rectangle((Point)locWnd, this.Size);
						ScreenUtils.EnsureVisibleRect(ref rect);
						DesktopBounds = rect;
						StartPosition = FormStartPosition.Manual;
					}
				}
			}
			base.OnLayout (levent);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Change the main window which owns this dialog. Since this dialog attempts to stay
		/// alive as long as the app is alive (or, as long as there is a main window open),
		/// the app should call this to re-assign an owner any time the existing owner is
		/// closing.  This assumes that the find and replace edit boxes use the default
		/// vernacular writing system.
		/// </summary>
		public void SetOwner(IVwRootSite rootSite, Form newOwner, IVwPattern findPattern)
		{
			SetOwner(rootSite, newOwner, findPattern,
				m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Change the main window which owns this dialog. Since this dialog attempts to stay
		/// alive as long as the app is alive (or, as long as there is a main window open),
		/// the app should call this to re-assign an owner any time the existing owner is
		/// closing.
		/// </summary>
		/// <param name="rootSite">view</param>
		/// <param name="newOwner">The main window that owns the rootsite</param>
		/// <param name="findPattern">The find/replace pattern of the new owner.</param>
		/// <param name="wsEdit">writing system for the find and replace edit boxes</param>
		/// ------------------------------------------------------------------------------------
		public void SetOwner(IVwRootSite rootSite, Form newOwner, IVwPattern findPattern, int wsEdit)
		{
			CheckDisposed();

			m_vwRootsite = rootSite;
			if (m_vwRootsite != null && rootSite.RootBox != null)
				fweditFindText.StyleSheet = fweditReplaceText.StyleSheet = rootSite.RootBox.Stylesheet;

			if (newOwner != null && Owner != newOwner)
			{
				Owner = newOwner;
				m_vwselPattern = null;
				m_findEnvironment = null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check that the ws in the ITsString is still valid.  If it isn't, set it to the given
		/// default value.
		/// </summary>
		/// <param name="wsEdit">The ws edit.</param>
		/// <param name="tss">The TSS.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private ITsString EnsureValidWs(int wsEdit, ITsString tss)
		{
			if (tss != null)
			{
				ITsStrBldr tsb = tss.GetBldr();
				ITsTextProps ttp = tsb.get_Properties(0);
				int nVar;
				int ws = ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar);
				string sWs = m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(ws);
				if (sWs == null)
				{
					tsb.SetIntPropValues(0, tsb.Length, (int)FwTextPropType.ktptWs, nVar, wsEdit);
					return tsb.GetString();
				}
			}
			return tss;
		}
		#endregion

		#region Windows Form Designer generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			System.Windows.Forms.Button btnHelp;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FwFindReplaceDlg));
			System.Windows.Forms.Label lblFindText;
			this.tabControls = new System.Windows.Forms.TabControl();
			this.tabFind = new System.Windows.Forms.TabPage();
			this.tabReplace = new System.Windows.Forms.TabPage();
			this.panelSearchOptions = new System.Windows.Forms.Panel();
			this.chkUseRegularExpressions = new System.Windows.Forms.CheckBox();
			this.btnFormat = new System.Windows.Forms.Button();
			this.chkMatchCase = new System.Windows.Forms.CheckBox();
			this.chkMatchDiacritics = new System.Windows.Forms.CheckBox();
			this.chkMatchWholeWord = new System.Windows.Forms.CheckBox();
			this.chkMatchWS = new System.Windows.Forms.CheckBox();
			this.lblSearchOptions = new System.Windows.Forms.Label();
			this.panelBasic = new System.Windows.Forms.Panel();
			this.btnRegexMenuReplace = new System.Windows.Forms.Button();
			this.btnRegexMenuFind = new System.Windows.Forms.Button();
			this.lblReplaceFormat = new System.Windows.Forms.Label();
			this.lblReplaceFormatText = new System.Windows.Forms.Label();
			this.lblFindFormatText = new System.Windows.Forms.Label();
			this.lblFindFormat = new System.Windows.Forms.Label();
			this.btnClose = new System.Windows.Forms.Button();
			this.btnFindNext = new System.Windows.Forms.Button();
			this.btnMore = new System.Windows.Forms.Button();
			this.lblReplaceText = new System.Windows.Forms.Label();
			this.btnReplace = new System.Windows.Forms.Button();
			this.btnReplaceAll = new System.Windows.Forms.Button();
			this.m_okButton = new System.Windows.Forms.Button();
			this.fweditReplaceText = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.fweditFindText = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.mnuFormat = new System.Windows.Forms.ContextMenu();
			this.mnuWritingSystem = new System.Windows.Forms.MenuItem();
			this.mnuStyle = new System.Windows.Forms.MenuItem();
			btnHelp = new System.Windows.Forms.Button();
			lblFindText = new System.Windows.Forms.Label();
			this.tabControls.SuspendLayout();
			this.tabReplace.SuspendLayout();
			this.panelSearchOptions.SuspendLayout();
			this.panelBasic.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.fweditReplaceText)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.fweditFindText)).BeginInit();
			this.SuspendLayout();
			//
			// btnHelp
			//
			resources.ApplyResources(btnHelp, "btnHelp");
			btnHelp.Name = "btnHelp";
			btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
			//
			// lblFindText
			//
			resources.ApplyResources(lblFindText, "lblFindText");
			lblFindText.Name = "lblFindText";
			//
			// tabControls
			//
			this.tabControls.Controls.Add(this.tabFind);
			this.tabControls.Controls.Add(this.tabReplace);
			resources.ApplyResources(this.tabControls, "tabControls");
			this.tabControls.Name = "tabControls";
			this.tabControls.SelectedIndex = 0;
			this.tabControls.SelectedIndexChanged += new System.EventHandler(this.tabControls_SelectedIndexChanged);
			//
			// tabFind
			//
			resources.ApplyResources(this.tabFind, "tabFind");
			this.tabFind.Name = "tabFind";
			this.tabFind.UseVisualStyleBackColor = true;
			//
			// tabReplace
			//
			this.tabReplace.Controls.Add(this.panelSearchOptions);
			this.tabReplace.Controls.Add(this.panelBasic);
			resources.ApplyResources(this.tabReplace, "tabReplace");
			this.tabReplace.Name = "tabReplace";
			this.tabReplace.UseVisualStyleBackColor = true;
			//
			// panelSearchOptions
			//
			this.panelSearchOptions.Controls.Add(this.chkUseRegularExpressions);
			this.panelSearchOptions.Controls.Add(this.btnFormat);
			this.panelSearchOptions.Controls.Add(this.chkMatchCase);
			this.panelSearchOptions.Controls.Add(this.chkMatchDiacritics);
			this.panelSearchOptions.Controls.Add(this.chkMatchWholeWord);
			this.panelSearchOptions.Controls.Add(this.chkMatchWS);
			this.panelSearchOptions.Controls.Add(this.lblSearchOptions);
			resources.ApplyResources(this.panelSearchOptions, "panelSearchOptions");
			this.panelSearchOptions.Name = "panelSearchOptions";
			this.panelSearchOptions.Paint += new System.Windows.Forms.PaintEventHandler(this.panel2_Paint);
			//
			// chkUseRegularExpressions
			//
			resources.ApplyResources(this.chkUseRegularExpressions, "chkUseRegularExpressions");
			this.chkUseRegularExpressions.Name = "chkUseRegularExpressions";
			this.chkUseRegularExpressions.CheckedChanged += new System.EventHandler(this.chkUseRegularExpressions_CheckedChanged);
			//
			// btnFormat
			//
			resources.ApplyResources(this.btnFormat, "btnFormat");
			this.btnFormat.Name = "btnFormat";
			this.btnFormat.Click += new System.EventHandler(this.btnFormat_Click);
			//
			// chkMatchCase
			//
			resources.ApplyResources(this.chkMatchCase, "chkMatchCase");
			this.chkMatchCase.Name = "chkMatchCase";
			//
			// chkMatchDiacritics
			//
			resources.ApplyResources(this.chkMatchDiacritics, "chkMatchDiacritics");
			this.chkMatchDiacritics.Name = "chkMatchDiacritics";
			//
			// chkMatchWholeWord
			//
			resources.ApplyResources(this.chkMatchWholeWord, "chkMatchWholeWord");
			this.chkMatchWholeWord.Name = "chkMatchWholeWord";
			//
			// chkMatchWS
			//
			resources.ApplyResources(this.chkMatchWS, "chkMatchWS");
			this.chkMatchWS.Name = "chkMatchWS";
			this.chkMatchWS.CheckedChanged += new System.EventHandler(this.chkMatchWS_CheckedChanged);
			//
			// lblSearchOptions
			//
			resources.ApplyResources(this.lblSearchOptions, "lblSearchOptions");
			this.lblSearchOptions.Name = "lblSearchOptions";
			//
			// panelBasic
			//
			this.panelBasic.Controls.Add(this.btnRegexMenuReplace);
			this.panelBasic.Controls.Add(this.btnRegexMenuFind);
			this.panelBasic.Controls.Add(this.lblReplaceFormat);
			this.panelBasic.Controls.Add(this.lblReplaceFormatText);
			this.panelBasic.Controls.Add(this.lblFindFormatText);
			this.panelBasic.Controls.Add(this.lblFindFormat);
			this.panelBasic.Controls.Add(this.btnClose);
			this.panelBasic.Controls.Add(btnHelp);
			this.panelBasic.Controls.Add(this.btnFindNext);
			this.panelBasic.Controls.Add(lblFindText);
			this.panelBasic.Controls.Add(this.btnMore);
			this.panelBasic.Controls.Add(this.lblReplaceText);
			this.panelBasic.Controls.Add(this.btnReplace);
			this.panelBasic.Controls.Add(this.btnReplaceAll);
			this.panelBasic.Controls.Add(this.m_okButton);
			this.panelBasic.Controls.Add(this.fweditReplaceText);
			this.panelBasic.Controls.Add(this.fweditFindText);
			resources.ApplyResources(this.panelBasic, "panelBasic");
			this.panelBasic.Name = "panelBasic";
			//
			// btnRegexMenuReplace
			//
			resources.ApplyResources(this.btnRegexMenuReplace, "btnRegexMenuReplace");
			this.btnRegexMenuReplace.Name = "btnRegexMenuReplace";
			this.btnRegexMenuReplace.Click += new System.EventHandler(this.btnRegexMenuReplace_Click);
			//
			// btnRegexMenuFind
			//
			resources.ApplyResources(this.btnRegexMenuFind, "btnRegexMenuFind");
			this.btnRegexMenuFind.Name = "btnRegexMenuFind";
			this.btnRegexMenuFind.Click += new System.EventHandler(this.btnRegexMenuFind_Click);
			//
			// lblReplaceFormat
			//
			resources.ApplyResources(this.lblReplaceFormat, "lblReplaceFormat");
			this.lblReplaceFormat.Name = "lblReplaceFormat";
			//
			// lblReplaceFormatText
			//
			resources.ApplyResources(this.lblReplaceFormatText, "lblReplaceFormatText");
			this.lblReplaceFormatText.Name = "lblReplaceFormatText";
			//
			// lblFindFormatText
			//
			resources.ApplyResources(this.lblFindFormatText, "lblFindFormatText");
			this.lblFindFormatText.Name = "lblFindFormatText";
			//
			// lblFindFormat
			//
			resources.ApplyResources(this.lblFindFormat, "lblFindFormat");
			this.lblFindFormat.Name = "lblFindFormat";
			//
			// btnClose
			//
			this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.btnClose, "btnClose");
			this.btnClose.Name = "btnClose";
			this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
			//
			// btnFindNext
			//
			this.btnFindNext.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.btnFindNext, "btnFindNext");
			this.btnFindNext.Name = "btnFindNext";
			this.btnFindNext.Click += new System.EventHandler(this.OnFindNext);
			//
			// btnMore
			//
			resources.ApplyResources(this.btnMore, "btnMore");
			this.btnMore.Name = "btnMore";
			this.btnMore.Click += new System.EventHandler(this.btnMore_Click);
			//
			// lblReplaceText
			//
			resources.ApplyResources(this.lblReplaceText, "lblReplaceText");
			this.lblReplaceText.Name = "lblReplaceText";
			//
			// btnReplace
			//
			resources.ApplyResources(this.btnReplace, "btnReplace");
			this.btnReplace.Name = "btnReplace";
			this.btnReplace.Click += new System.EventHandler(this.OnReplace);
			//
			// btnReplaceAll
			//
			resources.ApplyResources(this.btnReplaceAll, "btnReplaceAll");
			this.btnReplaceAll.Name = "btnReplaceAll";
			this.btnReplaceAll.Click += new System.EventHandler(this.OnReplaceAll);
			//
			// m_okButton
			//
			resources.ApplyResources(this.m_okButton, "m_okButton");
			this.m_okButton.Name = "m_okButton";
			this.m_okButton.Click += new System.EventHandler(this.m_okButton_Click);
			//
			// fweditReplaceText
			//
			this.fweditReplaceText.AcceptsReturn = false;
			this.fweditReplaceText.AdjustStringHeight = true;
			resources.ApplyResources(this.fweditReplaceText, "fweditReplaceText");
			this.fweditReplaceText.BackColor = System.Drawing.SystemColors.Window;
			this.fweditReplaceText.controlID = null;
			this.fweditReplaceText.HasBorder = true;
			this.fweditReplaceText.Name = "fweditReplaceText";
			this.fweditReplaceText.SuppressEnter = false;
			this.fweditReplaceText.WordWrap = false;
			this.fweditReplaceText.Leave += new System.EventHandler(this.FwTextBox_Leave);
			this.fweditReplaceText.Enter += new System.EventHandler(this.FwTextBox_Enter);
			//
			// fweditFindText
			//
			this.fweditFindText.AcceptsReturn = false;
			this.fweditFindText.AdjustStringHeight = true;
			resources.ApplyResources(this.fweditFindText, "fweditFindText");
			this.fweditFindText.BackColor = System.Drawing.SystemColors.Window;
			this.fweditFindText.controlID = null;
			this.fweditFindText.HasBorder = true;
			this.fweditFindText.Name = "fweditFindText";
			this.fweditFindText.SuppressEnter = false;
			this.fweditFindText.WordWrap = false;
			this.fweditFindText.Leave += new System.EventHandler(this.FwTextBox_Leave);
			this.fweditFindText.Enter += new System.EventHandler(this.FwTextBox_Enter);
			//
			// mnuFormat
			//
			this.mnuFormat.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
			this.mnuWritingSystem,
			this.mnuStyle});
			//
			// mnuWritingSystem
			//
			this.mnuWritingSystem.Index = 0;
			resources.ApplyResources(this.mnuWritingSystem, "mnuWritingSystem");
			//
			// mnuStyle
			//
			this.mnuStyle.Index = 1;
			resources.ApplyResources(this.mnuStyle, "mnuStyle");
			//
			// FwFindReplaceDlg
			//
			this.AcceptButton = this.btnFindNext;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnClose;
			this.Controls.Add(this.tabControls);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.KeyPreview = true;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FwFindReplaceDlg";
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.tabControls.ResumeLayout(false);
			this.tabReplace.ResumeLayout(false);
			this.panelSearchOptions.ResumeLayout(false);
			this.panelSearchOptions.PerformLayout();
			this.panelBasic.ResumeLayout(false);
			this.panelBasic.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.fweditReplaceText)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.fweditFindText)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the CheckedChanged event of the chkMatchWS control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void chkMatchWS_CheckedChanged(object sender, EventArgs e)
		{
			SetFormatLabels();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Change Close button back to Cancel whenever we re-open the dialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnVisibleChanged(EventArgs e)
		{
			if (Visible)
			{	// this means we were hidden and now will be visible
				System.Resources.ResourceManager resources =
					new System.Resources.ResourceManager(typeof(FwFindReplaceDlg));
				btnClose.Text = resources.GetString("btnClose.Text");

				// set the "initial activate" state to true. This will allow the OnActivated
				// message handler to set the focus to the find text box.
				m_initialActivate = true;
			}
			base.OnVisibleChanged(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Open the help window when the help button is pressed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnHelp_Click(object sender, System.EventArgs e)
		{
			SetHelpTopicId();
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, s_helpTopic);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Method to handle the Click event of the Format button. Displays the format menu.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnFormat_Click(object sender, EventArgs e)
		{
			mnuFormat.MenuItems.Clear();
			PopulateWritingSystemMenu();
			PopulateStyleMenu();
			mnuFormat.MenuItems.Add(mnuWritingSystem);
			mnuFormat.MenuItems.Add(mnuStyle);
			mnuFormat.Show(btnFormat, new Point(0, btnFormat.Height));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This gets called whenever the user selects a writing system
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void WritingSystemMenu_Click(object sender, EventArgs e)
		{
			if (sender is MenuItem)
			{
				var item = (MenuItem)sender;
				if (item.Parent == mnuWritingSystem)
					ApplyWS(LastTextBoxInFocus, (int) item.Tag);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This gets called whenever the user selects a style from the style menu
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void StyleMenu_Click(object sender, EventArgs e)
		{
			if (sender is MenuItem)
			{
				var item = (MenuItem) sender;
				if (item.Parent == mnuStyle)
				{
					ApplyStyle(LastTextBoxInFocus, item.Text);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the Find Next button click event
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void OnFindNext(object sender, EventArgs e)
		{
			if (DataUpdateMonitor.IsUpdateInProgress())
				return;  // discard event

			FindNext();

			// After a find next, focus the find box and select the text in it.
			fweditFindText.Select();
			fweditFindText.SelectAll();
#if __MonoCS__
			RemoveWaitCursor(this);
#endif
		}

#if __MonoCS__
		/// <summary>
		/// Remove the wait cursor, which is left behind on several controls when the
		/// DataUpdateMonitor object is disposed.  This is a patch over a bug in Mono
		/// as far as I can tell. It fixes FWNX-659.
		/// </summary>
		/// <remarks>
		/// The strange thing is that the cursor on all these controls seems to already
		/// be set to Cursors.Default, but this fix works.
		/// </remarks>
		private void RemoveWaitCursor(Control ctl)
		{
			foreach (var c in ctl.Controls)
			{
				var control = c as Control;
				if (control != null)
					RemoveWaitCursor(control);
			}
			ctl.Cursor = Cursors.Default;
		}
#endif

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the Replace button click event. The first time the user presses the
		/// "Replace" button, we just find the next match; the second time we actually do the
		/// replace and then go on to find the next match.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void OnReplace(object sender, System.EventArgs e)
		{
			if (DataUpdateMonitor.IsUpdateInProgress())
				return;  // discard event

			var rootSite = ActiveView;
			if (rootSite == null)
				return;

			using (UndoTaskHelper undoTaskHelper = new UndoTaskHelper(rootSite, "kstidUndoReplace"))
			{
				// Replace the selection with the replacement text, but don't allow deletion
				// of objects, such as footnotes in the replaced text.
				DoReplace(CurrentSelection);
				undoTaskHelper.RollBack = false;
			}

			// After a replace, focus the find box and select the text in it.
			fweditFindText.Select();
			fweditFindText.SelectAll();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the Replace All button click event.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void OnReplaceAll(object sender, System.EventArgs e)
		{
			if (DataUpdateMonitor.IsUpdateInProgress())
				return;  // discard event
			string undo, redo;
			ResourceHelper.MakeUndoRedoLabels("kstidUndoReplace", out undo, out redo);
			using (new DataUpdateMonitor(this, "ReplaceAll"))
			using (new WaitCursor(this, true))
			using (UndoableUnitOfWorkHelper undoHelper =
				new UndoableUnitOfWorkHelper(m_cache.ServiceLocator.GetInstance<IActionHandler>(), undo, redo))
			{
				DoReplaceAll();
				undoHelper.RollBack = false;
			}

			// After a replace, focus the find box and select the text in it.
			fweditFindText.Select();
			fweditFindText.SelectAll();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Does the replace all.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void DoReplaceAll()
		{
			int replaceCount = 0;
			if (m_app != null)
				m_app.EnableMainWindows(false);
			var rootSite = ActiveView;
			if (rootSite == null)
				return;

			PrepareToFind();
			m_inReplace = true;

			if (MatchNotFound == null)
				MatchNotFound += NoMatchFound;
			try
			{
				DateTime start = DateTime.Now;
				// Do the replace all
				SetupFindPattern();
				SaveDialogValues();
				if (PatternIsValid())
				{
					m_searchKiller.AbortRequest = false;
					m_searchKiller.Control = this;	// used for redrawing
					m_searchKiller.StopControl = btnClose;	// need to know the stop button
					m_vwPattern.ReplaceWith = ReplaceText;

					int hvoRoot, frag;
					IVwViewConstructor vc;
					IVwStylesheet styleSheet;
					rootSite.RootBox.GetRootObject(out hvoRoot, out vc, out frag, out styleSheet);
					ReplaceAllCollectorEnv replaceAll = new ReplaceAllCollectorEnv(vc, DataAccess,
						hvoRoot, frag, m_vwPattern, m_searchKiller);
					replaceCount = replaceAll.ReplaceAll();

					Debug.WriteLine("Replace all took " + (DateTime.Now - start));
				}
			}
			finally
			{
				PostpareToFind(replaceCount > 0);
				if (m_app != null)
					m_app.EnableMainWindows(true);
				m_inReplace = false;
			}

			// Display a dialog box if the replace all finished or was stopped
			if (replaceCount > 0)
			{
				bool fShowMsg = true;
				string msg = string.Format(m_searchKiller.AbortRequest ?
					FwCoreDlgs.kstidReplaceAllStopped :
					FwCoreDlgs.kstidReplaceAllDone, replaceCount);

				if (MatchNotFound != null)
					fShowMsg = MatchNotFound(this, msg, MatchType.ReplaceAllFinished);

				if (!fShowMsg)
					return;

				try
				{
					if (MiscUtils.IsUnix)
					{
						// Get a wait cursor to display when waiting for the messagebox
						// to show. See FWNX-660.
						this.tabReplace.UseWaitCursor = true;
						this.tabReplace.Parent.UseWaitCursor = true;
					}

					MessageBox.Show(Owner, msg, m_app.ApplicationName);
				}
				finally
				{
					if (MiscUtils.IsUnix)
					{
						this.tabReplace.Parent.UseWaitCursor = false;
						this.tabReplace.UseWaitCursor = false;
					}
				}
			}
			else if (replaceCount == 0)
				InternalMatchNotFound(true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if no match was found during a find operation.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="defaultMsg"></param>
		/// <param name="type">status of the match</param>
		/// <returns>true if no match was found; otherwise false</returns>
		/// ------------------------------------------------------------------------------------
		private bool NoMatchFound(object sender, string defaultMsg, MatchType type)
		{
			if (type != MatchType.NoMatchFound)
				return false;
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Stops a find/replace
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void OnStop(object sender, System.EventArgs e)
		{
			m_searchKiller.AbortRequest = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Close the Find/Replace dialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnClose_Click(object sender, System.EventArgs e)
		{
			Hide();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a click on the OK button.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_okButton_Click(object sender, System.EventArgs e)
		{
			if (!CanFindNext())
			{
				this.DialogResult = DialogResult.Cancel;
				Close();

				return;
			}

			SaveDialogValues(); // This needs to be done before the Regex is checked because it sets up m_vwPattern

			// LT-3310 - make sure if it's a regular expression that it is valid.
			// The following technique is what was added to the filtering code, so
			// it makes some sense to follow that pattern here.  This allows us to
			// look for an ICU RegEx error code first and handle it here in the dlg.
			if (PatternIsValid())
			{
				this.DialogResult = DialogResult.OK;
				Close();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies the pattern is valid. If user wants to use a regex, this validates the
		/// regular expression.
		/// </summary>
		/// <returns><c>true</c> if regular expression is valid or if we don't use regular
		/// expressions, <c>false</c> if regEx is invalid.</returns>
		/// ------------------------------------------------------------------------------------
		private bool PatternIsValid()
		{
			if (chkUseRegularExpressions.Checked)
			{
				IMatcher testMatcher = new RegExpMatcher(m_vwPattern);
				if (!testMatcher.IsValid())
				{
					DisplayInvalidRegExMessage(testMatcher.ErrorMessage());
					return false;
				}
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays a message box that the regular expression is invalid.
		/// </summary>
		/// <param name="errorMessage">The error message.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void DisplayInvalidRegExMessage(string errorMessage)
		{
			string errMsg = string.Format(FwCoreDlgs.kstidErrorInRegEx,
				errorMessage);
			MessageBox.Show(this, errMsg, FwCoreDlgs.kstidErrorInRegExHeader,
				MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the event when the check on "Use Regular Expressions" changes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void chkUseRegularExpressions_CheckedChanged(object sender, System.EventArgs e)
		{
			if (chkUseRegularExpressions.Checked)
			{
				chkMatchDiacritics.Enabled = false;
				chkMatchDiacritics.Checked = false;
				chkMatchWholeWord.Enabled = false;
				chkMatchWholeWord.Checked = false;
				chkMatchWS.Enabled = false;
				chkMatchWS.Checked = false;

				btnRegexMenuFind.Enabled = btnRegexMenuReplace.Enabled = true;
			}
			else
			{
				chkMatchDiacritics.Enabled = true;
				chkMatchWholeWord.Enabled = true;
				chkMatchWS.Enabled = true;

				btnRegexMenuFind.Enabled = btnRegexMenuReplace.Enabled = false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Instead of closing, just try to hide.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnClosing(CancelEventArgs e)
		{
			// Save location.
			if (m_mediator != null)
			{
				m_mediator.PropertyTable.SetProperty(kPersistenceLabel + "DlgLocation", Location);
				m_mediator.PropertyTable.SetProperty(kPersistenceLabel + "ShowMore",
					Height == m_heightDlgMore ? "true" : "false");
			}
			base.OnClosing(e);
			// If no other handler of this event tried to intervene, the dialog itself will
			// prevent closing and just hide itself.
			if (e.Cancel == false && ! m_inGetSpecs)
			{
				e.Cancel = true;
				Hide();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When the user switches between Find and Replace tabs, we need to transfer ownership
		/// of the panels that hold the controls and hide/show/change controls as appropriate.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void tabControls_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if (tabControls.SelectedTab == tabFind)
			{
				// If a replace is in progress, don't allow a change to the find tab
				if (m_inReplace)
				{
					tabControls.SelectedTab = tabReplace;
					return;
				}
				SuspendLayout();
				tabReplace.Controls.Clear();
				tabFind.Controls.Add(panelSearchOptions);
				tabFind.Controls.Add(panelBasic);
				btnReplace.Hide();
				btnReplaceAll.Hide();
				btnMore.Location = btnReplaceAll.Location;
				chkUseRegularExpressions.Enabled = true;
				fweditReplaceText.Hide();
				lblReplaceText.Hide();
				fweditFindText.Select();
			}
			else
			{
				// if a find is in progress, don't allow a switch to the replace tab
				if (m_inFind)
				{
					tabControls.SelectedTab = tabFind;
					return;
				}
				SuspendLayout();
				tabFind.Controls.Clear();
				tabReplace.Controls.Add(panelSearchOptions);
				tabReplace.Controls.Add(panelBasic);
				if(!m_inGetSpecs)
				{
					btnReplace.Show();
					btnReplaceAll.Show();
				}
				// Move More button beside Replace button
				btnMore.Location = new Point(btnReplace.Location.X - btnReplace.Width - 6,
					btnReplace.Location.Y);
				fweditReplaceText.Show();
				lblReplaceText.Show();
				fweditFindText.Select();
				if (m_fDisableReplacePatternMatching && (ModifierKeys & Keys.Shift) != Keys.Shift)
				{
					if (chkUseRegularExpressions.Checked)
						chkUseRegularExpressions.Checked = false;
					chkUseRegularExpressions.Enabled = false;
				}
			}

			SetHelpTopicId();
			SetFormatLabels();
			EnableRegexMenuReplaceButton();
			ResumeLayout();
		}

		private void SetHelpTopicId()
		{
			// there were two help topics. Now there is just one.
			// NOTE: since this is a common dialog, don't change the help topic ids! Instead,
			// change the string that your help topic provider returns for those two help
			// topic ids (i.e. change LexTextDll\HelpTopicPaths.resx or TeResources\HelpTopicPaths.resx
			// See C:\fw\Src\FwResources\HelpTopicPaths.resx
			if (tabControls.SelectedTab == tabFind)
			{
				s_helpTopic = "khtpFind"; // default
				var mainWindow = m_app == null ? null : m_app.ActiveMainWindow as IFindAndReplaceContext;
				if (mainWindow != null)
					s_helpTopic = mainWindow.FindTabHelpId ?? s_helpTopic;
			}
			else
				s_helpTopic = "khtpReplace";

			if (tabControls != null && tabControls.TabCount <= 1 && s_helpTopic != "khtpFindNotebook")   //find/replace help topic for lexicon
				s_helpTopic = "khtpLexFind";

			if (Text == string.Format(FwCoreDlgs.khtpBulkReplaceTitle))
				s_helpTopic = "khtpBulkReplace";

			if(m_helpTopicProvider != null) // It will be null if we are running under the test program
				this.helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(s_helpTopic));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show more options.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void btnMore_Click(object sender, System.EventArgs e)
		{
			btnMore.Text = FwCoreDlgs.kstidFindLessButtonText;
			btnMore.Image = ResourceHelper.LessButtonDoubleArrowIcon;
			btnMore.Click -= new EventHandler(btnMore_Click);
			btnMore.Click += new EventHandler(btnLess_Click);
			tabControls.Height = m_heightTabControlMore;
			Height = m_heightDlgMore;
			panelSearchOptions.Visible = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show fewer options.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void btnLess_Click(object sender, System.EventArgs e)
		{
			btnMore.Text = m_sMoreButtonText;
			btnMore.Image = ResourceHelper.MoreButtonDoubleArrowIcon;
			btnMore.Click += new EventHandler(btnMore_Click);
			btnMore.Click -= new EventHandler(btnLess_Click);
			tabControls.Height = m_heightTabControlLess;
			Height = m_heightDlgLess;
			panelSearchOptions.Visible = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show the Regex Helper context menu for Find
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnRegexMenuFind_Click(object sender, EventArgs e)
		{
			regexContextMenuFind.Show(btnRegexMenuFind, new System.Drawing.Point(btnRegexMenuFind.Width, 0));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show the Regex Helper context menu for Replace
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnRegexMenuReplace_Click(object sender, EventArgs e)
		{
			regexContextMenuReplace.Show(btnRegexMenuReplace, new System.Drawing.Point(btnRegexMenuFind.Width, 0));
		}

		///-------------------------------------------------------------------------------
		/// <summary>
		/// Draws an etched line on the dialog to separate the Search Options from the
		/// basic controls.
		/// </summary>
		///-------------------------------------------------------------------------------
		private void panel2_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
		{
			int dxMargin = 10;
			int left = lblSearchOptions.Right;
			LineDrawing.Draw(e.Graphics, left,
				(lblSearchOptions.Top + lblSearchOptions.Bottom) / 2,
				tabControls.Right - left - dxMargin, LineTypes.Etched);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle special keystrokes in the dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnKeyDown(KeyEventArgs e)
		{
			// Handle the F3 (Find Next shorcut) when the dialog is active (but not in spec-only
			// mode, since that mode can't actually perform a Find!)
			if (!m_inGetSpecs && e.KeyCode == Keys.F3)
				OnFindNext(null, EventArgs.Empty);

			// When the Find or Replace edit boxes are focused, Enter is not handled
			// by them so handle here. Make it activate the FindNext button which is the
			// default button for the dialog.
			// NOTE (TimS): This seems to be handled correctly by setting the "accept" button
			// on the dialog so it was taken out.
			//else if (e.KeyCode == Keys.Enter)
			//{
			//    if (m_inGetSpecs)
			//        m_okButton_Click(this, EventArgs.Empty);
			//    else
			//    {
			//        FindNext();
			//    }
			//}
			else
				base.OnKeyDown(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Activate dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnActivated(EventArgs e)
		{
			if (!m_messageFilterInstalled && !DesignMode)
			{
				m_messageFilterInstalled = true;
				Application.AddMessageFilter(this);
			}

			base.OnActivated(e);
			// TODO (TimS): possibly make ShowRangeSelAfterLostFocus an interface method so
			// other apps (i.e. DN) can do this.
			if (m_vwRootsite is SimpleRootSite)
			{
				SimpleRootSite site = m_vwRootsite as SimpleRootSite;
				if (!site.IsDisposed)
					site.ShowRangeSelAfterLostFocus = true;
			}
			if (m_initialActivate)
			{
				fweditFindText.Select();
				m_initialActivate = false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove the message filter when the dialog loses focus
		/// (Don't mistake this for onLoseFocus...this dialog never loses focus, it never
		/// has it, only it's sub-controls do.)
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnDeactivate(EventArgs e)
		{
			if (m_messageFilterInstalled && !DesignMode)
			{
				Application.RemoveMessageFilter(this);
				m_messageFilterInstalled = false;
			}
			base.OnDeactivate(e);
			// TODO (TimS): possibly make ShowRangeSelAfterLostFocus an interface method so
			// other apps (i.e. DN) can do this.
			if (m_vwRootsite is SimpleRootSite)
			{
				SimpleRootSite site = m_vwRootsite as SimpleRootSite;
				if (!site.IsDisposed)
					site.ShowRangeSelAfterLostFocus = false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When the focus arrives on a TSS edit control, the other edit control needs to have
		/// the selection removed from it. Also, the entered box needs to have all of the text
		/// selected.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void FwTextBox_Enter(object sender, EventArgs e)
		{
			if (tabControls.SelectedTab == tabReplace)
			{
				if (sender == fweditFindText)
					fweditReplaceText.RemoveSelection();
				else
					fweditFindText.RemoveSelection();
			}
			((FwTextBox)sender).SelectAll();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Needed to keep track of the last Tss edit control to have focus, for the purpose of
		/// setting styles, etc.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void FwTextBox_Leave(object sender, EventArgs e)
		{
			m_lastTextBoxInFocus = (FwTextBox)sender;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a text changed event in an FW edit box. The style labels need to be
		/// updated when the text changes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandleTextChanged(object sender, EventArgs e)
		{
			SetFormatLabels();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If enable is false, save the current enable state of the control and disable it.
		/// If it is true, restore the previous enable state of each control.
		/// </summary>
		/// <param name="ctrl"></param>
		/// <param name="enable"></param>
		/// ------------------------------------------------------------------------------------
		private void AdjustControlState(Control ctrl, bool enable)
		{
			if (m_enableStates == null)
				return;

			if (enable)
			{
				bool reallyEnable = true;
				if (m_enableStates.ContainsKey(ctrl))
				{
					reallyEnable = m_enableStates[ctrl];
				}
				ctrl.Enabled = reallyEnable;
			}
			else
			{
				m_enableStates[ctrl] = ctrl.Enabled; // Remember it's previous enabled state.
				ctrl.Enabled = false;
			}
		}
		#endregion

		#region Methods where the work is actually done.
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Enables or disables all the controls except the close/stop/cancel button on the
		/// find/replace dialog.
		/// </summary>
		/// <param name="enable">True to enable all the controls, false otherwise</param>
		/// ------------------------------------------------------------------------------------
		private void EnableControls(bool enable)
		{
			if (!enable)
			{
				if (m_enableStates == null)
					m_enableStates = new Dictionary<Control, bool>();
				else
					return; // already disabled, don't remember disabled state.
			}
			foreach (Control ctrl in panelBasic.Controls)
			{
				if (ctrl != btnClose)
					AdjustControlState(ctrl, enable);
			}
			foreach (Control ctrl in panelSearchOptions.Controls)
				AdjustControlState(ctrl, enable);
			if (enable)
			{
				m_enableStates.Clear();
				m_enableStates = null; // So we know to save on next disable.
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Replace the existing selection with the string in the replace box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void DoReplace(IVwSelection sel)
		{
			SetupFindPattern();
			if (IsReplacePossible(sel))
			{
				// See if we are just trying to replace formatting.
				bool fEmptySearchPattern = (FindText.Length == 0);
				m_vwPattern.ReplaceWith = ReplaceText;

				DoReplacement(sel, m_vwPattern.ReplacementText, m_vwPattern.MatchOldWritingSystem,
					fEmptySearchPattern);
			}
			else
			{
				// REVIEW(TimS): Should we beep or something to tell the user that the replace
				// could not happen (TE-8289)?
			}

			btnClose.Text = FwCoreDlgs.kstidClose;

			FindNext();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Setup the find pattern and clear out the selection for the pattern.  This is done
		/// at the start of a NEW search only.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void SetupFindPattern()
		{
			if (m_prevSearchText == null || !m_prevSearchText.Equals(FindText))
			{
				m_vwselPattern = null;
				m_prevSearchText = FindText;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the next match.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void FindNext()
		{
			CheckDisposed();

			Find(true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the previous match.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void FindPrevious()
		{
			CheckDisposed();

			Find(false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes a replace.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Replace()
		{
			CheckDisposed();

			OnReplace(null, new EventArgs());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Attempts to find a pattern match in the view starting from the specified selection.
		/// </summary>
		/// <param name="sel">Starting position</param>
		/// <param name="forward">indicates whether to search forward or backward</param>
		/// ------------------------------------------------------------------------------------
		private void FindFrom(IVwSelection sel, bool forward)
		{
			FindCollectorEnv.LocationInfo startLocation = null;
			var rootSite = ActiveView;
			if (rootSite == null)
				return;

			if (sel != null)
			{
				SelectionHelper helper = SelectionHelper.Create(sel, rootSite);
				startLocation = new FindCollectorEnv.LocationInfo(helper);
			}
			FindCollectorEnv.LocationInfo locationInfo = m_findEnvironment.FindNext(startLocation);
			if (locationInfo != null)
			{
				SelectionHelper selHelper = SelectionHelper.Create(rootSite);
				selHelper.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
					locationInfo.m_location);
				selHelper.SetLevelInfo(SelectionHelper.SelLimitType.End,
					locationInfo.m_location);
				selHelper.IchAnchor = locationInfo.m_ichMin;
				selHelper.IchEnd = locationInfo.m_ichLim;
				selHelper.SetNumberOfPreviousProps(SelectionHelper.SelLimitType.Anchor,
					locationInfo.m_cpropPrev);
				selHelper.SetNumberOfPreviousProps(SelectionHelper.SelLimitType.End,
					locationInfo.m_cpropPrev);
				selHelper.SetTextPropId(SelectionHelper.SelLimitType.Anchor, locationInfo.m_tag);
				selHelper.SetTextPropId(SelectionHelper.SelLimitType.End, locationInfo.m_tag);
				m_vwselPattern = selHelper.SetSelection(rootSite, true, true,
					VwScrollSelOpts.kssoDefault);
				Debug.Assert(m_vwselPattern != null, "We need a selection after a find!");
				//if (Owner != null)
				//    Owner.Activate();
				//if (rootSite is Control)
				//    ((Control) rootSite).Focus();
				rootSite.RootBox.Activate(VwSelectionState.vssOutOfFocus);
			}
		}

		IVwRootSite ActiveView
		{
			get
			{
				if (m_vwRootsite != null)
				{
					if (!(m_vwRootsite is Control))
						return m_vwRootsite; // Maybe in testing? Just playing safe.
					if (!((Control)m_vwRootsite).IsDisposed)
						return m_vwRootsite;
				}
				// if the current one is null or disposed, see if we can get one from the main window
				if (Owner != null && !Owner.IsDisposed)
				{
					try
					{
						var newSite = ReflectionHelper.GetProperty(Owner, "ActiveView") as IVwRootSite;
						if (newSite != null)
							m_vwRootsite = newSite;
					}
					catch (Exception)
					{
					}
				}
				if (m_vwRootsite != null)
				{
					if (!(m_vwRootsite is Control))
						return m_vwRootsite; // Maybe in testing? Just playing safe.
					if (!((Control)m_vwRootsite).IsDisposed)
						return m_vwRootsite;
				}
				return null; // If we can't find anything better than a disposed control, return null.
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the next match based on the current pattern settings
		/// </summary>
		/// <param name="fSearchForward">If true, search forward; otherwise search backward</param>
		/// ------------------------------------------------------------------------------------
		private void Find(bool fSearchForward)
		{
			// If no find was done before, show the dialog or focus it.
			if (!btnFindNext.Enabled)
			{
				if (!Visible)
					Show();
				else
					Focus();
				return;
			}

			var rootSite = ActiveView;
			if (rootSite == null)
				return;

			if (m_fLastDirectionForward != fSearchForward)
			{
				// Changing search direction. Reset current selection (resets the search limit)
				m_vwselPattern = null;
				m_fLastDirectionForward = fSearchForward;
			}

			SetupFindPattern();

			// Get the selection from the root box in order to compare it with the one from
			// the pattern.
			IVwSelection vwselRootb = rootSite.RootBox.Selection;

			// If the pattern's selection is different from the current selection in the
			// rootbox or if a new search has been started then set things up to begin
			// searching at the current selection.
			bool fFirstTry = (m_vwselPattern == null || m_vwselPattern != vwselRootb);
			if (fFirstTry)
			{
				int hvoRoot, frag;
				IVwViewConstructor vc;
				IVwStylesheet styleSheet;
				rootSite.RootBox.GetRootObject(out hvoRoot, out vc, out frag, out styleSheet);
				if (fSearchForward)
				{
					m_findEnvironment = new FindCollectorEnv(vc, DataAccess, hvoRoot, frag,
						m_vwPattern, m_searchKiller);
				}
				else
				{
					m_findEnvironment = new ReverseFindCollectorEnv(vc, DataAccess, hvoRoot, frag,
						m_vwPattern, m_searchKiller);
				}
			}
			Debug.Assert(m_findEnvironment != null);

			if (vwselRootb == null)
				vwselRootb = rootSite.RootBox.MakeSimpleSel(true, true, false, true);

			// Find the pattern.
			if (vwselRootb != null)
			{
				// Even though the find doesn't technically update any data, we don't
				// want the user to be able to change the data underneath us while the
				// find is happening.
				using (new DataUpdateMonitor(this, "Find"))
				{
					// Change the cancel button to a stop
					PrepareToFind();
					m_inFind = true;

					try
					{
						SaveDialogValues();
						if (PatternIsValid())
						{
							m_searchKiller.AbortRequest = false;
							FindFrom(vwselRootb, fSearchForward);
							if (!m_findEnvironment.FoundMatch)
								AttemptWrap(fFirstTry, fSearchForward);
						}
					}
					finally
					{
						PostpareToFind(false);
						m_inFind = false;
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Prepares to find.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void PrepareToFind()
		{
			// Change the close button into the 'Stop' button
			btnClose.Tag = btnClose.Text;
			btnClose.Text = FwCoreDlgs.kstidStop;
			btnClose.Click -= btnClose_Click;
			btnClose.Click += OnStop;

			// Disable controls
			EnableControls(false);
			btnClose.Focus();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Postpares to find.
		/// </summary>
		/// <param name="fMakeCloseBtnSayClose">True to change the the close button to say
		/// "close", otherwise it will go back to whatever it was"</param>
		/// ------------------------------------------------------------------------------------
		private void PostpareToFind(bool fMakeCloseBtnSayClose)
		{
			// Enable controls
			EnableControls(true);
			// Restore the close button
			btnClose.Text = (fMakeCloseBtnSayClose) ? FwCoreDlgs.kstidClose : (string)btnClose.Tag;
			btnClose.Click += new EventHandler(btnClose_Click);
			btnClose.Click -= new EventHandler(OnStop);
			MatchNotFound -= new MatchNotFoundHandler(NoMatchFound);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save the values set in the dialog in the pattern.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SaveDialogValues()
		{
			m_vwPattern.Pattern = FindText;
			m_vwPattern.MatchOldWritingSystem = chkMatchWS.Checked;
			m_vwPattern.MatchDiacritics = chkMatchDiacritics.Checked;
			m_vwPattern.MatchWholeWord = chkMatchWholeWord.Checked;
			m_vwPattern.MatchCase = chkMatchCase.Checked;
			m_vwPattern.UseRegularExpressions = chkUseRegularExpressions.Checked;
			m_vwPattern.ReplaceWith = ReplaceText;
			m_resultReplaceText = ReplaceText;
			SimpleStringMatcher.SetupPatternCollating(m_vwPattern, m_cache);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Attempts to wrap and continue searching if we hit the bottom of the view.
		/// </summary>
		/// <returns>True if a match was found. Otherwise, false.</returns>
		/// ------------------------------------------------------------------------------------
		private void AttemptWrap(bool fFirstTry, bool fSearchForward)
		{
			Debug.Assert(m_findEnvironment != null);
			m_findEnvironment.HasWrapped = true;

			// Have we gone full circle and reached the point where we started?
			if (m_findEnvironment.StoppedAtLimit)
				InternalMatchNotFound(false);
			else
			{
				// Wrap around to start searching at the top or bottom of the view.
				FindFrom(null, fSearchForward);

				// If, after wrapping around to begin searching from the top, we hit the
				// starting point, then display the same message as if we went full circle.
				if (!m_findEnvironment.FoundMatch)
					InternalMatchNotFound(fFirstTry);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks for a subscriber to the MatchNotFound event and displays the appropriate not
		/// found message if the subscriber says it's ok, or if there is no subscriber.
		/// </summary>
		/// <param name="fFirstTry">Determines what message to display.</param>
		/// ------------------------------------------------------------------------------------
		private void InternalMatchNotFound(bool fFirstTry)
		{
			m_vwselPattern = null;
			bool fShowMsg = true;

			string defaultMsg = fFirstTry ? FwCoreDlgs.kstidNoMatchMsg :
				FwCoreDlgs.kstidNoMoreMatchesMsg;

			if (MatchNotFound != null)
			{
				fShowMsg = MatchNotFound(this, defaultMsg, fFirstTry ?
					MatchType.NoMatchFound : MatchType.NoMoreMatchesFound);
			}

			if (fShowMsg && !m_searchKiller.AbortRequest)
			{
				// Only show message that entire document was searched if the search was
				// not aborted (TE-3567).
				Enabled = false;
				MessageBox.Show(Owner, defaultMsg, m_app.ApplicationName);
				Enabled = true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current selection from the root site.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected IVwSelection CurrentSelection
		{
			get
			{
				var rootSite = ActiveView;
				if (rootSite == null)
					return null;

				IVwRootBox rootBox = rootSite.RootBox;
				if (rootBox == null)
					return null;

				return rootBox.Selection;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the current selection can be replaced with the replace text.
		/// </summary>
		/// <param name="vwsel">current selection to check</param>
		/// <returns>true if the selection can be replaced, else false</returns>
		/// ------------------------------------------------------------------------------------
		private bool IsReplacePossible(IVwSelection vwsel)
		{
			// If there is no selection then replace is impossible.
			if (vwsel == null)
				return false;

			// Is the current selection the same as what is in the find box?
			if (!m_vwPattern.MatchWhole(vwsel))
				return false;

			// Is the selection editable?
			return vwsel.CanFormatChar;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Perform a single instance of a text replace
		/// </summary>
		/// <param name="sel">The current (new) selection in the view where we just did a find.
		/// Presumably, this matches the Find Text string.</param>
		/// <param name="tssReplace">The tss string that the user entered in the Replace box
		/// </param>
		/// <param name="fUseWS"></param>
		/// <param name="fEmptySearch"></param>
		/// <remarks>TODO TE-973: searching for writing systems.</remarks>
		/// ------------------------------------------------------------------------------------
		protected void DoReplacement(IVwSelection sel, ITsString tssReplace, bool fUseWS,
			bool fEmptySearch)
		{
			// Get the properties we will apply, except for the writing system/ows and/or sStyleName.
			ITsString tssSel;
			bool fGotItAll;
			sel.GetFirstParaString(out tssSel, " ", out fGotItAll);
			if (!fGotItAll)
				return; // desperate defensive programming.

			// Get ORCs from selection so that they can be appended after the text has been replaced.
			ITsStrBldr stringBldr = tssSel.GetBldr();
			ReplaceAllCollectorEnv.ReplaceString(stringBldr, tssSel, 0, tssSel.Length,
				tssReplace, 0, fEmptySearch, fUseWS);

			// finally - do the replacement
			sel.ReplaceWithTsString(
				stringBldr.GetString().get_NormalizedForm(FwNormalizationMode.knmNFD));
		}

		#endregion

		#region Protected properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The data access for the find and replace dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected ISilDataAccess DataAccess
		{
			get
			{
				var rootSite = ActiveView;
				if (rootSite == null)
					return null;
				return rootSite.RootBox.DataAccess;
			}
		}
		#endregion

		#region Protected helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given an FwTextBox, we get the name of the WS of the current selection. If the
		/// selection spans multiple writing systems, we return an empty string.
		/// </summary>
		/// <param name="fwtextbox">An FwTextBox (either the Find or Replace box)</param>
		/// <returns>Empty string if there is more than one writing system contained in the
		/// selection or if the TsString doesn't have a writing system property (if that's
		/// even possible). Otherwise, the UI name of the writing system.</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual CoreWritingSystemDefinition GetCurrentWS(FwTextBox fwtextbox)
		{
			int hvoWs = SelectionHelper.GetWsOfEntireSelection(fwtextbox.Selection);
			if (hvoWs == 0)
				return null;
			return m_cache.ServiceLocator.WritingSystemManager.Get(hvoWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fill the Writing Systems menu with an alphebetized list of all writing systems
		/// defined in this language project. The writing system of the current selection
		/// (if there is exactly one) will be checked; otherwise, nothing will be checked.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void PopulateWritingSystemMenu()
		{
			// First clear any items added previously
			mnuWritingSystem.MenuItems.Clear();
			EventHandler clickEvent = WritingSystemMenu_Click;

			// Convert from Set to List, since the Set can't sort.
			List<CoreWritingSystemDefinition> writingSystems = m_cache.ServiceLocator.WritingSystems.AllWritingSystems.ToList();
			writingSystems.Sort((x, y) => x.DisplayLabel.CompareTo(y.DisplayLabel));
			CoreWritingSystemDefinition sCurrentWs = GetCurrentWS(LastTextBoxInFocus);
			foreach (CoreWritingSystemDefinition ws in writingSystems)
				mnuWritingSystem.MenuItems.Add(new MenuItem(ws.DisplayLabel, clickEvent) {Checked = sCurrentWs == ws, Tag = ws.Handle});
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fill the Style menu the "No style" item, plus a an alphabetized list of all
		/// character styles in stylesheet of the last Fw Edit Box to have focus. The style of
		/// the current selection (if there is exactly one) will be checked. If the selection
		/// contains no style, then "No style" will be checked. If the selection covers multiple
		/// styles, nothing will be checked.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void PopulateStyleMenu()
		{
			// TODO: Convert this method to use StyleListHelper.

			// First clear any items added previously
			mnuStyle.MenuItems.Clear();
			EventHandler clickEvent = new EventHandler(StyleMenu_Click);

			string sSelectedStyle = LastTextBoxInFocus.SelectedStyle;
			MenuItem mnuItem = new MenuItem(FwCoreDlgs.kstidNoStyle, clickEvent);
			mnuItem.Checked = (sSelectedStyle == string.Empty);
			mnuStyle.MenuItems.Add(mnuItem);

			mnuItem = new MenuItem(StyleUtils.DefaultParaCharsStyleName, clickEvent);
			mnuItem.Checked = (sSelectedStyle == FwStyleSheet.kstrDefaultCharStyle);
			mnuStyle.MenuItems.Add(mnuItem);

			int count = 0;
			if (LastTextBoxInFocus.StyleSheet != null)
				count = LastTextBoxInFocus.StyleSheet.CStyles;
			string styleName;
			List<string> styleNames = new List<string>(count / 2);
			for (int i = 0; i < count; i++)
			{
				styleName = LastTextBoxInFocus.StyleSheet.get_NthStyleName(i);
				if (LastTextBoxInFocus.StyleSheet.GetType(styleName) == 1) // character style
				{
					ContextValues context =
						(ContextValues)LastTextBoxInFocus.StyleSheet.GetContext(styleName);

					// Exclude Internal and InternalMappable style contexts
					if (context != ContextValues.Internal &&
						context != ContextValues.InternalMappable)
					{
						styleNames.Add(styleName);
					}
				}
			}
			styleNames.Sort();

			foreach (string s in styleNames)
			{
				mnuItem = new MenuItem(s, clickEvent);
				mnuItem.Checked = (sSelectedStyle == s);
				mnuStyle.MenuItems.Add(mnuItem);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Applies the specified style to the current selection of the Tss string in the
		/// specified Tss edit control
		/// </summary>
		/// <param name="fwTextBox">The Tss edit control whose selection should have the
		/// specified style applied to it.</param>
		/// <param name="sStyle">The name of the style to apply</param>
		/// ------------------------------------------------------------------------------------
		public void ApplyStyle(FwTextBox fwTextBox, string sStyle)
		{
			CheckDisposed();

			// Apply the specified style to the current selection
			if (sStyle.ToLowerInvariant() == FwCoreDlgs.kstidNoStyle.ToLowerInvariant())
				sStyle = null;
			else if (sStyle.ToLowerInvariant() == StyleUtils.DefaultParaCharsStyleName.ToLowerInvariant())
				sStyle = FwStyleSheet.kstrDefaultCharStyle;
			fwTextBox.ApplyStyle(sStyle);
			SetFormatLabels();

			fwTextBox.Select();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Applies the specified writing system to the current selection of the Tss string in
		/// the specified Tss edit control
		/// </summary>
		/// <param name="fwTextBox">The Tss edit control whose selection should have the
		/// specified style applied to it.</param>
		/// <param name="hvoWs">The ID of the writing system to apply</param>
		/// ------------------------------------------------------------------------------------
		public void ApplyWS(FwTextBox fwTextBox, int hvoWs)
		{
			CheckDisposed();

			fwTextBox.ApplyWS(hvoWs);
			if (chkMatchWS.Enabled)
				chkMatchWS.Checked = true;
			SetFormatLabels();
			fwTextBox.Select();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates visibility and values of format labels used to show selected styles in
		/// find and replace text boxes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetFormatLabels()
		{
			if (tabControls.SelectedTab == tabFind)
			{
				lblReplaceFormat.Hide();
				lblReplaceFormatText.Hide();
				SetFormatLabels(fweditFindText, lblFindFormat, lblFindFormatText);
			}
			else
			{
				SetFormatLabels(fweditFindText, lblFindFormat, lblFindFormatText);
				SetFormatLabels(fweditReplaceText, lblReplaceFormat, lblReplaceFormatText);
			}
			btnFindNext.Enabled = btnReplace.Enabled = btnReplaceAll.Enabled =
				CanFindNext();
		}

		private bool CanFindNext()
		{
			return (fweditFindText.Text != string.Empty || lblFindFormatText.Text != string.Empty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates visibility and content of labels depending on char styles in passed TsString.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetFormatLabels(FwTextBox textBox, Label format, Label formatText)
		{
			ITsString tss = textBox.Tss;
			CoreWritingSystemDefinition currentWs = GetCurrentWS(textBox);
			// Check for writing systems and styles that are applied to the tss
			bool fShowLabels = false;
			int prevWs = -1;
			string prevStyleName = string.Empty;
			bool multipleWs = false;
			bool multipleStyles = false;
			for (int i = 0; i < tss.RunCount; i++)
			{
				ITsTextProps ttp = tss.get_Properties(i);

				// check for writing systems
				int nVar;
				int ws = ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar);
				if (prevWs != ws && prevWs != -1)
					multipleWs = true;
				prevWs = ws;

				// check for styles
				string charStyle = ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
				if (charStyle != prevStyleName && prevStyleName != string.Empty)
					multipleStyles = true;
				prevStyleName = charStyle;
			}
			if (prevStyleName == null)
				prevStyleName = string.Empty;
			else if (prevStyleName == FwStyleSheet.kstrDefaultCharStyle)
				prevStyleName = StyleUtils.DefaultParaCharsStyleName;

			Debug.Assert(prevWs > 0, "We should always have a writing system");

			// no more than 1 style and only 1 writing system
			if (!multipleStyles && !multipleWs)
			{
				// Not displaying anything
				if (prevStyleName == string.Empty && !chkMatchWS.Checked)
					formatText.Text = string.Empty;
				// Just have one style
				else if (prevStyleName != string.Empty)
				{
					fShowLabels = true;
					if (!chkMatchWS.Checked)
						formatText.Text = prevStyleName;
					else
					{
						formatText.Text = string.Format(FwCoreDlgs.kstidOneStyleOneWS,
							prevStyleName, currentWs == null ? string.Empty : currentWs.DisplayLabel);
					}
				}
				// No style (WS displayed)
				else if (chkMatchWS.Checked)
				{
					fShowLabels = true;
					formatText.Text = currentWs == null ? string.Empty : currentWs.DisplayLabel;
				}
			}
			// multiple styles or multiple writing systems (displayed)
			else if (multipleStyles || (multipleWs && chkMatchWS.Checked))
			{
				fShowLabels = true;
				// multiple styles
				if (multipleStyles)
				{
					// only one writing system or multiple writing systems (not displayed)
					if (!multipleWs || (multipleWs && !chkMatchWS.Checked))
					{
						if (!chkMatchWS.Checked) // don't show writing system info
							formatText.Text = FwCoreDlgs.kstidMultipleStyles;
						else // show writing system info
						{
							formatText.Text = string.Format(FwCoreDlgs.kstidMultipleStylesOneWS,
								currentWs == null ? string.Empty : currentWs.DisplayLabel);
						}
					}
					// multiple writing systems (displayed)
					else if (multipleWs && chkMatchWS.Checked)
						formatText.Text = FwCoreDlgs.kstidMultipleStylesMultipleWS;
				}
				// Multiple writing systems and no more than 1 style
				else if (multipleWs && !multipleStyles)
				{
					if (prevStyleName == string.Empty) // no style applied
						formatText.Text = FwCoreDlgs.kstidMultipleWritingSystems;
					else // one style applied
					{
						formatText.Text = string.Format(FwCoreDlgs.kstidOneStyleMultipleWS,
							prevStyleName);
					}
				}
			}
			// multiple writing systems (not displayed) and one style
			else if (multipleWs && !chkMatchWS.Checked && prevStyleName != string.Empty)
			{
				formatText.Text = prevStyleName;
				fShowLabels = true;
			}

			format.Visible = fShowLabels;
			formatText.Visible = fShowLabels;
		}
		#endregion

		#region Public properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the text to find
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITsString FindText
		{
			get
			{
				CheckDisposed();

				return fweditFindText.Tss;
			}
			set
			{
				CheckDisposed();

				fweditFindText.Tss = TsStringUtils.GetCleanTsString(value);
				HandleTextChanged(fweditFindText, null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the text to replace
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITsString ReplaceText
		{
			get
			{
				CheckDisposed();

				return fweditReplaceText.Tss;
			}
			set
			{
				CheckDisposed();

				fweditReplaceText.Tss = TsStringUtils.GetCleanTsString(value);
				HandleTextChanged(fweditReplaceText, null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the text to replace after OK has closed the dialog and ReplaceText will crash.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITsString ResultReplaceText
		{
			get
			{
				CheckDisposed();
				return m_resultReplaceText;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a reference to the last Tss edit control to have focus. Needed for applying
		/// styles and writing systems.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwTextBox LastTextBoxInFocus
		{
			get
			{
				CheckDisposed();

				return m_lastTextBoxInFocus;
			}
		}
		#endregion

		#region IMessageFilter Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provide tabbing with the view controls and handle the ESC key to close the find dialog
		/// </summary>
		/// <param name="m"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool PreFilterMessage(ref Message m)
		{
			CheckDisposed();

			if (m.Msg == (int)Win32.WinMsgs.WM_CHAR)
			{
				// Handle TAB and Shift-TAB
				if (m.WParam == (System.IntPtr)Win32.VirtualKeycodes.VK_TAB)
				{
					SelectNextControl(this.ActiveControl, ModifierKeys != Keys.Shift, true, true, true);
					return true;
				}

				// NOTE (TimS): This seems to be handled correctly by setting the "cancel" button
				// on the dialog so it was taken out.
				//// An ESC key will cause the form to close.
				//if (m.WParam == (System.IntPtr)Win32.VirtualKeycodes.VK_ESCAPE)
				//{
				//    Close();
				//    return true;
				//}

				// JohnT: don't do this, for some reason in at least some contexts
				// (e.g., see LT-4723, in the Flex Bulk Edit Find/Replace tab Setup),
				// the keystrokes just don't happen. Better to let ones we don't care
				// about be handled the default way.
				//base.OnKeyPress(new KeyPressEventArgs((char)m.WParam));
				//return true;
			}
			return false;
		}
		#endregion
	}

	/// <summary>
	/// An interface that a Form can implement in order to configure the Find and Replace dialog.
	/// </summary>
	public interface IFindAndReplaceContext
	{
		/// <summary>
		/// The ID to pass to the help topic provider to get help for the Find tab.
		/// </summary>
		string FindTabHelpId { get; }
	}

	#region VwPatternSerializableSettings class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Wrapper to serialize/deserialize basic settings for VwPattern
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class VwPatternSerializableSettings
	{
		bool m_fNewlyCreated = false;
		IVwPattern m_pattern;
		/// <summary>
		/// use this interface to deserialize settings to new pattern
		/// </summary>
		public VwPatternSerializableSettings()
		{
			// create a new mattern to capture deserialized settings.
			m_pattern = VwPatternClass.Create();
			m_fNewlyCreated = true;
		}

		/// <summary>
		/// use this interface to serialize the given pattern
		/// </summary>
		/// <param name="pattern"></param>
		public VwPatternSerializableSettings(IVwPattern pattern)
		{
			m_pattern = pattern;
		}

		/// <summary>
		/// When class is used with deserializer,
		/// use this to get the pattern that was (or is to be) setup with
		/// the deserialized settings.
		/// returns null, if we haven't created one.
		/// </summary>
		public IVwPattern NewPattern
		{
			get
			{
				if (m_fNewlyCreated)
					return m_pattern;
				return null;
			}
		}

		/// <summary>
		///
		/// </summary>
		public string IcuCollatingRules
		{
			get { return m_pattern.IcuCollatingRules; }
			set { m_pattern.IcuCollatingRules = value; }
		}
		/// <summary>
		///
		/// </summary>
		public string IcuLocale
		{
			get { return m_pattern.IcuLocale; }
			set { m_pattern.IcuLocale = value; }
		}
		/// <summary>
		///
		/// </summary>
		public bool MatchCase
		{
			get { return m_pattern.MatchCase; }
			set { m_pattern.MatchCase = value; }
		}

		/// <summary>
		///
		/// </summary>
		public bool MatchCompatibility
		{
			get { return m_pattern.MatchCompatibility; }
			set { m_pattern.MatchCompatibility = value; }
		}
		/// <summary>
		///
		/// </summary>
		public bool MatchDiacritics
		{
			get { return m_pattern.MatchDiacritics; }
			set { m_pattern.MatchDiacritics = value; }
		}
		/// <summary>
		///
		/// </summary>
		public bool MatchExactly
		{
			get { return m_pattern.MatchExactly; }
			set { m_pattern.MatchExactly = value; }
		}
		/// <summary>
		///
		/// </summary>
		public bool MatchOldWritingSystem
		{
			get { return m_pattern.MatchOldWritingSystem;  }
			set { m_pattern.MatchOldWritingSystem = value; }
		}
		/// <summary>
		///
		/// </summary>
		public bool MatchWholeWord
		{
			get { return m_pattern.MatchWholeWord; }
			set { m_pattern.MatchWholeWord = value; }
		}

		int m_patternWs = 0;
		/// <summary>
		/// the (first) ws used to construct the Pattern tss.
		/// </summary>
		public int PatternWs
		{
			get
			{
				if (m_patternWs == 0 && m_pattern.Pattern != null)
					m_patternWs = TsStringUtils.GetWsAtOffset(m_pattern.Pattern, 0);
				return m_patternWs;
			}
			set
			{
				m_patternWs = value;
				TryCreatePatternTss();
			}
		}

		private void TryCreatePatternTss()
		{
			if (m_patternWs != 0)
			{
				// create a monoWs pattern text for the new pattern.
				m_pattern.Pattern = TsStringUtils.MakeTss(m_patternString, m_patternWs);
			}
		}

		string m_patternString = "";
		/// <summary>
		///
		/// </summary>
		public string PatternAsString
		{
			get
			{
				if (String.IsNullOrEmpty(m_patternString) && m_pattern.Pattern != null)
					m_patternString = m_pattern.Pattern.Text;
				return m_patternString;
			}
			set
			{
				m_patternString = value;
				if (m_patternString == null)
					m_patternString = "";
				TryCreatePatternTss();
			}
		}

		string m_replaceWithString = "";
		/// <summary>
		///
		/// </summary>
		public string ReplaceWithAsString
		{
			get
			{
				if (String.IsNullOrEmpty(m_replaceWithString) && m_pattern.ReplaceWith != null)
					m_replaceWithString = m_pattern.ReplaceWith.Text;
				return m_replaceWithString;
			}
			set
			{
				m_replaceWithString = value;
				if (m_replaceWithString == null)
					m_replaceWithString = "";
				TryCreateReplaceWithTss();
			}
		}

		private void TryCreateReplaceWithTss()
		{
			if (m_replaceWithWs != 0)
			{
				// create a monoWs pattern text for the new pattern.
				m_pattern.ReplaceWith = TsStringUtils.MakeTss(m_replaceWithString, m_replaceWithWs);
			}
		}

		int m_replaceWithWs = 0;
		/// <summary>
		/// the (first) ws used to construct the ReplaceWith tss.
		/// </summary>
		public int ReplaceWithWs
		{
			get
			{
				if (m_replaceWithWs == 0 && m_pattern.ReplaceWith != null)
					m_replaceWithWs = TsStringUtils.GetWsAtOffset(m_pattern.ReplaceWith, 0);
				return m_replaceWithWs;
			}
			set
			{
				m_replaceWithWs = value;
				TryCreateReplaceWithTss();
			}
		}

		/// <summary>
		///
		/// </summary>
		public bool ShowMore
		{
			get { return m_pattern.ShowMore; }
			set { m_pattern.ShowMore = value; }
		}
		/// <summary>
		///
		/// </summary>
		public bool StoppedAtLimit
		{
			get { return m_pattern.StoppedAtLimit; }
			set { m_pattern.StoppedAtLimit = value; }
		}
		/// <summary>
		///
		/// </summary>
		public bool UseRegularExpressions
		{
			get { return m_pattern.UseRegularExpressions; }
			set { m_pattern.UseRegularExpressions = value;  }
		}
	}
	#endregion

	#region SearchKiller
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Implements a search killer
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class SearchKiller: IVwSearchKiller
	{
		private bool m_abort = false;
		private Control m_ownerControl = null;
		private Control m_stopControl = null;
		private bool stopButtonDown = false;

		#region IVwSearchKiller Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the window handle for the search operation - ignored in this case
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Window
		{
			set { }
		}

		/// <summary>Owning control</summary>
		public Control Control { set { m_ownerControl = value; } }
		/// <summary>Stop button control</summary>
		public Control StopControl { set { m_stopControl = value; } }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get/set the abort status
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool AbortRequest
		{
			get { return m_abort; }
			set { m_abort = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process any pending window messages
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void FlushMessages()
		{
			if (m_ownerControl != null)
				m_ownerControl.Update();

#if !__MonoCS__ // Currently (Aug 2010) Mono Winforms on X11 doesn't support PeekMessage with filtering.
			// Process keystrokes and lbutton events so the user can stop the dlg work.
			// This should allow the dlg to be stopped mid stream with out the risk
			// of the DoEvents call.
			// The reason this change works is due to the 'polling' type of design of the
			// calling code.  This method is called frequently during the 'action' of the
			// dlg.
			Win32.MSG msg;
			while (PeekMessage(Win32.WinMsgs.WM_KEYDOWN, Win32.WinMsgs.WM_KEYUP, out msg) ||
				PeekMessage(Win32.WinMsgs.WM_LBUTTONDOWN, Win32.WinMsgs.WM_LBUTTONUP, out msg))
			{
				if (msg.message == (int)Win32.WinMsgs.WM_LBUTTONDOWN)
				{
					if (m_stopControl != null && msg.hwnd == m_stopControl.Handle)
						stopButtonDown = true;
					else
						stopButtonDown = false;
				}
				else if (msg.message == (int)Win32.WinMsgs.WM_LBUTTONUP)
				{
					if (m_stopControl != null && msg.hwnd == m_stopControl.Handle && stopButtonDown)
					{
						(m_stopControl as Button).PerformClick();
						stopButtonDown = false;
					}
				}
				else if (msg.message == (int)Win32.WinMsgs.WM_KEYDOWN &&
					msg.wParam == (System.IntPtr)Win32.VirtualKeycodes.VK_ESCAPE &&
					m_stopControl != null && msg.hwnd == m_stopControl.Handle)
				{
					(m_stopControl as Button).PerformClick();
				}

				if (!Win32.IsDialogMessage(m_ownerControl.Handle, ref msg))
				{
					Win32.TranslateMessage(ref msg);
					Win32.DispatchMessage(ref msg);
				}
			}
#endif
		}

#if !__MonoCS__ // Currently (Aug 2010) Mono Winforms on X11 doesn't support PeekMessage with filtering.
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Peeks at the pending messages and if it finds any message in the given range, that
		/// message is removed from the stack and passed back to be handled immediately.
		/// </summary>
		/// <param name="min">The minumum message to handle.</param>
		/// <param name="max">The maximum message to handle.</param>
		/// <param name="msg">The message found, if any.</param>
		/// <returns><c>true</c> if a matching message is found; <c>false</c> otherwise.</returns>
		/// ------------------------------------------------------------------------------------
		private bool PeekMessage(Win32.WinMsgs min, Win32.WinMsgs max, out Win32.MSG msg)
		{
			msg = new Win32.MSG();
			return Win32.PeekMessage(ref msg, m_ownerControl.Handle, (uint)min, (uint)max,
				(uint)Win32.PeekFlags.PM_REMOVE);
		}
#endif
		#endregion
	}
	#endregion
}
