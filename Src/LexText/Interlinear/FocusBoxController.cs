using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Globalization;
using System.Xml;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FdoUi;
using SIL.Utils;

namespace SIL.FieldWorks.IText
{
	public partial class FocusBoxController : UserControl, IFlexComponent, ISelectOccurrence, SimpleRootSite.ISuppressDefaultKeyboardOnKillFocus
	{
		internal IAnalysisControlInternal m_sandbox;
		private IVwStylesheet m_stylesheet;
		protected InterlinLineChoices m_lineChoices;

		/// <summary>
		/// currently only valid after SelectOccurrence has a valid occurrence.
		/// </summary>
		internal FdoCache Cache { get; set; }

		public FocusBoxController()
		{
			this.Visible = false;
			InitializeComponent();
			btnLinkNextWord.GotFocus += HandleFocusWrongButton;
			btnMenu.GotFocus += HandleFocusWrongButton;
		}

		internal bool IsDirty
		{
			get { return m_sandbox.IsDirty; }
		}

		// There is no logical reason for other buttons ever to get the focus. But .NET helpfully focuses the link words button
		// as we hide the focus box. And in some other circumstance, which I can't even figure out, it focuses the menu button.
		// I can't figure out how to prevent it, but it's better for the confirm
		// changes button to have it instead, since that's the button that is supposed to have the same function
		// as Enter, so if .NET activates some button because the user presses Enter while it has focus,
		// it had better be that one. See FWR-3399 and FWR-3453.
		void HandleFocusWrongButton(object sender, EventArgs e)
		{
			btnConfirmChanges.Focus();
		}

		public void UpdateLineChoices(InterlinLineChoices choices)
		{
			// Under certain circumstances this can get called when sandbox is null (LT-11468)
			if (m_sandbox != null)
				m_sandbox.UpdateLineChoices(choices);
		}

		public FocusBoxController(IVwStylesheet stylesheet, InterlinLineChoices lineChoices)
			: this()
		{
			m_stylesheet = stylesheet;
			m_lineChoices = lineChoices;
		}

		#region Implementation of IPropertyTableProvider

		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call IPropertyTable.DoStuff.
		/// </summary>
		public IPropertyTable PropertyTable { get; private set; }

		#endregion

		#region Implementation of IPublisherProvider

		/// <summary>
		/// Get the IPublisher.
		/// </summary>
		public IPublisher Publisher { get; private set; }

		#endregion

		#region Implementation of ISubscriberProvider

		/// <summary>
		/// Get the ISubscriber.
		/// </summary>
		public ISubscriber Subscriber { get; private set; }

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="propertyTable">Interface to a property table.</param>
		/// <param name="publisher">Interface to the publisher.</param>
		/// <param name="subscriber">Interface to the subscriber.</param>
		public void InitializeFlexComponent(IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber)
		{
			PropertyTable = propertyTable;
			Publisher = publisher;
			Subscriber = subscriber;

			SetToolTips();
		}

		#endregion

		private void SetToolTips()
		{
#if RANDYTODO
			toolTip.SetToolTip(btnBreakPhrase,
							   AppendShortcutToToolTip(m_mediator.CommandSet["CmdBreakPhrase"] as Command));
			toolTip.SetToolTip(btnConfirmChanges,
							   AppendShortcutToToolTip(m_mediator.CommandSet["CmdApproveAndMoveNext"] as Command));
			toolTip.SetToolTip(btnLinkNextWord,
							   AppendShortcutToToolTip(m_mediator.CommandSet["CmdMakePhrase"] as Command));
			toolTip.SetToolTip(btnUndoChanges,
							   AppendShortcutToToolTip(m_mediator.CommandSet["CmdUndo"] as Command));
			toolTip.SetToolTip(btnConfirmChangesForWholeText,
							   AppendShortcutToToolTip(m_mediator.CommandSet["CmdApproveForWholeTextAndMoveNext"] as Command));
#endif
		}

		internal InterlinDocForAnalysis InterlinDoc
		{
			get
			{
				if (Parent != null && Parent is InterlinDocForAnalysis)
					return Parent as InterlinDocForAnalysis;
				else
					return null;
			}
		}

