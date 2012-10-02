using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Globalization;

using XCore;

namespace SIL.FieldWorks.IText
{
	public partial class FocusBoxController : UserControl
	{
		private Sandbox m_sandbox = null;

		public FocusBoxController()
		{
			this.Visible = false;
			InitializeComponent();
		}

		public FocusBoxController(Sandbox sandbox) : this()
		{
			if (sandbox != null)
			{
				this.InterlinWordControl = sandbox;
			}
		}

		private InterlinDocChild InterlinDoc
		{
			get
			{
				if (m_sandbox != null)
					return m_sandbox.InterlinDoc;
				else if (Parent != null && Parent is InterlinDocChild)
					return Parent as InterlinDocChild;
				else
					return null;
			}
		}

		/// <summary>
		/// removes this control from parent, and
		/// lets this control know we are in the process
		/// of removing the control so we can suppress
		/// updating the display of this control during that
		/// process.
		/// </summary>
		bool m_fUninstallingControl = false;
		internal void Uninstall()
		{
			if (Parent != null)
			{
				try
				{
					m_fUninstallingControl = true;
					this.SuspendLayout();
					this.Visible = false;
					Control parent = Parent;
					parent.SuspendLayout();
					parent.Controls.Remove(this);
					parent.ResumeLayout();
					this.ResumeLayout();
				}
				finally
				{
					m_fUninstallingControl = false;
				}
			}
		}

		public Sandbox InterlinWordControl
		{
			get
			{
				return m_sandbox;
			}
			set
			{
				m_sandbox = value;
				if (m_sandbox == null)
					return;
				this.SuspendLayout();
				panelSandbox.SuspendLayout();
				if (!panelSandbox.Controls.Contains(m_sandbox))
				{
					m_sandbox.SandboxChangedEvent += new SandboxChangedEventHandler(m_sandbox_SandboxChangedEvent);
					panelSandbox.Controls.Add(m_sandbox); // Makes it real and may give it a root box.
					if (m_sandbox.RootBox == null)
						m_sandbox.MakeRoot();	// adding sandbox to Controls doesn't (always?) make rootbox.
					if (RightToLeftWritingSystem && btnConfirmChanges.Location.X != 0)
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

					toolTip.SetToolTip(btnBreakPhrase,
						AppendShortcutToToolTip(m_sandbox.Mediator.CommandSet["CmdBreakPhrase"] as Command));
					toolTip.SetToolTip(btnConfirmChanges,
						AppendShortcutToToolTip(m_sandbox.Mediator.CommandSet["CmdApproveAndMoveNext"] as Command));
					toolTip.SetToolTip(btnLinkNextWord,
						AppendShortcutToToolTip(m_sandbox.Mediator.CommandSet["CmdMakePhrase"] as Command));
					toolTip.SetToolTip(btnUndoChanges,
						AppendShortcutToToolTip(m_sandbox.Mediator.CommandSet["CmdUndo"] as Command));
					toolTip.SetToolTip(btnConfirmChangesForWholeText,
						AppendShortcutToToolTip(m_sandbox.Mediator.CommandSet["CmdApproveForWholeTextAndMoveNext"] as Command));
				}
				UpdateButtonState();

				// add the sandbox plus some padding.
				m_sandbox.Visible = true;
				panelSandbox.ResumeLayout();
				this.ResumeLayout();
			}
		}

		private bool RightToLeftWritingSystem
		{
			get { return m_sandbox != null && m_sandbox.RightToLeftWritingSystem; }
		}

		protected override void OnLayout(LayoutEventArgs e)
		{
			base.OnLayout(e);

			AdjustSizeAndLocationForControls(this.Visible);
		}

		internal void AdjustSizeAndLocationForControls(bool fAdjustOverallSize)
		{
			if (RightToLeftWritingSystem)
			{
				if (panelSandbox.Width != m_sandbox.Width)
					panelSandbox.Width = m_sandbox.Width;
				if (m_sandbox.Location.X != panelSandbox.Width - m_sandbox.Width)
					m_sandbox.Location = new Point(panelSandbox.Width - m_sandbox.Width, m_sandbox.Location.Y);
			}

			if (m_sandbox != null && fAdjustOverallSize)
			{
				panelControlBar.Width = panelSandbox.Width; // if greater than min width.
				// move control panel to bottom of sandbox panel.
				panelControlBar.Location = new Point(panelControlBar.Location.X, panelSandbox.Height - 1);
				// move side bar to right of sandbox panel.
				if (RightToLeftWritingSystem)
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

		void m_sandbox_SandboxChangedEvent(object sender, SandboxChangedEventArgs e)
		{
			UpdateButtonState_Undo();
		}

		private void UpdateButtonState()
		{
			// only update button state when we're fully installed.
			if (InterlinDoc == null || !InterlinDoc.IsFocusBoxInstalled || m_fUninstallingControl)
				return;
			// we're fully installed, so update the buttons.
			if (InterlinDoc.ShowLinkWordsIcon)
			{
				btnLinkNextWord.Visible = true;
				btnLinkNextWord.Enabled = true;
			}
			else
			{
				btnLinkNextWord.Visible = false;
				btnLinkNextWord.Enabled = false;
			}

			if (InterlinDoc.ShowBreakPhraseIcon)
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
		}

		private void UpdateButtonState_Undo()
		{
			if (InterlinWordControl.Caches.DataAccess.IsDirty())
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
			InterlinDoc.OnJoinWords(m_sandbox.Mediator.CommandSet["CmdMakePhrase"] as Command);
		}

		private void btnBreakPhrase_Click(object sender, EventArgs e)
		{
			InterlinDoc.OnBreakPhrase(m_sandbox.Mediator.CommandSet["CmdBreakPhrase"] as Command);
		}

		private void btnUndoChanges_Click(object sender, EventArgs e)
		{
			InterlinWordControl.OnUndo(null);
			UpdateButtonState();
		}

		private void btnConfirmChanges_Click(object sender, EventArgs e)
		{
			InterlinDoc.ApproveGuessOrChangesAndMoveNext();
		}


		private void btnConfirmChangesForWholeText_Click(object sender, EventArgs e)
		{
			InterlinDoc.ApproveGuessOrChangesForWholeTextAndMoveNext(m_sandbox.Mediator.CommandSet["CmdApproveForWholeTextAndMoveNext"] as Command);
		}

		private void btnMenu_Click(object sender, EventArgs e)
		{
			XWindow window = (XWindow)m_sandbox.Mediator.PropertyTable.GetValue("window");

			window.ShowContextMenu("mnuFocusBox",
				btnMenu.PointToScreen(new Point(btnMenu.Width / 2, btnMenu.Height / 2)),
				null,
				null);
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

		private string AppendShortcutToToolTip(string toolTip, string shortcut)
		{
			if (String.IsNullOrEmpty(shortcut))
				return toolTip;
			return String.Format("{0} ({1})", toolTip, shortcut);
		}

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
}