		internal AnalysisOccurrence SelectedOccurrence { get; set; }
		internal AnalysisTree InitialAnalysis { get; set; }

		#region Sandbox

		internal IAnalysisControlInternal InterlinWordControl
		{
			get { return m_sandbox; }
			set { m_sandbox = value; }
		}

		#region Sandbox setup
		/// <summary>
		/// Change root of Sandbox or create it; Lay it out and figure its size;
		/// tell m_vc the size.
		/// </summary>
		/// <returns></returns>
		private void ChangeOrCreateSandbox(AnalysisOccurrence selected)
		{
			this.SuspendLayout();
			panelSandbox.SuspendLayout();
			if (InterlinDoc != null)
			{
				InterlinDoc.RecordGuessIfNotKnown(selected);
			}
			int color = (int)CmObjectUi.RGB(DefaultBackColor);
			//if this sandbox is presenting a wordform with multiple possible analyses then set the
			//bg color indicator
			if (selected.Analysis.Analysis == null && selected.Analysis.Wordform != null &&
				SandboxBase.GetHasMultipleRelevantAnalyses(selected.Analysis.Wordform))
			{
				color = InterlinVc.MultipleApprovedGuessColor;
			}

			if (m_sandbox == null)
			{
				m_sandbox = CreateNewSandbox(selected);
				m_sandbox.MultipleAnalysisColor = color;
			}
			else
			{
				//set the color before switching so that the color is correct when DisplayWordForm is called
				m_sandbox.MultipleAnalysisColor = color;
				m_sandbox.SwitchWord(selected);
			}
			UpdateButtonState();
			// add the sandbox plus some padding.
			panelSandbox.ResumeLayout();
			this.ResumeLayout();

			SetSandboxSize();
		}

		internal virtual IAnalysisControlInternal CreateNewSandbox(AnalysisOccurrence selected)
		{
			var sandbox = new Sandbox(selected.Analysis.Cache, m_stylesheet,
				m_lineChoices, selected, this)
			{
				SizeToContent = true,
				ShowMorphBundles = true,
				StyleSheet = m_stylesheet
			};
			sandbox.InitializeFlexComponent(PropertyTable, Publisher, Subscriber);
			// Layout will ignore size.
			//sandbox.Mediator = Mediator;
			panelSandbox.Controls.Add(sandbox); // Makes it real and may give it a root box.
			// Note: adding sandbox to Controls doesn't always MakeRoot(), because OnHandleCreated happens
			// only when the parent control is Visible.
			if (sandbox.RootBox == null)
				sandbox.MakeRoot();
			AdjustControlsForRightToLeftWritingSystem(sandbox);
			// this is needed for the Undo button.
			sandbox.SandboxChangedEvent += m_sandbox_SandboxChangedEvent;
			return sandbox as IAnalysisControlInternal;
		}

		// Set the size of the sandbox on the VC...if it exists yet.
		void SetSandboxSize()
		{
			// Make the focus box the size it really needs to be for the current object.
			// This will adjust its size, but we already know we're trying to do that,
			// so we don't want the notification...it attempts work we're already in
			// the middle of and may confuse things or produce flashing.
			AdjustSizeAndLocationForControls(true);
		}

		/// <summary>
		/// NOTE: currently needs to get called after sandbox.MakeRoot, since that's when RightToLeftWritingSystem is valid.
		/// </summary>
		/// <param name="sandbox"></param>
		private void AdjustControlsForRightToLeftWritingSystem(Sandbox sandbox)
		{
			if (sandbox.RightToLeftWritingSystem && btnConfirmChanges.Location.X != 0)
			{
				panelSandbox.Anchor = AnchorStyles.Right | AnchorStyles.Top;
				// make buttons RightToLeft oriented.
				btnConfirmChanges.Anchor = AnchorStyles.Left;
				btnConfirmChanges.Location = new Point(0, btnConfirmChanges.Location.Y);
				btnConfirmChangesForWholeText.Anchor = AnchorStyles.Left;
				btnConfirmChangesForWholeText.Location =
					new Point(btnConfirmChanges.Width, btnConfirmChangesForWholeText.Location.Y);
				btnUndoChanges.Anchor = AnchorStyles.Left;
				btnUndoChanges.Location = new Point(
					btnConfirmChanges.Width + btnConfirmChangesForWholeText.Width, btnUndoChanges.Location.Y);
				btnMenu.Anchor = AnchorStyles.Right;
				btnMenu.Location = new Point(panelControlBar.Width - btnMenu.Width, btnMenu.Location.Y);
			}
		}

		protected override void OnLayout(LayoutEventArgs e)
		{
			base.OnLayout(e);

			AdjustSizeAndLocationForControls(this.Visible);
		}

		bool m_fAdjustingSize = false;
		internal void AdjustSizeAndLocationForControls(bool fAdjustOverallSize)
		{
			if (m_fAdjustingSize)
				return;
			m_fAdjustingSize = true;
			try
			{
				if (m_sandbox != null && m_sandbox.RightToLeftWritingSystem && m_sandbox is UserControl)
				{
					UserControl sandbox = m_sandbox as UserControl;
					if (panelSandbox.Width != sandbox.Width)
						panelSandbox.Width = sandbox.Width;
					if (sandbox.Location.X != panelSandbox.Width - sandbox.Width)
						sandbox.Location = new Point(panelSandbox.Width - sandbox.Width, sandbox.Location.Y);
				}

				if (m_sandbox != null && fAdjustOverallSize)
				{
					panelControlBar.Width = panelSandbox.Width; // if greater than min width.
					// move control panel to bottom of sandbox panel.
					panelControlBar.Location = new Point(panelControlBar.Location.X, panelSandbox.Height - 1);
					// move side bar to right of sandbox panel.
					if (m_sandbox.RightToLeftWritingSystem)
					{
						panelSidebar.Location = new Point(0, panelSidebar.Location.Y);
						panelControlBar.Location = new Point(panelSidebar.Width, panelControlBar.Location.Y);
						panelSandbox.Location = new Point(panelSidebar.Width, panelSandbox.Location.Y);
					}
					else
					{
						panelSidebar.Location = new Point(panelSandbox.Width, panelSidebar.Location.Y);
					}

					this.Size = new Size(panelSidebar.Width + Math.Max(panelSandbox.Width, panelControlBar.Width),
						panelControlBar.Height + panelSandbox.Height);
				}
			}
			finally
			{
				m_fAdjustingSize = false;
			}
		}

		#endregion Sandbox setup

		/// <summary>
		/// turn focus over to sandbox
		/// </summary>
		/// <param name="e"></param>
		protected override void OnGotFocus(EventArgs e)
		{
			base.OnGotFocus(e);
			FocusSandbox();
		}

		/// <summary>
		/// Call this rather than "FocusBox.Focus()" because we don't want focus passing through this
		/// to the sandbox if we can help it. One problem can be that as the main view loses focus it
		/// resets the default keyboard.
		/// </summary>
		internal void FocusSandbox()
		{
			if (m_sandbox != null && m_sandbox is UserControl)
				(m_sandbox as UserControl).Focus();
		}

		#endregion

		#region InterlinDoc interface

		#region ISelectOccurrence Members

		public virtual void SelectOccurrence(AnalysisOccurrence selected)
		{
			SelectedOccurrence = selected;
			InitialAnalysis = new AnalysisTree();
			if (SelectedOccurrence != null)
			{
				if (Cache == null)
					Cache = SelectedOccurrence.Analysis.Cache;
				InitialAnalysis.Analysis = SelectedOccurrence.Analysis;
			}
			ChangeOrCreateSandbox(selected);
		}

		#endregion

		internal bool MakeDefaultSelection(object parameter)
		{
			if (IsDisposed)
				return false; // result is not currently used, not sure what it should be.
			InterlinWordControl.MakeDefaultSelection();
			return true;
		}

		#endregion InterlinDoc interface

		void m_sandbox_SandboxChangedEvent(object sender, SandboxChangedEventArgs e)
		{
			UpdateButtonState_Undo();
		}

		internal bool ShowLinkWordsIcon
		{
			get
			{
				return HaveValidOccurrence() && SelectedOccurrence.CanMakePhraseWithNextWord();
			}
		}

		private bool HaveValidOccurrence()
		{
			return SelectedOccurrence !=null && SelectedOccurrence.IsValid;
		}

		internal bool ShowBreakPhraseIcon
		{
			get
			{
				return HaveValidOccurrence() && SelectedOccurrence.CanBreakPhrase();
			}
		}

		private void UpdateButtonState()
		{
			// only update button state when we're fully installed.
			if (InterlinDoc == null || !InterlinDoc.IsFocusBoxInstalled)
				return;
			// we're fully installed, so update the buttons.
			if (ShowLinkWordsIcon)
			{
				btnLinkNextWord.Visible = true;
				btnLinkNextWord.Enabled = true;
			}
			else
			{
				btnLinkNextWord.Visible = false;
				btnLinkNextWord.Enabled = false;
			}

			if (ShowBreakPhraseIcon)
			{
				btnBreakPhrase.Visible = true;
				btnBreakPhrase.Enabled = true;
			}
			else
			{
				btnBreakPhrase.Visible = false;
				btnBreakPhrase.Enabled = false;
			}
			UpdateButtonState_Undo();
#if RANDYTODO
			// LT-11406: Somehow JoinWords (and BreakPhrase) leaves the selection elsewhere,
			// this should make it select the default location.
			m_mediator.IdleQueue.Add(IdleQueuePriority.Medium, MakeDefaultSelection);
#endif

		}

		private void UpdateButtonState_Undo()
		{
			if (InterlinWordControl != null && InterlinWordControl.HasChanged)
			{
				btnUndoChanges.Visible = true;
				btnUndoChanges.Enabled = true;
			}
			else
			{
				btnUndoChanges.Visible = false;
				btnUndoChanges.Enabled = false;
			}
		}

		private void btnLinkNextWord_Click(object sender, EventArgs e)
		{
#if RANDYTODO
			OnJoinWords(m_mediator.CommandSet["CmdMakePhrase"] as Command);
#endif
		}

		/// <summary>
		/// Note: Assume we are in the OnDisplayShowLinkWords is true context.
		/// </summary>
		public bool OnJoinWords(object arg)
		{
#if RANDYTODO
			var cmd = (ICommandUndoRedoText)arg;
			UndoableUnitOfWorkHelper.Do(cmd.UndoText, cmd.RedoText, Cache.ActionHandlerAccessor,
				() =>
					{
						SelectedOccurrence.MakePhraseWithNextWord();
						if (InterlinDoc != null)
						{
							InterlinDoc.RecordGuessIfNotKnown(SelectedOccurrence);
						}
					});
			InterlinWordControl.SwitchWord(SelectedOccurrence);
			UpdateButtonState();RANDYTODO
#endif
			return true;
		}

		/// <summary>
		/// split the current occurrence into occurrences for each word in the phrase-wordform.
		/// (if it IsPhrase)
		/// </summary>
		public void OnBreakPhrase(object arg)
		{
#if RANDYTODO
			// (LT-8069) in some odd circumstances, the break phrase icon lingers on the tool bar menu when it should
			// have disappeared. If we're in that state, just return.
			if (!ShowBreakPhraseIcon)
				return;
			var cmd = (ICommandUndoRedoText)arg;
			UndoableUnitOfWorkHelper.Do(cmd.UndoText, cmd.RedoText, Cache.ActionHandlerAccessor,
				() => SelectedOccurrence.BreakPhrase());
			InterlinWordControl.SwitchWord(SelectedOccurrence);
			UpdateButtonState();
#endif
		}

		private void btnBreakPhrase_Click(object sender, EventArgs e)
		{
#if RANDYTODO
			OnBreakPhrase(m_mediator.CommandSet["CmdBreakPhrase"] as Command);
#endif
		}

		private void btnUndoChanges_Click(object sender, EventArgs e)
		{
			// LT-14001 the only time we don't want the sandbox changed event to fire
			// and update the undo button state is when we're actually processing
			// the undo! Other changes to the sandbox need to update the undo button state.
			var sandbox = m_sandbox as SandboxBase;
			if (sandbox != null)
				sandbox.SandboxChangedEvent -= m_sandbox_SandboxChangedEvent;
			InterlinWordControl.Undo();
			if (sandbox != null)
				sandbox.SandboxChangedEvent += m_sandbox_SandboxChangedEvent;
			UpdateButtonState();
		}

		private void btnConfirmChanges_Click(object sender, EventArgs e)
		{
#if RANDYTODO
			if (this is FocusBoxControllerForDisplay)
				(this as FocusBoxControllerForDisplay).OnApproveAndMoveNext();
#endif
		}


		private void btnConfirmChangesForWholeText_Click(object sender, EventArgs e)
		{
#if RANDYTODO
			ApproveGuessOrChangesForWholeTextAndMoveNext(m_mediator.CommandSet["CmdApproveForWholeTextAndMoveNext"] as Command);
#endif
		}

		private void btnMenu_Click(object sender, EventArgs e)
		{
			IFwMainWnd window = PropertyTable.GetValue<IFwMainWnd>("window");

#if RANDYTODO
			window.ShowContextMenu("mnuFocusBox",
				btnMenu.PointToScreen(new Point(btnMenu.Width / 2, btnMenu.Height / 2)),
				null,
				null);
#endif
		}

		private string ShortcutText(Keys shortcut)
		{
			string shortcutText = "";
			if (shortcut != Keys.None)
			{
				shortcutText = TypeDescriptor.GetConverter(typeof(Keys)).ConvertToString(null, CultureInfo.InvariantCulture, shortcut);
			}
			return shortcutText;
		}

#if RANDYTODO
		private string AppendShortcutToToolTip(Command command)
		{
			string shortcutText = ShortcutText(command.Shortcut);
			if (command.Id == "CmdApproveAndMoveNext" && shortcutText.IndexOf('+') > 0)
			{
				// alter this one, since there can be two key combinations that should work for it (Control-key is not always necessary).
				shortcutText = shortcutText.Insert(0, "(");
				shortcutText = shortcutText.Insert(shortcutText.IndexOf('+') + 1, ")");
			}
			string tooltip = command.ToolTip;
			return AppendShortcutToToolTip(tooltip, shortcutText);
		}
#endif

		private string AppendShortcutToToolTip(string toolTip, string shortcut)
		{
			if (String.IsNullOrEmpty(shortcut))
				return toolTip;
			return String.Format("{0} ({1})", toolTip, shortcut);
		}

		// consider updating the button state in another way
		protected override void OnParentChanged(EventArgs e)
		{
			base.OnParentChanged(e);
			UpdateButtonState();
		}

		protected override void OnVisibleChanged(EventArgs e)
		{
			base.OnVisibleChanged(e);
			UpdateButtonState();
		}

		private void toolTip_Popup(object sender, PopupEventArgs e)
		{
			// for some reason, tool tips for sub controls only work
			// when the parent control has a tool tip.
			// we only want to show tool tips for the buttons.
			if (!(e.AssociatedControl is Button))
			{
				// we don't want to actually show this tool tip.
				e.Cancel = true;
			}

		}
	}

	/// <summary>
	/// implements everything needed by the FocuxBoxControl
	/// </summary>
	internal interface IAnalysisControlInternal
	{
		bool RightToLeftWritingSystem { get; }
		bool HasChanged { get; }
		void Undo();
		void SwitchWord(AnalysisOccurrence selected);
		void MakeDefaultSelection();
		bool ShouldSave(bool fSaveGuess);
		AnalysisTree GetRealAnalysis(bool fSaveGuess, out IWfiAnalysis obsoleteAna);
		int GetLineOfCurrentSelection();
		bool SelectOnOrBeyondLine(int startLine, int increment);
		void UpdateLineChoices(InterlinLineChoices choices);
		int MultipleAnalysisColor { set; }
		bool IsDirty { get; }
	}

	/// <summary />
	public partial class FocusBoxControllerForDisplay : FocusBoxController
	{
		public FocusBoxControllerForDisplay(IVwStylesheet stylesheet, InterlinLineChoices lineChoices, bool rightToLeft)
			: base(stylesheet, lineChoices)
		{
			m_fRightToLeft = rightToLeft;
		}
	}
}
